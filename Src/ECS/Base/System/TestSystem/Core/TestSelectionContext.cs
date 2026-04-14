using System;

/// <summary>
/// TestSystem 的统一选中上下文。
/// </summary>
internal sealed class TestSelectionContext
{
    /// <summary>当前被测试面板选中的实体。</summary>
    public IEntity? SelectedEntity { get; private set; }

    /// <summary>当选中实体变化时发出。</summary>
    public event Action<IEntity?>? SelectionChanged;

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
        SelectionChanged?.Invoke(entity); // 广播新的选中实体
        return true;
    }
}
