/// <summary>
/// Data 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Data
    {
        public const string PropertyChanged = "data:property_changed";
        public readonly record struct PropertyChangedEventData(string Key, object? OldValue, object? NewValue);

        public const string Reset = "data:reset";
        public readonly record struct ResetEventData();
    }
}
