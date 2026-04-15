/// <summary>
/// Feature 单次运行的结束原因。
/// </summary>
public enum FeatureEndReason
{
    /// <summary>正常完成。</summary>
    Completed = 0,

    /// <summary>由玩家、AI 或业务逻辑主动取消。</summary>
    Cancelled = 1,

    /// <summary>被外部状态打断，例如眩晕、死亡、沉默。</summary>
    Interrupted = 2,

    /// <summary>执行失败后结束。</summary>
    Failed = 3
}
