using System.Collections.Generic;

/// <summary>
/// 技能系统相关的 DataKey 定义
/// </summary>
public static partial class DataKey
{
    // ============ 基础信息 ============
    /// <summary>技能图标路径</summary>
    public const string AbilityIcon = "AbilityIcon";
    /// <summary>技能类型 (Active/Passive)</summary>
    public const string AbilityType = "AbilityType";
    /// <summary>技能等级</summary>
    public const string AbilityLevel = "AbilityLevel";
    /// <summary>技能最大等级</summary>
    public const string AbilityMaxLevel = "AbilityMaxLevel";
    /// <summary>技能基础伤害</summary>
    public const string AbilityDamage = "AbilityDamage";

    // ============ 冷却系统 ============
    /// <summary>基础冷却时间 (秒)</summary>
    public const string AbilityCooldown = "AbilityCooldown";

    // ============ 充能系统 ============
    /// <summary>是否使用充能系统 (bool)</summary>
    public const string IsAbilityUsesCharges = "IsAbilityUsesCharges";
    /// <summary>最大充能次数</summary>
    public const string AbilityMaxCharges = "AbilityMaxCharges";
    /// <summary>当前充能次数</summary>
    public const string AbilityCurrentCharges = "AbilityCurrentCharges";
    /// <summary>
    /// 单次充能恢复时间 (秒)。
    /// 特殊值：当 ChargeTime <= 0 时，不启动自动恢复，实现“限制使用次数”技能。
    /// 此类技能只能通过事件 (AddCharge) 或方法 (AddCharges) 恢复充能。
    /// </summary>
    public const string AbilityChargeTime = "AbilityChargeTime";

    // ============ 消耗系统 ============
    /// <summary>消耗类型 (None/Mana/Energy/Ammo/Health)</summary>
    public const string AbilityCostType = "AbilityCostType";
    /// <summary>消耗数量</summary>
    public const string AbilityCostAmount = "AbilityCostAmount";

    // ============ 触发配置 ============
    /// <summary>触发模式 [Flags] (Manual/OnEvent/Periodic/Permanent)</summary>
    public const string AbilityTriggerMode = "AbilityTriggerMode";
    /// <summary>触发事件类型 (当 TriggerMode 包含 OnEvent)</summary>
    public const string AbilityTriggerEvent = "AbilityTriggerEvent";
    /// <summary>触发概率</summary>
    public const string AbilityTriggerChance = "AbilityTriggerChance";

    // ============ 执行模式 ============
    /// <summary>技能执行模式 (Instant/Chain/Channel/Projectile)</summary>
    public const string AbilityExecutionMode = "AbilityExecutionMode";

    // ============ 目标系统 - 5 层分解 ============
    /// <summary>目标选取 (Self/Unit/Point/EventSource/Cursor)</summary>
    public const string AbilityTargetSelection = "AbilityTargetSelection";
    /// <summary>目标几何形状 (Single/Circle/Box/Line/Cone/Global)</summary>
    public const string AbilityTargetGeometry = "AbilityTargetGeometry";
    /// <summary>目标阵营过滤 [Flags] (Friendly/Enemy/Neutral/Self)</summary>
    public const string AbilityTargetTeamFilter = "AbilityTargetTeamFilter";
    /// <summary>目标排序方式 (Nearest/Farthest/LowestHealth/Random)</summary>
    public const string AbilityTargetSorting = "AbilityTargetSorting";
    /// <summary>目标数量上限</summary>
    public const string AbilityMaxTargets = "AbilityMaxTargets";

    // ============ 目标几何参数 ============
    /// <summary>施法距离（技能可释放的最大距离；0=无限制；瞄准射程/索敌半径均用此值）</summary>
    public const string AbilityCastRange = "AbilityCastRange";
    /// <summary>效果半径（圆形/扇形 AOE 的作用半径；冲刺=位移距离；AOE=爆炸范围）</summary>
    public const string AbilityEffectRadius = "AbilityEffectRadius";
    /// <summary>效果长度（矩形/线形 AOE 的长度维度）</summary>
    public const string AbilityEffectLength = "AbilityEffectLength";
    /// <summary>效果宽度（矩形/线形 AOE 的宽度维度）</summary>
    public const string AbilityEffectWidth = "AbilityEffectWidth";
    /// <summary>技能角度（扇形 AOE 的张角，度数）</summary>
    public const string AbilityAngle = "AbilityAngle";
    /// <summary>链式弹跳次数</summary>
    public const string AbilityChainCount = "AbilityChainCount";
    /// <summary>链式弹跳范围</summary>
    public const string AbilityChainRange = "AbilityChainRange";
    /// <summary>链式弹跳延时 (秒)</summary>
    public const string AbilityChainDelay = "AbilityChainDelay";
    /// <summary>链式伤害衰减系数 (0-100)</summary>
    public const string AbilityChainDamageDecay = "AbilityChainDamageDecay";

    // ============ 状态标记 ============
    /// <summary>技能是否已解锁</summary>
    public const string AbilityUnlocked = "AbilityUnlocked";
    /// <summary>技能是否启用</summary>
    public const string AbilityEnabled = "AbilityEnabled";
    /// <summary>技能是否正在执行</summary>
    public const string AbilityIsActive = "AbilityIsActive";

    // ============ 主动技能输入 ============
    /// <summary>当前选中的主动技能索引（存储在拥有者 Data 中）</summary>
    public const string CurrentActiveAbilityIndex = "CurrentActiveAbilityIndex";
}
