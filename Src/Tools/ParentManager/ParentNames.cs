/// <summary>
/// 预定义的父节点名称常量
/// <para>与 ObjectPoolNames 类似，统一管理避免魔法字符串</para>
/// </summary>
public static class ParentNames
{
    // === 对象池容器 ===
    /// <summary>敌人对象池容器</summary>
    public const string EnemyPool = "EnemyPool";
    /// <summary>子弹对象池容器</summary>
    public const string BulletPool = "BulletPool";
    /// <summary>技能对象池容器</summary>
    public const string AbilityPool = "AbilityPool";
    /// <summary>血条UI对象池容器</summary>
    public const string HealthBarPool = "HealthBarPool";

    // === Entity 容器 ===
    /// <summary>Unit类型实体容器</summary>
    public const string UnitContainer = "UnitContainer";
    /// <summary>玩家实体容器</summary>
    public const string PlayerContainer = "PlayerContainer";
    /// <summary>技能实体容器</summary>
    public const string AbilityContainer = "AbilityContainer";

    // === UI 容器 ===
    /// <summary>UI根容器</summary>
    public const string UIContainer = "UIContainer";
    /// <summary>HUD容器</summary>
    public const string HUDContainer = "HUDContainer";
}
