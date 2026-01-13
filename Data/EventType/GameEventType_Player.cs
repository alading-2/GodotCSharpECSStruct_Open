/// <summary>
/// Player 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Player
    {
        /// <summary>玩家等级变更</summary>
        public const string LevelChanged = "player:level_changed";
        /// <summary>玩家资源变更</summary>
        public const string ResourceChanged = "player:resource_changed";
    }
}
