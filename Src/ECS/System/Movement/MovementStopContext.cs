using Godot;

/// <summary>
/// 一次运动停止时传递给策略的统一上下文。
/// </summary>
public readonly struct MovementStopContext
{
    /// <summary>
    /// 创建一个新的停止上下文。
    /// </summary>
    public MovementStopContext(
        MovementStopReason reason,
        MoveMode mode,
        MovementParams @params,
        Node2D? collisionTarget = null,
        MoveMode nextMode = MoveMode.None)
    {
        Reason = reason;
        Mode = mode;
        Params = @params;
        CollisionTarget = collisionTarget;
        NextMode = nextMode;
    }

    /// <summary>
    /// 停止原因。
    /// </summary>
    public MovementStopReason Reason { get; }

    /// <summary>
    /// 停止前的运动模式。
    /// </summary>
    public MoveMode Mode { get; }

    /// <summary>
    /// 本次运动最终参数与统计快照。
    /// </summary>
    public MovementParams Params { get; }

    /// <summary>
    /// 若因碰撞停止，则为碰撞目标；否则为 null。
    /// </summary>
    public Node2D? CollisionTarget { get; }

    /// <summary>
    /// 停止后即将切换到的下一模式；若没有则为 <c>MoveMode.None</c>。
    /// </summary>
    public MoveMode NextMode { get; }

    /// <summary>
    /// 当前停止是否属于“完成”语义（自然完成或碰撞完成）。
    /// </summary>
    public bool IsCompleted => Reason == MovementStopReason.Completed || Reason == MovementStopReason.Collision;

    /// <summary>
    /// 当前停止是否由新运动请求打断。
    /// </summary>
    public bool IsInterrupted => Reason == MovementStopReason.Interrupted;

    /// <summary>
    /// 当前停止是否由碰撞触发。
    /// </summary>
    public bool IsCollision => Reason == MovementStopReason.Collision;
}
