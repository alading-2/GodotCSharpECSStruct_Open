/// <summary>
/// 运动策略停止原因。
/// </summary>
public enum MovementStopReason
{
    /// <summary>
    /// 自然完成（策略主动完成、时间到、距离到）。
    /// </summary>
    Completed,

    /// <summary>
    /// 因运动碰撞进入统一完成流程。
    /// </summary>
    Collision,

    /// <summary>
    /// 被新的运动请求打断并切换策略。
    /// </summary>
    Interrupted,

    /// <summary>
    /// 组件注销时被动停止当前策略。
    /// </summary>
    ComponentUnregistered,
}
