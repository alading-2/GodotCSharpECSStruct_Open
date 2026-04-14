/// <summary>
/// TestSystem 对通用鼠标选择结果事件的适配。
/// </summary>
public partial class TestSystem
{
    /// <summary>
    /// 绑定通用鼠标选择系统的结果事件。
    /// </summary>
    private void BindMouseSelectionEvents()
    {
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
    }

    /// <summary>
    /// 解绑通用鼠标选择系统的结果事件。
    /// </summary>
    private void UnbindMouseSelectionEvents()
    {
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
    }

    /// <summary>
    /// 通用鼠标选择完成后，把结果回写到 TestSystem 当前选中实体。
    /// </summary>
    private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompletedEventData evt)
    {
        if (!ShouldAcceptMouseSelection())
        {
            return;
        }

        SetSelectedEntity(evt.PrimaryEntity ?? (evt.Entities.Count > 0 ? evt.Entities[0] : null));
    }

    /// <summary>
    /// 判断 TestSystem 是否应该消费本次全局鼠标选择结果。
    /// </summary>
    private bool ShouldAcceptMouseSelection()
    {
        return _panelVisible && _selectionToggle.ButtonPressed;
    }
}
