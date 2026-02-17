/// <summary>
/// 技能触发结果 - 统一表示 TryTriggerAbility 的返回状态
/// </summary>
public enum TriggerResult
{
    /// <summary>技能立即执行成功（同步流水线完成）</summary>
    Success,

    /// <summary>触发失败（就绪检查不通过、无目标等）</summary>
    Failed,

    /// <summary>等待玩家输入目标（异步，Point 类型技能进入瞄准模式）</summary>
    WaitingForTarget,
}
