using System.Collections.Generic;
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
        // 技能图标
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityIcon, DisplayName = "技能图标", Category = DataCategory_Ability.Basic, Type = typeof(string), DefaultValue = "" });
        // 技能类型
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityType, DisplayName = "技能类型", Category = DataCategory_Ability.Basic, Type = typeof(AbilityType), DefaultValue = AbilityType.Passive });
        // 技能等级
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityLevel, DisplayName = "技能等级", Category = DataCategory_Ability.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1 });
        // 最大等级
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxLevel, DisplayName = "最大等级", Category = DataCategory_Ability.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1 });
        // 技能伤害
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityDamage, DisplayName = "技能伤害", Category = DataCategory_Ability.Effect, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

        // ============ 冷却系统 ============
        // 冷却时间
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCooldown, DisplayName = "冷却时间", Category = DataCategory_Ability.Cooldown, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

        // ============ 充能系统 ============
        // 是否使用充能
        DataRegistry.Register(new DataMeta { Key = DataKey.IsAbilityUsesCharges, DisplayName = "是否使用充能", Category = DataCategory_Ability.Charge, Type = typeof(bool), DefaultValue = false });
        // 最大充能次数
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxCharges, DisplayName = "最大充能", Category = DataCategory_Ability.Charge, Type = typeof(int), DefaultValue = 0, MinValue = 0 });
        // 当前充能次数
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCurrentCharges, DisplayName = "当前充能", Category = DataCategory_Ability.Charge, Type = typeof(int), DefaultValue = 0, MinValue = 0 });
        // 充能时间
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityChargeTime, DisplayName = "充能时间", Category = DataCategory_Ability.Charge, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

        // ============ 消耗系统 ============
        // 消耗类型
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCostType, DisplayName = "消耗类型", Category = DataCategory_Ability.Cost, Type = typeof(AbilityCostType), DefaultValue = AbilityCostType.None });
        // 消耗数量
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityCostAmount, DisplayName = "消耗数量", Category = DataCategory_Ability.Cost, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

        // ============ 触发配置 ============
        // 触发模式
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerMode, DisplayName = "触发模式", Category = DataCategory_Ability.Trigger, Type = typeof(AbilityTriggerMode), DefaultValue = AbilityTriggerMode.None });
        // 触发事件
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerEvent, DisplayName = "触发事件", Category = DataCategory_Ability.Trigger, Type = typeof(List<string>), DefaultValue = new List<string>() });
        // 触发概率
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTriggerChance, DisplayName = "触发概率", Category = DataCategory_Ability.Trigger, Type = typeof(float), DefaultValue = 0f, MinValue = 0f, MaxValue = 100f, IsPercentage = true });

        // ============ 目标系统 - 5 层分解 ============
        // 目标选取
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTargetSelection, DisplayName = "目标原点", Category = DataCategory_Ability.Target, Type = typeof(AbilityTargetSelection), DefaultValue = AbilityTargetSelection.None });
        // 目标几何形状
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTargetGeometry, DisplayName = "目标几何形状", Category = DataCategory_Ability.Target, Type = typeof(AbilityTargetGeometry), DefaultValue = AbilityTargetGeometry.Single });
        // 阵营过滤
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTargetTeamFilter, DisplayName = "阵营过滤", Category = DataCategory_Ability.Target, Type = typeof(AbilityTargetTeamFilter), DefaultValue = AbilityTargetTeamFilter.None });
        // 目标排序
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityTargetSorting, DisplayName = "目标排序", Category = DataCategory_Ability.Target, Type = typeof(AbilityTargetSorting), DefaultValue = AbilityTargetSorting.None });
        // 最大目标
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityMaxTargets, DisplayName = "最大目标", Category = DataCategory_Ability.Target, Type = typeof(int), DefaultValue = 1, MinValue = 1 });

        // ============ 目标几何参数 ============
        // 技能范围
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityRange, DisplayName = "技能范围", Category = DataCategory_Ability.Target, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        // 技能宽度
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityWidth, DisplayName = "技能宽度", Category = DataCategory_Ability.Target, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });
        // 技能长度
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityLength, DisplayName = "技能长度", Category = DataCategory_Ability.Target, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });
        // 技能角度
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityAngle, DisplayName = "技能角度", Category = DataCategory_Ability.Target, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = 360 });
        // 弹跳次数
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityChainCount, DisplayName = "弹跳次数", Category = DataCategory_Ability.Target, Type = typeof(int), DefaultValue = 0, MinValue = 0 });
        // 弹跳范围
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityChainRange, DisplayName = "弹跳范围", Category = DataCategory_Ability.Target, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

        // ============ 状态标记 ============
        // 已解锁
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityUnlocked, DisplayName = "已解锁", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = true });
        // 已启用
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityEnabled, DisplayName = "已启用", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = true });
        // 执行中
        DataRegistry.Register(new DataMeta { Key = DataKey.AbilityIsActive, DisplayName = "执行中", Category = DataCategory_Ability.State, Type = typeof(bool), DefaultValue = false });
    }
}
