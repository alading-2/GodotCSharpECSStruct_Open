/// <summary>
/// 技能检查阶段（决定 CheckCanUse 事件的执行顺序）
/// </summary>
public enum AbilityCheckPhase
{
    /// <summary>
    /// 初始化/最优先检查 (如：技能是否启用)
    /// </summary>
    Init = 2000,

    /// <summary>
    /// 冷却检查 (失败代价小，优先检查)
    /// </summary>
    Cooldown = 1500,

    /// <summary>
    /// 成本检查 (消耗类，如魔法、耐力)
    /// </summary>
    Cost = 1000,

    /// <summary>
    /// 目标有效性检查 (涉及空间查询，开销较大，放在后面)
    /// </summary>
    TargetValidity = 500,

    /// <summary>
    /// 其他自定义条件
    /// </summary>
    Custom = 0
}
