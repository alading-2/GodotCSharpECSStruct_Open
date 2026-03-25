using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 3】追踪目标实体
/// <para>动态追踪 DataKey.MoveTargetNode 节点。目标丢失时降级为 FixedDirection。</para>
/// </summary>
public class TargetEntityStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.TargetEntity, new TargetEntityStrategy());
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D node) return 0f;

        var targetNode = data.Get<Node2D>(DataKey.MoveTargetNode);
        if (targetNode == null || !GodotObject.IsInstanceValid(targetNode))
        {
            // 目标丢失，降级为固定方向直线飞行
            var fallback = MovementStrategyRegistry.Get(MoveMode.FixedDirection);
            return fallback?.Update(entity, data, delta) ?? 0f;
        }

        Vector2 toTarget = targetNode.GlobalPosition - node.GlobalPosition;
        float dist = toTarget.Length();

        float reach = MovementHelper.GetReachDistance(data);
        if (dist <= reach)
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return -1f; // 到达完成
        }

        float speed = data.Get<float>(DataKey.MoveSpeed);
        Vector2 dir = toTarget / dist;
        float actualStep = Mathf.Min(speed * delta, dist);

        Vector2 velocity = dir * speed;
        data.Set(DataKey.Velocity, velocity);

        // 位移由调度器统一执行
        return actualStep;
    }
}
