/// <summary>
/// 事件上下文基类
/// 
/// Context Object Pattern 用于事件系统中的"请求-响应"模式：
/// - 发布者创建 Context 对象并通过事件传递
/// - 订阅者修改 Context（填写结果、设置阻止等）
/// - 发布者读取 Context 获取处理结果
/// 
/// 详细设计文档：Docs/框架/ECS/Event/Context模式设计.md
/// </summary>
public class EventContext
{
    // ================= 流程控制 =================

    /// <summary>
    /// 是否已处理（任一订阅者标记后保持）
    /// </summary>
    public bool IsHandled { get; protected set; }

    /// <summary>
    /// 是否阻止后续订阅者执行
    /// 类似 DOM Event 的 stopPropagation()
    /// </summary>
    public bool IsPropagationStopped { get; private set; }

    /// <summary>
    /// 阻止事件继续传播给后续订阅者
    /// </summary>
    public void StopPropagation() => IsPropagationStopped = true;

    // ================= 结果状态 =================

    /// <summary>
    /// 操作/检查是否成功（默认 true）
    /// - 对于检查型事件：true 表示允许，false 表示阻止
    /// - 对于操作型事件：true 表示成功，false 表示失败
    /// </summary>
    public bool Success { get; protected set; } = true;

    /// <summary>
    /// 失败/阻止原因
    /// </summary>
    public string? FailReason { get; protected set; }

    /// <summary>
    /// 标记为失败或阻止
    /// </summary>
    /// <param name="reason">原因</param>
    public void SetFailed(string reason)
    {
        Success = false;
        IsHandled = true;
        // 记录第一个失败原因
        FailReason ??= reason;
    }
}
