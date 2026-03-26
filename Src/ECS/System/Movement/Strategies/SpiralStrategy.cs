using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 6】螺旋运动。
/// <para>在圆周环绕基础上增加半径渐变：半径从 <c>OrbitRadius</c> 逐步逼近 <c>OrbitTargetRadius</c>，达到后以新半径继续环绕。</para>
/// <code>
/// entity.Events.Emit(GameEventType.Unit.MovementStarted,
///     new GameEventType.Unit.MovementStartedEventData(MoveMode.Spiral, new MovementParams
///     {
///         Mode              = MoveMode.Spiral,
///         OrbitCenter       = centerPos,
///         OrbitRadius       = 200f,        // 初始半径
///         OrbitTargetRadius = 50f,         // 目标半径（收缩/扩张到此停止）
///         OrbitAngularSpeed = Mathf.Pi,
///         OrbitRadialSpeed  = 50f,         // 可选，半径变化速度（像素/秒）
///         OrbitClockwise    = false,       // 可选
///         MaxDuration       = -1f,         // 可选
///     }));
/// </code>
/// <para>【典型用途】螺旋收束弹幕、由外向内聚拢的卫星、旋转波纹特效。</para>
/// </summary>
public class SpiralStrategy : IMovementStrategy
{
    private float _currentAngle;
    private float _currentRadius;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Spiral, () => new SpiralStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        _currentRadius = @params.OrbitRadius;
        Vector2 toSelf = node.GlobalPosition - @params.OrbitCenter;
        _currentAngle = toSelf.LengthSquared() > 0.001f ? toSelf.Angle() : 0f;
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();

        float targetRadius = @params.OrbitTargetRadius;
        if (!Mathf.IsEqualApprox(_currentRadius, targetRadius))
        {
            float radialSpeed = @params.OrbitRadialSpeed > 0f ? @params.OrbitRadialSpeed : 50f;
            float dr = radialSpeed * delta;
            _currentRadius = targetRadius > _currentRadius
                ? Mathf.Min(_currentRadius + dr, targetRadius)
                : Mathf.Max(_currentRadius - dr, targetRadius);
        }

        return MovementHelper.OrbitStep(
            node, data,
            @params.OrbitCenter, _currentRadius,
            @params.OrbitAngularSpeed, @params.OrbitClockwise,
            ref _currentAngle, delta);
    }
}
