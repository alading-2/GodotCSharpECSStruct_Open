/// <summary>
/// Feature 处理器接口 - 提供代码驱动的完整生命周期钩子
///
/// 适用场景：
/// - 需要在授予/移除时执行复杂逻辑（超出修改器能表达的范围）
/// - 需要在激活时执行自定义效果并返回结果（配合 AbilitySystem 使用）
/// - 需要在启用/禁用时响应状态变化（如被动光环暂停/恢复）
///
/// 完整生命周期：Granted → [Enabled ⇄ Disabled] → [Activated → Execute → Ended]* → Removed
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

    // ===== 一次性：授予/移除 =====

    /// <summary>Feature 被授予时调用（Granted 阶段）</summary>
    /// <param name="context">包含 Owner 和 Feature 的上下文</param>
    void OnGranted(FeatureContext context);

    /// <summary>Feature 被移除时调用（Removed 阶段，早于修改器回滚）</summary>
    /// <param name="context">包含 Owner 和 Feature 的上下文</param>
    void OnRemoved(FeatureContext context);

    // ===== 启用/禁用 =====

    /// <summary>
    /// Feature 被启用时调用（Enable 阶段，可选）
    /// 适用于被动光环等需要在启用时恢复效果的 Feature
    /// </summary>
    void OnEnabled(FeatureContext context) { }

    /// <summary>
    /// Feature 被禁用时调用（Disable 阶段，可选）
    /// 适用于被动光环等需要在禁用时暂停效果的 Feature
    /// </summary>
    void OnDisabled(FeatureContext context) { }

    // ===== 激活/执行/结束 =====

    /// <summary>
    /// Feature 一次激活开始时调用（Activated 阶段，可选）
    /// 通知阶段：Feature 已被激活，用于启动动画、播放音效等前置操作。
    /// 适用于 Manual / OnEvent / Periodic 触发模式的 Feature。
    /// </summary>
    void OnActivated(FeatureContext context) { }

    /// <summary>
    /// Feature 执行效果并返回结果（Execute 阶段，可选）
    /// 命令阶段：在 Activated 之后、Ended 之前调用，用于执行具体效果。
    /// 返回值通过 FeatureContext.ExecuteResult 传递给调用方。
    /// 返回 null 表示该 Feature 没有执行结果（如被动光环、纯数据 Feature）。
    /// </summary>
    object? OnExecute(FeatureContext context) => null;

    /// <summary>
    /// Feature 一次激活结束时调用（Ended 阶段，可选）
    /// 对应 AbilitySystem 执行完效果后
    /// </summary>
    void OnEnded(FeatureContext context) { }
}
