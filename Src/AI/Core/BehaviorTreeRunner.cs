/// <summary>
/// 行为树运行器 - 管理行为树的 Tick 执行
/// <para>
/// 职责：
/// - 持有根节点引用
/// - 每帧调用根节点的 Evaluate
/// - 记录上一帧状态，支持中断检测
/// </para>
/// </summary>
public class BehaviorTreeRunner
{
    private static readonly Log _log = new(nameof(BehaviorTreeRunner));

    /// <summary>行为树根节点</summary>
    public BehaviorNode Root { get; private set; }

    /// <summary>上一帧运行结果</summary>
    public NodeState LastState { get; private set; } = NodeState.Success;

    /// <summary>当前是否有节点在 Running 状态</summary>
    public bool IsRunning => LastState == NodeState.Running;

    public BehaviorTreeRunner(BehaviorNode root)
    {
        Root = root;
    }

    /// <summary>
    /// 每帧驱动行为树
    /// </summary>
    /// <param name="ctx">AI 处理上下文</param>
    /// <returns>本帧根节点的返回状态</returns>
    public NodeState Tick(AIContext ctx)
    {
        if (Root == null)
        {
            _log.Warn("行为树根节点为空，跳过 Tick");
            return NodeState.Failure;
        }

        LastState = Root.Evaluate(ctx);
        return LastState;
    }

    /// <summary>
    /// 重置行为树所有节点状态
    /// </summary>
    public void Reset()
    {
        Root?.Reset();
        LastState = NodeState.Success;
    }

    /// <summary>
    /// 替换行为树根节点（热切换行为树）
    /// </summary>
    public void SetTree(BehaviorNode newRoot)
    {
        Root?.Reset();
        Root = newRoot;
        LastState = NodeState.Success;
    }
}
