using Godot;

/// <summary>
/// 动作节点：发起攻击请求
/// <para>
/// 通过 EventBus 发射 attack:requested 事件，由 AttackComponent 执行实际攻击。
/// 攻击期间停止移动并面向目标。
/// </para>
/// <para>
/// 返回值：
/// - Running：正在执行攻击（前摇中或刚发起请求）
/// - Failure：目标丢失、攻击后摇/冷却中
/// - Success：不会返回（攻击是异步的）
/// </para>
/// </summary>
public class RequestAttackAction : BehaviorNode
{
    /// <summary>
    /// 创建攻击请求动作节点
    /// </summary>
    public RequestAttackAction() : base("发起攻击") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get<Node2D>(DataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        var attackState = ctx.Entity.Data.Get<AttackState>(DataKey.AttackState);

        // 攻击期间：面向目标但停止移动
        Vector2 faceDir = (target.GlobalPosition - selfNode.GlobalPosition).Normalized();
        ctx.Entity.Data.Set(DataKey.AIMoveDirection, faceDir);
        ctx.Entity.Data.Set(DataKey.AIMoveSpeedMultiplier, 0f);

        if (attackState != AttackState.Idle)
        {
            if (attackState == AttackState.WindUp)
            {
                // 前摇动画执行中：保持阻塞，等待打出这一刀
                return NodeState.Running;
            }
            // Recovery / 追加CD冷却期：此次攻击已经完成，
            // 返回 Failure 让攻击序列失败，Selector 可转入追逐序列
            return NodeState.Failure;
        }

        // 至此必定是真正开始新的单次攻击
        ctx.Entity.Events?.Emit(GameEventType.Attack.Requested,
            new GameEventType.Attack.RequestedEventData(target));

        // 刚派发完事件，攻击动作将在未来几帧异步执行
        return NodeState.Running;
    }
}
