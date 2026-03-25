using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 1】固定方向冲锋
/// <para>沿 DataKey.Velocity 向量匀速直线运动。适用于直线子弹或已知速度向量的实体。</para>
/// </summary>
public class FixedDirectionStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.FixedDirection, new FixedDirectionStrategy());
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        Vector2 velocity = data.Get<Vector2>(DataKey.Velocity);
        if (velocity.LengthSquared() < 0.001f) return 0f;

        // Velocity 已在 Data 中，由调度器统一执行位移
        return velocity.Length() * delta;
    }
}
