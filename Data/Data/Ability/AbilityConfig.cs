using Godot;


namespace Slime.Config.Abilities
{
    [GlobalClass]
    public partial class AbilityConfig : Resource
    {
        /// <summary>
        /// 技能名称
        /// </summary>
        [ExportGroup("基础信息")]
        [DataKey(DataKey.Name)]
        [Export] public string? Name { get; set; }
        /// <summary>
        /// 技能描述
        /// </summary>
        [DataKey(DataKey.Description)]
        [Export] public string? Description { get; set; }
        /// <summary>
        /// 技能图标
        /// </summary>
        [DataKey(DataKey.AbilityIcon)]
        [Export] public Texture2D? AbilityIcon { get; set; }
        /// <summary>
        /// 当前级别
        /// </summary>
        [DataKey(DataKey.AbilityLevel)]
        [Export] public int AbilityLevel { get; set; } = 1;
        /// <summary>
        /// 最大级别
        /// </summary>
        [DataKey(DataKey.AbilityMaxLevel)]
        [Export] public int AbilityMaxLevel { get; set; } = 5;

        /// <summary>
        /// 实体类型
        /// </summary>
        [ExportGroup("技能类型")]
        [DataKey(DataKey.EntityType)]
        [Export] public EntityType EntityType { get; set; } = EntityType.Ability;
        /// <summary>
        /// 技能类型
        /// </summary>
        [DataKey(DataKey.AbilityType)]
        [Export] public AbilityType AbilityType { get; set; }
        /// <summary>
        /// 触发模式
        /// </summary>
        [DataKey(DataKey.AbilityTriggerMode)]
        [Export] public AbilityTriggerMode AbilityTriggerMode { get; set; }

        /// <summary>
        /// 消耗类型
        /// </summary>
        [ExportGroup("消耗与冷却")]
        [DataKey(DataKey.AbilityCostType)]
        [Export] public AbilityCostType AbilityCostType { get; set; }
        /// <summary>
        /// 消耗数值
        /// </summary>
        [DataKey(DataKey.AbilityCostAmount)]
        [Export] public float AbilityCostAmount { get; set; }
        /// <summary>
        /// 冷却时间 (秒)
        /// </summary>
        [DataKey(DataKey.AbilityCooldown)]
        [Export] public float AbilityCooldown { get; set; }

        /// <summary>
        /// 是否使用充能系统
        /// </summary>
        [ExportGroup("充能系统")]
        [DataKey(DataKey.IsAbilityUsesCharges)]
        [Export] public bool IsAbilityUsesCharges { get; set; }
        /// <summary>
        /// 最大充能层数
        /// </summary>
        [DataKey(DataKey.AbilityMaxCharges)]
        [Export] public int AbilityMaxCharges { get; set; }
        /// <summary>
        /// 充能时间 (秒)
        /// </summary>
        [DataKey(DataKey.AbilityChargeTime)]
        [Export] public float AbilityChargeTime { get; set; }

        /// <summary>
        /// 目标选择方式
        /// </summary>
        [ExportGroup("目标选择")]
        [DataKey(DataKey.AbilityTargetSelection)]
        [Export] public AbilityTargetSelection AbilityTargetSelection { get; set; }
        /// <summary>
        /// 目标几何形状
        /// </summary>
        [DataKey(DataKey.AbilityTargetGeometry)]
        [Export] public GeometryType AbilityTargetGeometry { get; set; }
        /// <summary>
        /// 目标阵营过滤
        /// </summary>
        [DataKey(DataKey.AbilityTargetTeamFilter)]
        [Export] public AbilityTargetTeamFilter AbilityTargetTeamFilter { get; set; }
        /// <summary>
        /// 目标排序方式
        /// </summary>
        [DataKey(DataKey.AbilityTargetSorting)]
        [Export] public AbilityTargetSorting AbilityTargetSorting { get; set; }

        /// <summary>
        /// 施法距离（索敌/瞄准射程；0=无限制）
        /// </summary>
        [DataKey(DataKey.AbilityCastRange)]
        [Export] public float AbilityCastRange { get; set; }
        /// <summary>
        /// 效果半径（圆形/扇形 AOE 半径；冲刺=位移距离）
        /// </summary>
        [DataKey(DataKey.AbilityEffectRadius)]
        [Export] public float AbilityEffectRadius { get; set; }
        /// <summary>
        /// 效果长度（矩形/线形 AOE 长度维度）
        /// </summary>
        [DataKey(DataKey.AbilityEffectLength)]
        [Export] public float AbilityEffectLength { get; set; }
        /// <summary>
        /// 效果宽度（矩形/线形 AOE 宽度维度）
        /// </summary>
        [DataKey(DataKey.AbilityEffectWidth)]
        [Export] public float AbilityEffectWidth { get; set; }

        /// <summary>
        /// 链式弹跳次数
        /// </summary>
        [ExportGroup("链式效果")]
        [DataKey(DataKey.AbilityChainCount)]
        [Export] public int AbilityChainCount { get; set; }
        /// <summary>
        /// 链式弹跳范围
        /// </summary>
        [DataKey(DataKey.AbilityChainRange)]
        [Export] public float AbilityChainRange { get; set; }
        /// <summary>
        /// 链式弹跳延时 (秒)
        /// </summary>
        [DataKey(DataKey.AbilityChainDelay)]
        [Export] public float AbilityChainDelay { get; set; } = 0.2f;
        /// <summary>
        /// 链式伤害衰减系数 (0-100)
        /// </summary>
        [DataKey(DataKey.AbilityChainDamageDecay)]
        [Export] public float AbilityChainDamageDecay { get; set; } = 100f;
        /// <summary>
        /// 最大目标数量
        /// </summary>
        [DataKey(DataKey.AbilityMaxTargets)]
        [Export] public int AbilityMaxTargets { get; set; }

        /// <summary>
        /// 技能伤害数值
        /// </summary>
        [ExportGroup("伤害效果")]
        // 这里只是示例，也许应该有一个DamageInfo配置？暂时先这样
        [DataKey(DataKey.AbilityDamage)]
        [Export] public float AbilityDamage { get; set; }
    }
}
