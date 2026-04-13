using System;

/// <summary>
/// 条件节点 (Condition Node) - 行为树"观察"世界的眼睛
/// <para>
/// 作用：用于评估 AI 的当前环境状态、自身数据状况或外部输入。属于"叶子节点" (无子节点)。
/// 
/// 核心原则：
/// 1. 瞬时返回：条件节点应该是极轻量级的查询，它**永远不应该**返回 <see cref="NodeState.Running"/>。
/// 2. 二元结果：仅仅回答 "是" (返回 Success) 或 "否" (返回 Failure)。
/// 3. 只读性：理论上，大部分条件检查不应该修改游戏世界状态，不过有时它们会缓存一些查询结果到 <see cref="AIContext.Data"/> 中以便后续节点使用。
/// </para>
/// </summary>
public class ConditionNode : BehaviorNode
{
    /// <summary>持有的委托方法，传入 AIContext 并返回布尔判断结果</summary>
    private readonly Func<AIContext, bool> _condition;

    /// <summary>
    /// 创建一个新的条件检查节点。
    /// </summary>
    /// <param name="name">节点名称（方便调试查阅，如 "是否有目标"）</param>
    /// <param name="condition">实际执行条件判断的 Lambda 表达式或方法引用</param>
    public ConditionNode(string name, Func<AIContext, bool> condition) : base(name)
    {
        _condition = condition;
    }

    /// <summary>
    /// 评估此条件。如果返回 true 定义为 Success，否则为 Failure。
    /// </summary>
    public override NodeState Evaluate(AIContext ctx)
    {
        return _condition(ctx) ? NodeState.Success : NodeState.Failure;
    }
}

/// <summary>
/// 动作节点 (Action Node) - 行为树影响世界的双手
/// <para>
/// 作用：命令 AI 执行真正的实质性操作。属于"叶子节点"。这是行为树中最核心的业务逻辑承载者。
/// 
/// 状态解析：
/// - <see cref="NodeState.Success"/>: 动作瞬间完成，或经过多帧努力后已达成最终目标。
/// - <see cref="NodeState.Failure"/>: 动作无法执行（例如：被眩晕打断，目标忽然离线等）。
/// - <see cref="NodeState.Running"/>: 动作需要持续一段时间（多帧）才能完成。例如：角色正在跑步靠近目标点但还未抵达。
/// </para>
/// </summary>
public class ActionNode : BehaviorNode
{
    /// <summary>持有的委托方法，传入 AIContext 并直接返回行为树的运行状态</summary>
    private readonly Func<AIContext, NodeState> _action;

    /// <summary>
    /// 创建一个新的动作执行节点。
    /// </summary>
    /// <param name="name">节点名称（方便调试查阅，如 "移动追击"、"播放攻击动画"）</param>
    /// <param name="action">实际执行动作逻辑的 Lambda 表达式或方法引用</param>
    public ActionNode(string name, Func<AIContext, NodeState> action) : base(name)
    {
        _action = action;
    }

    /// <summary>
    /// 执行赋予的动作，并返回执行后的状态给父节点层。
    /// </summary>
    public override NodeState Evaluate(AIContext ctx)
    {
        return _action(ctx);
    }
}
