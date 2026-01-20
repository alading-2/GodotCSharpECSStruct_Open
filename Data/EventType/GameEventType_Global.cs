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

        // === 单位全局事件 ===
        /// <summary>
        /// 单位被击杀（全局广播）
        /// </summary>
        /// <remarks>
        /// <para>发送者：HealthComponent（HP≤0）</para>
        /// <para>监听者：DamageStatisticsSystem（击杀统计）、LifecycleComponent（通过 Victim 筛选）</para>
        /// </remarks>
        public const string UnitKilled = "global:unit_killed";
        /// <summary>单位被击杀事件数据</summary>
        public readonly record struct UnitKilledEventData(
            IEntity? Victim,
            IEntity? Killer,
            DeathType DeathType = DeathType.Normal,
            DamageType DamageType = DamageType.True
        );

        // === 属性/等级全局事件 ===
        /// <summary>
        /// 单位等级提升 (全局广播)
        /// </summary>
        public const string LevelUp = "global:level_up";
        /// <summary>等级提升事件数据</summary>
        public readonly record struct LevelUpEventData(IEntity Entity, int OldLevel, int NewLevel);

    }
}
