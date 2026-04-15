/// <summary>
/// Feature 上下文 - 贯穿一次生命周期操作与 Action 执行的统一上下文对象
///
/// 在 Granted/Removed/Activated/Execute/Ended 各阶段传递，也可直接作为 IFeatureAction.Execute 的数据载体。
/// 刻意不包含任何子系统专有类型（如 AbilityEntity / CastContext），
/// 保持 FeatureSystem 对上层系统的零依赖。
/// 调用方（如 AbilitySystem）自行将专有数据塞入 ActivationData，从 ExecuteResult 读取执行结果。
///
/// 输入/输出对称设计：
/// - 输入：ActivationData（object?）— 调用方写入，Handler 读取并转型
/// - 输出：ExecuteResult（object?）— Handler 通过 OnExecute 返回值写入，调用方读取并转型
/// </summary>
public class FeatureContext
{
    /// <summary>拥有该 Feature 的实体</summary>
    public IEntity? Owner { get; set; }

    /// <summary>Feature 实体（承载 Data 与 Events 的任意 IEntity）</summary>
    public IEntity? Feature { get; set; }

    /// <summary>运行时实例（含 Owner/FeatureEntity 及状态快捷访问）</summary>
    public FeatureInstance? Instance { get; set; }

    /// <summary>
    /// 单次运行阶段的来源上下文（Activated/Execute/Ended 时才有，Granted/Removed 时为 null）
    /// 类型由调用方决定，如 AbilitySystem 传入 CastContext，
    /// IFeatureHandler 通过 ctx.ActivationData as CastContext 取用。
    /// </summary>
    public object? ActivationData { get; set; }

    /// <summary>
    /// OnExecute 阶段的返回值。
    /// 与 ActivationData（输入）对称设计：FeatureSystem 层面是 object?，
    /// 子系统层面自行转型为专有类型（如 AbilityExecutedResult）。
    /// </summary>
    public object? ExecuteResult { get; set; }

    /// <summary>触发源事件数据（OnEvent 触发时携带，其余为 null）</summary>
    public object? SourceEventData { get; set; }

    //=======================Action的参数=====================
    /// <summary>Action 侧对 SourceEventData 的语义别名</summary>
    public object? TriggerEventData
    {
        get => SourceEventData;
        set => SourceEventData = value;
    }

    /// <summary>Action 间共享的临时数据（跨 Action 传值用）</summary>
    public System.Collections.Generic.Dictionary<string, object> ExtraData { get; } = new();
}
