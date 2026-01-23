using Godot;

/// <summary>
/// 全局游戏事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Global
    {
        // === 波次事件 ===
        /// <summary>波次开始</summary>
        public const string WaveStarted = "global:wave_started";
        /// <summary>波次开始事件数据</summary>
        public readonly record struct WaveStartedEventData(int WaveIndex);
        /// <summary>波次完成</summary>
        public const string WaveCompleted = "global:wave_completed";
        /// <summary>波次完成事件数据</summary>
        public readonly record struct WaveCompletedEventData(int WaveIndex);

        // === 实体全局事件 ===
        /// <summary>Entity 生成</summary>
        public const string EntitySpawned = "global:entity:spawned";
        /// <summary>Entity 生成事件数据</summary>
        public readonly record struct EntitySpawnedEventData(IEntity Entity);

        // === 游戏状态 ===
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
