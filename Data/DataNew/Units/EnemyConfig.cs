using Godot;

namespace Brotato.Data.Config.Units
{
    [GlobalClass]
    public partial class EnemyConfig : UnitConfig
    {
        [ExportGroup("敌人专有")]
        [Export] public int ExpReward { get; set; } = 1;

        [ExportGroup("AI 配置")]
        [Export] public float DetectionRange { get; set; } = 400f;
        [Export] public float AttackRange { get; set; } = 50f;

        // 使用 Resource 类型的 SpawnRule，支持嵌套编辑
        [Export] public SpawnRule? SpawnRule { get; set; }
    }
}
