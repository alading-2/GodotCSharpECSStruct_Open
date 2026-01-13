/// <summary>
/// 属性分类枚举 - 用于 UI 展示和数据组织
/// </summary>
public enum AttributeCategory
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
    /// 魔法系统（魔法值、魔法恢复等）
    /// </summary>
    Mana,

    /// <summary>
    /// 攻击系统（伤害、攻速等）
    /// </summary>
    Attack,

    /// <summary>
    /// 防御系统（防御、魔抗等）
    /// </summary>
    Defense,

    /// <summary>
    /// 技能系统（技能伤害、冷却缩减等）
    /// </summary>
    Skill,

    /// <summary>
    /// 移动系统（速度）
    /// </summary>
    Movement,

    /// <summary>
    /// 闪避相关（闪避率、无视闪避等）
    /// </summary>
    Dodge,

    /// <summary>
    /// 暴击相关（暴击率、暴击伤害等）
    /// </summary>
    Crit,

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
