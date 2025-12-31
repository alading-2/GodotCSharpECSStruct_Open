using Godot;

/// <summary>
/// 单位基础资源配置类（抽象基类）
/// 定义 Player 和 Enemy 的共同属性
/// </summary>
[GlobalClass]
public abstract partial class UnitResource : Resource
{
    /// <summary>
    /// 单位的显示名称
    /// </summary>
    [Export] public string UnitName { get; set; } = "";

    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public float MaxHp { get; set; } = 100;

    /// <summary>
    /// 移动速度
    /// </summary>
    [Export] public float Speed { get; set; } = 100;

    /// <summary>
    /// 基础伤害
    /// </summary>
    [Export] public float Damage { get; set; } = 10;

    /// <summary>
    /// 单位的显示场景（通常包含 AnimatedSprite2D）
    /// 统一命名为 VisualScene，方便 EntityManager 自动加载
    /// </summary>
    [Export] public PackedScene VisualScene { get; set; }
}
