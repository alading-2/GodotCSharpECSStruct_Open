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
        [ExportGroup("Spawn Rule")]
        [Export] public bool IsEnableSpawnRule { get; set; } = true;
        [Export] public SpawnPositionStrategy SpawnStrategy { get; set; } = SpawnPositionStrategy.Rectangle;
        [Export] public int SpawnMinWave { get; set; } = 0;
        [Export] public int SpawnMaxWave { get; set; } = -1;
        [Export] public float SpawnInterval { get; set; } = 1.0f;
        [Export] public int SpawnMaxCountPerWave { get; set; } = -1;
        [Export] public int SingleSpawnCount { get; set; } = 1;
        [Export] public int SingleSpawnVariance { get; set; } = 0;
        [Export] public float SpawnStartDelay { get; set; } = 0f;
        [Export] public int SpawnWeight { get; set; } = 10;
    }
}
