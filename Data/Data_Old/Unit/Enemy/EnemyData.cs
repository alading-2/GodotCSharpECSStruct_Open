using System.Collections.Generic;
using Brotato.Data.ResourceManagement;
using Godot;
/// <summary>
/// 敌人数据（纯数据，无逻辑）
/// </summary>
public static class EnemyData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {
        ["鱼人"] = new()
        {
            // === 基础信息 ===
            { DataKey.Name, "鱼人" }, // Name
            { DataKey.Team, Team.Enemy }, // 阵营
            { DataKey.EntityType, EntityType.Unit }, // 实体类型
            { DataKey.VisualScenePath, ResourceManagement.Load<PackedScene>("鱼人", ResourceCategory.Asset) }, // 视觉场景路径
            { DataKey.HealthBarHeight, 100f }, // 血条高度（Y轴偏移）
            { DataKey.ExpReward, 2 }, // 经验奖励
            
            // === 生命属性 ===
            { DataKey.BaseHp, 20f }, // 基础生命值
            { DataKey.CurrentHp, 20f }, // 当前生命值
            
            // === 攻击属性 ===
            { DataKey.BaseAttack, 5f }, // 基础攻击力
            { DataKey.BaseAttackSpeed, 80f }, // 攻速80 (每秒攻击0.8次)
            { DataKey.Range, 50f }, // 近战攻击范围
            
            // === 防御属性 ===
            { DataKey.BaseDefense, 1f }, // 基础防御值
            
            // === 移动属性 ===
            { DataKey.MoveSpeed, 80f }, // 移动速度
            
            // === AI配置 ===
            { DataKey.DetectionRange, 400f }, // 索敌范围
            { DataKey.AttackRange, 50f }, // AI攻击判定范围
            
            // === AI配置 ===
            { DataKey.DetectionRange, 400f }, // 索敌范围
            { DataKey.AttackRange, 50f }, // AI攻击判定范围
        },

        ["豺狼人"] = new()
        {
            // === 基础信息 ===
            { DataKey.Name, "豺狼人" }, // Name
            { DataKey.Team, Team.Enemy }, // 阵营
            { DataKey.EntityType, EntityType.Unit }, // 实体类型
            { DataKey.VisualScenePath, ResourceManagement.Load<PackedScene>("豺狼人", ResourceCategory.Asset) }, // 视觉场景路径
            { DataKey.HealthBarHeight, 200f }, // 血条高度（Y轴偏移）
            { DataKey.ExpReward, 5 }, // 经验奖励
            
            // === 生命属性 ===
            { DataKey.BaseHp, 50f }, // 基础生命值
            { DataKey.CurrentHp, 50f }, // 当前生命值
            
            // === 攻击属性 ===
            { DataKey.BaseAttack, 10f }, // 基础攻击力
            { DataKey.BaseAttackSpeed, 100f }, // 攻速100 (每秒1次攻击)
            { DataKey.Range, 60f }, // 攻击范围
            { DataKey.CritRate, 5f }, // 5%暴击率
            { DataKey.CritDamage, 150f }, // 暴击伤害150%
            
            // === 防御属性 ===
            { DataKey.BaseDefense, 3f }, // 更高的防御
            
            // === 移动属性 ===
            { DataKey.MoveSpeed, 100f }, // 移动速度
            
            // === AI配置 ===
            { DataKey.DetectionRange, 500f }, // 索敌范围
            { DataKey.AttackRange, 60f }, // AI攻击判定范围
            
            // === AI配置 ===
            { DataKey.DetectionRange, 500f }, // 索敌范围
            { DataKey.AttackRange, 60f }, // AI攻击判定范围
        }
    };
}
