using System;

/// <summary>
/// 游戏定时器对象
/// 由 TimerManager 创建和管理
/// 支持对象池复用，避免频繁 GC
/// </summary>
public class GameTimer
{
    /// <summary> 定时器唯一ID </summary>
    public long Id { get; internal set; }

    /// <summary> 持续时间（秒） </summary>
    public float Duration { get; set; }

    /// <summary> 当前流逝时间 </summary>
    public float Elapsed { get; private set; }

    /// <summary> 剩余时间 </summary>
    public float Remaining => Math.Max(0, Duration - Elapsed);

    /// <summary> 完成进度 (0.0 - 1.0) </summary>
    public float Progress => Duration > 0 ? Math.Clamp(Elapsed / Duration, 0f, 1f) : 1f;

    /// <summary> 是否循环 </summary>
    public bool IsLoop { get; set; }

    /// <summary> 是否使用不受 TimeScale 影响的真实时间 </summary>
    public bool UseUnscaledTime { get; set; }

    /// <summary> 是否已暂停 </summary>
    public bool IsPaused { get; set; }

    /// <summary> 是否已完成 </summary>
    public bool IsDone { get; internal set; }

    /// <summary> 是否已被取消 </summary>
    public bool IsCancelled { get; internal set; }

    /// <summary> 定时器标签（用于批量管理） </summary>
    public string? Tag { get; set; }

    // 回调事件
    public event Action? OnComplete;
    public event Action? OnLoop;
    public event Action<float>? OnUpdate;

    public GameTimer(float duration, bool isLoop = false, bool useUnscaledTime = false)
    {
        Duration = duration;
        IsLoop = isLoop;
        UseUnscaledTime = useUnscaledTime;
        Elapsed = 0;
        IsDone = false;
        IsPaused = false;
        IsCancelled = false;
    }

    /// <summary>
    /// 重置定时器状态（对象池复用）
    /// </summary>
    internal void Reset(float duration, bool isLoop, bool useUnscaledTime)
    {
        Duration = duration;
        IsLoop = isLoop;
        UseUnscaledTime = useUnscaledTime;
        Elapsed = 0;
        IsDone = false;
        IsPaused = false;
        IsCancelled = false;
        Tag = null;
    }

    /// <summary>
    /// 清理事件订阅（对象池回收前调用）
    /// </summary>
    internal void Clear()
    {
        OnComplete = null;
        OnLoop = null;
        OnUpdate = null;
        Tag = null;
    }

    /// <summary>
    /// 暂停定时器
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// 恢复定时器
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// 取消定时器
    /// </summary>
    public void Cancel()
    {
        IsCancelled = true;
        IsDone = true;
    }

    /// <summary>
    /// 立即完成定时器（触发回调）
    /// </summary>
    public void Complete(bool triggerCallback = true)
    {
        Elapsed = Duration;
        if (triggerCallback)
        {
            OnComplete?.Invoke();
        }
        IsDone = true;
    }

    /// <summary>
    /// 内部更新逻辑
    /// </summary>
    internal void Update(float delta)
    {
        if (IsDone || IsPaused) return;

        Elapsed += delta;
        OnUpdate?.Invoke(Progress);

        if (Elapsed >= Duration)
        {
            if (IsLoop)
            {
                // 循环定时器：保留超出部分的时间，确保精度
                Elapsed -= Duration;
                OnLoop?.Invoke();

                // 防止单帧时间过长导致多次触发
                // 如果 Elapsed 仍然 >= Duration，说明掉帧严重，直接归零
                if (Elapsed >= Duration)
                {
                    Elapsed = 0;
                }
            }
            else
            {
                // 单次定时器
                Elapsed = Duration;
                IsDone = true;
                OnComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// 添加完成回调（内部使用）
    /// </summary>
    internal void AddCompleteCallback(Action callback)
    {
        OnComplete += callback;
    }
}
