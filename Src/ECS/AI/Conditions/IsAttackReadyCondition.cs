/// <summary>
/// 条件节点：检查攻击组件是否处于可发起新攻击的空闲状态
/// <para>
/// 读取 DataKey.AttackState，仅当 Idle 时返回 Success。
/// WindUp / Recovery / 冷却期均返回 Failure。
/// </para>
/// </summary>
public class IsAttackReadyCondition : BehaviorNode
{
    /// <summary>
    /// 创建攻击冷却检测条件节点
    /// </summary>
    public IsAttackReadyCondition() : base("攻击冷却就绪") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var state = ctx.Entity.Data.Get<AttackState>(DataKey.AttackState);
        return state == AttackState.Idle ? NodeState.Success : NodeState.Failure;
    }
}
