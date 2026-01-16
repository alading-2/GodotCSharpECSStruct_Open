using System;
using Godot;

/// <summary>
/// 全局事件总线
/// <para>采用混合模式以平衡性能与灵活性：</para>
/// <para>1. 传统 C# 静态事件 (Legacy)：用于强类型、极高性能要求的核心流程。</para>
/// <para>2. 新版通用事件总线 (Global)：用于模块间解耦、动态事件订阅及跨模块通信。</para>
/// </summary>
public static class GlobalEventBus
{
    private static readonly Log _log = new Log("GlobalEventBus");

    /// <summary>
    /// 全局通用事件总线实例。
    /// <para>推荐用于大多数非核心高频的游戏逻辑事件（如 UI 更新、成就触发、关卡状态变更等）。</para>
    /// </summary>
    public static readonly EventBus Global = new EventBus();

    // ============================================================
    // 便捷触发方法 (Convenience Trigger Methods)
    // ============================================================

    /// <summary>
    /// 触发波次开始事件
    /// </summary>
    /// <param name="waveIndex">当前开始的波次索引（从 0 或 1 开始，取决于配置）</param>
    public static void TriggerWaveStarted(int waveIndex)
    {
        Global.Emit(GameEventType.Global.WaveStarted, new GameEventType.Global.WaveStartedEventData(waveIndex));
    }

    /// <summary>
    /// 触发波次完成事件
    /// </summary>
    /// <param name="waveIndex">刚刚完成的波次索引</param>
    public static void TriggerWaveCompleted(int waveIndex)
    {
        Global.Emit(GameEventType.Global.WaveCompleted, new GameEventType.Global.WaveCompletedEventData(waveIndex));
    }

    /// <summary>
    /// 触发游戏开始事件
    /// </summary>
    public static void TriggerGameStart()
    {
        Global.Emit(GameEventType.Global.GameStart, new GameEventType.Global.GameStartEventData());
    }

    /// <summary>
    /// 触发游戏结束事件
    /// </summary>
    /// <param name="isVictory">是否胜利</param>
    public static void TriggerGameOver(bool isVictory)
    {
        Global.Emit(GameEventType.Global.GameOver, new GameEventType.Global.GameOverEventData(isVictory));
    }

    /// <summary>
    /// 触发单位击杀事件（全局广播）
    /// </summary>
    /// <remarks>
    /// 由 HealthComponent 在 HP≤0 时调用，供全局系统（统计、成就、任务）监听。
    /// </remarks>
    /// <param name="data">击杀事件数据，包含受害者、击杀者等信息</param>
    public static void TriggerUnitKilled(GameEventType.Global.UnitKilledEventData data)
    {
        Global.Emit(GameEventType.Global.UnitKilled, data);
    }
}
