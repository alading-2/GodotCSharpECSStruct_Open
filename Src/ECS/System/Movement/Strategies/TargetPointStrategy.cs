using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 2】目标点冲锋
/// <para>向 DataKey.MoveTargetPoint 直线运动，到达后返回 -1 标记完成。</para>
/// <para>位移补偿：单帧步长超过剩余距离时，直接修正到目标点避免抖动。</para>
/// </summary>
public class TargetPointStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.TargetPoint, new TargetPointStrategy());
    }

    /// <returns>估算位移量；返回 -1 表示运动完成（由调度器触发 OnMoveComplete）</returns>
    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D node) return 0f;

        Vector2 target = data.Get<Vector2>(DataKey.MoveTargetPoint);
        Vector2 toTarget = target - node.GlobalPosition;
        float dist = toTarget.Length();

        float reach = MovementHelper.GetReachDistance(data);
        if (dist <= reach)
        {
            // 到达：写入指向目标的速度，由调度器执行最后一步位移
            data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f));
            return -1f;
        }

        float speed = data.Get<float>(DataKey.MoveSpeed);
        Vector2 dir = toTarget / dist;
        float step = speed * delta;

        if (step >= dist)
        {
            // 单帧超越目标：写入精确速度
            data.Set(DataKey.Velocity, toTarget / Mathf.Max(delta, 0.001f));
            return -1f;
        }

        Vector2 velocity = dir * speed;
        data.Set(DataKey.Velocity, velocity);

        // 位移由调度器统一执行
        return step;
    }
}
