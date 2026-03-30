using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】冲锋（统一冲刺 / 追点 / 追踪）。
/// <para>
/// 将原有的 Dash / TargetPoint / TargetEntity 三种冲锋策略合并为一。
/// 方向优先级：<c>TargetNode</c> &gt; <c>TargetPoint</c> &gt; <c>Angle</c> &gt; 右方向（Vector2.Right 兜底）。
/// </para>
/// <para>
/// 速度模型：<c>ActionSpeed</c> 为初速度，<c>Acceleration</c> 为冲锋加速度（像素/秒²）。
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
///         MaxDuration       = 2f,          // 最大持续时间，追踪不需要设置距离
///         DestroyOnComplete = true,
///         // Charge
///         isTrackTarget     = true,        // 【可选】打开追踪目标
///         TargetNode        = enemyNode,   // 【可选】必须：追踪目标
///         ReachDistance = 20, // 【可选】到达距离阈值，追踪一般都要设置
///     }));
///
/// 【使用示例 2：冲向固定坐标点】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         Mode = MoveMode.Charge,
///         MaxDuration       = 2f,                     // 最大持续时间，不用设置距离
///         DestroyOnComplete = true,
///         // Charge
///         TargetPoint       = new Vector2(900, 360),  // 必须：目标点
///     }));
///
/// 【使用示例 3：固定方向冲刺（方向角 / 右方向兜底）】
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Charge, new MovementParams
///     {
///         Mode = MoveMode.Charge,
///         MaxDistance = 800f,     // 最大移动距离
///         MaxDuration = 1.5f,     // 最大持续时间
///         ActionSpeed = 200f,      // 初速度
///         Acceleration = 600f,     // 冲锋加速度
///         // Charge
///         Angle = 30,
///     }));
/// </code>
/// </para>
/// </summary>
public class ChargeStrategy : IMovementStrategy
{
    private static readonly Log _log = new Log("ChargeStrategy");

    private Vector2 _lockedDirection;
    private float _currentSpeed;

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

        _currentSpeed = Mathf.Max(0f, @params.ActionSpeed);

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

        if (!Mathf.IsZeroApprox(@params.Acceleration))
            _currentSpeed = Mathf.Max(0f, _currentSpeed + @params.Acceleration * delta);

        if (_currentSpeed < 0.001f) return MovementUpdateResult.Continue();

        // 追踪模式：每帧更新朝向目标方向，目标失效后维持最后方向继续飞行
        if (@params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            Vector2 toTarget = @params.TargetNode.GlobalPosition - node.GlobalPosition;
            if (toTarget.LengthSquared() > 0.001f)
                _lockedDirection = toTarget.Normalized();
        }

        if (_lockedDirection.LengthSquared() < 0.001f) return MovementUpdateResult.Continue();

        // ReachDistance 到达判定：追踪目标节点 / 目标坐标时，进入阈值范围则提前完成
        if (@params.ReachDistance > 0f)
        {
            Vector2 checkPos;
            bool hasTarget;

            if (@params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
            {
                checkPos = @params.TargetNode.GlobalPosition;
                hasTarget = true;
            }
            else if (@params.TargetPoint != Vector2.Zero)
            {
                checkPos = @params.TargetPoint;
                hasTarget = true;
            }
            else
            {
                checkPos = Vector2.Zero;
                hasTarget = false;
            }

            if (hasTarget && MovementHelper.HasReachedTarget(node.GlobalPosition, checkPos, @params.ReachDistance))
                return MovementUpdateResult.Complete();
        }

        data.Set(DataKey.Velocity, _lockedDirection * _currentSpeed);
        return MovementUpdateResult.Continue(_currentSpeed * delta);
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

        // 3. 角度（度，非零时使用，内部转弧度）
        if (!Mathf.IsZeroApprox(@params.Angle))
            return Vector2.Right.Rotated(Mathf.DegToRad(@params.Angle));

        _log.Warn("未能解析冲锋方向（TargetNode/TargetPoint/Angle 均无效），使用默认方向 Vector2.Right。\n"
            + $"当前位置={node.GlobalPosition}, TargetPoint={@params.TargetPoint}, Angle={@params.Angle}");

        return Vector2.Right;
    }
}
