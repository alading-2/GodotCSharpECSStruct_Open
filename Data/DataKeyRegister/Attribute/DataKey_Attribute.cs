/// <summary>
/// 数据键定义 - 类型安全的数据访问
/// 使用常量而非枚举，支持 Mod 扩展
/// </summary>
public static partial class DataKey
{
    // ========== 生命相关 ==========
    // 生命值
    public const string BaseHp = "BaseHp"; // 基础生命值
    public const string HpBonus = "HpBonus"; // 生命加成（百分比）
    public const string FinalHp = "FinalHp"; // 最终生命值（计算属性）,即最大生命值
    public const string CurrentHp = "CurrentHp"; // 当前生命值
    public const string HpPercent = "HpPercent"; // 生命值百分比（计算属性）

    // 生命恢复
    public const string BaseHpRegen = "BaseHpRegen"; // 基础生命恢复
    public const string HpRegenBonus = "HpRegenBonus"; // 生命恢复加成（百分比）
    public const string PercentHpRegen = "PercentHpRegen"; // 百分比生命恢复
    public const string FinalHpRegen = "FinalHpRegen"; // 最终生命恢复（计算属性）

    // 吸血
    public const string LifeSteal = "LifeSteal"; // 物理吸血百分比
    // ========== 魔法相关 ==========
    // 魔法值
    public const string BaseMana = "BaseMana"; // 基础魔法值
    public const string ManaBonus = "ManaBonus"; // 魔法加成（百分比）
    public const string FinalMana = "FinalMana"; // 最终魔法值（计算属性）
    public const string CurrentMana = "CurrentMana"; // 当前法力值
    public const string ManaPercent = "ManaPercent"; // 魔法值百分比（计算属性）
    // 魔法恢复
    public const string BaseManaRegen = "BaseManaRegen"; // 基础魔法恢复
    public const string ManaRegenBonus = "ManaRegenBonus"; // 魔法恢复加成（百分比）
    public const string PercentManaRegen = "PercentManaRegen"; // 百分比魔法恢复
    public const string FinalManaRegen = "FinalManaRegen"; // 最终魔法恢复（计算属性）

    // ========== 攻击相关 ==========
    // 攻击力
    public const string BaseAttack = "BaseAttack"; // 基础攻击力
    public const string AttackBonus = "AttackBonus"; // 攻击力加成（百分比）
    public const string FinalAttack = "FinalAttack"; // 最终攻击力（计算属性）

    // 攻击速度
    public const string BaseAttackSpeed = "BaseAttackSpeed"; // 基础攻速
    public const string AttackSpeedBonus = "AttackSpeedBonus"; // 攻速加成（百分比）
    public const string FinalAttackSpeed = "FinalAttackSpeed"; // 最终攻速（计算属性）
    public const string AttackInterval = "AttackInterval"; // 攻击间隔（计算属性）

    // 伤害增幅
    public const string DamageAmplification = "DamageAmplification"; // 伤害增幅（百分比）

    // 穿透
    public const string Penetration = "Penetration"; // 物理穿透

    // 攻击阶段
    public const string AttackWindUpTime = "AttackWindUpTime"; // 攻击前摇时长（秒，默认0=即时判定）
    public const string AttackRecoveryTime = "AttackRecoveryTime"; // 攻击后摇时长（秒，默认0=无后摇）

    // 其他攻击属性
    public const string AttackRange = "AttackRange"; // 攻击范围


    public const string Knockback = "Knockback"; // 击退力度

    // ========== 防御相关 ==========
    // 防御力
    public const string BaseDefense = "BaseDefense"; // 基础防御
    public const string DefenseBonus = "DefenseBonus"; // 防御加成（百分比）
    public const string FinalDefense = "FinalDefense"; // 最终防御（计算属性）

    // 魔法抗性
    // public const string BaseMagicResist = "BaseMagicResist"; // 基础魔抗
    // public const string MagicResistBonus = "MagicResistBonus"; // 魔抗加成（百分比）
    // public const string FinalMagicResist = "FinalMagicResist"; // 最终魔抗（计算属性）

    // 伤害减免
    public const string DamageReduction = "DamageReduction"; // 伤害减免（百分比）

    public const string Shield = "Shield"; // 护盾
    public const string Thorns = "Thorns"; // 反伤
    public const string DamageTakenMultiplier = "DamageTakenMultiplier"; // 受到伤害倍率
    public const string Armor = "Armor"; // 护甲（兼容旧系统）
    public const string InvincibilityTime = "InvincibilityTime"; // 无敌时间

    // ========== 技能相关 ==========
    public const string BaseSkillDamage = "BaseSkillDamage"; // 基础技能伤害（百分比）
    public const string SkillDamageBonus = "SkillDamageBonus"; // 技能伤害加成（百分比）
    public const string FinalSkillDamage = "FinalSkillDamage"; // 最终技能伤害（计算属性）
    public const string CooldownReduction = "CooldownReduction"; // 技能冷却缩减（百分比）

    // ========== 移动相关 ==========
    public const string MoveSpeed = "MoveSpeed"; // 移动速度
    public const string MoveSpeedBonus = "MoveSpeedBonus"; // 移动速度加成（百分比）
    public const string FinalMoveSpeed = "FinalMoveSpeed"; // 最终移动速度（计算属性）

    // ========== 闪避相关 ==========
    // 闪避率
    public const string DodgeChance = "DodgeChance"; // 闪避率

    // ========== 暴击相关 ==========
    // 暴击率
    public const string CritRate = "CritRate"; // 物理暴击率（百分比）

    // 暴击伤害
    public const string CritDamage = "CritDamage"; // 物理暴击伤害（百分比）

    // ========== 资源系统 ==========
    public const string PickupRange = "PickupRange"; // 拾取范围
    public const string ExpGain = "ExpGain"; // 经验倍率
    public const string LuckBonus = "LuckBonus"; // 幸运值
    public const string MagnetSpeed = "MagnetSpeed"; // 磁铁吸附速度
    public const string MagnetEnabled = "MagnetEnabled"; // 磁铁开关

    // ========== 计算数据（只读） ==========
    public const string EffectiveHp = "EffectiveHp"; // 有效生命
    public const string DPS = "DPS"; // 每秒伤害
}
