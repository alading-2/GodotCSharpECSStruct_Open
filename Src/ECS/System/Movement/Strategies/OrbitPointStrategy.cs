using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 4】围绕固定点环绕。
/// <para>让实体围绕固定世界坐标做圆周运动，OnEnter 自动从当前位置推导初始极角，不会出现第一帧跳变。</para>
/// <para><code>
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.OrbitPoint, new MovementParams
///     {
///         Mode             = MoveMode.OrbitPoint,
///         OrbitCenter      = centerPos,
///         OrbitRadius      = 100f,
///         OrbitAngularSpeed = Mathf.Pi,   // 弧度/秒
///         OrbitClockwise   = false,        // 可选，默认逆时针
///         MaxDuration      = -1f,          // 可选，-1 不限制
///     }));
/// </code></para>
/// <para>
/// 【典型用途】固定圆心护盾、围绕地面锚点旋转的特效、固定轨道的弹幕圆环。
/// `OrbitEntityStrategy` 和 `SpiralStrategy` 的进入阶段都会复用本策略的极角初始化逻辑。
/// </para>
/// </summary>
public class OrbitPointStrategy : IMovementStrategy
{
    private float _currentAngle;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.OrbitPoint, () => new OrbitPointStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        Vector2 toSelf = node.GlobalPosition - @params.OrbitCenter;
        _currentAngle = toSelf.LengthSquared() > 0.001f ? toSelf.Angle() : 0f;
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        return MovementHelper.OrbitStep(
            node, data,
            @params.OrbitCenter, @params.OrbitRadius,
            @params.OrbitAngularSpeed, @params.OrbitClockwise,
            ref _currentAngle, delta);
    }
}
