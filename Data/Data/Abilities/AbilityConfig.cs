using Godot;


namespace Brotato.Data.Config.Abilities
{
    [GlobalClass]
    public partial class AbilityConfig : Resource
    {
        [ExportGroup("基础信息")]
        [Export] public string? Name { get; set; }
        [Export] public string? Description { get; set; }
        [Export] public Texture2D? AbilityIcon { get; set; }
        [Export] public int AbilityLevel { get; set; } = 1;
        [Export] public int AbilityMaxLevel { get; set; } = 5;

        [ExportGroup("技能类型")]
        [Export] public EntityType EntityType { get; set; } = EntityType.Ability;
        [Export] public AbilityType AbilityType { get; set; }
        [Export] public AbilityTriggerMode AbilityTriggerMode { get; set; }

        [ExportGroup("消耗与冷却")]
        [Export] public AbilityCostType AbilityCostType { get; set; }
        [Export] public float AbilityCostAmount { get; set; }
        [Export] public float AbilityCooldown { get; set; }

        [ExportGroup("充能系统")]
        [Export] public bool IsAbilityUsesCharges { get; set; }
        [Export] public int AbilityMaxCharges { get; set; }
        [Export] public float AbilityChargeTime { get; set; }

        [ExportGroup("目标选择")]
        [Export] public AbilityTargetSelection AbilityTargetSelection { get; set; }
        [Export] public AbilityTargetGeometry AbilityTargetGeometry { get; set; }
        [Export] public AbilityTargetTeamFilter AbilityTargetTeamFilter { get; set; }
        [Export] public AbilityTargetSorting AbilityTargetSorting { get; set; }

        [Export] public float AbilityRange { get; set; }
        [Export] public float AbilityLength { get; set; } // 矩形长度或扇形半径
        [Export] public float AbilityWidth { get; set; }  // 矩形宽度或扇形角度

        [ExportGroup("链式效果")]
        [Export] public int AbilityChainCount { get; set; }
        [Export] public float AbilityChainRange { get; set; }
        [Export] public int AbilityMaxTargets { get; set; }

        [ExportGroup("伤害效果")]
        // 这里只是示例，也许应该有一个DamageInfo配置？暂时先这样
        [Export] public float AbilityDamage { get; set; }
    }
}
