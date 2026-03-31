//------------------------------------------------------------------------------
//* <ResourceGenerator>
//*     此文件由 ResourceGenerator 自动生成，请勿手动修改。
//*     来源：Data/Data/Collision/ 目录下所有 .tscn 文件（按节点名称字母序排列）。
//*     重新运行 ResourceGenerator 会覆盖本文件。
//* </ResourceGenerator>
//------------------------------------------------------------------------------

using System.Collections.Generic;

/// <summary>
/// 碰撞类型枚举
/// 值来源：Data/Data/Collision/ 目录下 .tscn 场景的根节点名称（字母序）
/// Custom = 0 永远保留，其余值按字母序从 1 开始，添加新场景请始终追加在末尾以防止枚举值错位
/// </summary>
public enum CollisionType
{
    Custom = 0,
    EffectCollision = 1,      // Area2D, layer=0, mask=0
    EnemyCollision = 2,       // CharacterBody2D, layer=4, mask=5
    EnemyHurtboxSensor = 3,   // Area2D, layer=64, mask=128
    PlayerCollision = 4,      // CharacterBody2D, layer=2, mask=1
    PlayerHurtboxSensor = 5,  // Area2D, layer=8, mask=4
    PlayerPickupSensor = 6,   // Area2D, layer=16, mask=0
}

/// <summary>
/// 碰撞类型注册表
/// 提供 CollisionType ↔ (Layer, Mask) 双向 O(1) 查找
/// </summary>
public static class CollisionTypeRegistry
{
    /// <summary>CollisionType → (Layer, Mask) 正向查找</summary>
    public static readonly IReadOnlyDictionary<CollisionType, (uint Layer, uint Mask)> LayerMaskByType =
        new Dictionary<CollisionType, (uint Layer, uint Mask)>
        {
            { CollisionType.EffectCollision,     (0,  0)   },
            { CollisionType.EnemyCollision,      (4,  5)   },
            { CollisionType.EnemyHurtboxSensor,  (64, 128) },
            { CollisionType.PlayerCollision,     (2,  1)   },
            { CollisionType.PlayerHurtboxSensor, (8,  4)   },
            { CollisionType.PlayerPickupSensor,  (16, 0)   },
        };

    /// <summary>
    /// Layer → CollisionType 反向查找（仅包含 layer != 0 的唯一层）
    /// layer=0 的类型（如 EffectCollision）无法反向识别，FromLayer 返回 Custom
    /// </summary>
    public static readonly IReadOnlyDictionary<uint, CollisionType> TypeByLayer =
        new Dictionary<uint, CollisionType>
        {
            { 2,  CollisionType.PlayerCollision     },
            { 4,  CollisionType.EnemyCollision      },
            { 8,  CollisionType.PlayerHurtboxSensor },
            { 16, CollisionType.PlayerPickupSensor  },
            { 64, CollisionType.EnemyHurtboxSensor  },
        };

    /// <summary>通过 collision_layer 反向查找 CollisionType（O(1)）</summary>
    public static CollisionType FromLayer(uint layer) =>
        TypeByLayer.TryGetValue(layer, out var t) ? t : CollisionType.Custom;

    /// <summary>通过 CollisionType 获取对应 (Layer, Mask)（O(1)），未找到返回 (0, 0)</summary>
    public static (uint Layer, uint Mask) GetLayerMask(CollisionType type) =>
        LayerMaskByType.TryGetValue(type, out var lm) ? lm : (0u, 0u);
}
