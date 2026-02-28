using System.Collections.Generic;

/// <summary>
/// 组合节点基类 (Composite Node) - 可拥有多个子节点以管理执行流
/// <para>
/// 组合节点自身并不执行具体的动作，它的核心作用是管理和控制其包含的多个子节点的执行顺序与逻辑结构。
/// 控制流通常分为：
/// - <see cref="SequenceNode"/>: 顺序节点（逻辑与 AND），依次执行所有子节点，全体成功才算成功。
/// - <see cref="SelectorNode"/>: 选择节点（逻辑或 OR），依次检查所有子节点，找到一个成功的即可。
/// </para>
/// </summary>
public abstract class CompositeNode : BehaviorNode
{
    /// <summary>
    /// 该节点包容的子节点列表，它们将按照列表顺序进行评估。
    /// </summary>
    protected readonly List<BehaviorNode> Children = new();

    protected CompositeNode(string name = "") : base(name) { }

    /// <summary>
    /// 动态添加一个子节点到队尾。
    /// <para>
    /// 返回自身 (<c>this</c>) 以支持代码中流畅的链式调用（Fluent API 风格），
    /// 使构建行为树的代码在视觉上更具层级结构。
    /// </para>
    /// </summary>
    /// <param name="child">要加入的子层节点</param>
    /// <returns>当前组合节点，支持链式连续 AddChild()</returns>
    public CompositeNode Add(BehaviorNode child)
    {
        Children.Add(child);
        return this;
    }

    /// <summary>
    /// 重置当前组合节点及其所有子孙节点的状态。
    /// 通过遍历 <see cref="Children"/> 递归调用。
    /// </summary>
    public override void Reset(AIContext? ctx = null)
    {
        foreach (var child in Children)
            child.Reset(ctx);
    }

    /// <summary>
    /// 重置除指定索引外的所有子节点状态。
    /// 用于 Selector 切换分支时，清理旧分支的运行时状态。
    /// </summary>
    /// <param name="exceptIndex">不需要重置的子节点索引</param>
    /// <param name="ctx">AI 处理上下文（可选，用于清理黑板数据）</param>
    protected void ResetChildrenExcept(int exceptIndex, AIContext? ctx = null)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            if (i != exceptIndex)
                Children[i].Reset(ctx);
        }
    }
}

/// <summary>
/// 序列节点 (Sequence) - 对应布尔运算的逻辑"与" (AND)
/// <para>
/// 执行逻辑：
/// 1. 严格按照预定义顺序（从左到右/从上到下）逐个评估子节点。
/// 2. 如果某个子节点返回 <see cref="NodeState.Failure"/> (失败)，则立刻停止后续节点评估，向父级报告整体 Failure。
/// 3. 如果某个子节点返回 <see cref="NodeState.Running"/> (运行中)，则终止当前帧的评估，向父级报告整体 Running。
/// 4. 只有当**所有**子节点都顺利返回 <see cref="NodeState.Success"/> (成功) 时，它最终才会向父级报告整体 Success。
/// </para>
/// <para>
/// 记忆特性 (Memory/Stateful)：内部使用 <c>_currentIndex</c> 记下当前正处于 Running 状态的子节点索引。
/// 下一帧被 Tick 时，将直接从该节点**继续**执行，而不是从头(索引 0)重新开始评估所有前置的条件或动作节点，
/// 大大提升性能并保证了动作序列的不被随意打断。
/// </para>
/// </summary>
public class SequenceNode : CompositeNode
{
    /// <summary>内部指针，用于“记忆”目前执行到了哪一个子节点</summary>
    private int _currentIndex;

    public SequenceNode(string name = "Sequence") : base(name) { }

    public override NodeState Evaluate(AIContext ctx)
    {
        // 从上一次保存的内部指针位置开始迭代，避免重复执行已经处于 Success 状态的前置节点
        for (int i = _currentIndex; i < Children.Count; i++)
        {
            var state = Children[i].Evaluate(ctx);

            switch (state)
            {
                case NodeState.Failure:
                    // AND 逻辑：遇假即假。一旦其中任意一步失败，必须重置指针并宣告失败。
                    _currentIndex = 0;
                    return NodeState.Failure;

                case NodeState.Running:
                    // 遇延时执行，记住位置，并告诉父级本序列正在 Running 中。
                    _currentIndex = i;
                    return NodeState.Running;

                case NodeState.Success:
                    // 当前步成功，继续执行下一循环的子节点
                    continue;
            }
        }

        // 所有子节点遍历完毕且都没有失败/未完成，重置指针以便下次使用，宣告完美成功。
        _currentIndex = 0;
        return NodeState.Success;
    }

    /// <inheritdoc/>
    public override void Reset(AIContext? ctx = null)
    {
        _currentIndex = 0;
        base.Reset(ctx);
    }
}

/// <summary>
/// 选择节点 (Selector) - 对应布尔运算的逻辑"或" (OR)，也常被称为 Priority 优先选择节点
/// <para>
/// 执行逻辑：
/// 1. **每帧从头**按优先级顺序依次评估每个子节点。（列表越前面的优先级越高）
/// 2. 如果某个子节点返回 <see cref="NodeState.Success"/> (成功)，立即中止后续评估，向父级报告整体 Success。
/// 3. 如果某个子节点返回 <see cref="NodeState.Running"/> (运行中)，向父级报告整体 Running。
/// 4. 只有当**所有**子节点均返回 <see cref="NodeState.Failure"/> (失败) 时，向父级报告整体 Failure。
/// </para>
/// <para>
/// 优先级中断 (Priority Preemption)：每帧始终从索引 0 开始评估，确保高优先级分支随时可以抢占低优先级分支。
/// 当活跃分支切换时（例如从巡逻切到攻击），自动重置旧分支的状态，避免残留的运行时数据影响后续决策。
/// </para>
/// </summary>
public class SelectorNode : CompositeNode
{
    /// <summary>内部指针，记录上帧处于运行状态的子节点（用于检测分支切换）</summary>
    private int _currentIndex = -1;

    public SelectorNode(string name = "Selector") : base(name) { }

    public override NodeState Evaluate(AIContext ctx)
    {
        // 始终从头评估，确保高优先级分支能随时抢占低优先级分支
        for (int i = 0; i < Children.Count; i++)
        {
            var state = Children[i].Evaluate(ctx);

            switch (state)
            {
                case NodeState.Success:
                    // 方案成功，重置其他分支状态
                    if (_currentIndex != i && _currentIndex >= 0)
                        ResetChildrenExcept(i, ctx);
                    _currentIndex = -1;
                    return NodeState.Success;

                case NodeState.Running:
                    // 如果切换到了不同的分支，重置旧分支
                    if (_currentIndex != i && _currentIndex >= 0)
                        ResetChildrenExcept(i, ctx);
                    _currentIndex = i;
                    return NodeState.Running;

                case NodeState.Failure:
                    // 此方案不可行，转入下一个备选方案
                    continue;
            }
        }

        // 所有备选方案都失败
        _currentIndex = -1;
        return NodeState.Failure;
    }

    /// <inheritdoc/>
    public override void Reset(AIContext? ctx = null)
    {
        _currentIndex = -1;
        base.Reset(ctx);
    }
}
