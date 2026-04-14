/// <summary>
/// TestSystem 的统一选中上下文。
/// </summary>
internal sealed class TestSelectionContext
{
    /// <summary>当前被测试面板选中的实体。</summary>
    public IEntity? SelectedEntity { get; private set; }

    /// <summary>选中上下文局部事件总线。</summary>
    public EventBus Events { get; } = new();

    /// <summary>
    /// 更新当前选中实体。
    /// </summary>
    /// <returns>实体是否发生变化。</returns>
    public bool SetSelectedEntity(IEntity? entity)
    {
        if (ReferenceEquals(SelectedEntity, entity))
        {
            return false;
        }

        SelectedEntity = entity;
        Events.Emit(
            GameEventType.Global.TestSystemSelectionChanged,
            new GameEventType.Global.TestSystemSelectionChangedEventData(
                entity // 当前选中实体
            )
        );
        return true;
    }
}
