/// <summary>
/// 敌人行为树预制树工厂 (Enemy Behavior Tree Builder)
/// <para>
/// 职责：仅负责将 <see cref="EnemyBehaviorBlocks"/> 中的积木块拼装成完整的预制行为树。
/// 不包含任何原子节点逻辑，所有积木块定义在 <see cref="EnemyBehaviorBlocks"/>。
/// </para>
/// <para>
/// 使用方式示例：
/// <code>
/// // 直接使用预制树（最快）
/// var tree = EnemyBehaviorTreeBuilder.BuildMeleeEnemyTree();
/// 
/// // 自由组合积木块（更灵活）
/// var tree = new SelectorNode("自定义")
///     .Add(EnemyBehaviorBlocks.ChaseBranch())
///     .Add(EnemyBehaviorBlocks.PatrolBranch());
/// </code>
/// </para>
/// </summary>
public static class EnemyBehaviorTreeBuilder
{
    // ================= 预制行为树（完整方案） =================

    /// <summary>
    /// 构建标准近战敌人行为树
    /// <para>
    /// 结构：
    /// <code>
    /// Selector(根)
    /// ├─ AttackBranch   → 攻击序列（索敌→范围→冷却→停步→面向→攻击）
    /// ├─ ChaseBranch    → 追逐序列（索敌→追到攻击距离停步）
    /// └─ PatrolBranch   → 巡逻序列（圆环随机选点→等待）
    /// </code>
    /// </para>
    /// </summary>
    public static BehaviorNode BuildMeleeEnemyTree()
    {
        return new SelectorNode("近战敌人")
            .Add(EnemyBehaviorBlocks.AttackBranch())
            .Add(EnemyBehaviorBlocks.ChaseBranch())
            .Add(EnemyBehaviorBlocks.PatrolBranch());
    }

    /// <summary>
    /// 构建带技能的近战敌人行为树（技能优先于普通攻击）
    /// <para>
    /// 结构：SkillBranch → AttackBranch → ChaseBranch → PatrolBranch
    /// </para>
    /// </summary>
    /// <param name="abilityName">自动施放的技能名称</param>
    public static BehaviorNode BuildSkillMeleeTree(string abilityName)
    {
        return new SelectorNode("技能近战敌人")
            .Add(EnemyBehaviorBlocks.SkillBranch(abilityName))
            .Add(EnemyBehaviorBlocks.AttackBranch())
            .Add(EnemyBehaviorBlocks.ChaseBranch())
            .Add(EnemyBehaviorBlocks.PatrolBranch());
    }

    /// <summary>
    /// 构建会逃跑的近战敌人行为树（低血量时逃跑）
    /// <para>
    /// 结构：FleeBranch → AttackBranch → ChaseBranch → PatrolBranch
    /// </para>
    /// </summary>
    /// <param name="hpThreshold">触发逃跑的血量阈值（0~100，默认 30%）</param>
    public static BehaviorNode BuildFleeingMeleeTree(float hpThreshold = 30f)
    {
        return new SelectorNode("逃跑近战敌人")
            .Add(EnemyBehaviorBlocks.FleeBranch(hpThreshold))
            .Add(EnemyBehaviorBlocks.AttackBranch())
            .Add(EnemyBehaviorBlocks.ChaseBranch())
            .Add(EnemyBehaviorBlocks.PatrolBranch());
    }

    /// <summary>
    /// 构建纯巡逻敌人行为树（只会随机游荡，不会攻击和追逐）
    /// </summary>
    public static BehaviorNode BuildWandererTree()
    {
        return EnemyBehaviorBlocks.PatrolBranch();
    }

    /// <summary>
    /// 构建纯追逐敌人行为树（发现目标就追，追到就停，不攻击）
    /// </summary>
    public static BehaviorNode BuildChaserTree()
    {
        return new SelectorNode("追逐敌人")
            .Add(EnemyBehaviorBlocks.ChaseBranch())
            .Add(EnemyBehaviorBlocks.PatrolBranch());
    }
}
