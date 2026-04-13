/// <summary>
/// 装饰节点基类 (Decorator Node)
/// <para>
/// 装饰节点的设计模式来源于经典的 "装饰器模式"。在行为树中，它：
/// 1. 严格只拥有一个子节点 (<see cref="Child"/>)。
/// 2. 不改变子节点的内部逻辑，而是"拦截"或"修改"子节点的返回值（例如取反、强制成功）。
/// 3. 可以用于控制子节点的执行频率（例如冷却、延迟）或设定执行前置条件。
/// </para>
/// </summary>
public abstract class DecoratorNode : BehaviorNode
{
    /// <summary>被装饰的唯一子节点</summary>
    protected BehaviorNode Child;

    protected DecoratorNode(BehaviorNode child, string name = "") : base(name)
    {
        Child = child;
    }

    /// <summary>
    /// 当装饰器被重置时，递归将重置指令传递给子节点。
    /// </summary>
    public override void Reset(AIContext? ctx = null)
    {
        Child?.Reset(ctx);
    }
}

/// <summary>
/// 反转装饰器 (Inverter) - 对应布尔逻辑的 "非" (NOT)
/// <para>
/// 作用：颠倒子节点的成功/失败结果。
/// - 子节点返回 Success → 本节点返回 Failure
/// - 子节点返回 Failure → 本节点返回 Success
/// - 子节点返回 Running → 本节点依然返回 Running (因为动作还没做完)
/// </para>
/// <para>
/// 场景示例：结合 ConditionNode，用于表达 "如果不包含 XXX 状态"。
/// </para>
/// </summary>
public class InverterNode : DecoratorNode
{
    public InverterNode(BehaviorNode child) : base(child, "Inverter") { }

    public override NodeState Evaluate(AIContext ctx)
    {
        var state = Child.Evaluate(ctx);
        return state switch
        {
            NodeState.Success => NodeState.Failure,
            NodeState.Failure => NodeState.Success,
            _ => state
        };
    }
}

/// <summary>
/// 强制成功装饰器 (Always Succeed)
/// <para>
/// 作用：屏蔽子节点的失败结果。
/// 无论子节点最终返回什么结果，本节点都会向其父节点报告 Success（除非子节点正在 Running）。
/// </para>
/// <para>
/// 场景示例：
/// 通常放在 <see cref="SequenceNode"/> (顺序节点) 中，用来标记该步骤是一个 "可选" 或 "必定通过" 的步骤。
/// 比如：尝试播放一段嘲讽动画，即使播放失败，也不应打断后续的真正攻击行为。
/// </para>
/// </summary>
public class AlwaysSucceedNode : DecoratorNode
{
    public AlwaysSucceedNode(BehaviorNode child) : base(child, "AlwaysSucceed") { }

    public override NodeState Evaluate(AIContext ctx)
    {
        var state = Child.Evaluate(ctx);
        // 如果子节点还没执行完，继续维持 Running 状态；如果执行完了，不管成功失败都算 Success。
        return state == NodeState.Running ? NodeState.Running : NodeState.Success;
    }
}

/// <summary>
/// 冷却装饰器 (Cooldown) - 频率控制器
/// <para>
/// 作用：限制子节的执行频率。
/// 1. 当子节点尚未冷却完毕时，直接拦截请求，返回 Failure（通常会导致父节点 Selector 尝试其他分支）。
/// 2. 当冷却完毕且子节点成功执行完毕后，重置冷却时间。
/// </para>
/// <para>
/// 场景示例：用于限制 AI 某些特定技能（如大招、冲锋）的释放频率，避免连续使用。
/// </para>
/// </summary>
public class CooldownNode : DecoratorNode
{
    /// <summary>固定的冷却周期时长 (秒)</summary>
    private readonly float _cooldownTime;

    /// <summary>冷却计时器，非 null 表示冷却中</summary>
    private GameTimer? _timer;

    public CooldownNode(BehaviorNode child, float cooldownTime)
        : base(child, $"Cooldown({cooldownTime}s)")
    {
        _cooldownTime = cooldownTime;
    }

    public override NodeState Evaluate(AIContext ctx)
    {
        // 1. 冷却中，直接阻断
        if (_timer != null)
            return NodeState.Failure;

        // 2. 冷却就绪，执行子节点
        var state = Child.Evaluate(ctx);

        // 3. 只有当子节点 "成功" 时，才启动冷却定时器
        if (state == NodeState.Success)
        {
            _timer = TimerManager.Instance.Delay(_cooldownTime).OnComplete(() => _timer = null);
        }

        return state;
    }

    /// <inheritdoc/>
    public override void Reset(AIContext? ctx = null)
    {
        _timer?.Cancel();
        _timer = null;
        base.Reset(ctx);
    }
}
