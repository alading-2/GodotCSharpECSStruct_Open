using System;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 全局定时器管理器
/// 
/// 用法：
/// TimerManager.Instance.CreateTimer(...)
/// 
/// 特性：
/// - 支持受 TimeScale 影响的游戏时间
/// - 支持不受 TimeScale 影响的真实时间 (UI动画等)
/// - 支持循环定时器
/// - 支持标签管理 (批量暂停/取消)
/// - 自动生命周期绑定 (Node 销毁时自动清理)
/// - 集成项目统一对象池系统 (零 GC 压力)
/// </summary>
public partial class TimerManager : Node
{
    /// <summary>
    /// 自动注册到引导器 (AutoLoad)
    /// 使用 ModuleInitializer 确保在程序集加载时自动完成注册，无需手动在编辑器中配置。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize() => AutoLoad.Register("TimerManager", "res://Src/Tools/Timer/TimerManager.cs", AutoLoad.Priority.Tool, "ObjectPoolInit");

    /// <summary> 获取全局单例实例 </summary>
    public static TimerManager Instance => _instance;
    private static TimerManager _instance;

    private static readonly Log _log = new("TimerManager");

    /// <summary> 定时器对象池，减少频繁创建销毁带来的 GC 压力 </summary>
    private ObjectPool<GameTimer> _timerPool;

    /// <summary> 上一帧的时间戳（毫秒），用于计算真实时间流逝 </summary>
    private ulong _lastTicksMsec;

    /// <summary> 真实时间增量（不受 Engine.TimeScale 影响） </summary>
    private float _unscaledDeltaTime;

    public override void _EnterTree()
    {
        _instance = this;
        _lastTicksMsec = Time.GetTicksMsec();

        // 从全局管理器获取定时器对象池
        _timerPool = ObjectPoolManager.GetPool<GameTimer>(ObjectPoolNames.TimerPool);

        if (_timerPool == null)
        {
            _log.Error("无法获取 TimerPool，请确保 ObjectPoolInit 已正确初始化");
            return;
        }

        // 绑定场景树的 ProcessFrame 信号，确保在每帧开始时计算真实时间
        GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
    }

    public override void _ExitTree()
    {
        // 场景切换或退出时清理所有定时器和池
        _timerPool?.ReleaseAll();
        _timerPool?.Destroy();
        _instance = null;
    }

    /// <summary>
    /// 每帧执行一次，计算两次调用之间真实经过的时间。
    /// 这种方式比使用 delta 转换更准确，因为它直接读取系统时钟。
    /// </summary>
    private void OnProcessFrame()
    {
        ulong currentTicks = Time.GetTicksMsec();
        _unscaledDeltaTime = (currentTicks - _lastTicksMsec) / 1000.0f;
        _lastTicksMsec = currentTicks;
    }

    public override void _Process(double delta)
    {
        ProcessTimers(delta);
    }

    /// <summary>
    /// 核心更新逻辑：遍历所有活跃定时器并更新它们的状态。
    /// </summary>
    /// <param name="delta">Godot 传入的每帧增量时间（受 TimeScale 影响）</param>
    private void ProcessTimers(double delta)
    {
        float scaledDelta = (float)delta;
        float unscaledDelta = _unscaledDeltaTime;

        // 使用 ObjectPool 的 ForEachActive 遍历所有当前正在使用的定时器
        _timerPool.ForEachActive(timer =>
        {
            // 1. 检查是否已完成。如果是，则将其归还到对象池。
            if (timer.IsDone)
            {
                _timerPool.Release(timer);
                return;
            }

            // 2. 检查是否已暂停。
            if (timer.IsPaused) return;

            // 3. 根据定时器配置，选择使用“游戏时间”或“真实时间”进行更新。
            float dt = timer.UseUnscaledTime ? unscaledDelta : scaledDelta;
            timer.Update(dt);
        });
    }

    /// <summary>
    /// 创建一个单次定时器
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="onComplete">完成后的回调</param>
    /// <param name="useUnscaledTime">是否使用真实时间（默认为 false，受游戏倍速影响）</param>
    /// <returns>返回创建好的定时器对象，调用者需要在 _ExitTree 中手动 Cancel 归还对象池</returns>
    public GameTimer CreateTimer(float duration, Action onComplete = null, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        timer.Configure(duration, false, useUnscaledTime);
        timer.Id = Guid.NewGuid().ToString(); // 分配唯一ID以便于精确控制

        if (onComplete != null) timer.OnComplete += onComplete;

        return timer;
    }

    /// <summary>
    /// 创建一个循环定时器
    /// </summary>
    /// <param name="interval">循环间隔时间（秒）</param>
    /// <param name="onLoop">每次循环结束时的回调</param>
    /// <param name="useUnscaledTime">是否使用真实时间</param>
    /// <returns>返回创建好的定时器对象，调用者需要在 _ExitTree 中手动 Cancel 归还对象池</returns>
    public GameTimer CreateLoopTimer(float interval, Action onLoop, bool useUnscaledTime = false)
    {
        var timer = _timerPool.Get();
        timer.Configure(interval, true, useUnscaledTime);
        timer.Id = Guid.NewGuid().ToString();

        if (onLoop != null) timer.OnLoop += onLoop;

        return timer;
    }

    /// <summary>
    /// 根据唯一 ID 手动取消特定的定时器，TimerManager 会将其收回池中，且不会触发 OnComplete 回调。
    /// </summary>
    public void Cancel(string id)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Id == id) timer.Cancel();
        });
    }

    /// <summary>
    /// 根据标签批量取消定时器
    /// </summary>
    /// <param name="tag">目标标签</param>
    public void CancelByTag(string tag)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Tag == tag) timer.Cancel();
        });
    }

    /// <summary>
    /// 批量设置所有活跃定时器的暂停状态
    /// </summary>
    public void SetAllTimerPaused(bool paused)
    {
        _timerPool.ForEachActive(timer =>
        {
            timer.IsPaused = paused;
        });
    }

    /// <summary>
    /// 根据标签批量设置暂停状态
    /// </summary>
    public void SetAllTimerPausedByTag(string tag, bool paused)
    {
        _timerPool.ForEachActive(timer =>
        {
            if (timer.Tag == tag) timer.IsPaused = paused;
        });
    }

    /// <summary> 获取当前正在运行的定时器总数 </summary>
    public int GetActiveTimerCount() => _timerPool.ActiveCount;

    /// <summary> 获取对象池统计信息 (活跃数, 总池容量) </summary>
    public (int Active, int Pooled) GetStats()
    {
        var stats = _timerPool.GetStats();
        return (stats.ActiveCount, stats.Count);
    }
}
