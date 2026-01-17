using System.Runtime.CompilerServices;
using Godot;
/// <summary>
/// 技能系统 DataKey 元数据注册
/// </summary>
public partial class DataRegister_Ability : Node
{
    private static readonly Log _log = new Log("DataRegister_Ability");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DataRegister_Ability",
            Path = "res://Data/DataKeyRegister/Ability/DataRegister_Ability.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("DataRegister_Ability注册技能数据...");
        // ============ 基础信息 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityDescription, DisplayName = "技能描述", Category = DataCategory_Ability.Basic, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityIcon, DisplayName = "技能图标", Category = DataCategory_Ability.Basic, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityType, DisplayName = "技能类型", Category = DataCategory_Ability.Basic, Type = typeof(int), DefaultValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityLevel, DisplayName = "技能等级", Category = DataCategory_Ability.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = 10 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxLevel, DisplayName = "最大等级", Category = DataCategory_Ability.Basic, Type = typeof(int), DefaultValue = 5, MinValue = 1, MaxValue = 10 });

        // ============ 冷却系统 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCooldown, DisplayName = "冷却时间", Category = DataCategory_Ability.Cooldown, Type = typeof(float), DefaultValue = 1.0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCooldownRemaining, DisplayName = "剩余冷却", Category = DataCategory_Ability.Cooldown, Type = typeof(float), DefaultValue = 0f });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityIsCoolingDown, DisplayName = "冷却中", Category = DataCategory_Ability.Cooldown, Type = typeof(bool), DefaultValue = false });

        // ============ 充能系统 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxCharges, DisplayName = "最大充能", Category = DataCategory_Ability.Charge, Type = typeof(int), DefaultValue = 1, MinValue = 1 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCurrentCharges, DisplayName = "当前充能", Category = DataCategory_Ability.Charge, Type = typeof(int), DefaultValue = 1, MinValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityChargeTime, DisplayName = "充能时间", Category = DataCategory_Ability.Charge, Type = typeof(float), DefaultValue = 5.0f, MinValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityChargeTimer, DisplayName = "充能计时", Category = DataCategory_Ability.Charge, Type = typeof(float), DefaultValue = 0f });

        // ============ 消耗系统 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCostType, DisplayName = "消耗类型", Category = DataCategory_Ability.Cost, Type = typeof(int), DefaultValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCostAmount, DisplayName = "消耗数量", Category = DataCategory_Ability.Cost, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

        // ============ 触发配置 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerMode, DisplayName = "触发模式", Category = DataCategory_Ability.Trigger, Type = typeof(int), DefaultValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerEvent, DisplayName = "触发事件", Category = DataCategory_Ability.Trigger, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerInterval, DisplayName = "触发间隔", Category = DataCategory_Ability.Trigger, Type = typeof(float), DefaultValue = 1.0f, MinValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerChance, DisplayName = "触发概率", Category = DataCategory_Ability.Trigger, Type = typeof(float), DefaultValue = 1.0f, MinValue = 0, MaxValue = 1, IsPercentage = true });

        // ============ 目标配置 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTargetType, DisplayName = "目标类型", Category = DataCategory_Ability.Target, Type = typeof(int), DefaultValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxTargets, DisplayName = "最大目标数", Category = DataCategory_Ability.Target, Type = typeof(int), DefaultValue = 1, MinValue = 1 });

        // ============ 效果参数 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityDamage, DisplayName = "技能伤害", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityDamageMultiplier, DisplayName = "伤害倍率", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 1.0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityHealAmount, DisplayName = "治疗量", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityRange, DisplayName = "技能范围", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 100f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityDuration, DisplayName = "持续时间", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityProjectileSpeed, DisplayName = "投射物速度", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 500f, MinValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityProjectileCount, DisplayName = "投射物数量", Category = DataCategory_Ability.Effect, Type = typeof(int), DefaultValue = 1, MinValue = 1 });

        // ============ 召唤效果 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilitySummonId, DisplayName = "召唤物ID", Category = DataCategory_Ability.Summon, Type = typeof(string), DefaultValue = "" });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilitySummonCount, DisplayName = "召唤数量", Category = DataCategory_Ability.Summon, Type = typeof(int), DefaultValue = 1, MinValue = 1 });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilitySummonDuration, DisplayName = "召唤持续时间", Category = DataCategory_Ability.Summon, Type = typeof(float), DefaultValue = 10f, MinValue = 0 });

        // ============ 状态标记 ============
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityUnlocked, DisplayName = "已解锁", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityEnabled, DisplayName = "已启用", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityIsActive, DisplayName = "执行中", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = false });
    }
}
