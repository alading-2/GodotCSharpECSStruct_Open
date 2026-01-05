using System;

/// <summary>
/// 游戏定时器对象
/// 由 TimerManager 创建和管理，支持高精度计时、回调机制及对象池复用。
/// 实现 IPoolable 接口以支持对象池管理。
/// </summary>
public class GameTimer : IPoolable
{
    /// <summary> 定时器唯一ID，使用 Guid 确保全局唯一性 </summary>
    public string Id { get; internal set; } = string.Empty;

    /// <summary> 定时器设定的总持续时间（秒） </summary>
    public float Duration { get; set; }

    /// <summary> 当前已经流逝的时间（秒） </summary>
    public float Elapsed { get; private set; }

    /// <summary> 距离结束还剩下的时间（秒） </summary>
    public float Remaining => Math.Max(0, Duration - Elapsed);

    /// <summary> 当前进度的百分比 (0.0 表示开始，1.0 表示完成) </summary>
    public float Progress => Duration > 0 ? Math.Clamp(Elapsed / Duration, 0f, 1f) : 1f;

    /// <summary> 是否为循环定时器（完成后自动重置并重新计时） </summary>
    public bool IsLoop { get; set; }

    /// <summary> 
    /// 是否使用不受 Engine.TimeScale 影响的真实时间。
    /// true: 用于 UI 动画或暂停菜单。
    /// false: 用于受游戏倍速或暂停影响的战斗逻辑。
    /// </summary>
    public bool UseUnscaledTime { get; set; }

    /// <summary> 是否处于暂停状态 </summary>
    public bool IsPaused { get; set; }

    /// <summary> 是否已完成（或已取消） </summary>
    public bool IsDone { get; internal set; }

    /// <summary> 是否是被手动取消的 </summary>
    public bool IsCancelled { get; internal set; }

    /// <summary> 
    /// 定时器标签。用于批量管理（例如：取消所有带 "Buff" 标签的定时器）。
    /// </summary>
    public string? Tag { get; set; }

    // --- 回调事件 ---

    /// <summary> 定时器自然结束时触发的回调 </summary>
    public event Action? OnComplete;

    /// <summary> 循环定时器每完成一轮时触发的回调 </summary>
    public event Action? OnLoop;

    /// <summary> 每帧更新时触发的回调，参数为当前进度 (0.0 - 1.0) </summary>
    public event Action<float>? OnUpdate;

    /// <summary>
    /// 构造函数（仅供对象池使用）
    public GameTimer()
    {
        Reset();
    }

    /// <summary>
    /// 配置定时器参数（业务层调用）
    /// 从对象池取出后，通过此方法设置定时器的运行参数
    /// </summary>
    /// <param name="duration">持续时间</param>
    /// <param name="isLoop">是否循环</param>
    /// <param name="useUnscaledTime">是否使用真实时间</param>
    internal void Configure(float duration, bool isLoop, bool useUnscaledTime)
    {
        Duration = duration;
        IsLoop = isLoop;
        UseUnscaledTime = useUnscaledTime;
        Elapsed = 0;
        IsDone = false;
        IsPaused = false;
        IsCancelled = false;
    }

    // ============================================================
    // IPoolable 接口实现
    // ============================================================

    /// <summary>
    /// [IPoolable] 从池中取出时调用
    /// </summary>
    public void OnPoolAcquire()
    {
        // 取出时不需要特殊处理，业务层会通过 Configure 设置参数
    }

    /// <summary>
    /// [IPoolable] 归还到池中时调用 - 清理事件订阅，防止内存泄漏
    /// </summary>
    public void OnPoolRelease()
    {
        OnComplete = null;
        OnLoop = null;
        OnUpdate = null;
    }

    /// <summary>
    /// [IPoolable] 重置数据 - 恢复到对象池默认状态
    /// </summary>
    public void OnPoolReset()
    {
        Reset();
    }

    /// <summary>
    /// 重置定时器状态
    /// </summary>
    public void Reset()
    {
        // 清理标识符
        Id = string.Empty;
        Tag = null;

        // 恢复默认状态
        Duration = 0;
        Elapsed = 0;
        IsLoop = false;
        UseUnscaledTime = false;
        IsPaused = false;
        IsDone = false;
        IsCancelled = false;
    }

    /// <summary>
    /// 暂停计时
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// 恢复计时
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// 取消定时器
    /// 标记为已完成且已取消，此时 TimerManager 会将其收回池中，且不会触发 OnComplete 回调。
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        IsDone = true;
    }

    /// <summary>
    /// 强制立即完成定时器
    /// </summary>
    /// <param name="triggerCallback">是否触发 OnComplete 回调（默认为 true）</param>
    public void Complete(bool triggerCallback = true)
    {
        if (IsDone) return;

        Elapsed = Duration;
        IsDone = true;

        if (triggerCallback)
        {
            OnComplete?.Invoke();
        }
    }

    /// <summary>
    /// 内部更新逻辑，由 TimerManager 每帧调用
    /// </summary>
    /// <param name="delta">本帧流逝的时间（由 TimerManager 根据 UseUnscaledTime 传入对应的 delta）</param>
    internal void Update(float delta)
    {
        // 如果已完成、已取消或已暂停，则停止更新
        if (IsDone || IsPaused) return;

        // 累加时间
        Elapsed += delta;

        // 触发每帧进度更新回调
        OnUpdate?.Invoke(Progress);

        // 检查是否达到或超过目标时长
        if (Elapsed >= Duration)
        {
            if (IsLoop)
            {
                // --- 循环模式计时精度优化 ---
                // 1. 减去一个周期，而不是直接归零。
                // 这样可以保留超出 Duration 的那部分时间（Elapsed % Duration），
                // 确保在长时间运行或帧率波动时，定时器的总触发次数是准确的。
                Elapsed -= Duration;

                // 2. 触发循环回调
                OnLoop?.Invoke();

                // 3. 极端情况防护：如果单帧 delta 极大（如严重掉帧），导致减去一个周期后
                // 仍然大于 Duration，则强制归零，防止在一帧内产生过多的逻辑堆积。
                if (Elapsed >= Duration)
                {
                    Elapsed = 0;
                }
            }
            else
            {
                // --- 单次模式 ---
                Elapsed = Duration;
                IsDone = true;
                OnComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// 内部便捷方法：添加完成回调
    /// </summary>
    internal void AddCompleteCallback(Action callback)
    {
        OnComplete += callback;
    }
}
