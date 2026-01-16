using System.Collections.Generic;

/// <summary>
/// 敌人数据（纯数据，无逻辑）
/// </summary>
public static class EnemyData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {
        ["鱼人"] = new()
        {
            { DataKey.Name, "鱼人" },
            { DataKey.VisualScenePath, "res://assets/Unit/Enemy/鱼人/AnimatedSprite2D/AnimatedSprite2D.tscn" },
            { DataKey.ExpReward, 2 },
            { DataKey.BaseHp, 20f },
            { DataKey.MoveSpeed, 80f },
            { DataKey.BaseAttack, 5f },
            { DataKey.SpawnRule, new SpawnRule
                {
                    MinWave = 1,
                    MaxWave = -1,
                    SpawnInterval = 2.0f,
                    SingleSpawnCount = 3,
                    SingleSpawnVariance = 1,
                    StartDelay = 0f,
                    Strategy = SpawnPositionStrategy.Perimeter
                }
            }
        },

        ["豺狼人"] = new()
        {
            { DataKey.Name, "豺狼人" },
            { DataKey.VisualScenePath, "res://assets/Unit/Enemy/豺狼人/AnimatedSprite2D/AnimatedSprite2D.tscn" },
            { DataKey.ExpReward, 5 },
            { DataKey.BaseHp, 50f },
            { DataKey.MoveSpeed, 100f },
            { DataKey.BaseAttack, 10f },
            { DataKey.SpawnRule, new SpawnRule
                {
                    MinWave = 3,
                    MaxWave = -1,
                    SpawnInterval = 3.0f,
                    SingleSpawnCount = 2,
                    SingleSpawnVariance = 0,
                    StartDelay = 5f,
                    Strategy = SpawnPositionStrategy.Perimeter
                }
            }
        }
    };
}
