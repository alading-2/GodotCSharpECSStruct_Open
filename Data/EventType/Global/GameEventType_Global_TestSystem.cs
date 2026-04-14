/// <summary>
/// Global TestSystem 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>TestSystem 当前选中实体变化</summary>
        public const string TestSystemSelectionChanged = "global:test_system:selection_changed";
        /// <summary>TestSystem 当前选中实体变化事件数据</summary>
        public readonly record struct TestSystemSelectionChangedEventData(IEntity? Entity);
    }
}
