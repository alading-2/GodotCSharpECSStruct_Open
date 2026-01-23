using System.Collections.Generic;

/// <summary>
/// 玩家数据（纯数据，无逻辑）
/// </summary>
public static class PlayerData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {
        ["Player1"] = new()
        {
            // === 基础信息 ===
            { DataKey.Name, "Player1" }, // Name
            { DataKey.Team, Team.Player }, // 阵营
            { DataKey.EntityType, EntityType.Unit }, // 实体类型
            { DataKey.VisualScenePath, "res://assets/Unit/Player/德鲁伊/AnimatedSprite2D/AnimatedSprite2D.tscn" }, // 视觉场景路径
            { DataKey.HealthBarHeight, 60f }, // 血条高度（Y轴偏移）
            
            // === 生命属性 ===
            { DataKey.BaseHp, 100f }, // 基础生命值
            { DataKey.CurrentHp, 100f }, // 当前生命值
            { DataKey.BaseHpRegen, 1f }, // 每秒恢复1点生命
            { DataKey.LifeSteal, 0f }, // 初始无吸血
            
            // === 魔法属性 ===
            { DataKey.BaseMana, 50f }, // 基础魔法值
            { DataKey.CurrentMana, 50f }, // 当前魔法值
            { DataKey.BaseManaRegen, 2f }, // 每秒恢复2点魔法
            
            // === 攻击属性 ===
            { DataKey.BaseAttack, 10f }, // 基础攻击力
            { DataKey.BaseAttackSpeed, 100f }, // 基础攻速
            { DataKey.Range, 150f }, // 攻击范围
            { DataKey.CritRate, 5f }, // 5%暴击率
            { DataKey.CritDamage, 150f }, // 暴击伤害150%
            { DataKey.Penetration, 0f }, // 护甲穿透
            
            // === 防御属性 ===
            { DataKey.BaseDefense, 5f }, // 基础防御值
            { DataKey.DamageReduction, 0f }, // 伤害减免百分比
            
            // === 技能属性 ===
            { DataKey.BaseSkillDamage, 100f }, // 技能伤害100%
            { DataKey.CooldownReduction, 0f }, // 冷却缩减
            
            // === 移动属性 ===
            { DataKey.MoveSpeed, 200f }, // 移动速度
            
            // === 其他属性 ===
            { DataKey.PickupRange, 100f }, // 拾取范围
            { DataKey.DodgeChance, 0f }, // 闪避率
        }
    };
}
