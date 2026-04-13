using Godot;

/// <summary>
/// 动作节点：调用 TargetSelector 搜索范围内最近的敌方目标
/// <para>
/// 包含仇恨锁定机制：已有目标时仅校验是否超出丢失范围，不会换目标。
/// 无目标时在 DetectionRange 内搜索最近敌方单位并缓存到 DataKey.TargetNode。
/// </para>
/// <para>
/// 返回值：
/// - Success：成功找到或维持了目标
/// - Failure：范围内无目标
/// </para>
/// </summary>
public class FindEnemyAction : BehaviorNode
{
    private static readonly Log _log = new("FindEnemy");

    /// <summary>
    /// 创建索敌动作节点
    /// </summary>
    public FindEnemyAction() : base("索敌") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        // 1. 已有存活目标：检查是否超出丢失范围
        var targetNode = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);
        if (targetNode != null && GodotObject.IsInstanceValid(targetNode))
        {
            float loseDist = ctx.Entity.Data.Get<float>(DataKey.LoseTargetRange);
            if (selfNode.GlobalPosition.DistanceTo(targetNode.GlobalPosition) > loseDist)
            {
                // 目标逃脱太远，放弃追踪
                ctx.Entity.Data.Remove(DataKey.TargetNode);
            }
            else
            {
                // 目标健在且未追丢，维持仇恨
                return NodeState.Success;
            }
        }

        // 2. 无目标，使用 TargetSelector 搜索最近敌方

        float detectionRange = ctx.Entity.Data.Get<float>(DataKey.DetectionRange);
        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = selfNode.GlobalPosition,
            Range = detectionRange,
            CenterEntity = ctx.Entity,
            TeamFilter = AbilityTargetTeamFilter.Enemy,
            Sorting = TargetSorting.HighestThreat,
            MaxTargets = 1
        });

        if (targets != null && targets.Count > 0)
        {
            var target = targets[0];
            if (target is Node2D node2D)
            {
                ctx.Entity.Data.Set(DataKey.TargetNode, node2D);
                _log.Debug($"发现新目标开始锁定: {node2D.Name}");
                return NodeState.Success;
            }
        }

        return NodeState.Failure;
    }
}
