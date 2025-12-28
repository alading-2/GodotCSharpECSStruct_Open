using Godot;

/// <summary>
/// 敌人基础资源配置类。
/// 用于定义敌人的静态属性，如生命值、速度、经验奖励及对应的场景文件。
/// </summary>
[GlobalClass]
public partial class EnemyResource : Resource
{
    /// <summary>
    /// 敌人的显示名称
    /// </summary>
    [Export] public string EnemyName { get; set; } = "";

    /// <summary>
    /// 默认生成策略（例如：屏幕外生成）
    /// </summary>
    [Export] public SpawnPositionStrategy DefaultStrategy { get; set; } = SpawnPositionStrategy.Offscreen;

    /// <summary>
    /// 最大生命值
    /// </summary>
    [Export] public float MaxHp { get; set; } = 50;

    /// <summary>
    /// 移动速度
    /// </summary>
    [Export] public float Speed { get; set; } = 100;

    /// <summary>
    /// 碰撞伤害
    /// </summary>
    [Export] public float Damage { get; set; } = 10;

    /// <summary>
    /// 击杀后奖励的经验值
    /// </summary>
    [Export] public int ExpReward { get; set; } = 5;

    /// <summary>
    /// 敌人对应的场景文件 (.tscn)
    /// </summary>
    [Export] public PackedScene EnemyScene { get; set; }
}
