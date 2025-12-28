using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 敌人生成系统 - 核心的“敌人生成与波次管理系统”。
/// <para>采用 Godot 计时器（Timer）驱动，逻辑清晰且性能优异。</para>
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
		AutoLoad.Register("SpawnSystem", "res://Src/ECS/System/Spawn/SpawnSystem.tscn", AutoLoad.Priority.System);
	}

	private static readonly Log _log = new Log("SpawnSystem");

	/// <summary>
	/// 全局访问单例，方便其他模块通过 SpawnSystem.Instance 调用接口。
	/// </summary>
	public static SpawnSystem Instance { get; private set; }

	// === 策划配置数据 ===
	/// <summary>
	/// 全局生成配置，包含所有敌人生成规则。
	/// 可以直接在编辑器分配，也可以在代码中动态配置。
	/// </summary>
	[Export] public SpawnConfig Config { get; set; } = new();

	// === 实时运行状态 ===
	/// <summary> 当前进行的波次索引（从 1 开始），-1 表示尚未开始 </summary>
	public int CurrentWaveIndex { get; private set; } = -1;

	/// <summary> 标识当前是否正处于战斗波次中 </summary>
	public bool IsWaveActive { get; private set; }

	// === 内部组件 ===
	private Timer _waveTimer;       // 控制波次总时长
	private Timer _checkTimer;      // 核心轮询计时器（替代大量独立的 Rule Timer）

	// 运行时状态跟踪 - 使用 struct 避免每波生成大量状态对象导致的 GC 压力
	private struct RuleRuntimeState
	{
		public EnemySpawnRule Rule;
		public float AccumulatedTime; // 累积时间
	}
	private List<RuleRuntimeState> _activeStates = new();

	/// <summary>
	/// 初始化系统：设置单例并初始化计时器。
	/// </summary>
	public override void _Ready()
	{
		Instance = this;

		// 初始化波次计时器
		_waveTimer = new Timer { Name = "WaveDurationTimer", OneShot = true };
		_waveTimer.Timeout += OnWaveTimeout;
		AddChild(_waveTimer);

		// 初始化核心检查计时器 (0.2s 循环)
		_checkTimer = new Timer { Name = "SpawnCheckTimer", WaitTime = 0.2f, OneShot = false };
		_checkTimer.Timeout += OnCheckTimerTimeout;
		AddChild(_checkTimer);

		// 监听游戏事件
		EventBus.GameStart += OnGameStart;
		EventBus.GameOver += OnGameOver;
		_log.Info("SpawnSystem (Single-Timer Architecture) 初始化完成");
	}

	/// <summary>
	/// 清理系统资源：解绑事件、清理单例、停止计时器。
	/// </summary>
	public override void _ExitTree()
	{
		// 解绑 EventBus 事件
		EventBus.GameStart -= OnGameStart;
		EventBus.GameOver -= OnGameOver;

		// 清理单例引用
		if (Instance == this)
			Instance = null;

		_waveTimer?.Stop();
		_checkTimer?.Stop();
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
	/// <param name="isVictory">是否胜利</param>
	public void OnGameOver(bool isVictory)
	{
		IsWaveActive = false;
		_waveTimer?.Stop();
		_checkTimer?.Stop();
		_activeStates.Clear();
		KillAllEnemies();
	}

	/// <summary>
	/// 开启指定索引的波次。
	/// </summary>
	/// <param name="waveIndex">波次索引 (1-based)</param>
	public void StartWave(int waveIndex)
	{
		if (Config == null) { _log.Error("SpawnConfig 未配置！"); return; }

		// 检查是否超过最大波次（如果 SpawnConfig.MaxWaves > 0）
		if (Config.MaxWaves > 0 && waveIndex > Config.MaxWaves)
		{
			_log.Info("已通过最大波次，触发游戏结束");
			EventBus.TriggerGameOver(true);
			return;
		}

		CurrentWaveIndex = waveIndex;
		IsWaveActive = true;

		// 1. 设置波次总时长计时器
		_waveTimer.WaitTime = Config.WaveDuration;
		_waveTimer.Start();

		// 2. 初始化规则状态
		_activeStates.Clear();

		// 安全检查：防止 SpawnRules 为 null
		if (Config.SpawnRules == null)
		{
			_log.Error("SpawnConfig 中的 SpawnRules 列表为空（null）！");
			return;
		}

		foreach (var rule in Config.SpawnRules)
		{
			if (IsRuleActiveForWave(rule, waveIndex))
			{
				// 按需验证：仅检查当前波次激活的规则是否有对应的对象池
				if (ObjectPoolManager.GetPool(rule.EnemyData.EnemyName) == null)
				{
					_log.Error($"波次 {waveIndex} 激活了规则 '{rule.EnemyData.EnemyName}'，但未找到对应的对象池！请检查预加载配置。");
					continue;
				}

				_activeStates.Add(new RuleRuntimeState
				{
					Rule = rule,
					// 首个敌人生成的延迟时间
					AccumulatedTime = rule.StartDelay > 0 ? -rule.StartDelay : 0
				});
			}
		}

		// 启动检查循环
		_checkTimer.Start();

		_log.Info($"波次 {waveIndex} 开始! 持续时间: {Config.WaveDuration}s, 激活规则数: {_activeStates.Count}");
		// 通过事件总线通知 UI 和其他系统
		EventBus.TriggerWaveStarted(waveIndex);
	}

	/// <summary>
	/// 波次超时回调：当波次时长到达时自动调用。
	/// </summary>
	private void OnWaveTimeout()
	{
		if (!IsWaveActive) return;
		IsWaveActive = false;

		_waveTimer.Stop();
		_checkTimer.Stop();
		_activeStates.Clear();

		_log.Info($"第 {CurrentWaveIndex}波进攻结束!");
		// 触发波次完成事件，通常用于开启商店界面或奖励选择
		EventBus.TriggerWaveCompleted(CurrentWaveIndex);
	}

	// ========================================
	// 核心生成逻辑 (单 Timer 驱动)
	// ========================================

	private void OnCheckTimerTimeout()
	{
		if (!IsWaveActive) return;

		float delta = (float)_checkTimer.WaitTime;

		// 使用 for 循环配合索引访问，因为 RuleRuntimeState 现在是 struct
		// struct 在 List 中是值存储，foreach 会产生副本，修改副本不会同步回 List
		for (int i = 0; i < _activeStates.Count; i++)
		{
			var state = _activeStates[i];

			// 累积时间
			state.AccumulatedTime += delta;

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
					SpawnBatch(count, state.Rule.EnemyData.DefaultStrategy);
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
	/// <param name="strategy">生成策略 (默认 Offscreen)</param>
	public void SpawnBatch(int count, SpawnPositionStrategy strategy = SpawnPositionStrategy.Offscreen)
	{
		// 1. 计算位置
		// 获取当前视口（Viewport），用于确定屏幕边界和相机位置，确保敌人能正确生成在玩家视野外
		var viewport = GetViewport();

		// 使用默认参数包进行计算。后续可根据 rule 或特定需求动态调整参数
		// SpawnPositionParams 是 struct，分配在栈上，无 GC 压力
		var spawnParams = new SpawnPositionParams();

		var positions = SpawnPositionCalculator.GetSpawnPositions(strategy, count, spawnParams, viewport);

		// 2. 循环生成
		// 修正：统一使用 PoolNames.EnemyPool，不再依赖传入的 enemyName
		var pool = ObjectPoolManager.GetPool(PoolNames.EnemyPool);
		if (pool == null)
		{
			_log.Error($"无法手动生成敌人：找不到对应的对象池 '{PoolNames.EnemyPool}'。");
			return;
		}

		// 尝试使用 ObjectPool 的 SpawnBatch (如果支持 Node 父节点处理)
		// 当前 ObjectPool<T> 有 Spawn(parent)，我们可以循环调用
		// 或者扩展 ObjectPool 增加 SpawnBatch(parent, count)

		// 这里手动循环 Spawn
		foreach (var pos in positions)
		{
			if (pool is ObjectPool<Node> nodePool)
			{
				var enemy = nodePool.Get();
				if (enemy is Node2D node2d) node2d.GlobalPosition = pos;
			}
		}
	}

	/// <summary>
	/// 基础生成接口：从对象池中取出一个敌人并放置到指定位置。
	/// </summary>
	public Node? SpawnEnemy(string enemyName, Vector2 position)
	{
		// 修正：忽略 enemyName，强制使用统一的 EnemyPool
		var pool = ObjectPoolManager.GetPool(PoolNames.EnemyPool);
		if (pool is ObjectPool<Node> nodePool)
		{
			var enemy = nodePool.Get();
			if (enemy is Node2D node2d) node2d.GlobalPosition = position;
			return enemy;
		}
		return null;
	}

	// 兼容旧接口，只需重定向参数名
	public void SpawnEnemyOffscreen(string enemyName, float distance = 50f)
	{
		SpawnBatch(1, SpawnPositionStrategy.Offscreen);
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
	private bool IsRuleActiveForWave(EnemySpawnRule rule, int waveIndex)
	{
		// 基础安全性检查：规则或关联的敌人数据不能为空
		if (rule?.EnemyData == null) return false;

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
		if (Config == null) return;

		// 遍历所有规则涉及的敌人类型
		foreach (var rule in Config.SpawnRules)
		{
			if (rule.EnemyData == null) continue;

			// 获取对应的对象池并执行全量回收
			var pool = ObjectPoolManager.GetPool(rule.EnemyData.EnemyName);
			if (pool is IObjectPool typedPool)
			{
				// ReleaseAll 会将所有从该池生成的活跃对象归还到池中
				typedPool.ReleaseAll();
			}
		}
		_log.Debug("已清理所有活跃敌人。");
	}
}
