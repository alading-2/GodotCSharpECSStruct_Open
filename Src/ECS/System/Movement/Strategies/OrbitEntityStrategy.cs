using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 5】围绕目标实体环绕。
/// <para>圆心为 <c>TargetNode</c> 的实时位置，每帧同步后复用固定圆心轨道逻辑。目标失效时停止位移但不主动完成。</para>
/// <para><code>
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.OrbitEntity, new MovementParams
///     {
///         Mode             = MoveMode.OrbitEntity,
///         TargetNode       = targetNode,
///         OrbitRadius      = 100f,
///         OrbitAngularSpeed = Mathf.Pi,   // 弧度/秒
///         OrbitClockwise   = false,        // 可选
///         MaxDuration      = -1f,          // 可选
///     }));
/// </code></para>
/// <para>【典型用途】围绕敌人旋转的护盾、跟着 Boss 转圈的子弹、盘旋飞行特效。</para>
/// </summary>
public class OrbitEntityStrategy : IMovementStrategy
{
    private float _currentAngle;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.OrbitEntity, () => new OrbitEntityStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        Vector2 center = @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode)
            ? @params.TargetNode.GlobalPosition
            : node.GlobalPosition;

        Vector2 toSelf = node.GlobalPosition - center;
        _currentAngle = toSelf.LengthSquared() > 0.001f ? toSelf.Angle() : 0f;
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();
        if (@params.TargetNode == null || !GodotObject.IsInstanceValid(@params.TargetNode))
            return MovementUpdateResult.Continue();

        Vector2 center = @params.TargetNode.GlobalPosition;
        return MovementHelper.OrbitStep(
            node, data,
            center, @params.OrbitRadius,
            @params.OrbitAngularSpeed, @params.OrbitClockwise,
            ref _currentAngle, delta);
    }
}
