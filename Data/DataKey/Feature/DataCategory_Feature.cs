/// <summary>
/// Feature 属性分类枚举 - 用于 UI 展示和数据组织
/// </summary>
public enum DataCategory_Feature
{
    /// <summary>基础信息（名称、描述、分类）</summary>
    Basic,

    /// <summary>触发配置（触发模式、事件、概率、间隔）</summary>
    Trigger,

    /// <summary>修改器配置列表</summary>
    Modifier,

    /// <summary>运行时状态（是否启用、激活次数等）</summary>
    State
}
