using System.Collections.Generic;

/// <summary>
/// 玩家数据（纯数据，无逻辑）
/// </summary>
public static class PlayerData
{
    public static readonly Dictionary<string, Dictionary<string, object>> Configs = new()
    {
        ["Player1"] = new()
        {
            { DataKey.Name, "Player1" },
            { DataKey.VisualScenePath, "res://assets/Unit/Player/AnimatedSprite2D.tscn" },
            { DataKey.BaseHp, 100f },
            { DataKey.MoveSpeed, 200f },
            { DataKey.BaseAttack, 10f },
            { DataKey.PickupRange, 100f }
        }
    };
}
