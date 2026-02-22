/// <summary>
/// Unit 生命周期相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位状态变化</summary>
        public const string StateChanged = "unit:state_changed";
        /// <summary>单位状态变化事件数据</summary>
        public readonly record struct StateChangedEventData(string Key, string OldValue, string NewValue);

        /// <summary>单位开始复活</summary>
        public const string Reviving = "unit:reviving";
        /// <summary>单位开始复活事件数据</summary>
        public readonly record struct RevivingEventData(float Duration);

        /// <summary>单位复活完成</summary>
        public const string Revived = "unit:revived";
        /// <summary>单位复活完成事件数据</summary>
        public readonly record struct RevivedEventData();
    }
}
