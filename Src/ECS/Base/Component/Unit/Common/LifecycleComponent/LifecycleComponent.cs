using Godot;
using System;

// ================= 枚举定义 =================

/// <summary>
/// 生命周期状态
/// </summary>
public enum LifecycleState
{
    Alive,      // 存活
    Dead,       // 已死亡
    Reviving,   // 复活中
}

/// <summary>
/// 死亡类型
/// </summary>
public enum DeathType
{
    Normal,     // 普通死亡（敌人）
    Hero,       // 英雄死亡（可复活）
    Instant,    // 瞬间死亡（不可复活）
    Summon,     // 召唤物过期
}

/// <summary>
/// 生命周期组件 - 单位生命周期状态机
///
/// 核心职责：
/// - 管理生命周期状态（Alive/Dead/Reviving）
/// - 监听 HP 变化触发死亡判定
/// - 管理存活时间（召唤物自动过期）
/// - 提供 Kill() 和 Revive() 方法
/// - 分发事件：Dead、Reviving、Revived
///
/// 设计原则：
/// - 单一职责：只管理生命周期状态
/// - 不直接修改 HP：委托 HealthComponent
/// - 不直接管理状态标记：通过 Data 系统 (DataKey.IsDead 等)
/// - 事件驱动：通过 EventBus 通知状态变化
/// </summary>
public partial class LifecycleComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(LifecycleComponent));

    // ================= 状态 =================

    /// <summary> 当前生命周期状态 </summary>
    public LifecycleState State => _data.Get<LifecycleState>(DataKey.LifecycleState);

    /// <summary> 死亡类型 </summary>
    public DeathType DeathType => _data.Get<DeathType>(DataKey.DeathType);

    /// <summary> 是否可以复活 </summary>
    public bool CanRevive => _data.Get<bool>(DataKey.CanRevive);

    /// <summary> 死亡次数 </summary>
    public int DeathCount => _data.Get<int>(DataKey.DeathCount);

    /// <summary> 最大生存时间（秒），-1 表示永久 </summary>
    public float MaxLifeTime => _data.Get<float>(DataKey.MaxLifeTime);

    // ================= 配置 =================

    /// <summary> 复活所需时间（秒） </summary>
    public float ReviveDuration { get; set; } = GlobalConfig.HeroReviveTime;

    /// <summary> 复活后无敌时间（秒） </summary>
    public float ReviveInvulnerabilityDuration { get; set; } = GlobalConfig.ReviveinvulnerableTime;

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    /// <summary> 生命周期计时器：用于召唤物等限时单位 </summary>
    private GameTimer? _lifeTimer;
    /// <summary> 复活计时器：用于英雄复活倒计时 </summary>
    private GameTimer? _reviveTimer;
    /// <summary> 普通单位死亡动画结束后延迟销毁计时器 </summary>
    private GameTimer? _deathLingerTimer;
    /// <summary> 单位原始碰撞层，用于复活后恢复 </summary>
    private uint _originalCollisionLayer;

    // ================= IComponent =================

    /// <summary>
    /// 当组件注册到实体时调用。
    /// 初始化组件依赖、绑定事件并设置初始生命周期状态。
    /// </summary>
    /// <param name="entity">持有此组件的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;
        }

        // ✅ 全局监听 Kill 事件（通过 Victim 筛选是否是自己）
        GlobalEventBus.Global.On<GameEventType.Unit.KilledEventData>(
            GameEventType.Unit.Killed, OnUnitKilled);

        // ✅ 监听数据变化事件（处理 Spawn 后动态设置 MaxLifeTime 的场景）
        _entity?.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged, OnDataChanged);

        // ✅ 监听动画播放完毕事件（Hero 死亡动画结束后启动复活，普通单位延迟销毁）
        _entity?.Events.On<GameEventType.Unit.AnimationFinishedEventData>(
            GameEventType.Unit.AnimationFinished, OnAnimationFinished);

        // 初始化状态为 Alive，确保单位生成后立即可用
        ChangeState(LifecycleState.Alive);

        // ✅ 主动检查并启动计时器（处理配置预设 MaxLifeTime 的场景）
        UpdateLifeTimer();
    }

    /// <summary>
    /// 数据变化事件处理：响应 MaxLifeTime 变化
    /// </summary>
    private void OnDataChanged(GameEventType.Data.PropertyChangedEventData data)
    {
        if (data.Key == DataKey.MaxLifeTime)
        {
            UpdateLifeTimer();
        }
    }

    /// <summary>
    /// 更新生命周期计时器
    /// 根据当前 MaxLifeTime 值决定是否启动/更新/取消计时器
    /// 支持运行时动态修改 MaxLifeTime（如延长/缩短召唤物寿命的 Buff）
    /// </summary>
    private void UpdateLifeTimer()
    {
        // 取消旧计时器
        _lifeTimer?.Cancel();
        _lifeTimer = null;

        // 根据当前 MaxLifeTime 决定是否启动新计时器
        if (MaxLifeTime > 0)
        {
            _lifeTimer = TimerManager.Instance?.Delay(MaxLifeTime)
                .OnComplete(() => Kill(DeathType.Summon));
            _log.Debug($"启动生命周期计时器: {MaxLifeTime}s");
        }
    }

    /// <summary>
    /// 当组件从实体注销时调用。
    /// 负责清理事件监听和正在运行的计时器，防止内存泄漏。
    /// </summary>
    public void OnComponentUnregistered()
    {
        // Cancel global event subscription
        GlobalEventBus.Global.Off<GameEventType.Unit.KilledEventData>(
            GameEventType.Unit.Killed, OnUnitKilled);

        // 取消计时器
        _lifeTimer?.Cancel();
        _reviveTimer?.Cancel();
        _deathLingerTimer?.Cancel();
        _lifeTimer = null;
        _reviveTimer = null;
        _deathLingerTimer = null;

        _entity = null;
        _data = null;
    }

    // ================= 状态查询 =================

    /// <summary> 
    /// 检查单位当前是否处于存活状态。
    /// 只有处于 Alive 状态的单位才能进行攻击、移动等行为。
    /// </summary>
    public bool StateIsAlive() => State == LifecycleState.Alive;

    /// <summary> 
    /// 检查单位是否已死亡。
    /// 死亡状态的单位通常不参与碰撞和 AI 决策。
    /// </summary>
    public bool StateIsDead() => State == LifecycleState.Dead;

    /// <summary> 
    /// 检查单位是否正处于复活倒计时流程中。
    /// </summary>
    public bool StateIsReviving() => State == LifecycleState.Reviving;



    // ================= 单位被击杀事件监听 =================

    /// <summary>
    /// 当 HealthComponent 判定 HP<=0 后的回调。
    /// 执行死亡流程。
    /// </summary>
    private void OnUnitKilled(GameEventType.Unit.KilledEventData data)
    {
        // 全局事件筛选：只处理自己被击杀的事件
        if (data.Victim != _entity) return;

        if (StateIsAlive())
        {
            // 使用事件中的死亡类型，如果未指定则使用组件默认值
            Kill(data.DeathType);
        }
    }

    // ================= 状态机 =================

    /// <summary>
    /// 内部状态切换方法。
    /// 负责更新 State 属性并向事件总线广播状态变化事件。
    /// </summary>
    /// <param name="newState">目标状态</param>
    private void ChangeState(LifecycleState newState)
    {
        if (State == newState) return;

        var oldState = State;
        // ✅ 通过 Data 修改状态（符合纯数据驱动规范）
        _data.Set(DataKey.LifecycleState, newState);

        _log.Debug($"状态变化: {oldState} -> {newState}");

        // 触发生命周期状态变化事件，方便其他系统（如 UI、动画、AI）响应
        _entity?.Events.Emit(GameEventType.Unit.StateChanged,
            new GameEventType.Unit.StateChangedEventData(
                "LifecycleState", oldState.ToString(), newState.ToString()));
    }

    // ================= 核心方法 =================

    /// <summary>
    /// 执行单位死亡逻辑。
    /// 负责进入 Dead 状态、标记 Data 属性、同步 HP 以及触发相关事件。
    /// 如果是普通死亡（非英雄），还会自动销毁 Entity。
    /// </summary>
    /// <param name="deathType">死亡原因/类型</param>
    public void Kill(DeathType deathType = DeathType.Normal)
    {
        // 只有存活状态的单位才能被杀死，防止重复触发死亡逻辑
        if (!StateIsAlive()) return;

        // ✅ 通过 Data 记录死亡类型（符合纯数据驱动规范）
        _data?.Set(DataKey.DeathType, deathType);
        _data?.Add(DataKey.DeathCount, 1);
        ChangeState(LifecycleState.Dead);

        // 在 Data 容器中同步死亡标记，供无状态系统（如渲染器）查询
        _data?.Set(DataKey.IsDead, true);

        // 将 HP 归零
        _data?.Set(DataKey.CurrentHp, 0f);

        _log.Info($"单位死亡, 类型: {deathType}");

        // 向实体局部事件总线也发送 Killed 事件，让 UnitAnimationComponent 能收到并播放死亡动画
        _entity?.Events.Emit(GameEventType.Unit.Killed,
            new GameEventType.Unit.KilledEventData(
                Victim: _entity,
                Killer: null,
                DeathType: deathType));

        // 根据死亡类型决定后续行为
        switch (deathType)
        {
            case DeathType.Hero:
                // 英雄死亡：状态保持 Dead，等待 OnAnimationFinished 回调再启动复活
                break;

            case DeathType.Instant:
                // 瞬间死亡：跳过动画直接销毁
                DestroyEntity();
                break;

            case DeathType.Normal:
            case DeathType.Summon:
            default:
                // 普通死亡：启动保底计时器（防止没有 dead 动画时永远不被销毁）
                // 有 dead 动画时，OnAnimationFinished 会取消此计时器并重启 0.5 秒短计时器
                _deathLingerTimer = TimerManager.Instance?.Delay(GlobalConfig.EnemyDeathLingerTime)
                    .OnComplete(DestroyEntity);
                break;
        }
    }

    /// <summary>
    /// 将实体标记为死亡状态并执行销毁流程。
    /// 适用于普通、瞬间、召唤物等非英雄死亡类型。
    /// </summary>
    private void DestroyEntity()
    {
        // 进入最终死亡状态，禁止任何后续交互
        ChangeState(LifecycleState.Dead);

        // 通过 EntityManager 统一回收或释放节点，保证对象池与节点树一致性
        if (_entity is Node entityNode)
        {
            _log.Debug($"实体已销毁，死亡类型：{DeathType}");
            EntityManager.Destroy(entityNode);
        }
    }

    /// <summary>
    /// 动画播放完毕回调（由 UnitAnimationComponent 发出，携带动画名）。
    /// - dead 动画 + Hero：进入 Dead/Reviving 流程
    /// - dead 动画 + 普通单位：延迟 0.5 秒后销毁
    /// </summary>
    private void OnAnimationFinished(GameEventType.Unit.AnimationFinishedEventData data)
    {
        // 只处理 dead 动画
        if (data.AnimName != Anim.Dead) return;

        if (State != LifecycleState.Dead) return;

        if (DeathType == DeathType.Hero)
        {
            StartRevive();
        }
        else
        {
            // 普通单位：dead 动画播完，立即进入 Dead 状态锁住动画，取消保底计时器，0.5 秒后销毁
            ChangeState(LifecycleState.Dead);
            _deathLingerTimer?.Cancel();
            _deathLingerTimer = TimerManager.Instance?.Delay(0.5f)
                .OnComplete(DestroyEntity);
        }
    }

    /// <summary>
    /// 启动复活流程。
    /// 切换到 Reviving 状态，并启动计时器在持续时间内逐步恢复血量。
    /// </summary>
    private void StartRevive()
    {
        ChangeState(LifecycleState.Reviving);

        // 广播复活开始事件，供 UI 显示复活进度条等
        _entity?.Events.Emit(GameEventType.Unit.Reviving,
            new GameEventType.Unit.RevivingEventData(ReviveDuration));

        // 启动复活计时器，每 0.1 秒执行一次进度回调
        _reviveTimer = TimerManager.Instance?.Countdown(ReviveDuration, 0.1f)
            .OnCountdown((elapsed, progress) =>
            {
                // 随着复活进度增加，逐步恢复血量并触发 HealthChanged 事件让血条 UI 更新
                float maxHp = _data.Get<float>(DataKey.FinalHp);
                float newHp = maxHp * progress;
                float oldHp = _data.Get<float>(DataKey.CurrentHp);
                _data.Set(DataKey.CurrentHp, newHp);
                _entity?.Events.Emit(GameEventType.Data.HealthChanged,
                    new GameEventType.Data.HealthChangedEventData(oldHp, newHp));
            })
            .OnComplete(CompleteRevive); // 计时结束，完成复活
    }

    /// <summary>
    /// 完成复活。
    /// 重置各项状态、恢复满血、应用复活无敌并回到 Alive 状态。
    /// </summary>
    private void CompleteRevive()
    {
        // 1. 恢复至满血，触发 HealthChanged 事件通知 UI 更新
        float oldHp = _data.Get<float>(DataKey.CurrentHp);
        float fullHp = _data.Get<float>(DataKey.FinalHp);
        _data.Set(DataKey.CurrentHp, fullHp);
        _entity?.Events.Emit(GameEventType.Data.HealthChanged,
            new GameEventType.Data.HealthChangedEventData(oldHp, fullHp));

        // 2. 清除死亡标记
        _data.Set(DataKey.IsDead, false);

        // 3. 应用复活后的短暂无敌保护（防止复活瞬间被围攻致死）
        if (ReviveInvulnerabilityDuration > 0)
        {
            _data?.Set(DataKey.IsInvulnerable, true);
            TimerManager.Instance?.Delay(ReviveInvulnerabilityDuration)
                .OnComplete(() => _data?.Set(DataKey.IsInvulnerable, false));
        }

        // 4. 回到存活状态
        ChangeState(LifecycleState.Alive);

        // 5. 广播复活完成事件
        _entity?.Events.Emit(GameEventType.Unit.Revived,
            new GameEventType.Unit.RevivedEventData());
        _log.Info("单位复活完成");
    }

    /// <summary>
    /// 立即强制复活（由外部系统或技能调用）。
    /// 跳过复活倒计时，直接执行完成复活逻辑。
    /// </summary>
    public void Revive()
    {
        // 只有处于死亡状态的单位才能复活
        if (State != LifecycleState.Dead) return;

        // 如果已经在复活中，取消当前的复活计时器
        _reviveTimer?.Cancel();
        _reviveTimer = null;

        CompleteRevive();
    }
}
