using Godot;

[GlobalClass]
public partial class PlayerAttribute : Resource
{
    [ExportGroup("Base Attributes")]
    [Export] public float BaseDamage { get; set; } = 10f;
    [Export] public float BaseSpeed { get; set; } = 100f;
    [Export] public float BaseCritRate { get; set; } = 0.05f;
    [Export] public float BaseCritMultiplier { get; set; } = 1.5f;

    [ExportGroup("Movement (Velocity)")]
    [Export] public float MaxSpeed { get; set; } = 200f;
    [Export] public float Acceleration { get; set; } = 1000f;
    [Export] public float Friction { get; set; } = 800f;

    [ExportGroup("Follow AI")]
    [Export] public float FollowSpeed { get; set; } = 100f;
    [Export] public float StopDistance { get; set; } = 10f;

    [ExportGroup("Health")]
    [Export] public float MaxHp { get; set; } = 100f;

    [ExportGroup("Combat (Hitbox/Hurtbox)")]
    [Export] public float Damage { get; set; } = 10f;
    [Export] public float Knockback { get; set; } = 100f;
    [Export] public float InvincibilityTime { get; set; } = 0.5f;

    [ExportGroup("Pickup")]
    [Export] public float MagnetSpeed { get; set; } = 300f;
    [Export] public bool MagnetEnabled { get; set; } = false;
}
