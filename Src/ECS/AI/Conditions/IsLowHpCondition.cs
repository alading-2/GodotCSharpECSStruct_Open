/// <summary>
/// 条件节点：检查自身生命值是否低于阈值
/// <para>
/// 用于触发逃跑、变阶段、使用治疗技能等行为。
/// </para>
/// <para>
/// 返回值：
/// - Success：当前血量百分比低于阈值
/// - Failure：血量充足或无生命数据
/// </para>
/// </summary>
public class IsLowHpCondition : BehaviorNode
{
    private readonly float _hpThresholdPercent;

    /// <summary>
    /// 创建低血量检测条件节点
    /// </summary>
    /// <param name="hpThresholdPercent">触发阈值（0~100，默认 30，即低于 30% 时触发）</param>
    public IsLowHpCondition(float hpThresholdPercent = 30f) : base($"低血量({hpThresholdPercent}%)")
    {
        _hpThresholdPercent = hpThresholdPercent;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        if (!ctx.Entity.Data.Has(DataKey.HpPercent)) return NodeState.Failure;

        float hpPercent = ctx.Entity.Data.Get<float>(DataKey.HpPercent);
        return hpPercent < _hpThresholdPercent ? NodeState.Success : NodeState.Failure;
    }
}
