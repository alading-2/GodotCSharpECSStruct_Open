using System;
using Godot;

/// <summary>
/// 全局事件总线
/// 混合模式：
/// 1. 传统 C# 静态事件 (Legacy) - 用于强类型、高性能的核心流程
/// 2. 新版通用事件总线 (Global) - 用于解耦、动态事件、跨模块通信
/// </summary>
public static class GlobalEventBus
{
    private static readonly Log _log = new Log("GlobalEventBus");

    /// <summary>
    /// 新版全局通用事件总线
    /// 推荐用于非核心高频的所有逻辑事件
    /// </summary>
    public static readonly EventBus Global = new EventBus();

    // === 战斗事件 (Legacy -> Global) ===
    // public static event Action<Node, Vector2>? EnemyDied;      // 已迁移至 GameEventType.Global.EnemyDied
    // public static event Action<float>? PlayerDamaged;          // 已迁移至 GameEventType.Global.PlayerDamaged
    // public static event Action<float>? PlayerHealed;           // 已迁移至 GameEventType.Global.PlayerHealed

    // === 波次事件 ===
    // public static event Action<int>? WaveStarted;              // 已迁移至 GameEventType.Global.WaveStarted (Record)
    // public static event Action<int>? WaveCompleted;            // 已迁移至 GameEventType.Global.WaveCompleted

    // === 经验/升级事件 ===
    // public static event Action<int>? ExperienceGained;         // 已迁移至 GameEventType.Global.ExperienceGained
    // public static event Action<int>? LevelUp;                  // 已迁移至 GameEventType.Global.LevelUp

    // === 游戏状态事件 ===
    // public static event Action? GameStart;
    // public static event Action? GamePause;
    // public static event Action? GameResume;
    // public static event Action<bool>? GameOver;

    // === 便捷触发方法 ===

    public static void TriggerEnemyDied(Node enemy, Vector2 position)
    {
        Global.Emit(GameEventType.Global.EnemyDied, new GameEventType.Global.EnemyDiedEventData(enemy, position));
    }

    public static void TriggerWaveStarted(int waveIndex)
    {
        Global.Emit(GameEventType.Global.WaveStarted, new GameEventType.Global.WaveStartedEventData(waveIndex));
    }

    public static void TriggerWaveCompleted(int waveIndex)
    {
        Global.Emit(GameEventType.Global.WaveCompleted, new GameEventType.Global.WaveCompletedEventData(waveIndex));
    }

    public static void TriggerGameStart()
    {
        Global.Emit(GameEventType.Global.GameStart, new GameEventType.Global.GameStartEventData());
    }
    public static void TriggerGameOver(bool isVictory)
    {
        Global.Emit(GameEventType.Global.GameOver, new GameEventType.Global.GameOverEventData(isVictory));
    }
}
