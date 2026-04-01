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
/// Custom = 0 永远保留；添加新场景请勿改变已有场景文件名，否则枚举值会错位
/// </summary>
public enum CollisionType
{
    Custom = 0,
    EffectCollision = 1,      // layer=0, mask=0
    EnemyCollision = 2,      // layer=4, mask=5
    EnemyHurtboxSensor = 3,      // layer=64, mask=128
    PlayerCollision = 4,      // layer=2, mask=1
    PlayerHurtboxSensor = 5,      // layer=8, mask=4
    PlayerPickupSensor = 6,      // layer=16, mask=0
}

/// <summary>
/// 碰撞类型注册表 - 纯数据，仅存储各类型的 Layer/Mask 映射
/// 查询方法请使用 CollisionTypeQuery（Data/Data/Collision/CollisionTypeQuery.cs）
/// </summary>
public static class CollisionTypeRegistry
{
    /// <summary>CollisionType → (Layer, Mask) 正向字典</summary>
    public static readonly IReadOnlyDictionary<CollisionType, (uint Layer, uint Mask)> LayerMaskByType =
        new Dictionary<CollisionType, (uint Layer, uint Mask)>
    {
        { CollisionType.EffectCollision, (0u, 0u) },
        { CollisionType.EnemyCollision, (4u, 5u) },
        { CollisionType.EnemyHurtboxSensor, (64u, 128u) },
        { CollisionType.PlayerCollision, (2u, 1u) },
        { CollisionType.PlayerHurtboxSensor, (8u, 4u) },
        { CollisionType.PlayerPickupSensor, (16u, 0u) },
    };

    /// <summary>(Layer, Mask) 元组 → CollisionType 反向字典（包含 layer=0 的类型）</summary>
    public static readonly IReadOnlyDictionary<(uint Layer, uint Mask), CollisionType> TypeByLayerMask =
        new Dictionary<(uint Layer, uint Mask), CollisionType>
    {
        { (0u, 0u), CollisionType.EffectCollision },
        { (4u, 5u), CollisionType.EnemyCollision },
        { (64u, 128u), CollisionType.EnemyHurtboxSensor },
        { (2u, 1u), CollisionType.PlayerCollision },
        { (8u, 4u), CollisionType.PlayerHurtboxSensor },
        { (16u, 0u), CollisionType.PlayerPickupSensor },
    };
}
