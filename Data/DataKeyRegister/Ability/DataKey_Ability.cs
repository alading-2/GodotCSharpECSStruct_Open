/// <summary>
/// 技能系统相关的 DataKey 定义
/// </summary>
public static partial class DataKey
{
    // ============ 基础信息 ============
    /// <summary>技能描述</summary>
    public const string AbilityDescription = "AbilityDescription";
    /// <summary>技能图标路径</summary>
    public const string AbilityIcon = "AbilityIcon";
    /// <summary>技能类型 (Active/Passive/Weapon)</summary>
    public const string AbilityType = "AbilityType";
    /// <summary>技能等级</summary>
    public const string AbilityLevel = "AbilityLevel";
    /// <summary>技能最大等级</summary>
    public const string AbilityMaxLevel = "AbilityMaxLevel";

    // ============ 冷却系统 ============
    /// <summary>基础冷却时间 (秒)</summary>
    public const string AbilityCooldown = "AbilityCooldown";
    /// <summary>当前剩余冷却时间</summary>
    public const string AbilityCooldownRemaining = "AbilityCooldownRemaining";
    /// <summary>是否正在冷却</summary>
    public const string AbilityIsCoolingDown = "AbilityIsCoolingDown";

    // ============ 充能系统 ============
    /// <summary>最大充能次数</summary>
    public const string AbilityMaxCharges = "AbilityMaxCharges";
    /// <summary>当前充能次数</summary>
    public const string AbilityCurrentCharges = "AbilityCurrentCharges";
    /// <summary>单次充能恢复时间 (秒)</summary>
    public const string AbilityChargeTime = "AbilityChargeTime";
    /// <summary>充能恢复计时器</summary>
    public const string AbilityChargeTimer = "AbilityChargeTimer";

    // ============ 消耗系统 ============
    /// <summary>消耗类型 (None/Mana/Energy/Ammo/Health)</summary>
    public const string AbilityCostType = "AbilityCostType";
    /// <summary>消耗数量</summary>
    public const string AbilityCostAmount = "AbilityCostAmount";

    // ============ 触发配置 ============
    /// <summary>触发模式 (Manual/OnEvent/Periodic/Permanent/Auto)</summary>
    public const string AbilityTriggerMode = "AbilityTriggerMode";
    /// <summary>触发事件类型 (当 TriggerMode=OnEvent)</summary>
    public const string AbilityTriggerEvent = "AbilityTriggerEvent";
    /// <summary>触发间隔 (当 TriggerMode=Periodic)</summary>
    public const string AbilityTriggerInterval = "AbilityTriggerInterval";
    /// <summary>触发概率 (0~1, 可选)</summary>
    public const string AbilityTriggerChance = "AbilityTriggerChance";

    // ============ 目标配置 ============
    /// <summary>目标类型 (Self/SingleEnemy/AllEnemies/...)</summary>
    public const string AbilityTargetType = "AbilityTargetType";
    /// <summary>目标数量上限</summary>
    public const string AbilityMaxTargets = "AbilityMaxTargets";

    // ============ 效果参数 ============
    /// <summary>技能基础伤害</summary>
    public const string AbilityDamage = "AbilityDamage";
    /// <summary>技能伤害倍率 (支持修改器)</summary>
    public const string AbilityDamageMultiplier = "AbilityDamageMultiplier";
    /// <summary>技能治疗量</summary>
    public const string AbilityHealAmount = "AbilityHealAmount";
    /// <summary>技能作用范围</summary>
    public const string AbilityRange = "AbilityRange";
    /// <summary>技能持续时间</summary>
    public const string AbilityDuration = "AbilityDuration";
    /// <summary>投射物速度</summary>
    public const string AbilityProjectileSpeed = "AbilityProjectileSpeed";
    /// <summary>投射物数量</summary>
    public const string AbilityProjectileCount = "AbilityProjectileCount";

    // ============ 召唤效果 ============
    /// <summary>召唤物配置 ID</summary>
    public const string AbilitySummonId = "AbilitySummonId";
    /// <summary>召唤物数量</summary>
    public const string AbilitySummonCount = "AbilitySummonCount";
    /// <summary>召唤物持续时间</summary>
    public const string AbilitySummonDuration = "AbilitySummonDuration";

    // ============ 状态标记 ============
    /// <summary>技能是否已解锁</summary>
    public const string AbilityUnlocked = "AbilityUnlocked";
    /// <summary>技能是否启用</summary>
    public const string AbilityEnabled = "AbilityEnabled";
    /// <summary>技能是否正在执行</summary>
    public const string AbilityIsActive = "AbilityIsActive";
    /// <summary>技能拥有者引用</summary>
    public const string AbilityOwner = "AbilityOwner";
}
