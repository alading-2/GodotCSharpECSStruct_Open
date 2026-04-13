using Godot;

/// <summary>
/// 动作节点：原地等待指定时间
/// <para>
/// 停止移动，进入 Idle 状态，等待 TimerManager 回调完成后返回 Success。
/// 通常配合 RandomWanderAction 使用：到达巡逻点后发呆。
/// </para>
/// <para>
/// 使用的 DataKey：
/// - PatrolWaitTime：等待时长（秒）
/// - PatrolWaitDone：定时器是否已完成（bool，由 TimerManager 回调写入）
/// </para>
/// <para>
/// 返回值：
/// - Running：正在等待中
/// - Success：等待完成
/// </para>
/// </summary>
public class WaitIdleAction : BehaviorNode
{
    private GameTimer? _timer;

    /// <summary>
    /// 创建原地等待动作节点
    /// </summary>
    public WaitIdleAction() : base("原地等待") { }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var data = ctx.Entity.Data;

        // 首次进入：启动定时器
        if (_timer == null)
        {
            float waitTime = data.Get<float>(DataKey.PatrolWaitTime, 1.5f);
            data.Set(DataKey.PatrolWaitDone, false);

            _timer = TimerManager.Instance.Delay(waitTime).OnComplete(() =>
            {
                data.Set(DataKey.PatrolWaitDone, true);
                _timer = null;
            });
        }

        // 停止移动，进入 Idle
        data.Set(DataKey.AIMoveDirection, Vector2.Zero);
        data.Set(DataKey.AIMoveSpeedMultiplier, 0f);
        data.Set(DataKey.AIState, AIState.Idle);

        // 检查完成标记
        if (data.Get<bool>(DataKey.PatrolWaitDone))
        {
            data.Set(DataKey.PatrolWaitDone, false);
            return NodeState.Success;
        }

        return NodeState.Running;
    }

    /// <inheritdoc/>
    public override void Reset(AIContext? ctx = null)
    {
        _timer?.Cancel();
        _timer = null;
        ctx?.Entity.Data.Set(DataKey.PatrolWaitDone, false);
    }
}
