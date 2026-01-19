/// <summary>
/// 技能系统上下文类定义
/// 
/// 这些类用于事件驱动架构中的"请求-响应"模式：
/// - 调用者创建 Context 对象并通过事件传递
/// - 响应者（如 Component）填写结果
/// - 调用者读取结果进行后续处理
/// 
/// 设计原则：
/// - 继承自标准基类（BlockableContext / OperationContext）
/// - 分离到独立文件，保持事件定义文件的简洁
/// 
/// 详细设计文档：Docs/框架/ECS/Event/Context模式设计.md
/// </summary>

// ================= 执行结果 =================

/// <summary>
/// 技能执行结果
/// 用于 Executed 事件，记录技能执行的详细信息
/// </summary>
public class AbilityExecuteResult : EventContext
{
    // Success 和 FailReason (ErrorMessage) 已由 OperationContext 提供

    /// <summary>造成的总伤害</summary>
    public float TotalDamage { get; set; }

    /// <summary>治疗的总量</summary>
    public float TotalHeal { get; set; }

    /// <summary>命中的目标数</summary>
    public int TargetsHit { get; set; }
}
