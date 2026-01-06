/// <summary>
/// 数据键定义 - 类型安全的数据访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static class DataKey
{
    // === 基础信息 ===
    public const string Name = "Name";
    public const string Level = "Level";

    // === 生命系统 ===
    public const string MaxHp = "MaxHp";
    public const string CurrentHp = "CurrentHp";
    public const string HpRegen = "HpRegen";
    public const string LifeSteal = "LifeSteal";
    public const string Armor = "Armor";
    public const string InvincibilityTime = "InvincibilityTime";

    // === 攻击系统 ===
    public const string Damage = "Damage";
    public const string AttackSpeed = "AttackSpeed";
    public const string CritChance = "CritChance";
    public const string CritDamage = "CritDamage";
    public const string Range = "Range";
    public const string Knockback = "Knockback";

    // === 防御系统 ===
    public const string DodgeChance = "DodgeChance";
    public const string DamageReduction = "DamageReduction";
    public const string Thorns = "Thorns";
    public const string Shield = "Shield"; // 护盾
    public const string MagicResist = "MagicResist"; // 魔抗 (预留)
    public const string DamageTakenMultiplier = "DamageTakenMultiplier"; // 易伤/减伤乘区 (默认 1.0)

    // === 移动系统 ===
    public const string Speed = "Speed";
    public const string MaxSpeed = "MaxSpeed";
    public const string Acceleration = "Acceleration";
    public const string FollowSpeed = "FollowSpeed";
    public const string StopDistance = "StopDistance";

    // === 资源系统 ===
    public const string PickupRange = "PickupRange";
    public const string ExpGain = "ExpGain";
    public const string LuckBonus = "LuckBonus";
    public const string MagnetSpeed = "MagnetSpeed";
    public const string MagnetEnabled = "MagnetEnabled";

    // === 特殊机制 ===
    public const string Pierce = "Pierce";
    public const string ProjectileCount = "ProjectileCount";
    public const string AreaSize = "AreaSize";

    // === 计算数据（只读） ===
    public const string AttackInterval = "AttackInterval";
    public const string EffectiveHp = "EffectiveHp";
    public const string DPS = "DPS";
}
