using Godot;

/// <summary>
/// 动作节点：随机游荡 - 在当前位置附近的圆环区域内随机选点并移动
/// <para>
/// 选点策略：以自身当前位置为圆心，在 [minWanderRadius, maxWanderRadius] 圆环内随机取点，
/// 确保每次巡逻点都有足够的距离，避免原地踏步。
/// </para>
/// <para>
/// 使用的 DataKey：
/// - PatrolTargetPoint：当前巡逻目标点（运行时黑板数据）
/// </para>
/// <para>
/// 返回值：
/// - Running：正在向目标点移动
/// - Success：已到达目标点（通常由上层 Sequence 接 WaitIdleAction）
/// - Failure：Entity 无效
/// </para>
/// </summary>
public class RandomWanderAction : BehaviorNode
{
    private readonly float _minWanderRadius;
    private readonly float _maxWanderRadius;
    private readonly float _arrivalThreshold;
    private readonly float _speedMultiplier;

    /// <summary>
    /// 创建随机游荡动作节点
    /// </summary>
    /// <param name="minWanderRadius">圆环内径（默认 200），确保不会原地踏步</param>
    /// <param name="maxWanderRadius">圆环外径（默认 400），控制游荡范围上限</param>
    /// <param name="arrivalThreshold">到达阈值（默认 20），小于此距离视为到达</param>
    /// <param name="speedMultiplier">移动速度倍率（默认 0.5，即半速巡逻）</param>
    public RandomWanderAction(
        float minWanderRadius = 300f,
        float maxWanderRadius = 800f,
        float arrivalThreshold = 20f,
        float speedMultiplier = 0.5f) : base("随机游荡")
    {
        _minWanderRadius = minWanderRadius;
        _maxWanderRadius = maxWanderRadius;
        _arrivalThreshold = arrivalThreshold;
        _speedMultiplier = speedMultiplier;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        Vector2 patrolTarget = ctx.Entity.Data.Get<Vector2>(DataKey.PatrolTargetPoint, Vector2.Zero);

        // 判断是否需要选新目标点（首次 or 已到达）
        bool needNewTarget = patrolTarget == Vector2.Zero ||
            selfNode.GlobalPosition.DistanceTo(patrolTarget) < _arrivalThreshold;

        if (needNewTarget)
        {
            // 以当前位置为圆心，在圆环内随机选下一个巡逻点
            patrolTarget = PickRingPoint(selfNode);
            ctx.Entity.Data.Set(DataKey.PatrolTargetPoint, patrolTarget);

            // 到达后返回 Success，让上层 Sequence 继续（WaitIdleAction 等待）
            return NodeState.Success;
        }

        // 尚未到达：朝目标点移动
        Vector2 direction = (patrolTarget - selfNode.GlobalPosition).Normalized();
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, direction);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, _speedMultiplier);
        ctx.Entity.Data.Set(DataKey.AIState, AIState.Patrolling);

        return NodeState.Running;
    }

    /// <summary>
    /// 在自身当前位置的圆环区域内随机取一个目标点
    /// <para>内径保证最小移动距离，外径控制最大漫游范围。</para>
    /// </summary>
    private Vector2 PickRingPoint(Node2D selfNode)
    {
        var results = PositionTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring,
            Origin = selfNode.GlobalPosition,
            InnerRange = _minWanderRadius,
            Range = _maxWanderRadius,
            MaxTargets = 1
        });

        return results[0];
    }

    /// <inheritdoc/>
    public override void Reset(AIContext? ctx = null)
    {
        // 被高优先级分支打断时（如攻击抢占巡逻），清除巡逻目标点。
        // 下次重新进入巡逻分支时，needNewTarget 判断为 true，从当前位置重新选点，
        // 避免敌人恢复巡逻时"瞬移"到被打断前的旧目标点位置附近立刻 Success。
        ctx?.Entity.Data.Set(DataKey.PatrolTargetPoint, Vector2.Zero);
    }
}
