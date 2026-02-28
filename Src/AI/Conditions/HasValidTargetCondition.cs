using Godot;

/// <summary>
/// 条件节点：检查是否拥有一个存活、合法的目标
/// <para>
/// 纯查询节点，只读取 DataKey.TargetNode 并校验目标有效性。
/// 不负责索敌（索敌由 FindEnemyAction 完成）。
/// </para>
/// </summary>
public class HasValidTargetCondition : BehaviorNode
{
    /// <summary>
    /// 创建有效目标检测条件节点
    /// </summary>
    public HasValidTargetCondition() : base("有有效目标") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);

        // 目标不存在或已被释放
        if (target == null || !GodotObject.IsInstanceValid(target))
        {
            ctx.Entity.Data.Remove(DataKey.TargetNode);
            return NodeState.Failure;
        }

        // 目标是否仍具备生命体征（Dead/Reviving 视为无效）
        if (target is IEntity targetEntity)
        {
            var state = targetEntity.Data.Get<LifecycleState>(DataKey.LifecycleState);
            if (state == LifecycleState.Dead ||
                state == LifecycleState.Reviving)
            {
                ctx.Entity.Data.Remove(DataKey.TargetNode);
                return NodeState.Failure;
            }
        }

        return NodeState.Success;
    }
}
