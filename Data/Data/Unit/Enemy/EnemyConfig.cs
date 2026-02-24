using Godot;

namespace Slime.Config.Units
{
    [GlobalClass]
    public partial class EnemyConfig : UnitConfig
    {
        /// <summary>
        /// 击杀经验值奖励
        /// </summary>
        [ExportGroup("敌人专有")]
        [DataKey(DataKey.ExpReward)]
        [Export] public int ExpReward { get; set; } = 1;

        /// <summary>
        /// AI 检测范围
        /// </summary>
        [ExportGroup("AI 配置")]
        [Export] public float DetectionRange { get; set; } = 400f;

        /// <summary>
        /// 是否启用生成规则
        /// </summary>
        [ExportGroup("Spawn Rule")]
        [DataKey(DataKey.IsEnableSpawnRule)]
        [Export] public bool IsEnableSpawnRule { get; set; } = true;
        /// <summary>
        /// 生成位置策略 (Rectangle/Circle)
        /// </summary>
        [DataKey(DataKey.SpawnStrategy)]
        [Export] public SpawnPositionStrategy SpawnStrategy { get; set; } = SpawnPositionStrategy.Rectangle;
        /// <summary>
        /// 起始波次 (从第几波开始生成)
        /// </summary>
        [DataKey(DataKey.SpawnMinWave)]
        [Export] public int SpawnMinWave { get; set; } = 0;
        /// <summary>
        /// 截止波次 (-1表示无限制)
        /// </summary>
        [DataKey(DataKey.SpawnMaxWave)]
        [Export] public int SpawnMaxWave { get; set; } = -1;
        /// <summary>
        /// 生成间隔 (秒)
        /// </summary>
        [DataKey(DataKey.SpawnInterval)]
        [Export] public float SpawnInterval { get; set; } = 1.0f;
        /// <summary>
        /// 单波次最大生成数量 (-1表示无限制)
        /// </summary>
        [DataKey(DataKey.SpawnMaxCountPerWave)]
        [Export] public int SpawnMaxCountPerWave { get; set; } = -1;
        /// <summary>
        /// 单次生成数量
        /// </summary>
        [DataKey(DataKey.SingleSpawnCount)]
        [Export] public int SingleSpawnCount { get; set; } = 1;
        /// <summary>
        /// 生成数量波动值 (最终数量 = Count ± Variance)
        /// </summary>
        [DataKey(DataKey.SingleSpawnVariance)]
        [Export] public int SingleSpawnVariance { get; set; } = 0;
        /// <summary>
        /// 波次开始后的首次生成延迟 (秒)
        /// </summary>
        [DataKey(DataKey.SpawnStartDelay)]
        [Export] public float SpawnStartDelay { get; set; } = 0f;
        /// <summary>
        /// 生成权重 (用于随机生成池)
        /// </summary>
        [DataKey(DataKey.SpawnWeight)]
        [Export] public int SpawnWeight { get; set; } = 10;
    }
}
