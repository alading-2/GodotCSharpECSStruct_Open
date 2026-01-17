/// <summary>
/// 技能类型
/// </summary>
public enum AbilityType
{
    /// <summary>主动技能 - 需要玩家手动触发，可能有充能</summary>
    Active = 0,
    /// <summary>被动技能 - 自动触发，无充能概念</summary>
    Passive = 1,
    /// <summary>武器技能 - 与武器绑定，自动攻击</summary>
    Weapon = 2
}

/// <summary>
/// 技能触发模式 - 核心区分组件
/// </summary>
public enum AbilityTriggerMode
{
    // ============ 主动技能触发 ============
    /// <summary>手动触发 - 需要玩家按键输入</summary>
    Manual = 0,

    // ============ 被动技能触发 ============
    /// <summary>事件触发 - 监听特定事件 (如受击、击杀)</summary>
    OnEvent = 1,
    /// <summary>周期触发 - 固定时间间隔 (如光环每0.5秒)</summary>
    Periodic = 2,
    /// <summary>永久生效 - 无触发概念 (如属性加成)</summary>
    Permanent = 3,

    // ============ 武器技能触发 ============
    /// <summary>自动触发 - 满足条件自动释放 (如自动攻击)</summary>
    Auto = 4
}

/// <summary>
/// 技能目标类型
/// </summary>
public enum AbilityTargetType
{
    /// <summary>自身</summary>
    Self = 0,
    /// <summary>单个敌人 (最近)</summary>
    SingleEnemy = 1,
    /// <summary>所有敌人</summary>
    AllEnemies = 2,
    /// <summary>区域范围</summary>
    AreaOfEffect = 3,
    /// <summary>投射物路径</summary>
    Projectile = 4,
    /// <summary>光标位置</summary>
    Cursor = 5,
    /// <summary>事件来源 (如攻击者)</summary>
    EventSource = 6,
    /// <summary>随机敌人</summary>
    RandomEnemy = 7
}

/// <summary>
/// 技能消耗类型
/// </summary>
public enum AbilityCostType
{
    /// <summary>无消耗</summary>
    None = 0,
    /// <summary>魔法值</summary>
    Mana = 1,
    /// <summary>能量</summary>
    Energy = 2,
    /// <summary>弹药</summary>
    Ammo = 3,
    /// <summary>生命值</summary>
    Health = 4
}
