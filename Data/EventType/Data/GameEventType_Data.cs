/// <summary>
/// Data 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Data
    {
        /// <summary>属性变更</summary>
        public const string PropertyChanged = "data:property_changed";
        /// <summary>属性变更事件数据</summary>
        public readonly record struct PropertyChangedEventData(string Key, object? OldValue, object? NewValue);
        /// <summary>数据重置</summary>
        public const string Reset = "data:reset";
        /// <summary>数据重置事件数据</summary>
        public readonly record struct ResetEventData();
        /// <summary>生命值变更</summary>
        public const string HealthChanged = "unit:health_changed";
        /// <summary>生命值变更事件数据</summary>
        public readonly record struct HealthChangedEventData(float OldHp, float NewHp);
    }
}
