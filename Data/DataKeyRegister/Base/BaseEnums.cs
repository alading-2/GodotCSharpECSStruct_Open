/// <summary>
/// 阵营枚举
/// </summary>
public enum Team
{
    /// <summary>中立</summary>
    Neutral = 0,
    /// <summary>玩家</summary>
    Player = 1,
    /// <summary>敌人</summary>
    Enemy = 2,
}

/// <summary>
/// 实体物理/技术类型
/// </summary>
public enum EntityType
{
    /// <summary>空</summary>
    None = 0,
    /// <summary>单位 (生物)</summary>
    Unit = 1,
    /// <summary>投射物 (子弹)</summary>
    Projectile = 2,
    /// <summary>建筑</summary>
    Structure = 4,
    /// <summary>物品 (掉落物)</summary>
    Item = 8,
    /// <summary>技能实体</summary>
    Ability = 16,
    /// <summary>Buff实体</summary>
    Buff = 32,
    /// <summary>陷阱</summary>
    Trap = 64,
}
