using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 5】实体环绕
/// <para>以 DataKey.MoveTargetNode 为动态圆心进行环绕。每帧同步目标位置后复用 OrbitPoint 逻辑。</para>
/// </summary>
public class OrbitEntityStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.OrbitEntity, new OrbitEntityStrategy());
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        var targetNode = data.Get<Node2D>(DataKey.MoveTargetNode);
        if (targetNode == null || !GodotObject.IsInstanceValid(targetNode)) return 0f;

        // 同步目标位置到环绕圆心
        data.Set(DataKey.OrbitCenterPoint, targetNode.GlobalPosition);

        // 复用 OrbitPoint 策略
        var orbitStrategy = MovementStrategyRegistry.Get(MoveMode.OrbitPoint);
        return orbitStrategy?.Update(entity, data, delta) ?? 0f;
    }
}
