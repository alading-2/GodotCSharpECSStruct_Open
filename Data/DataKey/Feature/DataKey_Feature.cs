/// <summary>
/// Feature 系统相关的 DataKey 定义
/// </summary>
public static partial class DataKey
{
    // ============ 基础信息 ============

    // Feature 分类标签（用于 UI 分组展示）
    public static readonly DataMeta FeatureCategory = DataRegistry.Register(
        new DataMeta { Key = nameof(FeatureCategory), DisplayName = "Feature分类", Category = DataCategory_Feature.Basic, Type = typeof(string), DefaultValue = "" });

    // Feature 处理器 ID（与 IFeatureHandler.FeatureId 对应）
    public static readonly DataMeta FeatureHandlerId = DataRegistry.Register(
        new DataMeta { Key = nameof(FeatureHandlerId), DisplayName = "Feature处理器ID", Category = DataCategory_Feature.Basic, Type = typeof(string), DefaultValue = "" });

    // ============ 修改器配置（绕过约束系统，直接存储 Godot Array） ============

    /// <summary>Feature 挂载的属性修改器列表（Godot.Collections.Array of FeatureModifierEntry）</summary>
    public const string FeatureModifiers = "FeatureModifiers";

    // ============ 状态标记 ============

    // Feature 是否当前处于启用状态
    public static readonly DataMeta FeatureEnabled = DataRegistry.Register(
        new DataMeta { Key = nameof(FeatureEnabled), DisplayName = "Feature启用", Category = DataCategory_Feature.State, Type = typeof(bool), DefaultValue = true });

    // Feature 是否当前处于激活执行中
    public static readonly DataMeta FeatureIsActive = DataRegistry.Register(
        new DataMeta { Key = nameof(FeatureIsActive), DisplayName = "Feature激活中", Category = DataCategory_Feature.State, Type = typeof(bool), DefaultValue = false });

    // Feature 被激活的累计次数
    public static readonly DataMeta FeatureActivationCount = DataRegistry.Register(
        new DataMeta { Key = nameof(FeatureActivationCount), DisplayName = "激活次数", Category = DataCategory_Feature.State, Type = typeof(int), DefaultValue = 0, MinValue = 0 });
}
