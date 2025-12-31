using Godot;

/// <summary>
/// 敌人资源配置类
/// 继承自 UnitResource，添加敌人特有属性
/// </summary>
[GlobalClass]
public partial class EnemyResource : UnitResource
{
    /// <summary>
    /// 默认生成策略（限定为基础区域策略）
    /// </summary>
    [Export(PropertyHint.Enum, "Rectangle,Circle,Perimeter,Offscreen")]
    public SpawnPositionStrategy DefaultStrategy { get; set; } = SpawnPositionStrategy.Rectangle;

    /// <summary>
    /// 击杀后奖励的经验值
    /// </summary>
    [Export] public int ExpReward { get; set; } = 5;

    /// <summary>
    /// 碰撞伤害（与玩家接触时造成的伤害）
    /// </summary>
    [Export] public float ContactDamage { get; set; } = 10;
}
