/// <summary>
/// 条件节点：检查指定技能是否可以施放（冷却/充能就绪）
/// <para>
/// 通过技能名称从拥有者的技能列表中查找对应技能，
/// 并检查其冷却 (AbilityCooldown) 和充能 (AbilityCurrentCharges) 状态。
/// </para>
/// <para>
/// 返回值：
/// - Success：技能存在且可以施放
/// - Failure：技能不存在、正在冷却或充能不足
/// </para>
/// </summary>
public class IsAbilityReadyCondition : BehaviorNode
{
    private readonly string _abilityName;

    /// <summary>
    /// 创建技能就绪检测条件节点
    /// </summary>
    /// <param name="abilityName">目标技能名称（与技能配置中的 Name 对应）</param>
    public IsAbilityReadyCondition(string abilityName) : base($"技能就绪({abilityName})")
    {
        _abilityName = abilityName;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var ability = EntityManager.GetAbilityByName(ctx.Entity, _abilityName);
        if (ability == null) return NodeState.Failure;

        if (!ability.Data.Get<bool>(DataKey.FeatureEnabled)) return NodeState.Failure;

        // 充能模式：有充能次数即可施放
        bool usesCharges = ability.Data.Get<bool>(DataKey.IsAbilityUsesCharges);
        if (usesCharges)
        {
            int charges = ability.Data.Get<int>(DataKey.AbilityCurrentCharges);
            return charges > 0 ? NodeState.Success : NodeState.Failure;
        }

        // 冷却模式：IsAbilityActive 为 false 表示冷却完毕
        bool isActive = ability.Data.Get<bool>(DataKey.FeatureIsActive);
        return !isActive ? NodeState.Success : NodeState.Failure;
    }
}
