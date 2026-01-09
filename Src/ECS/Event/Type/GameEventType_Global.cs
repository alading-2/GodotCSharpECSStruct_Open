using Godot;

/// <summary>
/// 全局游戏事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Global
    {
        // === 战斗事件 ===

        /// <summary>敌人死亡 (全局广播)</summary>
        public const string EnemyDied = "global:enemy_died";
        public readonly record struct EnemyDiedEventData(Node Enemy, Vector2 Position);

        public const string PlayerDamaged = "global:player_damaged";
        public readonly record struct PlayerDamagedEventData(float Damage);

        public const string PlayerHealed = "global:player_healed";
        public readonly record struct PlayerHealedEventData(float Amount);

        // === 波次事件 ===
        public const string WaveStarted = "global:wave_started";
        public readonly record struct WaveStartedEventData(int WaveIndex);

        public const string WaveCompleted = "global:wave_completed";
        public readonly record struct WaveCompletedEventData(int WaveIndex);

        // === 经验/升级 ===
        public const string ExperienceGained = "global:experience_gained";
        public readonly record struct ExperienceGainedEventData(int Amount);

        public const string LevelUp = "global:level_up";
        public readonly record struct LevelUpEventData(int NewLevel);

        // === 游戏状态 ===
        public const string GameStart = "global:game_start";
        public readonly record struct GameStartEventData();

        public const string GamePause = "global:game_pause";
        public readonly record struct GamePauseEventData();

        public const string GameResume = "global:game_resume";
        public readonly record struct GameResumeEventData();

        public const string GameOver = "global:game_over";
        public readonly record struct GameOverEventData(bool IsVictory);
    }
}
