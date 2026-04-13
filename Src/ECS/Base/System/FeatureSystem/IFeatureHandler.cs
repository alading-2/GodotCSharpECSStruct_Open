/// <summary>
/// Feature 处理器接口 - 提供代码驱动的生命周期钩子
///
/// 适用场景：
/// - 需要在授予/移除时执行复杂逻辑（超出修改器能表达的范围）
/// - 需要在激活时执行自定义效果（配合 AbilitySystem 使用）
///
/// 简单属性类 Feature（只加减属性）无需实现此接口，
/// 直接在 FeatureDefinition.Modifiers 中配置即可。
///
/// 注册方式：在 [ModuleInitializer] 方法中调用 FeatureHandlerRegistry.Register(new MyHandler())
/// </summary>
public interface IFeatureHandler
{
    /// <summary>
    /// Feature 标识符（完整唯一 ID，对应 .tres FeatureHandlerId 字段）
    /// </summary>
    string FeatureId { get; }

    /// <summary>
    /// 分组路径，供 FeatureHandlerRegistry.GetByGroup() 查询使用。
    /// 格式："Ability.Movement"，注册时会自动向父级逐级索引（"Ability"、"Ability.Movement" 均可查到）。
    /// 留空则不参与分组索引。
    /// </summary>
    string FeatureGroup => string.Empty;

    /// <summary>Feature 被授予时调用（Granted 阶段）</summary>
    /// <param name="context">包含 Owner 和 Feature 的上下文</param>
    void OnGranted(FeatureContext context);

    /// <summary>Feature 被移除时调用（Removed 阶段，早于修改器回滚）</summary>
    /// <param name="context">包含 Owner 和 Feature 的上下文</param>
    void OnRemoved(FeatureContext context);

    /// <summary>
    /// Feature 一次激活开始时调用（Activated 阶段，可选）
    /// 适用于 Manual / OnEvent / Periodic 触发模式的 Feature
    /// </summary>
    void OnActivated(FeatureContext context) { }

    /// <summary>
    /// Feature 一次激活结束时调用（Ended 阶段，可选）
    /// 对应 AbilitySystem 执行完效果后
    /// </summary>
    void OnEnded(FeatureContext context) { }
}
