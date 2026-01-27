using Godot;

namespace Brotato.Data.Config.Units
{
    /// <summary>
    /// 单位配置基类
    /// 包含 Player 和 Enemy 共有的属性
    /// 属性名必须与 DataKey 中的字符串常量一致，以便自动加载
    /// </summary>
    public abstract partial class UnitConfig : Resource
    {
        [ExportGroup("基础信息")]
        [Export] public string Name { get; set; } = "未命名单位";
        [Export] public Team Team { get; set; }

        [ExportGroup("视觉")]
        /// <summary>
        /// 对应 DataKey.VisualScenePath
        /// Data.LoadFromResource 会自动读取此属性，并可选择转换为路径或直接使用
        /// </summary>
        [Export] public PackedScene? VisualScenePath { get; set; }

        [Export] public float HealthBarHeight { get; set; } = 100f;

        [ExportGroup("生命属性")]
        [Export] public float BaseHp { get; set; } = 100f;
        [Export] public float CurrentHp { get; set; } = 100f;
        [Export] public float BaseHpRegen { get; set; } = 0f;
        [Export] public float LifeSteal { get; set; } = 0f;

        [ExportGroup("攻击属性")]
        [Export] public float BaseAttack { get; set; } = 10f;
        [Export] public float BaseAttackSpeed { get; set; } = 100f;
        [Export] public float Range { get; set; } = 50f;
        [Export] public float CritRate { get; set; } = 0f;
        [Export] public float CritDamage { get; set; } = 150f;
        [Export] public float Penetration { get; set; } = 0f; // 护甲穿透

        [ExportGroup("防御属性")]
        [Export] public float BaseDefense { get; set; } = 0f;
        [Export] public float DamageReduction { get; set; } = 0f;

        [ExportGroup("移动属性")]
        [Export] public float MoveSpeed { get; set; } = 100f;

        [Export] public float DodgeChance { get; set; } = 0f; // 闪避率
    }
}
