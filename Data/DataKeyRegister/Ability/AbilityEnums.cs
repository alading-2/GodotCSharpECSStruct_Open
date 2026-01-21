using System;

/// <summary>
/// 技能类型
/// </summary>
public enum AbilityType
{
    /// <summary>主动技能 - 需要玩家手动触发，可能有充能</summary>
    Active = 0,
    /// <summary>被动技能 - 自动触发，无充能概念</summary>
    Passive = 1,
    /// <summary>武器技能 - 自动触发，无充能概念</summary>
    Weapon = 2,
}

/// <summary>
/// 技能触发模式 - [Flags] 位运算，支持多种触发同时生效
/// </summary>
[Flags]
public enum AbilityTriggerMode
{
    None = 0,

    // ============ 主动技能触发 ============
    /// <summary>手动触发 - 需要玩家按键输入施放技能</summary>
    Manual = 1 << 0,

    // ============ 被动技能触发 ============
    /// <summary>事件触发 - 监听特定事件 (如受击、击杀)</summary>
    OnEvent = 1 << 1,
    /// <summary>周期触发 - 固定时间间隔 (如光环每0.5秒)</summary>
    Periodic = 1 << 2,
    /// <summary>永久生效 - 无触发概念 (如属性加成)</summary>
    Permanent = 1 << 3,



    // ============ 组合预设 ============
    /// <summary>事件 + 周期 (如反伤光环)</summary>
    EventAndPeriodic = OnEvent | Periodic,
    /// <summary>手动 + 事件 (如条件主动技能)</summary>
    ManualAndEvent = Manual | OnEvent,
}

/// <summary>
/// 目标选取 - 决定从哪里开始选取目标
/// </summary>
public enum AbilityTargetSelection
{
    /// <summary>无目标（直接使用）</summary>
    None = 0,
    /// <summary>指定目标单位</summary>
    Unit = 1,
    /// <summary>指定地点</summary>
    Point = 2,
    /// <summary>单位/地点</summary>
    UnitOrPoint = 3,
}

/// <summary>
/// 目标几何形状 - 决定影响范围形状
/// </summary>
public enum AbilityTargetGeometry
{
    /// <summary>单体</summary>
    Single = 0,
    /// <summary>圆形 (需要 Range)</summary>
    Circle = 1,
    /// <summary>矩形 (需要 Width, Length)</summary>
    Box = 2,
    /// <summary>线性 (需要 Width, Length)</summary>
    Line = 3,
    /// <summary>扇形 (需要 Range, Angle)</summary>
    Cone = 4,
    /// <summary>链式弹跳 (需要 ChainCount, ChainRange)</summary>
    Chain = 5,
    /// <summary>全屏</summary>
    Global = 6,
}

/// <summary>
/// 目标阵营过滤 - [Flags] 位运算
/// </summary>
[Flags]
public enum AbilityTargetTeamFilter
{
    None = 0,
    /// <summary>友方</summary>
    Friendly = 1 << 0,
    /// <summary>敌方</summary>
    Enemy = 1 << 1,
    /// <summary>中立</summary>
    Neutral = 1 << 2,
    /// <summary>自身</summary>
    Self = 1 << 3,

    // ============ 组合预设 ============
    // 敌人
    AllEnemies = Enemy,
    // 友方和自身
    FriendlyAndSelf = Friendly | Self,
    // 所有
    All = Friendly | Enemy | Neutral | Self,
}

/// <summary>
/// 目标类型过滤 - [Flags] 位运算
/// </summary>
[Flags]
public enum AbilityTargetTypeFilter
{
    None = 0,
    /// <summary>单位 (生物)</summary>
    Unit = 1 << 0,
    /// <summary>投射物 (子弹)</summary>
    Projectile = 1 << 1,
    /// <summary>建筑</summary>
    Structure = 1 << 2,
    /// <summary>物品 (掉落物)</summary>
    Item = 1 << 3,
    /// <summary>技能实体</summary>
    Ability = 1 << 4,
    /// <summary>Buff实体</summary>
    Buff = 1 << 5,


    // ============ 组合预设 ============
    // 所有物理实体
    AllPhysical = Unit | Structure | Item,
    // 所有可被攻击的目标
    AllAttackable = Unit | Structure,
}

/// <summary>
/// 目标排序方式
/// </summary>
public enum AbilityTargetSorting
{
    /// <summary>无排序 (默认)</summary>
    None = 0,
    /// <summary>最近</summary>
    Nearest = 1,
    /// <summary>最远</summary>
    Farthest = 2,
    /// <summary>血量最低</summary>
    LowestHealth = 3,
    /// <summary>血量最高</summary>
    HighestHealth = 4,
    /// <summary>血量百分比最高</summary>
    HighestHealthPercent = 5,
    /// <summary>血量百分比最低</summary>
    LowestHealthPercent = 6,
    /// <summary>随机</summary>
    Random = 7,
    /// <summary>威胁值最高</summary>
    HighestThreat = 8,
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

/// <summary>
/// 技能激活结果
/// </summary>
public enum AbilityActivateResult
{
    /// <summary>成功</summary>
    Success = 0,
    /// <summary>已在执行中</summary>
    FailHasActivated = 1,
    /// <summary>标签条件不满足</summary>
    FailTagRequirement = 2,
    /// <summary>消耗不足</summary>
    FailCost = 3,
    /// <summary>冷却中</summary>
    FailCooldown = 4,
    /// <summary>无充能</summary>
    FailNoCharge = 5,
    /// <summary>无目标</summary>
    FailNoTarget = 6,
}
