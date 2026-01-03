/// <summary>
/// 数据分类枚举 - 用于 UI 展示和数据组织
/// </summary>
public enum DataCategory
{
    /// <summary>
    /// 基础信息（名称、等级等）
    /// </summary>
    Basic,

    /// <summary>
    /// 生命系统（生命值、护甲等）
    /// </summary>
    Health,

    /// <summary>
    /// 攻击系统（伤害、攻速等）
    /// </summary>
    Attack,

    /// <summary>
    /// 防御系统（闪避、减伤等）
    /// </summary>
    Defense,

    /// <summary>
    /// 移动系统（速度）
    /// </summary>
    Movement,

    /// <summary>
    /// 资源系统（拾取、经验等）
    /// </summary>
    Resource,

    /// <summary>
    /// 特殊机制（穿透、投射物等）
    /// </summary>
    Special,

    /// <summary>
    /// 计算数据（派生属性）
    /// </summary>
    Computed
}
