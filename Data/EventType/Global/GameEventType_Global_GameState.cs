/// <summary>
/// Global 游戏状态相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>游戏开始</summary>
        public const string GameStart = "global:game_start";
        /// <summary>游戏开始事件数据</summary>
        public readonly record struct GameStartEventData();

        /// <summary>游戏暂停</summary>
        public const string GamePause = "global:game_pause";
        /// <summary>游戏暂停事件数据</summary>
        public readonly record struct GamePauseEventData();

        /// <summary>游戏恢复</summary>
        public const string GameResume = "global:game_resume";
        /// <summary>游戏恢复事件数据</summary>
        public readonly record struct GameResumeEventData();

        /// <summary>游戏结束</summary>
        public const string GameOver = "global:game_over";
        /// <summary>游戏结束事件数据</summary>
        public readonly record struct GameOverEventData(bool IsVictory);
    }
}
