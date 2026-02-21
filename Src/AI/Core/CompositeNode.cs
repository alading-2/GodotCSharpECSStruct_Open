using System.Collections.Generic;

/// <summary>
/// 组合节点基类 - 拥有多个子节点
/// <para>
/// 派生类：
/// - <see cref="SequenceNode"/>: 顺序执行（AND 逻辑，全部成功才成功）
/// - <see cref="SelectorNode"/>: 选择执行（OR 逻辑，任一成功即成功）
/// </para>
/// </summary>
public abstract class CompositeNode : BehaviorNode
{
    protected readonly List<BehaviorNode> Children = new();

    protected CompositeNode(string name = "") : base(name) { }

    /// <summary>添加子节点（支持链式调用）</summary>
    public CompositeNode AddChild(BehaviorNode child)
    {
        Children.Add(child);
        return this;
    }

    public override void Reset()
    {
        foreach (var child in Children)
            child.Reset();
    }
}

/// <summary>
/// 序列节点（Sequence）- AND 逻辑
/// <para>
/// 按顺序评估每个子节点：
/// - 任一子节点返回 Failure → 整体 Failure
/// - 任一子节点返回 Running → 整体 Running
/// - 全部子节点返回 Success → 整体 Success
/// </para>
/// <para>
/// 支持"记忆"模式：记住上次 Running 的子节点索引，
/// 下一帧从该节点继续评估，而非从头开始。
/// </para>
/// </summary>
public class SequenceNode : CompositeNode
{
    private int _currentIndex;

    public SequenceNode(string name = "Sequence") : base(name) { }

    public override NodeState Evaluate(AIContext ctx)
    {
        for (int i = _currentIndex; i < Children.Count; i++)
        {
            var state = Children[i].Evaluate(ctx);

            switch (state)
            {
                case NodeState.Failure:
                    _currentIndex = 0;
                    return NodeState.Failure;

                case NodeState.Running:
                    _currentIndex = i;
                    return NodeState.Running;

                case NodeState.Success:
                    continue;
            }
        }

        _currentIndex = 0;
        return NodeState.Success;
    }

    public override void Reset()
    {
        _currentIndex = 0;
        base.Reset();
    }
}

/// <summary>
/// 选择节点（Selector）- OR 逻辑
/// <para>
/// 按优先级顺序评估每个子节点：
/// - 任一子节点返回 Success → 整体 Success
/// - 任一子节点返回 Running → 整体 Running
/// - 全部子节点返回 Failure → 整体 Failure
/// </para>
/// <para>
/// 支持"记忆"模式：记住上次 Running 的子节点索引。
/// </para>
/// </summary>
public class SelectorNode : CompositeNode
{
    private int _currentIndex;

    public SelectorNode(string name = "Selector") : base(name) { }

    public override NodeState Evaluate(AIContext ctx)
    {
        for (int i = _currentIndex; i < Children.Count; i++)
        {
            var state = Children[i].Evaluate(ctx);

            switch (state)
            {
                case NodeState.Success:
                    _currentIndex = 0;
                    return NodeState.Success;

                case NodeState.Running:
                    _currentIndex = i;
                    return NodeState.Running;

                case NodeState.Failure:
                    continue;
            }
        }

        _currentIndex = 0;
        return NodeState.Failure;
    }

    public override void Reset()
    {
        _currentIndex = 0;
        base.Reset();
    }
}
