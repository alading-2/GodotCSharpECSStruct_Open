/// <summary>
/// 2D 物理碰撞层常量。
/// <para>Layer 表示对象身份，Mask 表示对象关心谁；代码中统一使用此处常量，避免直接写魔法数字。</para>
/// </summary>
public static class CollisionLayers
{
    public const uint Terrain = 1u << 0;
    public const uint Player = 1u << 1;
    public const uint Enemy = 1u << 2;
    public const uint PlayerHurtbox = 1u << 3;
    public const uint PlayerPickup = 1u << 4;
    public const uint Projectile = 1u << 5;
    public const uint EnemyHurtbox = 1u << 6;
    public const uint WeaponHitbox = 1u << 7;
    public const uint SelectionPickable = 1u << 8;

    public const uint All = uint.MaxValue;
}
