using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot.Collections;
using Brotato.Data.Config.Units;
using Brotato.Data.ResourceManagement;

/// <summary>
/// 敌人生成系统 - 核心的"敌人生成与波次管理系统"。
/// <para>采用 TimerManager 驱动，统一管理所有计时器，性能优异且易于维护。</para>
/// </summary>
public partial class SpawnSystem : Node
{
    /// <summary>
    /// 模块初始化器：在程序集加载时自动执行。
    /// 通过 AutoLoad 框架注册此系统，确保其在游戏启动时作为单例存在，并能被全局访问。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // 注册到 AutoLoad，现在使用 .tscn 场景文件而非 .cs 脚本
        // 添加对 ObjectPoolInit 的依赖，确保敌人对象池先于生成系统初始化
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "SpawnSystem",
            Path = "res://Src/ECS/System/Spawn/SpawnSystem.tscn",
            Priority = AutoLoad.Priority.System,
            Dependencies = ["TimerManager"]
        });
    }

    private static readonly Log _log = new Log("SpawnSystem");

    /// <summary>
    /// 全局访问单例，方便其他模块通过 SpawnSystem.Instance 调用接口。
    /// </summary>
    public static SpawnSystem Instance { get; private set; }


    // === 实时运行状态 ===
    /// <summary> 当前进行的波次索引（从 1 开始），-1 表示尚未开始 </summary>
    public int CurrentWaveIndex { get; private set; } = -1;

    /// <summary> 标识当前是否正处于战斗波次中 </summary>
    public bool IsWaveActive { get; private set; }

    // === 内部组件 ===
    private GameTimer? _waveTimer;       // 控制波次总时长
    private GameTimer? _checkTimer;      // 核心轮询计时器(替代大量独立的 Rule Timer)）
    private const float CheckInterval = 0.1f;
    // 运行时状态跟踪 - 使用 struct 避免每波生成大量状态对象导致的 GC 压力
    // 运行时状态跟踪 - 使用 struct 避免每波生成大量状态对象导致的 GC 压力
    private struct RuleRuntimeState
    {
        public Resource Config;  // EnemyConfig (Resource)
        public SpawnRule Rule;     // 生成规则
        public float AccumulatedTime; // 累积时间
    }
    private List<RuleRuntimeState> _activeStates = new();

    /// <summary>
    /// 初始化系统：设置单例并初始化计时器。
    /// </summary>
    public override void _Ready()
    {
        Instance = this;

        // 监听游戏事件
        GlobalEventBus.Global.On(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.On<GameEventType.Global.GameOverEventData>(GameEventType.Global.GameOver, OnGameOver);

        _log.Info("SpawnSystem (TimerManager Architecture) 初始化完成");
    }



    /// <summary>
    /// 清理系统资源：解绑事件、清理单例、停止计时器。
    /// </summary>
    public override void _ExitTree()
    {
        // 解绑 EventBus 事件
        GlobalEventBus.Global.Off(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.Off<GameEventType.Global.GameOverEventData>(GameEventType.Global.GameOver, OnGameOver);

        // 清理单例引用
        if (Instance == this)
            Instance = null;

        // 归还计时器到对象池
        _waveTimer?.Cancel();
        _checkTimer?.Cancel();
        _waveTimer = null;
        _checkTimer = null;
        _activeStates.Clear();

        _log.Debug("SpawnSystem 已清理");
    }

    // ========================================
    // 游戏流程控制接口
    // ========================================

    /// <summary>
    /// 响应游戏开始事件，启动第一波。
    /// </summary>
    private void OnGameStart() => StartWave(1);

    /// <summary>
    /// 响应游戏结束事件，清理系统状态。
    /// </summary>
    /// <summary>
    /// 响应游戏结束事件，清理系统状态。
    /// </summary>
    /// <param name="evt">游戏结束数据</param>
    public void OnGameOver(GameEventType.Global.GameOverEventData evt)
    {
        IsWaveActive = false;
        _waveTimer?.Cancel();
        _checkTimer?.Cancel();
        _activeStates.Clear();
        KillAllEnemies();
    }

    /// <summary>
    /// 开启指定索引的波次。
    /// </summary>
    /// <param name="waveIndex">波次索引 (1-based)</param>
    public void StartWave(int waveIndex)
    {

        // 检查是否超过最大波次（如果 SpawnSystemConfig.MaxWaves > 0）
        if (SpawnSystemConfig.MaxWaves > 0 && waveIndex > SpawnSystemConfig.MaxWaves)
        {
            _log.Info("已通过最大波次，触发游戏结束");
            GlobalEventBus.Global.Emit(GameEventType.Global.GameOver, new GameEventType.Global.GameOverEventData(true));
            return;
        }

        CurrentWaveIndex = waveIndex;
        IsWaveActive = true;

        // 1. 创建波次总时长计时器
        _waveTimer?.Cancel(); // 取消旧计时器
        _waveTimer = TimerManager.Instance.Delay(SpawnSystemConfig.WaveDuration)
            .WithTag("SpawnSystem")
            .OnComplete(OnWaveTimeout);

        // 2. 初始化规则状态
        _activeStates.Clear();

        // 从 ResourceManagement 加载所有敌人配置
        var allEnemyConfigs = ResourceManagement.LoadAll<EnemyConfig>(ResourceCategory.EnemyConfig);

        foreach (var config in allEnemyConfigs)
        {
            var rule = config.SpawnRule;
            if (rule != null && IsRuleActiveForWave(rule, waveIndex))
            {
                _activeStates.Add(new RuleRuntimeState
                {
                    Config = config,
                    Rule = rule,
                    // 首个敌人生成的延迟时间
                    AccumulatedTime = rule.StartDelay > 0 ? -rule.StartDelay : 0
                });
            }
        }

        // 创建核心检查循环计时器 (CheckInterval 循环)
        _checkTimer?.Cancel(); // 取消旧计时器
        _checkTimer = TimerManager.Instance.Loop(CheckInterval)
            .WithTag("SpawnSystem")
            .OnLoop(OnCheckTimerTimeout);

        _log.Info($"波次 {waveIndex} 开始! 持续时间: {SpawnSystemConfig.WaveDuration}s, 激活规则数: {_activeStates.Count}");
        // 通过事件总线通知 UI 和其他系统
        GlobalEventBus.Global.Emit(GameEventType.Global.WaveStarted, new GameEventType.Global.WaveStartedEventData(waveIndex));
    }

    /// <summary>
    /// 波次超时回调：当波次时长到达时自动调用。
    /// </summary>
    private void OnWaveTimeout()
    {
        if (!IsWaveActive) return;
        IsWaveActive = false;

        _waveTimer?.Cancel();
        _checkTimer?.Cancel();
        _activeStates.Clear();

        _log.Info($"第 {CurrentWaveIndex}波进攻结束!");
        // 触发波次完成事件,通常用于开启商店界面或奖励选择
        GlobalEventBus.Global.Emit(GameEventType.Global.WaveCompleted, new GameEventType.Global.WaveCompletedEventData(CurrentWaveIndex));
    }

    // ========================================
    // 核心生成逻辑 (TimerManager 驱动)
    // ========================================

    private void OnCheckTimerTimeout()
    {
        if (!IsWaveActive) return;

        // 使用 for 循环配合索引访问，因为 RuleRuntimeState 现在是 struct
        // struct 在 List 中是值存储，foreach 会产生副本，修改副本不会同步回 List
        for (int i = 0; i < _activeStates.Count; i++)
        {
            var state = _activeStates[i];

            // 累积时间
            state.AccumulatedTime += CheckInterval;

            // 检查是否达到生成间隔
            // 追赶机制：如果卡顿导致时间跳跃，会一次性补足（但限制单帧最大次数以防卡死）
            int loopGuard = 0;
            while (state.AccumulatedTime >= state.Rule.SpawnInterval && loopGuard < 10)
            {
                state.AccumulatedTime -= state.Rule.SpawnInterval;
                loopGuard++;

                // 执行生成逻辑
                int count = state.Rule.SingleSpawnCount;
                // 每次生成时的随机波动
                if (state.Rule.SingleSpawnVariance > 0)
                {
                    count += GD.RandRange(-state.Rule.SingleSpawnVariance, state.Rule.SingleSpawnVariance);
                }

                if (count > 0)
                {
                    // 直接使用 state 中的 Config 配置和 Rule 中的策略
                    SpawnBatch(count, state.Config, state.Rule.Strategy);
                }
            }

            // 将修改后的 struct 重新赋值回 List
            _activeStates[i] = state;
        }
    }

    // ========================================
    // 公共接口
    // ========================================

    /// <summary>
    /// 手动批量生成敌人（用于测试或特殊事件）
    /// </summary>
    /// <param name="count">数量</param>
    /// <param name="enemyConfig">敌人配置资源</param>
    /// <param name="strategy">生成策略</param>
    public void SpawnBatch(int count, Resource enemyConfig, SpawnPositionStrategy strategy)
    {
        // 1. 计算位置
        // 获取当前视口（Viewport），用于确定屏幕边界和相机位置，确保敌人能正确生成在玩家视野外
        var viewport = GetViewport();

        // 使用默认参数包进行计算。后续可根据 rule 或特定需求动态调整参数
        // SpawnPositionParams 是 struct，分配在栈上，无 GC 压力
        var spawnParams = new SpawnPositionParams();

        var positions = SpawnPositionCalculator.GetSpawnPositions(strategy, count, spawnParams, viewport);

        // 2. 循环生成
        // 使用 EntityManager.Spawn 统一处理生成逻辑
        foreach (var pos in positions)
        {
            // 使用新的 EntitySpawnConfig 参数对象方式
            var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
            {
                Config = enemyConfig,
                UsingObjectPool = true,
                PoolName = ObjectPoolNames.EnemyPool,
                Position = pos
            });

            if (enemy == null)
            {
                // 如果生成失败（如池已满且策略为 discard），则跳过
                _log.Warn("生成敌人失败。");
            }
        }
    }

    // ========================================
    // 助手函数与内部校验
    // ========================================

    /// <summary>
    /// 判断指定的生成规则在当前波次是否激活。
    /// </summary>
    /// <param name="rule">生成规则配置</param>
    /// <param name="waveIndex">当前波次索引 (1-based)</param>
    /// <returns>如果当前波次在规则设定的 [MinWave, MaxWave] 范围内且数据合法，则返回 true</returns>
    private bool IsRuleActiveForWave(SpawnRule rule, int waveIndex)
    {
        // 基础安全性检查
        if (rule == null) return false;

        // 逻辑判断：
        // 1. waveIndex >= rule.MinWave: 当前波次必须达到规则要求的起始波次
        // 2. rule.MaxWave == -1: 表示该规则在起始波次后永久生效
        // 3. waveIndex <= rule.MaxWave: 如果设置了结束波次，当前波次不能超过它
        return waveIndex >= rule.MinWave && (rule.MaxWave == -1 || waveIndex <= rule.MaxWave);
    }

    /// <summary>
    /// 强制回收当前场景中由本系统生成的所有敌人。
    /// 通常在游戏结束、切换波次或特殊技能（如清屏）时调用。
    /// </summary>
    public void KillAllEnemies()
    {
        // 关键修复：必须使用注册时的常量池名 "EnemyPool" 而非类型名 "Enemy"
        var pool = ObjectPoolManager.GetPool<EnemyEntity>(ObjectPoolNames.EnemyPool);
        pool?.ReleaseAll();
        _log.Debug("已清理所有活跃敌人。");
    }
}
