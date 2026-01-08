using Godot;

/// <summary>
/// 敌人生成设置 - 定义某种敌人出现的波次范围和生成逻辑
/// </summary>
[GlobalClass]
public partial class EnemySpawnConfig : Resource
{
    /// <summary> 需要生成的敌人数据 </summary>
    [Export] public EnemyResource EnemyData { get; set; }

    /// <summary> 开始出现的最小波次（包含） </summary>
    [Export] public int MinWave { get; set; } = 0;

    /// <summary> 停止出现的最大波次（包含），-1 表示无限 </summary>
    [Export] public int MaxWave { get; set; } = -1;

    /// <summary> 
    /// 生成间隔（秒）。
    /// 可以扩展为曲线（Curve）以随波次降低间隔。
    /// </summary>
    [Export] public float SpawnInterval { get; set; } = 1.0f;

    /// <summary> 
    /// 单波次最大生成数量，-1 表示不限制（仅受间隔和波次时间限制）。
    /// </summary>
    [Export] public int MaxCountPerWave { get; set; } = -1;

    /// <summary> 每次触发生成时的基础数量（例如扎堆生成 5 个） </summary>
    [Export] public int SingleSpawnCount { get; set; } = 1;

    /// <summary> 每次生成时的随机波动（例如 5 +/- 2） </summary>
    [Export] public int SingleSpawnVariance { get; set; } = 0;

    /// <summary>
    /// 首个敌人生成的延迟时间（秒）
    /// </summary>
    [Export] public float StartDelay { get; set; } = 0f;

    /// <summary>
    /// 强度权重（可选，用于动态生成算法）
    /// </summary>
    [Export] public int Weight { get; set; } = 10;

    public EnemySpawnConfig() { }
}
