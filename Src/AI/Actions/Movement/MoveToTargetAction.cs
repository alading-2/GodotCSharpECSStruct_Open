using Godot;

/// <summary>
/// 动作节点：向当前目标移动
/// <para>
/// 通过 DataKey 设置移动方向和速度倍率，由 EnemyMovementComponent 执行实际物理移动。
/// </para>
/// <para>
/// 返回值：
/// - Running：正在向目标移动中
/// - Success：已到达停止距离（仅在传入 stopRangeKey 时生效）
/// - Failure：目标丢失
/// </para>
/// </summary>
public class MoveToTargetAction : BehaviorNode
{
    private readonly string? _stopRangeKey;

    /// <summary>
    /// 创建向目标移动的动作节点
    /// </summary>
    /// <param name="stopRangeKey">
    /// 停止距离的 DataKey（可选）。
    /// 传入后，到达该距离时停止移动并返回 Success；
    /// 不传则一直贴近目标，始终返回 Running。
    /// </param>
    public MoveToTargetAction(string? stopRangeKey = null) : base("移动到目标")
    {
        _stopRangeKey = stopRangeKey;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        float distance = selfNode.GlobalPosition.DistanceTo(target.GlobalPosition);

        // 已到达停止距离，停步并返回 Success
        if (_stopRangeKey != null)
        {
            float stopRange = ctx.Entity.Data.Get<float>(_stopRangeKey);
            if (distance <= stopRange)
            {
                ctx.Entity.Data.Set(DataKey.AIMoveDirection, Vector2.Zero);
                ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, 0.0f);
                return NodeState.Success;
            }
        }

        // 计算到目标的方向向量
        Vector2 direction = (target.GlobalPosition - selfNode.GlobalPosition).Normalized();

        // 通过 DataKey 设置移动意图
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, direction);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, 1.0f);

        // 更新 AI 状态标记
        ctx.Entity.Data.Set(DataKey.AIState, AIState.Chasing);

        return NodeState.Running;
    }
}
