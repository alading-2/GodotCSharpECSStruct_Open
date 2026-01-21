using System.Collections.Generic;

/// <summary>
/// 技能配置数据 (纯数据)
/// </summary>
public static class AbilityData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {
        // ================= 源氏-影 (冲刺) =================
        ["Dash"] = new()
        {
            { DataKey.Name, "Dash" },
            { DataKey.Description, "向前冲刺，消耗充能" },
            { DataKey.AbilityType, AbilityType.Active },
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual },
            
            // 充能配置（明确启用充能系统）
            { DataKey.IsAbilityUsesCharges, true },
            { DataKey.AbilityMaxCharges, 3 },
            { DataKey.AbilityChargeTime, 5.0f },
            { DataKey.AbilityCooldown, 0.5f }, // 冲刺之间的最小间隔
            
            // 目标配置
            { DataKey.AbilityTargetOrigin, AbilityTargetSelection.Unit },
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Line },
            { DataKey.AbilityRange, 300f },

            { DataKey.AbilityEnabled, true },
        },

        // ================= 裂地猛击 (普通CD技能) =================
        ["Slam"] = new()
        {
            { DataKey.Name, "Slam" },
            { DataKey.Description, "猛击地面，造成伤害" },
            { DataKey.AbilityType, AbilityType.Active },
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual },

            // 不使用充能，使用传统冷却
            { DataKey.IsAbilityUsesCharges, false },
            { DataKey.AbilityCooldown, 8.0f },

            // 目标配置
            { DataKey.AbilityTargetOrigin, AbilityTargetSelection.Unit },
            { DataKey.AbilityTargetGeometry, AbilityTargetGeometry.Circle },
            { DataKey.AbilityRange, 200f },
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.AllEnemies },

            { DataKey.AbilityEnabled, true },
        },

        // ================= 生命光环 (被动周期) =================
        ["RegenAura"] = new()
        {
            { DataKey.Name, "RegenAura" },
            { DataKey.Description, "每秒恢复生命值" },
            { DataKey.AbilityType, AbilityType.Passive },
            // 周期触发
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Periodic },
            { DataKey.AbilityTriggerInterval, 1.0f },

            { DataKey.AbilityTargetOrigin, AbilityTargetSelection.Unit },
            { DataKey.AbilityTargetTeamFilter, AbilityTargetTeamFilter.FriendlyAndSelf },

            { DataKey.AbilityEnabled, true },
        },

        // ================= 复活卷轴 (限制使用次数技能) =================
        // ChargeTime <= 0 表示不自动恢复，只能通过事件/方法恢复充能
        ["ReviveScroll"] = new()
        {
            { DataKey.Name, "ReviveScroll" },
            { DataKey.Description, "消耗后复活，全局限制次数" },
            { DataKey.AbilityType, AbilityType.Active },
            { DataKey.AbilityTriggerMode, AbilityTriggerMode.Manual },

            // 限制使用次数配置：ChargeTime = -1 表示不自动恢复
            { DataKey.IsAbilityUsesCharges, true },
            { DataKey.AbilityMaxCharges, 1 },
            { DataKey.AbilityChargeTime, -1f }, // 不自动恢复！
            { DataKey.AbilityCooldown, 0f },

            { DataKey.AbilityTargetOrigin, AbilityTargetSelection.Unit },
            { DataKey.AbilityEnabled, true },
        }
    };
}
