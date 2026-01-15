using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 伤害统计系统 - 负责波次统计重置和击杀统计
/// <para>监听波次开始事件重置玩家的波次统计数据。</para>
/// <para>监听全局 Kill 事件记录击杀数。</para>
/// </summary>
public partial class DamageStatisticsSystem : Node
{
    /// <summary>日志处理实例</summary>
    private static readonly Log _log = new("DamageStatisticsSystem");

    /// <summary>
    /// 自动注册到引导器 (AutoLoad)
    /// <para>利用 C# 模块初始化器特性，在程序集加载时自动将该系统注册为 Godot 单例/节点。</para>
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DamageStatisticsSystem",
            Path = "res://Src/ECS/System/DamageSystem/DamageStatisticsSystem.cs",
            Priority = AutoLoad.Priority.System
        });
    }

    public override void _EnterTree()
    {
        // 1. 订阅波次开始事件
        // 当新波次开始时，需要清除上一波的临时统计数据（如每波造成的伤害、击杀数等）
        GlobalEventBus.Global.On<GameEventType.Global.WaveStartedEventData>(
            GameEventType.Global.WaveStarted, OnWaveStarted);

        // 2. 订阅全局击杀事件
        // 伤害系统（HealthComponent）在目标死亡时会发送 Kill 事件，本系统负责持久化这些统计
        GlobalEventBus.Global.On<GameEventType.Unit.KillEventData>(
            GameEventType.Unit.Kill, OnUnitKilled);

        _log.Debug("DamageStatisticsSystem 初始化完成");
    }

    public override void _ExitTree()
    {
        // 务必取消订阅，防止内存泄漏和逻辑错误（尤其是单例事件总线）
        GlobalEventBus.Global.Off<GameEventType.Global.WaveStartedEventData>(
            GameEventType.Global.WaveStarted, OnWaveStarted);

        GlobalEventBus.Global.Off<GameEventType.Unit.KillEventData>(
            GameEventType.Unit.Kill, OnUnitKilled);
    }

    /// <summary>
    /// 波次开始时的处理逻辑
    /// </summary>
    /// <param name="data">包含当前波次索引等信息</param>
    private void OnWaveStarted(GameEventType.Global.WaveStartedEventData data)
    {
        _log.Debug($"波次 {data.WaveIndex} 开始，执行玩家统计数据重置");

        // 核心机制：重置玩家实体的波次相关数据键 (DataKey)
        // 敌人实体通常在波次结束或死亡时销毁，因此无需显式重置波次数据
        var players = EntityManager.GetEntitiesByType<Player>("Player");
        foreach (var player in players)
        {
            ResetWaveStats(player.Data);
        }
    }

    /// <summary>
    /// 执行具体的数据重置操作
    /// </summary>
    /// <param name="data">实体的动态数据容器</param>
    private void ResetWaveStats(Data data)
    {
        data.Set(DataKey.WaveDamageDealt, 0f);     // 重置波次造成伤害
        data.Set(DataKey.WaveDamageTaken, 0f);     // 重置波次承受伤害
        data.Set(DataKey.WaveHits, 0);             // 重置波次命中次数
        data.Set(DataKey.WaveKills, 0);            // 重置波次击杀数
        data.Set(DataKey.WaveCriticalHits, 0);     // 重置波次暴击次数
    }

    /// <summary>
    /// 处理单位死亡事件，累加击杀数值
    /// </summary>
    /// <param name="data">击杀事件上下文，包含凶手、受害者、伤害类型等</param>
    private void OnUnitKilled(GameEventType.Unit.KillEventData data)
    {
        if (data.Killer is not Godot.Node killerNode) return;

        // 查找归属的 IUnit（自身或沿 PARENT 向上）
        var killerUnit = EntityRelationshipManager.FindAncestorOfType<IUnit>(killerNode);
        if (killerUnit == null)
        {
            _log.Error($"击杀统计失败：无法找到归属的 IUnit，Killer={data.Killer}");
            return;
        }

        // 更新累计总数
        killerUnit.Data.Add(DataKey.TotalKills, 1);
        // 更新当波累计数
        killerUnit.Data.Add(DataKey.WaveKills, 1);

        _log.Debug($"[杀敌统计] {killerUnit} 成功击杀了 {data.Victim}");
    }

}

