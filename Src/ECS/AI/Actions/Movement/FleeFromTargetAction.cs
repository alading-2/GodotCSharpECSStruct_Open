using Godot;

/// <summary>
/// 动作节点：逃离当前目标
/// <para>
/// 以目标反方向全速移动，适用于低血量逃跑、远程敌人保持距离等场景。
/// </para>
/// <para>
/// 返回值：
/// - Running：正在逃跑中
/// - Failure：目标丢失或 Entity 无效
/// </para>
/// </summary>
public class FleeFromTargetAction : BehaviorNode
{
    private readonly float _speedMultiplier;

    /// <summary>
    /// 创建逃跑动作节点
    /// </summary>
    /// <param name="speedMultiplier">移动速度倍率（默认 1.0，全速逃跑）</param>
    public FleeFromTargetAction(float speedMultiplier = 1.0f) : base("逃离目标")
    {
        _speedMultiplier = speedMultiplier;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        // 反向逃跑：目标到自身的方向
        Vector2 fleeDirection = (selfNode.GlobalPosition - target.GlobalPosition).Normalized();

        ctx.Entity.Data.Set(DataKey.AIMoveDirection, fleeDirection);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, _speedMultiplier);
        ctx.Entity.Data.Set(DataKey.AIState, AIState.Fleeing);

        return NodeState.Running;
    }
}
