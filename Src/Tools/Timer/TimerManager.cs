using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 全局定时器管理器
/// 
/// 用法：
/// 代码调用：TimerManager.Instance.CreateTimer(...)
/// 
/// 特性：
/// - 支持受 TimeScale 影响的游戏时间
/// - 支持不受 TimeScale 影响的真实时间 (UI动画等)
/// - 支持循环定时器
/// - 支持标签管理 (批量暂停/取消)
/// - 自动生命周期绑定 (Node 销毁时自动清理)
/// - 内部对象池复用 (零 GC 压力)
/// </summary>
public partial class TimerManager : Node
{
    [ModuleInitializer]
    public static void Initialize() => AutoLoad.Register("TimerManager", "res://Src/Tools/Timer/TimerManager.cs", AutoLoad.Priority.System);

    private static TimerManager _instance;
    public static TimerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _log.Warn("TimerManager 未初始化。请确保已将其添加到 AutoLoad。");
            }
            return _instance;
        }
    }

    private static readonly Log _log = new("TimerManager");

    private readonly List<GameTimer> _timers = new();
    private readonly List<GameTimer> _timersToAdd = new();

    // 对象池：复用 GameTimer 实例，避免频繁 GC
    private readonly Stack<GameTimer> _timerPool = new();
    private const int MaxPoolSize = 100;

    // ID 生成器
    private long _nextId = 1;

    // 真实时间追踪（用于 UnscaledTime）
    private ulong _lastTicksMsec;
    private float _unscaledDeltaTime;

    public override void _EnterTree()
    {
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
        _lastTicksMsec = Time.GetTicksMsec();

        // 监听场景树暂停事件
        GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
    }

    public override void _ExitTree()
    {
        // 清理所有定时器
        foreach (var timer in _timers)
        {
            timer.Cancel();
        }
        _timers.Clear();
        _timersToAdd.Clear();
        _timerPool.Clear();

        _instance = null;
    }

    private void OnProcessFrame()
    {
        // 计算真实时间差（不受 TimeScale 影响）
        ulong currentTicks = Time.GetTicksMsec();
        _unscaledDeltaTime = (currentTicks - _lastTicksMsec) / 1000.0f;
        _lastTicksMsec = currentTicks;
    }

    public override void _Process(double delta)
    {
        ProcessTimers(delta);
    }

    private void ProcessTimers(double delta)
    {
        // 1. 添加待处理的定时器
        if (_timersToAdd.Count > 0)
        {
            _timers.AddRange(_timersToAdd);
            _timersToAdd.Clear();
        }

        // 2. 更新定时器
        float scaledDelta = (float)delta; // 受 TimeScale 影响
        float unscaledDelta = _unscaledDeltaTime; // 真实时间

        for (int i = 0; i < _timers.Count; i++)
        {
            var timer = _timers[i];
            if (timer.IsDone || timer.IsPaused) continue;

            // 根据定时器类型选择时间源
            float dt = timer.UseUnscaledTime ? unscaledDelta : scaledDelta;
            timer.Update(dt);
        }

        // 3. 清理已完成的定时器并回收到对象池
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            if (_timers[i].IsDone)
            {
                var timer = _timers[i];
                _timers.RemoveAt(i);
                RecycleTimer(timer);
            }
        }
    }

    /// <summary>
    /// 创建一个一次性定时器
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="useUnscaledTime">是否使用不受 TimeScale 影响的真实时间</param>
    /// <returns>定时器对象</returns>
    public GameTimer CreateTimer(float duration, Action onComplete = null, bool useUnscaledTime = false)
    {
        var timer = GetOrCreateTimer(duration, false, useUnscaledTime);
        if (onComplete != null) timer.OnComplete += onComplete;
        Register(timer);
        return timer;
    }

    /// <summary>
    /// 创建一个绑定 Node 生命周期的定时器
    /// Node 销毁时自动取消定时器
    /// </summary>
    public GameTimer CreateTimer(Node owner, float duration, Action onComplete = null, bool useUnscaledTime = false)
    {
        var timer = CreateTimer(duration, onComplete, useUnscaledTime);

        // 绑定生命周期
        owner.TreeExiting += () => timer.Cancel();

        return timer;
    }

    /// <summary>
    /// 创建一个循环定时器
    /// </summary>
    /// <param name="interval">间隔时间（秒）</param>
    /// <param name="onLoop">每次循环触发的回调</param>
    /// <param name="useUnscaledTime">是否使用不受 TimeScale 影响的真实时间</param>
    /// <returns>定时器对象</returns>
    public GameTimer CreateLoopTimer(float interval, Action onLoop, bool useUnscaledTime = false)
    {
        var timer = GetOrCreateTimer(interval, true, useUnscaledTime);
        if (onLoop != null) timer.OnLoop += onLoop;
        Register(timer);
        return timer;
    }

    /// <summary>
    /// 创建一个绑定 Node 生命周期的循环定时器
    /// </summary>
    public GameTimer CreateLoopTimer(Node owner, float interval, Action onLoop, bool useUnscaledTime = false)
    {
        var timer = CreateLoopTimer(interval, onLoop, useUnscaledTime);
        owner.TreeExiting += () => timer.Cancel();
        return timer;
    }

    /// <summary>
    /// 从对象池获取或创建新的定时器
    /// </summary>
    private GameTimer GetOrCreateTimer(float duration, bool isLoop, bool useUnscaledTime)
    {
        GameTimer timer;

        if (_timerPool.Count > 0)
        {
            timer = _timerPool.Pop();
            timer.Reset(duration, isLoop, useUnscaledTime);
        }
        else
        {
            timer = new GameTimer(duration, isLoop, useUnscaledTime);
        }

        return timer;
    }

    /// <summary>
    /// 回收定时器到对象池
    /// </summary>
    private void RecycleTimer(GameTimer timer)
    {
        if (_timerPool.Count < MaxPoolSize)
        {
            timer.Clear(); // 清理事件订阅
            _timerPool.Push(timer);
        }
    }

    /// <summary>
    /// 注册自定义定时器
    /// </summary>
    public void Register(GameTimer timer)
    {
        timer.Id = _nextId++;
        _timersToAdd.Add(timer);
    }

    /// <summary>
    /// 取消指定 ID 的定时器
    /// </summary>
    public void Cancel(long id)
    {
        var timer = _timers.Find(t => t.Id == id);
        timer?.Cancel();
    }

    /// <summary>
    /// 取消指定 Tag 的所有定时器
    /// </summary>
    public void CancelByTag(string tag)
    {
        foreach (var timer in _timers)
        {
            if (timer.Tag == tag) timer.Cancel();
        }

        foreach (var timer in _timersToAdd)
        {
            if (timer.Tag == tag) timer.Cancel();
        }
    }

    /// <summary>
    /// 暂停/恢复所有定时器
    /// </summary>
    public void SetPaused(bool paused)
    {
        foreach (var timer in _timers)
        {
            timer.IsPaused = paused;
        }
    }

    /// <summary>
    /// 暂停/恢复指定 Tag 的定时器
    /// </summary>
    public void SetPausedByTag(string tag, bool paused)
    {
        foreach (var timer in _timers)
        {
            if (timer.Tag == tag) timer.IsPaused = paused;
        }
    }

    /// <summary>
    /// 获取当前活跃的定时器数量
    /// </summary>
    public int GetActiveTimerCount() => _timers.Count;

    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    public (int Active, int Pooled) GetStats() => (_timers.Count, _timerPool.Count);
}
