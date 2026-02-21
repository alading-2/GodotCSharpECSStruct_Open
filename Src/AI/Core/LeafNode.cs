using System;

/// <summary>
/// 条件节点 - 叶子节点，用于检查条件
/// <para>
/// 返回 Success（条件满足）或 Failure（条件不满足），不应返回 Running。
/// </para>
/// </summary>
public class ConditionNode : BehaviorNode
{
    private readonly Func<AIContext, bool> _condition;

    /// <summary>
    /// 创建条件节点
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="condition">条件判定函数</param>
    public ConditionNode(string name, Func<AIContext, bool> condition) : base(name)
    {
        _condition = condition;
    }

    public override NodeState Evaluate(AIContext ctx)
    {
        return _condition(ctx) ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>
/// 动作节点 - 叶子节点，用于执行具体行为
/// <para>
/// 可返回 Success / Failure / Running。
/// Running 表示动作需要多帧完成（如移动到目标点）。
/// </para>
/// </summary>
public class ActionNode : BehaviorNode
{
    private readonly Func<AIContext, NodeState> _action;

    /// <summary>
    /// 创建动作节点
    /// </summary>
    /// <param name="name">节点名称</param>
    /// <param name="action">动作执行函数</param>
    public ActionNode(string name, Func<AIContext, NodeState> action) : base(name)
    {
        _action = action;
    }

    public override NodeState Evaluate(AIContext ctx)
    {
        return _action(ctx);
    }
}
