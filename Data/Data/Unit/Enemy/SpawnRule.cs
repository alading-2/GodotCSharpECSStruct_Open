using System.Collections.Generic;

/// <summary>
/// 生成规则
/// 定义敌人在特定波次范围的生成行为
/// </summary>
public class SpawnRule
{
    /// <summary> 生成策略（默认矩形范围） </summary>
    public SpawnPositionStrategy Strategy { get; set; } = SpawnPositionStrategy.Rectangle;

    /// <summary> 开始出现的最小波次（包含） </summary>
    public int MinWave { get; set; } = 0;

    /// <summary> 停止出现的最大波次（包含），-1 表示无限 </summary>
    public int MaxWave { get; set; } = -1;

    /// <summary> 生成间隔（秒） </summary>
    public float SpawnInterval { get; set; } = 1.0f;

    /// <summary> 单波次最大生成数量，-1 表示不限制 </summary>
    public int MaxCountPerWave { get; set; } = -1;

    /// <summary> 每次触发生成时的基础数量 </summary>
    public int SingleSpawnCount { get; set; } = 1;

    /// <summary> 每次生成时的随机波动 </summary>
    public int SingleSpawnVariance { get; set; } = 0;

    /// <summary> 首个敌人生成的延迟时间（秒） </summary>
    public float StartDelay { get; set; } = 0f;

    /// <summary> 强度权重（可选，用于动态生成算法） </summary>
    public int Weight { get; set; } = 10;
}
