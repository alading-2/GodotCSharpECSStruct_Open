using System;
using System.Runtime.CompilerServices;
using Godot;



/// <summary>
/// 全局定时器管理器 (TimerManager)
/// 
/// 核心职责：
/// 1. 统一驱动游戏内所有的非物理计时逻辑。
/// 2. 提供高性能的对象池化定时器 (GameTimer)，降低 GC 分配压力。
/// 3. 支持游戏时间 (Scaled) 与真实时间 (Unscaled) 的双重计时模式。
/// 4. 提供便捷的链式 API 用于配置回调、标签和生命周期。
/// 
/// 用法示例：
/// // 1. 简单延迟
/// TimerManager.Instance.Delay(2.0f).OnComplete(() => GD.Print("完成"));
/// 
/// // 2. 循环触发并带标签
/// TimerManager.Instance.Loop(1.0f).WithTag("Buff").OnLoop(() => ApplyBuffEffect());
/// 
/// // 3. 倒计时模式 (带进度回调)
/// TimerManager.Instance.Countdown(10.0f, 0.5f)
///     .Countdown((elapsed, progress) => UpdateUI(progress))
///     .OnComplete(() => OnTimeUp());
/// </summary>
public partial class TimerManager : Node
{
    /// <summary>
    /// 模块初始化器：利用 C# 属性在模块加载时自动将 TimerManager 注册到 AutoLoad 系统。
    /// 这样可以确保它在游戏启动时作为单例存在。
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(TimerManager),
            Scene = ResourceManagement.Load<PackedScene>(nameof(TimerManager), ResourceCategory.Tools),
            Priority = AutoLoad.Priority.Core,
            Dependencies = new[] { nameof(ObjectPoolInit) }
        });
    }

    /// <summary> 全局唯一单例访问点 </summary>
    public static TimerManager Instance;

    private static readonly Log _log = new(nameof(TimerManager));

    /// <summary> 
    /// 内部维护的 GameTimer 对象池。
    /// 通过 ForEachActive 遍历所有正在运行的定时器，确保高性能。
    /// </summary>
    private ObjectPool<GameTimer> _timerPool;

    /// <summary> 用于手动计算 UnscaledDeltaTime 的时间戳（毫秒） </summary>
    private ulong _lastTicksMsec;

    /// <summary> 
    /// 真实时间增量 (秒)。
    /// 即使 Engine.TimeScale 为 0，该值依然能反映真实的物理流逝时间。
    /// </summary>
    private float _unscaledDeltaTime;

    /// <summary>
    /// Node 进入场景树时的初始化逻辑。
    /// </summary>
    public override void _EnterTree()
    {
        Instance = this;
        _lastTicksMsec = Time.GetTicksMsec();

        // 从全局管理器获取专门为 GameTimer 准备的对象池
        _timerPool = ObjectPoolManager.GetPool<GameTimer>(ObjectPoolNames.TimerPool);

        if (_timerPool == null)
        {
            _log.Error("无法获取 TimerPool，请确保 ObjectPoolNames.TimerPool 已在 ObjectPoolInit 中注册");
            return;
        }

        // 绑定每一帧的原始处理开始信号，用于更新基础时间戳
        GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
    }

    /// <summary>
    /// Node 退出时的清理逻辑。
    /// 确保回收池中资源并断开单例，防止内存泄漏。
    /// </summary>
    public override void _ExitTree()
    {
        _timerPool?.ReleaseAll();
        _timerPool?.Destroy();
        Instance = null;
    }

    /// <summary>
    /// 手动计算真实的 DeltaTime。
    /// Godot 默认的 _Process(delta) 会受 TimeScale 影响，因此需要通过 Time.GetTicksMsec 自行计算。
    /// </summary>
    private void OnProcessFrame()
    {
        ulong currentTicks = Time.GetTicksMsec();
        _unscaledDeltaTime = (currentTicks - _lastTicksMsec) / 1000.0f;
        _lastTicksMsec = currentTicks;
    }

    /// <summary>
    /// 每一帧的计时器驱动。
    /// 将 Process 逻辑转发给 ProcessTimers 处理，统一区分缩放/非缩放时间。
    /// </summary>
    public override void _Process(double delta)
    {
        ProcessTimers(delta);
    }

    /// <summary>
    /// 核心更新逻辑：遍历所有活跃的定时器并分发时间增量。
    /// </summary>
    private void ProcessTimers(double delta)
    {
        float scaledDelta = (float)delta;
        float unscaledDelta = _unscaledDeltaTime;

        // 调用对象池的内部高效遍历方法
        _timerPool.ForEachActive(timer =>
        {
            // 延迟回收：在下一帧开始时回收上一帧已完成的定时器
            if (timer.IsDone)
            {
                _timerPool.Release(timer);
                return;
            }

            // 暂停逻辑：直接跳过更新
            if (timer.IsPaused) return;

            // 分发增量：根据定时器配置选择使用 scaled 还是 unscaled 时间
            float dt = timer.UseUnscaledTime ? unscaledDelta : scaledDelta;
            timer.Update(dt);
        });
    }

    // ============ 工厂方法 (Factory Methods) ============

    /// <summary>
    /// 创建【延迟定时器】：并在指定时间到达后触发回调。
    /// 适合：爆炸倒计时、一次性 UI 延时。
    /// </summary>
    /// <param name="duration">持续时间 (秒)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnComplete() 配置</returns>
    public GameTimer Delay(float duration, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        timer.Configure(duration, false, useUnscaledTime);
        timer.Id = Guid.NewGuid().ToString();
        return timer;
    }

    /// <summary>
    /// 创建【无限循环定时器】：每隔一段时间触发一次回调，永不停止直到手动取消。
    /// 适合：周期性伤血、自动回魔、每秒更新界面。
    /// </summary>
    /// <param name="interval">循环间隔 (秒)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnLoop() 配置</returns>
    public GameTimer Loop(float interval, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        timer.Configure(interval, true, useUnscaledTime, repeatCount: -1);
        timer.Id = Guid.NewGuid().ToString();
        return timer;
    }

    /// <summary>
    /// 创建【有限重复定时器】：每隔一段时间触发一次回调，直到达到执行次数。
    /// 适合：连发技能（如点击三连击）、分段任务提示。
    /// </summary>
    /// <param name="interval">执行间隔 (秒)</param>
    /// <param name="count">总执行次数</param>
    /// <param name="immediate">是否在创建后立即执行第一次回调 (默认 false)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnRepeat() 配置</returns>
    public GameTimer Repeat(float interval, int count, bool immediate = false, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        timer.Configure(interval, true, useUnscaledTime, repeatCount: count, immediate: immediate);
        timer.Id = Guid.NewGuid().ToString();
        return timer;
    }

    /// <summary>
    /// 创建【倒计时定时器】：在总时间内以指定频率触发回调，并在最后停止。
    /// 适合：副本结束倒计时、技能持续时间读条。
    /// </summary>
    /// <param name="duration">总持续总时长 (秒)</param>
    /// <param name="interval">每次回调（Tick）的触发频率 (秒)</param>
    /// <param name="immediate">是否在创建后立即执行第一次回调 (默认 false)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .Countdown() 配置进度回调</returns>
    public GameTimer Countdown(float duration, float interval, bool immediate = false, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        // 倒计时本质上是一个带总量限制的循环定时器
        timer.Configure(interval, true, useUnscaledTime, repeatCount: -1, totalDuration: duration, immediate: immediate);
        timer.Id = Guid.NewGuid().ToString();
        return timer;
    }

    // ============ 批量管理 (Batch Management) ============

    /// <summary>
    /// 通过分配的唯一 ID 精确取消某个定时器。
    /// </summary>
    /// <param name="id">Guid 字符串</param>
    public void Cancel(string id)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Id == id) timer.Cancel();
        });
    }

    /// <summary>
    /// 通过标签 (Tag) 批量取消一组定时器。
    /// 典型应用：单位死亡时，取消该单位身上所有正在计时的 Buff。
    /// </summary>
    /// <param name="tag">标签字符串 (如："Buff", "Skill")</param>
    public void CancelByTag(string tag)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Tag == tag) timer.Cancel();
        });
    }

    /// <summary>
    /// 批量切换所有活跃定时器的暂停状态。
    /// 通常用于全局游戏逻辑暂停（非引擎物理暂停）。
    /// </summary>
    public void SetAllTimerPaused(bool paused)
    {
        _timerPool.ForEachActive(timer =>
        {
            timer.IsPaused = paused;
        });
    }

    /// <summary>
    /// 根据标签批量切换暂停状态。
    /// 适合：暂停特定类别的业务（如：暂停所有怪物 AI 计时，但保留玩家计时）。
    /// </summary>
    public void SetAllTimerPausedByTag(string tag, bool paused)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Tag == tag) timer.IsPaused = paused;
        });
    }

    /// <summary> 
    /// 获取当前正在内存中运行（活跃）的定时器总数。
    /// 通常用于性能分析或调试监控。
    /// </summary>
    public int GetActiveTimerCount() => _timerPool.ActiveCount;

    /// <summary> 
    /// 获取对象池的即时统计状态。
    /// 返回值：Active (活跃数), Pooled (池内闲置数)
    /// </summary>
    public (int Active, int Pooled) GetStats()
    {
        var stats = _timerPool.GetStats();
        return (stats.ActiveCount, stats.Count);
    }
}
