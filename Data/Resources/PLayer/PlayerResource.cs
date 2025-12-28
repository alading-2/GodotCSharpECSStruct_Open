using Godot;

/// <summary>
/// 玩家基础资源配置类。
/// 用于定义玩家的初始属性、移动参数、战斗数值及拾取范围等。
/// </summary>
[GlobalClass]
public partial class PlayerResource : Resource
{
	[ExportGroup("基础属性 (Base Attributes)")]
	/// <summary>
	/// 基础伤害加成
	/// </summary>
	[Export] public float BaseDamage { get; set; } = 10f;
	/// <summary>
	/// 基础移动速度加成
	/// </summary>
	[Export] public float BaseSpeed { get; set; } = 100f;
	/// <summary>
	/// 基础暴击率 (0.0 - 1.0)
	/// </summary>
	[Export] public float BaseCritRate { get; set; } = 0.05f;
	/// <summary>
	/// 基础暴击倍率 (默认 1.5 倍)
	/// </summary>
	[Export] public float BaseCritMultiplier { get; set; } = 1.5f;

	[ExportGroup("移动参数 (Movement)")]
	/// <summary>
	/// 当前实际移动速度
	/// </summary>
	[Export] public float Speed { get; set; } = 200f;
	/// <summary>
	/// 加速度
	/// </summary>
	[Export] public float Acceleration { get; set; } = 10f;

	[ExportGroup("跟随 AI (Follow AI)")]
	/// <summary>
	/// 跟随速度（用于召唤物或特殊跟随逻辑）
	/// </summary>
	[Export] public float FollowSpeed { get; set; } = 100f;
	/// <summary>
	/// 停止跟随的距离
	/// </summary>
	[Export] public float StopDistance { get; set; } = 10f;

	[ExportGroup("生命值 (Health)")]
	/// <summary>
	/// 最大生命值
	/// </summary>
	[Export] public float MaxHp { get; set; } = 100f;

	[ExportGroup("战斗数值 (Combat)")]
	/// <summary>
	/// 攻击伤害
	/// </summary>
	[Export] public float Damage { get; set; } = 10f;
	/// <summary>
	/// 击退力度
	/// </summary>
	[Export] public float Knockback { get; set; } = 100f;
	/// <summary>
	/// 受伤后的无敌时间 (秒)
	/// </summary>
	[Export] public float InvincibilityTime { get; set; } = 0.5f;

	[ExportGroup("拾取 (Pickup)")]
	/// <summary>
	/// 磁铁吸引速度
	/// </summary>
	[Export] public float MagnetSpeed { get; set; } = 300f;
	/// <summary>
	/// 是否启用磁铁功能
	/// </summary>
	[Export] public bool MagnetEnabled { get; set; } = false;
}
