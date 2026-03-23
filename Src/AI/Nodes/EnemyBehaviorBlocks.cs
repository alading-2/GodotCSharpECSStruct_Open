/// <summary>
/// 可复用行为树积木块库 (Enemy Behavior Blocks)
/// <para>
/// 提供各种标准敌人行为的可复用子树（积木块）。
/// 每个方法返回一个完整的 <see cref="SequenceNode"/>，可直接添加到任意 <see cref="SelectorNode"/>。
/// </para>
/// <para>
/// 使用方式示例：
/// <code>
/// var tree = new SelectorNode("Boss")
///     .Add(EnemyBehaviorBlocks.FleeBranch(20f))
///     .Add(EnemyBehaviorBlocks.SkillBranch("终极技能"))
///     .Add(EnemyBehaviorBlocks.AttackBranch())
///     .Add(EnemyBehaviorBlocks.ChaseBranch())
///     .Add(EnemyBehaviorBlocks.PatrolBranch());
/// </code>
/// </para>
/// <para>
/// 运行机制：SelectorNode 每帧从头遍历积木块，找到第一个不返回 Failure 的分支执行。
/// SequenceNode 内部按序执行，任一步 Failure 整条链失败，Running 则下帧从该步继续。
/// </para>
/// </summary>
public static class EnemyBehaviorBlocks
{
    /// <summary>
    /// 攻击积木块：索敌 → 校验目标 → 范围检测 → 冷却检测 → 停步 → 面向 → 普通攻击
    /// <para>
    /// 任一前置条件失败（无目标/超出范围/冷却中），整个积木块返回 Failure，
    /// 上层 Selector 自动切到优先级更低的分支（通常是追逐）。
    /// </para>
    /// </summary>
    public static BehaviorNode AttackBranch()
    {
        return new SequenceNode("攻击序列")
            .Add(new FindEnemyAction()) //搜索敌人
            .Add(new HasValidTargetCondition()) //校验目标是否有效
            .Add(new IsInRangeCondition(DataKey.AttackRange)) //范围检测
            .Add(new IsAttackReadyCondition()) //攻击间隔检测
            .Add(new StopMovementAction()) //停止移动
            .Add(new FaceTargetAction()) //面向目标entity
            .Add(new RequestAttackAction()); //攻击请求
    }

    /// <summary>
    /// 技能攻击积木块：索敌 → 校验目标 → 技能范围 → 技能就绪 → 停步 → 面向 → 施法
    /// <para>
    /// 优先级通常高于 AttackBranch()，放在 Selector 更靠前的位置。
    /// </para>
    /// </summary>
    /// <param name="abilityName">技能名称（需与 EntityManager.AddAbility 中的名称一致）</param>
    /// <param name="rangeKey">技能射程 DataKey（默认 AttackRange，可改为 AbilityRange 等）</param>
    public static BehaviorNode SkillBranch(string abilityName, string rangeKey = nameof(DataKey.AttackRange))
    {
        return new SequenceNode("技能序列")
            .Add(new FindEnemyAction()) //搜索敌人
            .Add(new HasValidTargetCondition()) //校验目标是否有效
            .Add(new IsInRangeCondition(rangeKey)) //范围检测
            .Add(new IsAbilityReadyCondition(abilityName)) //技能就绪检测
            .Add(new StopMovementAction()) //停止移动
            .Add(new FaceTargetAction()) //面向目标entity
            .Add(new AutoCastAbilityAction(abilityName)); //自动施法
    }

    /// <summary>
    /// 逃跑积木块：低血量检测 → 索敌（确认威胁来源）→ 反向逃跑
    /// <para>
    /// 通常放在 Selector 最高优先级（第一位），低血时无视一切其他行为。
    /// </para>
    /// </summary>
    /// <param name="hpThreshold">触发逃跑的血量阈值（0~100，默认 30%）</param>
    public static BehaviorNode FleeBranch(float hpThreshold = 30f)
    {
        return new SequenceNode("逃跑序列")
            .Add(new IsLowHpCondition(hpThreshold)) //低血量检测
            .Add(new FindEnemyAction()) //搜索敌人
            .Add(new HasValidTargetCondition()) //校验目标是否有效
            .Add(new FleeFromTargetAction()); //反向逃跑
    }

    /// <summary>
    /// 追逐积木块：索敌 → 校验目标 → 向目标移动（到攻击距离自动停步）
    /// <para>
    /// 追到攻击范围内就返回 Success，上层 Selector 下帧会优先检查攻击分支。
    /// </para>
    /// </summary>
    public static BehaviorNode ChaseBranch()
    {
        return new SequenceNode("追逐序列")
            .Add(new FindEnemyAction()) //搜索敌人
            .Add(new HasValidTargetCondition()) //校验目标是否有效
            .Add(new MoveToTargetAction(DataKey.AttackRange)); //移动到目标位置
    }

    /// <summary>
    /// 巡逻积木块：圆环随机选点移动 → 到点后等待
    /// <para>
    /// 通常放在 Selector 最低优先级（最后一位），无目标时兜底行为。
    /// RandomWanderAction 以当前位置为圆心选点，被打断后 Reset 会清除旧目标点，
    /// 恢复时从当前位置重新选点。
    /// </para>
    /// </summary>
    public static BehaviorNode PatrolBranch()
    {
        return new SequenceNode("巡逻序列")
            .Add(new RandomWanderAction()) //随机移动
            .Add(new WaitIdleAction()); //等待
    }
}
