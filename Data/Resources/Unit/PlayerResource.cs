using Godot;

/// <summary>
/// 玩家资源配置类
/// 继承自 UnitResource，添加玩家特有属性
/// </summary>
[GlobalClass]
public partial class PlayerResource : UnitResource
{
    /// <summary>
    /// 初始经验值
    /// </summary>
    [Export] public int InitialExp { get; set; } = 0;

    /// <summary>
    /// 初始等级
    /// </summary>
    [Export] public int InitialLevel { get; set; } = 1;

    /// <summary>
    /// 初始武器槽位数量
    /// </summary>
    [Export] public int InitialWeaponSlots { get; set; } = 2;

    /// <summary>
    /// 拾取范围
    /// </summary>
    [Export] public float PickupRange { get; set; } = 100;
}
