/// <summary>
/// 碰撞语义类型枚举（位运算，手动维护）
/// <para>
/// 每个 Presets/Collision 下的 .tscn 场景对应一个独立的位标志，
/// 消费者通过位运算过滤感兴趣的碰撞类型：
/// <code>
/// (type &amp; CollisionType.Hurtbox) != 0    // 是受伤感应器（任意一种）
/// (type &amp; CollisionType.VisualBody) != 0  // 是视觉体碰撞（任意一种）
/// </code>
/// </para>
/// <para>
/// 与 CollisionTypeRegistry（ResourceGenerator 自动生成）的关系：
/// - CollisionTypeRegistry 存储 CollisionType 与 (layer, mask) 的正反映射
/// - 查询工具：CollisionTypeQuery.TryFromLayerMask / TryGetLayerMask
/// </para>
/// </summary>
[System.Flags]
public enum CollisionType : uint
{
    /// <summary>无碰撞类型标志</summary>
    None = 0,

    // ================= 单场景位标志（每个 .tscn 一个位） =================

    /// <summary>特效碰撞（Effect/EffectCollision.tscn，Area2D）</summary>
    EffectCollision = 1 << 0,

    /// <summary>敌人物理碰撞（Unit/EnemyCollision.tscn，CharacterBody2D）</summary>
    EnemyCollision = 1 << 1,

    /// <summary>玩家物理碰撞（Unit/PlayerCollision.tscn，CharacterBody2D）</summary>
    PlayerCollision = 1 << 3,

    /// <summary>敌人受击感应器（Sensor/EnemyHurtboxSensor.tscn，Area2D）</summary>
    EnemyHurtboxSensor = 1 << 2,

    /// <summary>玩家受击感应器（Sensor/PlayerHurtboxSensor.tscn，Area2D）</summary>
    PlayerHurtboxSensor = 1 << 4,

    /// <summary>玩家拾取感应器（Sensor/PlayerPickupSensor.tscn，Area2D）</summary>
    PlayerPickupSensor = 1 << 5,

    // ================= 组合类型（语义分组） =================

    /// <summary>视觉形体碰撞（VisualRoot 下的物理碰撞节点）</summary>
    VisualBody = EffectCollision | EnemyCollision | PlayerCollision,

    /// <summary>受伤感应器碰撞（Hurtbox Area2D 节点触发）</summary>
    Hurtbox = EnemyHurtboxSensor | PlayerHurtboxSensor,

    /// <summary>拾取感应器碰撞（Pickup Area2D 节点触发）</summary>
    Pickup = PlayerPickupSensor,

    /// <summary>组合：任意感应器类型（Hurtbox | Pickup）</summary>
    AnySensor = Hurtbox | Pickup,
}
