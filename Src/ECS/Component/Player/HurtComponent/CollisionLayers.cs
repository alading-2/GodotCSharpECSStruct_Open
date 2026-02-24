/// <summary>
/// 碰撞层级常量定义（位掩码）
/// <para>
/// Layer = 我是谁（身份标签），Mask = 我关注谁（扫描目标）。
/// 碰撞触发条件：A.Mask 包含 B.Layer，或 B.Mask 包含 A.Layer，满足其一即可。
/// </para>
/// <para>
/// 使用示例：
/// <code>
/// CollisionLayer = CollisionLayers.PlayerHurtbox;
/// CollisionMask  = CollisionLayers.Enemy | CollisionLayers.Projectile;
/// </code>
/// </para>
/// </summary>
public static class CollisionLayers
{
    // ==================== 物理体层（CharacterBody2D / StaticBody2D）====================

    /// <summary>Layer 1 - 地形/障碍物（阻挡所有物理体）</summary>
    public const uint Terrain = 1;

    /// <summary>Layer 2 - 玩家物理体</summary>
    public const uint Player = 2;

    /// <summary>Layer 3 - 敌人物理体（地形阻挡 + 敌人互相推开）</summary>
    public const uint Enemy = 4;

    /// <summary>Layer 6 - 子弹/投射物物理体</summary>
    public const uint Projectile = 32;

    // ==================== 感应区层（Area2D）====================

    /// <summary>Layer 4 - 玩家受伤感应区（HurtComponent 挂在玩家身上时使用）</summary>
    public const uint PlayerHurtbox = 8;

    /// <summary>Layer 5 - 玩家拾取感应区（PickupComponent 身份标签）</summary>
    public const uint PlayerPickup = 16;

    /// <summary>Layer 7 - 敌人受伤感应区（HurtComponent 挂在敌人身上时使用）</summary>
    public const uint EnemyHurtbox = 64;

    /// <summary>Layer 8 - 武器/技能命中感应区（HitComponent 使用）</summary>
    public const uint WeaponHitbox = 128;

    // ==================== 预设组合（常用 Mask 快捷值）====================

    /// <summary>玩家受伤区扫描目标：敌人物理体 + 子弹</summary>
    public const uint PlayerHurtboxMask = Enemy | Projectile;

    /// <summary>敌人受伤区扫描目标：玩家物理体 + 武器命中区</summary>
    public const uint EnemyHurtboxMask = Player | WeaponHitbox;

    /// <summary>武器命中区扫描目标：敌人受伤区</summary>
    public const uint WeaponHitboxMask = EnemyHurtbox;

    /// <summary>子弹扫描目标：敌人受伤区</summary>
    public const uint ProjectileMask = EnemyHurtbox;
}
