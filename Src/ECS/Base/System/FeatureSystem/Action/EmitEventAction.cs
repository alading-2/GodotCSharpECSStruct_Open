/// <summary>
/// 发出事件动作 - 通过 Owner 或 Feature 的局部 EventBus 发出一个事件
///
/// 适合触发跨系统通信，如通知 UI 更新、触发音效、通知 AI 等。
/// </summary>
public class EmitEventAction : IFeatureAction
{
    /// <summary>要发出的事件键名</summary>
    public string EventKey { get; set; } = "";

    /// <summary>事件数据（null 时发送空数据）</summary>
    public object? EventData { get; set; }

    /// <summary>在 Owner 的事件总线上发出（true = Owner，false = Feature）</summary>
    public bool EmitOnOwner { get; set; } = true;

    public void Execute(FeatureContext ctx)
    {
        if (string.IsNullOrEmpty(EventKey)) return;

        if (EmitOnOwner)
            ctx.Owner?.Events.Emit(EventKey, EventData);
        else
            ctx.Feature?.Events.Emit(EventKey, EventData);
    }
}
