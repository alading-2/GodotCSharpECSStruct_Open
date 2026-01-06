using System;

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Physical, // 物理
    Magical,  // 魔法
    True      // 真实（无视护甲）
}

/// <summary>
/// 伤害标签（位掩码），用于标记伤害属性
/// </summary>
[Flags]
public enum DamageTags
{
    None = 0,
    Melee = 1 << 0,       // 近战
    Ranged = 1 << 1,      // 远程（投射物）
    Area = 1 << 2,        // 范围伤害 (AOE)
    Persistent = 1 << 3,  // 持续伤害 (DOT)
    Explosion = 1 << 4,   // 爆炸
    Engineering = 1 << 5  // 工程学
}
