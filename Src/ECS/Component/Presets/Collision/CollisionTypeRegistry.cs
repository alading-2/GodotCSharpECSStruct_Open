//------------------------------------------------------------------------------
//* <ResourceGenerator>
//*     此文件由 ResourceGenerator 自动生成，请勿手动修改。
//*     来源：Src/ECS/Component/Presets/Collision/ 目录下所有 .tscn 文件。
//*     重新运行 ResourceGenerator 会覆盖本文件。
//* </ResourceGenerator>
//------------------------------------------------------------------------------

using System.Collections.Generic;

/// <summary>
/// 碰撞类型注册表（自动生成） - 存储 CollisionType <-> (Layer, Mask) 的双向映射
/// <para>
/// CollisionType 枚举定义在 CollisionType.cs（手动维护），本文件仅提供数据映射。
/// 查询方法请使用 CollisionTypeQuery（Src/ECS/Component/Presets/Collision/CollisionTypeQuery.cs）
/// </para>
/// </summary>
public static class CollisionTypeRegistry
{
    /// <summary>CollisionType → (Layer, Mask) 正向字典（仅包含单场景位标志，不含组合类型）</summary>
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

    /// <summary>(Layer, Mask) → CollisionType 反向字典</summary>
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
