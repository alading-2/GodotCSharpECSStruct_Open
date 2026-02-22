/// <summary>
/// Global 实体相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>Entity 生成</summary>
        public const string EntitySpawned = "global:entity:spawned";
        /// <summary>Entity 生成事件数据</summary>
        public readonly record struct EntitySpawnedEventData(IEntity Entity);
    }
}
