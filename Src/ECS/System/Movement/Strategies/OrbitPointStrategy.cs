using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 4】中心点环绕
/// <para>围绕 DataKey.OrbitCenterPoint 做圆周运动。</para>
/// <para>首帧自动推算初始极角，避免角度跳变。</para>
/// </summary>
public class OrbitPointStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.OrbitPoint, new OrbitPointStrategy());
    }

    /// <summary>
    /// 进入时根据实体当前位置推算初始极角，写入 Data 避免首帧角度跳变
    /// </summary>
    public void OnEnter(IEntity entity, Data data)
    {
        if (entity is not Node2D node) return;

        Vector2 center = data.Get<Vector2>(DataKey.OrbitCenterPoint);
        Vector2 toSelf = node.GlobalPosition - center;
        float initAngle = toSelf.LengthSquared() > 0.001f
            ? toSelf.Angle()
            : data.Get<float>(DataKey.OrbitAngle);
        data.Set(DataKey.OrbitAngle, initAngle);
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D node) return 0f;

        Vector2 center = data.Get<Vector2>(DataKey.OrbitCenterPoint);
        float radius = data.Get<float>(DataKey.OrbitRadius);
        float angularSpeed = data.Get<float>(DataKey.OrbitAngularSpeed);

        if (radius <= 0f || angularSpeed <= 0f) return 0f;

        bool clockwise = data.Get<bool>(DataKey.OrbitClockwise);
        float sign = clockwise ? -1f : 1f;

        float angle = data.Get<float>(DataKey.OrbitAngle) + sign * angularSpeed * delta;
        data.Set(DataKey.OrbitAngle, angle);

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Vector2 newPos = center + new Vector2(cos * radius, sin * radius);

        // 将绝对位置意图转换为速度，由调度器统一执行位移
        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();
        Vector2 velocity = displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero;
        data.Set(DataKey.Velocity, velocity);

        return displacement;
    }
}
