using Godot;
using System.Collections.Generic;

/// <summary>
/// 全局生成配置 - 静态配置方式（相当于常量调用）
/// </summary>
public static class SpawnSystemConfig
{
    /// <summary> 每一波的默认持续时间（秒） </summary>
    public const float WaveDuration = 60.0f;

    /// <summary> 最大波次数量 </summary>
    public const int MaxWaves = 20;

    /// <summary> 波次间隔时间（休息时间） </summary>
    public const float WaveBreakTime = 5.0f;

    /// <summary>
    /// 所有敌人的生成规则列表。
    /// 第一次访问时会从资源路径加载对应的 .tres 文件。
    /// </summary>
    public static List<EnemySpawnConfig> SpawnRules
    {
        get
        {
            return new List<EnemySpawnConfig>
                {
                    GD.Load<EnemySpawnConfig>("res://Data/Data/Spawn/SpawnConfig/豺狼人生成规则.tres"),
                    GD.Load<EnemySpawnConfig>("res://Data/Data/Spawn/SpawnConfig/鱼人生成规则.tres")
                };
        }
    }
}
