using System.Collections.Generic;

/// <summary>
/// 技能配置数据 (纯数据)
/// </summary>
public static class AbilityData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {

        // ================= 烈焰光环 (圆形周期伤害) =================
        ["CircleDamage"] = new()
        {
            { DataKey.Name, "CircleDamage" }, // 技能名称
            { DataKey.Description, "每秒对周围敌人造成伤害" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/fire_aura.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            { DataKey.AbilityMaxLevel, 5 }, // 技能最大等级
            { DataKey.AbilityType, AbilityType.Passive }, // 技能类型
            // 周期触发
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Periodic }, // 技能触发模式
            { DataKey.AbilityCooldown, 1.0f }, // 技能冷却时间
            // { DataKey.AbilityTriggerChance, 50.0f }, // 技能触发概率

            // 目标配置：圆形、敌人
            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Point }, // 目标选取
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Circle }, // 目标几何形状
            { DataKey.AbilityRange, 500f }, // 技能作用范围
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.Enemy }, // 目标队伍过滤器
            
            // 伤害配置
            { DataKey.AbilityDamage, 10.0f }, // 基础伤害，会受FinalSkillDamage影响
        },

        // ================= 源氏-影 (冲刺) =================
        ["Dash"] = new()
        {
            { DataKey.Name, "Dash" }, // 技能名称
            { DataKey.Description, "向前冲刺，消耗充能" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/dash.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            // { DataKey.AbilityMaxLevel, 1 }, // 技能最大等级
            { DataKey.AbilityType, AbilityType.Active }, // 技能类型
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual }, // 技能触发模式
            
            // 充能配置（明确启用充能系统）
            { DataKey.IsAbilityUsesCharges, true }, // 是否使用充能
            { DataKey.AbilityMaxCharges, 3 }, // 最大充能数
            { DataKey.AbilityChargeTime, 5.0f }, // 充能时间
            { DataKey.AbilityCooldown, 0.5f }, // 冲刺之间的最小间隔
            
            // 目标配置
            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Unit }, // 目标选取
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Line }, // 目标几何形状
            { DataKey.AbilityLength, 300f }, // 目标长度
            { DataKey.AbilityWidth, 100f }, // 目标宽度
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.Enemy }, // 目标队伍过滤器
        },

        // ================= 裂地猛击 (普通CD技能) =================
        ["Slam"] = new()
        {
            { DataKey.Name, "Slam" }, // 技能名称
            { DataKey.Description, "猛击地面，造成伤害" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/slam.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            { DataKey.AbilityMaxLevel, 5 }, // 技能最大等级
            { DataKey.AbilityCostType, AbilityCostType.Mana }, // 技能消耗类型
            { DataKey.AbilityCostAmount, 20f }, // 技能消耗数量
            { DataKey.AbilityType, AbilityType.Active }, // 技能类型
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual }, // 技能触发模式

            { DataKey.AbilityCooldown, 8.0f }, // 技能冷却时间

            // 目标配置
            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Unit }, // 目标选取
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Circle }, // 目标几何形状
            { DataKey.AbilityRange, 200f }, // 目标范围
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.Enemy }, // 目标队伍过滤器
        },

        // ================= 生命光环 (被动周期) =================
        ["RegenAura"] = new()
        {
            { DataKey.Name, "RegenAura" }, // 技能名称
            { DataKey.Description, "每秒恢复生命值" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/heal_aura.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            { DataKey.AbilityMaxLevel, 3 }, // 技能最大等级
            { DataKey.AbilityType, AbilityType.Passive }, // 技能类型
            // 周期触发
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Periodic }, // 技能触发模式
            { DataKey.AbilityCooldown, 1.0f }, // 技能冷却时间

            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Unit }, // 目标选取
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Circle }, // 目标几何形状
            { DataKey.AbilityRange, 300f }, // 目标范围
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.FriendlyAndSelf }, // 目标队伍过滤器
        },

        // ================= 复活卷轴 (限制使用次数技能) =================
        // ChargeTime <= 0 表示不自动恢复，只能通过事件/方法恢复充能
        ["ReviveScroll"] = new()
        {
            { DataKey.Name, "ReviveScroll" }, // 技能名称
            { DataKey.Description, "消耗后复活，全局限制次数" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/revive.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            { DataKey.AbilityMaxLevel, 1 }, // 技能最大等级
            { DataKey.AbilityType, AbilityType.Active }, // 技能类型
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual }, // 技能触发模式

            // 限制使用次数配置：ChargeTime = -1 表示不自动恢复
            { DataKey.IsAbilityUsesCharges, true }, // 是否使用充能
            { DataKey.AbilityMaxCharges, 1 }, // 最大充能数
            { DataKey.AbilityChargeTime, -1f }, // 不自动恢复！
            { DataKey.AbilityCooldown, 0f }, // 技能冷却时间

            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Unit }, // 目标选取
        },

        // ================= 链式闪电 (复杂目标选择示例) =================
        ["ChainLightning"] = new()
        {
            { DataKey.Name, "ChainLightning" }, // 技能名称
            { DataKey.Description, "释放链式闪电，弹跳多个目标" }, // 技能描述
            { DataKey.EntityType, EntityType.Ability }, // 技能类型
            { DataKey.AbilityIcon, "res://Assets/Abilities/lightning.png" }, // 技能图标
            { DataKey.AbilityLevel, 1 }, // 技能等级
            { DataKey.AbilityMaxLevel, 5 }, // 技能最大等级
            { DataKey.AbilityCostType, AbilityCostType.Mana }, // 技能消耗类型
            { DataKey.AbilityCostAmount, 50f }, // 技能消耗数量
            { DataKey.AbilityType, AbilityType.Active }, // 技能类型
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual }, // 技能触发模式
            
            // 冷却配置
            { DataKey.AbilityCooldown, 6.0f }, // 技能冷却时间
            
            // 目标配置 - 展示Chain几何形状
            { DataKey.AbilityTargetSelection, AbilityTargetSelection.Unit }, // 目标选取
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Chain }, // 目标几何形状
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.Enemy }, // 目标队伍过滤器
            { DataKey.AbilityTargetSorting, AbilityTargetSorting.Nearest },  // 优先选择最近的目标
            
            // Chain专属参数
            { DataKey.AbilityChainCount, 3 },  // 弹跳3次
            { DataKey.AbilityChainRange, 250f },  // 每次弹跳范围250
            { DataKey.AbilityMaxTargets, 4 },  // 最多命中4个目标(初始+3次弹跳)
            
            // 伤害配置
            { DataKey.AbilityDamage, 50f },  // 基础伤害，会受FinalSkillDamage影响
            { DataKey.AbilityRange, 500f },  // 初始施放范围
        },


    };
}
