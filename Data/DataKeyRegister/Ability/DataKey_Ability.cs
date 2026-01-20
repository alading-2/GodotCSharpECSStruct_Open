using System.Collections.Generic;

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
    /// <summary>技能类型 (Active/Passive)</summary>
    public const string AbilityType = "AbilityType";
    /// <summary>技能等级</summary>
    public const string AbilityLevel = "AbilityLevel";
    /// <summary>技能最大等级</summary>
    public const string AbilityMaxLevel = "AbilityMaxLevel";

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
    /// <summary>充能恢复计时器</summary>
    public const string AbilityChargeTimer = "AbilityChargeTimer";

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
    /// <summary>触发间隔 (当 TriggerMode 包含 Periodic)</summary>
    public const string AbilityTriggerInterval = "AbilityTriggerInterval";
    /// <summary>触发概率 (0~1, 可选)</summary>
    public const string AbilityTriggerChance = "AbilityTriggerChance";

    // ============ 目标系统 - 5 层分解 ============
    /// <summary>目标选取原点 (Self/Unit/Point/EventSource/Cursor)</summary>
    public const string AbilityTargetOrigin = "AbilityTargetOrigin";
    /// <summary>目标几何形状 (Single/Circle/Box/Line/Cone/Chain/Global)</summary>
    public const string AbilityTargetGeometry = "AbilityTargetGeometry";
    /// <summary>目标阵营过滤 [Flags] (Friendly/Enemy/Neutral/Self)</summary>
    public const string AbilityTargetTeamFilter = "AbilityTargetTeamFilter";
    /// <summary>目标类型过滤 [Flags] (Hero/Creep/Boss/...)</summary>
    public const string AbilityTargetTypeFilter = "AbilityTargetTypeFilter";
    /// <summary>目标排序方式 (Nearest/Farthest/LowestHealth/Random)</summary>
    public const string AbilityTargetSorting = "AbilityTargetSorting";
    /// <summary>目标数量上限</summary>
    public const string AbilityMaxTargets = "AbilityMaxTargets";

    // ============ 目标几何参数 ============
    /// <summary>技能作用范围 (圆形/扇形半径)</summary>
    public const string AbilityRange = "AbilityRange";
    /// <summary>技能宽度 (矩形/线性)</summary>
    public const string AbilityWidth = "AbilityWidth";
    /// <summary>技能长度 (矩形/线性)</summary>
    public const string AbilityLength = "AbilityLength";
    /// <summary>技能角度 (扇形)</summary>
    public const string AbilityAngle = "AbilityAngle";
    /// <summary>链式弹跳次数</summary>
    public const string AbilityChainCount = "AbilityChainCount";
    /// <summary>链式弹跳范围</summary>
    public const string AbilityChainRange = "AbilityChainRange";

    // ============ 标签系统 ============
    /// <summary>技能自身标签 List&lt;string&gt;</summary>
    public const string AbilityAssetTags = "AbilityAssetTags";
    /// <summary>激活所需标签 List&lt;string&gt;</summary>
    public const string AbilityActivationRequiredTags = "AbilityActivationRequiredTags";
    /// <summary>激活阻止标签 List&lt;string&gt;</summary>
    public const string AbilityActivationBlockedTags = "AbilityActivationBlockedTags";
    /// <summary>阻止其他技能标签 List&lt;string&gt;</summary>
    public const string AbilityBlockAbilitiesWithTags = "AbilityBlockAbilitiesWithTags";
    /// <summary>取消其他技能标签 List&lt;string&gt;</summary>
    public const string AbilityCancelAbilitiesWithTags = "AbilityCancelAbilitiesWithTags";

    // ============ 效果参数 ============
    /// <summary>技能基础伤害</summary>
    public const string AbilityDamage = "AbilityDamage";
    /// <summary>技能持续时间</summary>
    public const string AbilityDuration = "AbilityDuration";

    // ============ 状态标记 ============
    /// <summary>技能是否已解锁</summary>
    public const string AbilityUnlocked = "AbilityUnlocked";
    /// <summary>技能是否启用</summary>
    public const string AbilityEnabled = "AbilityEnabled";
    /// <summary>技能是否正在执行</summary>
    public const string AbilityIsActive = "AbilityIsActive";
}
