using System;
using Godot;

/// <summary>
/// 全局事件总线 - 使用 C# 原生 Event
/// 无需继承 Node，纯静态类
/// </summary>
public static class EventBus
{
    // === 战斗事件 ===
    public static event Action<Node, Vector2>? EnemyDied;      // 参数：敌人节点, 位置
    public static event Action<float>? PlayerDamaged;          // 参数：伤害值
    public static event Action<float>? PlayerHealed;           // 参数：治疗量

    // === 波次事件 ===
    public static event Action<int>? WaveStarted;              // 参数：波次编号
    public static event Action<int>? WaveCompleted;            // 参数：波次编号

    // === 经验/升级事件 ===
    public static event Action<int>? ExperienceGained;         // 参数：经验值
    public static event Action<int>? LevelUp;                  // 参数：新等级
    // public static event Action<UpgradeData>? UpgradeSelected;  // 参数：升级数据 (暂时注释，UpgradeData未定义)

    // === 游戏状态事件 ===
    public static event Action? GameStart;
    public static event Action? GamePause;
    public static event Action? GameResume;
    public static event Action<bool>? GameOver;  // 参数：是否胜利

    // === 便捷触发方法 ===

    public static void TriggerEnemyDied(Node enemy, Vector2 position)
    {
        EnemyDied?.Invoke(enemy, position);
    }

    public static void TriggerWaveStarted(int waveIndex)
    {
        WaveStarted?.Invoke(waveIndex);
    }

    public static void TriggerWaveCompleted(int waveIndex)
    {
        WaveCompleted?.Invoke(waveIndex);
    }

    public static void TriggerGameOver(bool isVictory)
    {
        GameOver?.Invoke(isVictory);
    }
}
