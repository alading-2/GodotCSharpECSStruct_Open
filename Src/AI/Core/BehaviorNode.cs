/// <summary>
/// 行为树节点返回状态
/// </summary>
public enum NodeState
{
    /// <summary>节点正在执行中（下一帧继续 Tick）</summary>
    Running,

    /// <summary>节点执行成功</summary>
    Success,

    /// <summary>节点执行失败</summary>
    Failure
}

/// <summary>
/// 行为树节点基类
/// <para>
/// 所有行为树节点（组合、装饰、叶子）的抽象基类。
/// 提供 Evaluate / OnReset 两个核心方法。
/// </para>
/// </summary>
public abstract class BehaviorNode
{
    /// <summary>节点名称（调试用）</summary>
    public string NodeName { get; set; }

    protected BehaviorNode(string name = "")
    {
        NodeName = string.IsNullOrEmpty(name) ? GetType().Name : name;
    }

    /// <summary>
    /// 评估此节点
    /// </summary>
    /// <param name="ctx">AI 处理上下文（包含 Entity、Data 等）</param>
    /// <returns>节点执行状态</returns>
    public abstract NodeState Evaluate(AIContext ctx);

    /// <summary>
    /// 重置节点状态（当行为树切换分支时调用）
    /// </summary>
    public virtual void Reset() { }
}
