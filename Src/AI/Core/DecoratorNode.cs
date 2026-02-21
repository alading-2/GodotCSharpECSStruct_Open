/// <summary>
/// 装饰节点基类 - 拥有一个子节点，对其返回值进行变换
/// </summary>
public abstract class DecoratorNode : BehaviorNode
{
    protected BehaviorNode Child;

    protected DecoratorNode(BehaviorNode child, string name = "") : base(name)
    {
        Child = child;
    }

    public override void Reset()
    {
        Child?.Reset();
    }
}

/// <summary>
/// 反转装饰器（Inverter）
/// <para>
/// - Success → Failure
/// - Failure → Success
/// - Running → Running (不变)
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
/// 强制成功装饰器（AlwaysSucceed）
/// <para>
/// 无论子节点返回什么，都返回 Success（Running 除外）。
/// 常用于 Sequence 中"可选"步骤。
/// </para>
/// </summary>
public class AlwaysSucceedNode : DecoratorNode
{
    public AlwaysSucceedNode(BehaviorNode child) : base(child, "AlwaysSucceed") { }

    public override NodeState Evaluate(AIContext ctx)
    {
        var state = Child.Evaluate(ctx);
        return state == NodeState.Running ? NodeState.Running : NodeState.Success;
    }
}

/// <summary>
/// 冷却装饰器（Cooldown）
/// <para>
/// 子节点执行成功后，进入冷却期。冷却期间直接返回 Failure。
/// 用于限制攻击频率等场景。
/// </para>
/// </summary>
public class CooldownNode : DecoratorNode
{
    private readonly float _cooldownTime;
    private float _remainingTime;

    public CooldownNode(BehaviorNode child, float cooldownTime)
        : base(child, $"Cooldown({cooldownTime}s)")
    {
        _cooldownTime = cooldownTime;
        _remainingTime = 0f;
    }

    public override NodeState Evaluate(AIContext ctx)
    {
        // 冷却中
        if (_remainingTime > 0f)
        {
            _remainingTime -= ctx.DeltaTime;
            return NodeState.Failure;
        }

        var state = Child.Evaluate(ctx);

        if (state == NodeState.Success)
        {
            _remainingTime = _cooldownTime;
        }

        return state;
    }

    public override void Reset()
    {
        _remainingTime = 0f;
        base.Reset();
    }
}
