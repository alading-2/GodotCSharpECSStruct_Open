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

    /// <summary>
    /// 剩余重复次数。
    /// -1 表示无限循环（需配合 IsLoop = true）
    /// 0 表示单次定时器
    /// >0 表示剩余重复次数
    /// </summary>
    public int RepeatCount { get; internal set; }

    /// <summary>
    /// 总持续时间（倒计时模式使用）。
    /// 当设置此值时，定时器在 TotalElapsed >= TotalDuration 时自动停止。
    /// 0 表示不启用倒计时模式。
    /// </summary>
    public float TotalDuration { get; set; }

    /// <summary>
    /// 总计已流逝时间（倒计时模式使用，累计跨周期）
    /// </summary>
    public float TotalElapsed { get; private set; }

    /// <summary>
    /// 倒计时剩余时间（秒）
    /// </summary>
    public float TotalRemaining => TotalDuration > 0 ? Math.Max(0, TotalDuration - TotalElapsed) : 0;

    /// <summary>
    /// 倒计时总进度 (0.0 - 1.0)
    /// </summary>
    public float TotalProgress => TotalDuration > 0 ? Math.Clamp(TotalElapsed / TotalDuration, 0f, 1f) : 0f;

    // ============ 私有回调委托（单一委托，非 event） ============

    private Action? _onComplete;
    private Action? _onLoop;
    private Action<int>? _onRepeat;
    private Action<float, float>? _onCountdown;
    private Action<float>? _onUpdate;

    /// <summary>
    /// 是否需要在第一帧立即触发回调（用于 Repeat 的 immediate 模式）
    /// </summary>
    private bool _shouldTriggerImmediately;

    // ============ 链式 API ============

    /// <summary>
    /// 设置完成回调（链式调用）
    /// </summary>
    public GameTimer OnComplete(Action callback)
    {
        _onComplete = callback;
        return this;
    }

    /// <summary>
    /// 设置循环回调（链式调用）
    /// </summary>
    public GameTimer OnLoop(Action callback)
    {
        _onLoop = callback;
        return this;
    }

    /// <summary>
    /// 设置重复回调（链式调用），参数为当前第几次执行（从 1 开始）
    /// </summary>
    public GameTimer OnRepeat(Action<int> callback)
    {
        _onRepeat = callback;
        return this;
    }

    /// <summary>
    /// 设置倒计时回调（链式调用），参数为 (已流逝时间, 进度 0-1)
    /// </summary>
    public GameTimer OnCountdown(Action<float, float> callback)
    {
        _onCountdown = callback;
        return this;
    }

    /// <summary>
    /// 设置进度更新回调（链式调用），参数为当前进度 (0.0 - 1.0)
    /// </summary>
    public GameTimer OnUpdate(Action<float> callback)
    {
        _onUpdate = callback;
        return this;
    }

    /// <summary>
    /// 设置标签（链式调用）
    /// </summary>
    public GameTimer WithTag(string tag)
    {
        Tag = tag;
        return this;
    }

    /// <summary>
    /// 设置立即执行模式（链式调用）：在第一帧立即触发一次回调
    /// </summary>
    public GameTimer Immediate()
    {
        _shouldTriggerImmediately = true;
        return this;
    }

    // ============ 构造与配置 ============

    /// <summary>
    /// 构造函数（仅供对象池使用）
    /// </summary>
    public GameTimer()
    {
        Reset();
    }

    /// <summary>
    /// 配置定时器参数（内部使用）
    /// </summary>
    internal void Configure(float duration, bool isLoop, bool useUnscaledTime, int repeatCount = -1, float totalDuration = 0, bool immediate = false)
    {
        Duration = duration;
        IsLoop = isLoop;
        UseUnscaledTime = useUnscaledTime;
        Elapsed = 0;
        IsDone = false;
        IsPaused = false;
        IsCancelled = false;
        RepeatCount = isLoop ? repeatCount : 0;
        TotalDuration = totalDuration;
        TotalElapsed = 0;
        _shouldTriggerImmediately = immediate;
    }

    // ============ IPoolable 接口实现 ============

    /// <summary>
    /// [IPoolable] 从池中取出时调用
    /// </summary>
    public void OnPoolAcquire()
    {
        // 取出时不需要特殊处理，业务层会通过 Configure 设置参数
    }

    /// <summary>
    /// [IPoolable] 归还到池中时调用 - 清理回调，防止内存泄漏
    /// </summary>
    public void OnPoolRelease()
    {
        _onComplete = null;
        _onLoop = null;
        _onUpdate = null;
        _onRepeat = null;
        _onCountdown = null;
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
        RepeatCount = 0;
        TotalDuration = 0;
        TotalElapsed = 0;
        _shouldTriggerImmediately = false;

        // 清理回调
        _onComplete = null;
        _onLoop = null;
        _onUpdate = null;
        _onRepeat = null;
        _onCountdown = null;
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
            _onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 核心更新逻辑：由 TimerManager 驱动
    /// 处理时间累积、回调分发、循环重置及生命周期状态变更。
    /// </summary>
    /// <param name="delta">当前帧的时间增量 (Scaled 或 Unscaled)</param>
    internal void Update(float delta)
    {
        // 状态检查：已完成或暂停的计时器不进行任何处理
        if (IsDone || IsPaused) return;

        // 0. 立即执行模式：在第一帧立即触发一次回调（参考 TypeScript CycleTimer 的 immediate 参数）
        if (_shouldTriggerImmediately)
        {
            _shouldTriggerImmediately = false;  // 只执行一次

            // 触发循环回调
            _onLoop?.Invoke();

            // 指定次数模式：立即消耗一次
            if (RepeatCount > 0)
            {
                RepeatCount--;
                _onRepeat?.Invoke(RepeatCount);

                if (RepeatCount <= 0)
                {
                    IsDone = true;
                    _onComplete?.Invoke();
                    return;
                }
            }
        }

        // 1. 累加流逝时间
        Elapsed += delta;

        // 进度更新回调：每帧触发，适用于读条、进度条展示等平滑 UI 更新
        _onUpdate?.Invoke(Progress);

        // 2. 检查当前周期是否达到设定时长
        if (Elapsed >= Duration)
        {
            if (IsLoop)
            {
                // --- 循环计时模式 ---

                // 精度补偿：减去 Duration 而非设为 0，确保在帧率波动时不会丢失剩余碎时间
                Elapsed -= Duration;

                // 累计全局流逝时间 (仅在循环模式下有意义)
                TotalElapsed += Duration;

                // 触发循环事件：每完成一个固定周期调用一次
                _onLoop?.Invoke();

                // A. 指定次数重复逻辑 (RepeatCount > 0)
                if (RepeatCount > 0)
                {
                    RepeatCount--;
                    // 回传当前剩余执行次数，方便业务层追踪进度
                    _onRepeat?.Invoke(RepeatCount);

                    // 达到预设次数后，状态设为完成并触发最终回调
                    if (RepeatCount <= 0)
                    {
                        IsDone = true;
                        _onComplete?.Invoke();
                        return;
                    }
                }

                // B. 倒计时总时间限制逻辑 (TotalDuration > 0)
                if (TotalDuration > 0)
                {
                    // 触发倒计时 Tick 回调，传递累计时间与总完成度
                    _onCountdown?.Invoke(TotalElapsed, TotalProgress);

                    // 检查是否达到最终终止时间（跨越多个循环周期后的最终终点）
                    if (TotalElapsed >= TotalDuration)
                    {
                        IsDone = true;
                        _onComplete?.Invoke();
                        return;
                    }
                }

                // 防护机制：如果 delta 极大（卡顿等原因）导致越过多个周期，
                // 在单帧内仅处理一个周期逻辑后将 Elapsed 归零，防止陷入死循环或逻辑混乱。
                if (Elapsed >= Duration)
                {
                    Elapsed = 0;
                }
            }
            else
            {
                // --- 单次计时模式 (One-shot) ---

                // 确保数据对齐
                Elapsed = Duration;
                // 标记生命周期结束，下一帧会被管理器回收进池
                IsDone = true;
                // 触发唯一的完成回调
                _onComplete?.Invoke();
            }
        }
    }
}
