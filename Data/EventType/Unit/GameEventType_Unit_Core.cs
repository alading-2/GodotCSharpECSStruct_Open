/// <summary>
/// Unit 核心事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位创建</summary>
        public const string Created = "unit:created";
        /// <summary>单位创建事件数据</summary>
        public readonly record struct CreatedEventData(IEntity Entity);

        /// <summary>单位销毁</summary>
        public const string Destroyed = "unit:destroyed";
        /// <summary>单位销毁事件数据</summary>
        public readonly record struct DestroyedEventData(IEntity Entity);
    }
}
