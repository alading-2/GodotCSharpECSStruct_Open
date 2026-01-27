using Godot;

namespace Brotato.Data.Config.Units
{
    [GlobalClass]
    public partial class PlayerConfig : UnitConfig
    {
        [ExportGroup("玩家专有")]
        [Export] public float BaseMana { get; set; } = 50f;
        [Export] public float CurrentMana { get; set; } = 50f;
        [Export] public float BaseManaRegen { get; set; } = 2f;

        [Export] public float PickupRange { get; set; } = 100f;
        [Export] public float BaseSkillDamage { get; set; } = 100f;
        [Export] public float CooldownReduction { get; set; } = 0f;
    }
}
