using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 13】冲锋（统一冲刺 / 追点 / 追踪）。
/// <para>
/// 将原有的 Dash / TargetPoint / TargetEntity 三种冲锋策略合并为一。
/// 方向优先级：<c>TargetNode</c> &gt; <c>TargetPoint</c> &gt; <c>Angle</c> &gt; 右方向（Vector2.Right 兜底）。
/// </para>
/// <para>
/// <c>isTrackTarget</c>（仅 <c>TargetNode</c> 有效时生效）：
/// <list type="bullet">
/// <item><c>true</c> = 每帧修正朝向目标（追踪模式），目标消失后维持最后方向继续飞行</item>
/// <item><c>false</c>（默认）= OnEnter 时一次性采样方向后锁定，不再更新</item>
/// </list>
/// </para>
/// <para>
/// <code>
/// 【使用示例 1：追踪目标实体（追踪导弹）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         Mode = MoveMode.Charge,
///         isTrackTarget     = true,        // 打开追踪目标
///         TargetNode        = enemyNode,   // 必须：追踪目标
///         MaxDuration       = 2f,          // 最大持续时间，追踪不需要设置距离
///         DestroyOnComplete = true,
///     }));
///
/// 【使用示例 2：冲向固定坐标点】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         Mode = MoveMode.Charge,
///         TargetPoint       = new Vector2(900, 360),  // 必须：目标点
///         MaxDuration       = 2f,                     // 最大持续时间，不用设置距离
///         DestroyOnComplete = true,
///     }));
///
/// 【使用示例 3：固定方向冲刺（方向角 / 右方向兜底）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         Mode = MoveMode.Charge,
///         Angle = 30,
///         MaxDistance = 800f,     // 最大移动距离
///         MaxDuration = 1.5f,     // 最大持续时间
///     }));
/// </code>
/// </para>
/// </summary>
public class ChargeStrategy : IMovementStrategy
{
    private static readonly Log _log = new Log("ChargeStrategy");

    private Vector2 _lockedDirection;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Charge, () => new ChargeStrategy());
    }

    /// <inheritdoc/>
    public bool CanBeInterrupted => false;

    /// <inheritdoc/>
    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        if (@params.isTrackTarget && @params.TargetNode == null)
            _log.Warn("isTrackTarget=true 但 TargetNode 未设置，追踪将无效，退化为固定方向冲刺。");

        // 非追踪模式：OnEnter 时一次性锁定方向
        if (!@params.isTrackTarget)
            _lockedDirection = ResolveDirection(node, @params);
    }

    /// <inheritdoc/>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();
        if (@params.ActionSpeed < 0.001f) return MovementUpdateResult.Continue();

        // 追踪模式：每帧更新朝向目标方向，目标失效后维持最后方向继续飞行
        if (@params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            Vector2 toTarget = @params.TargetNode.GlobalPosition - node.GlobalPosition;
            if (toTarget.LengthSquared() > 0.001f)
                _lockedDirection = toTarget.Normalized();
        }

        if (_lockedDirection.LengthSquared() < 0.001f) return MovementUpdateResult.Continue();

        data.Set(DataKey.Velocity, _lockedDirection * @params.ActionSpeed);
        return MovementUpdateResult.Continue(@params.ActionSpeed * delta);
    }

    /// <summary>OnEnter 时解析初始方向（优先级：TargetNode 采样位置 > TargetPoint > Angle > 右方向兜底）</summary>
    private static Vector2 ResolveDirection(Node2D node, MovementParams @params)
    {
        // 1. 目标实体（OnEnter 时采样位置，之后方向锁定）
        if (@params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            Vector2 toTarget = @params.TargetNode.GlobalPosition - node.GlobalPosition;
            if (toTarget.LengthSquared() > 0.001f)
                return toTarget.Normalized();
        }

        // 2. 目标点
        if (@params.TargetPoint != Vector2.Zero)
        {
            Vector2 toTarget = @params.TargetPoint - node.GlobalPosition;
            if (toTarget.LengthSquared() > 0.001f)
                return toTarget.Normalized();
        }

        // 3. 角度（非零时使用）
        if (!Mathf.IsZeroApprox(@params.Angle))
            return Vector2.Right.Rotated(@params.Angle);

        return Vector2.Right;
    }
}
