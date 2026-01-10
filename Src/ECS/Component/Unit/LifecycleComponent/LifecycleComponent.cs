using Godot;
using System;

// ================= 枚举定义 =================

/// <summary>
/// 生命周期状态
/// </summary>
public enum LifecycleState
{
    Spawning,   // 生成中
    Alive,      // 存活
    Dying,      // 死亡中（播放死亡动画）
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
/// - 管理生命周期状态（Spawning/Alive/Dying/Dead/Reviving）
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
    private static readonly Log _log = new("LifecycleComponent");

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
    public float ReviveDuration { get; set; } = Config.HeroReviveTime;

    /// <summary> 复活后无敌时间（秒） </summary>
    public float ReviveInvulnerabilityDuration { get; set; } = Config.ReviveinvulnerableTime;

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    /// <summary> 生命周期计时器：用于召唤物等限时单位 </summary>
    private GameTimer? _lifeTimer;
    /// <summary> 复活计时器：用于英雄复活倒计时 </summary>
    private GameTimer? _reviveTimer;

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

        // 监听 HP 变化：当血量降至 0 时自动触发死亡判定
        _entity?.Events.On<GameEventType.Data.HealthChangedEventData>(
            GameEventType.Data.HealthChanged, IsUnitDeath);

        // 监听死亡事件：处理销毁逻辑
        // _entity?.Events.On<GameEventType.Unit.DeadEventData>(
        //     GameEventType.Unit.Dead, OnUnitDead);

        // 初始化状态为 Alive，确保单位生成后立即可用
        ChangeState(LifecycleState.Alive);

        // 处理有时限的单位（如召唤物）：启动销毁倒计时
        if (MaxLifeTime > 0)
        {
            _lifeTimer = TimerManager.Instance?.Delay(MaxLifeTime)
                .OnComplete(() => Kill(DeathType.Summon));
        }
    }

    /// <summary>
    /// 当组件从实体注销时调用。
    /// 负责清理事件监听和正在运行的计时器，防止内存泄漏。
    /// </summary>
    public void OnComponentUnregistered()
    {

        // 停止所有活跃的计时器（生命周期或复活倒计时）
        _lifeTimer?.Cancel();
        _reviveTimer?.Cancel();
        _lifeTimer = null;
        _reviveTimer = null;

        _entity = null;
        _data = null;
    }

    /// <summary>
    /// 重置组件状态（主要用于对象池复用时）。
    /// 将状态恢复为初始值并重新启动必要的计时器。
    /// </summary>
    public void OnComponentReset()
    {
        // 清理旧的计时器，防止复用后产生冲突
        _reviveTimer?.Cancel();
        _lifeTimer?.Cancel();
        _reviveTimer = null;
        _lifeTimer = null;

        // 重新启动生命计时器（如果是召唤物）
        if (MaxLifeTime > 0)
        {
            _lifeTimer = TimerManager.Instance?.Delay(MaxLifeTime)
                .OnComplete(() => Kill(DeathType.Summon));
        }
    }

    // ================= 状态查询 =================

    /// <summary> 
    /// 检查单位当前是否处于存活状态。
    /// 只有处于 Alive 状态的单位才能进行攻击、移动等行为。
    /// </summary>
    public bool StateIsAlive() => State == LifecycleState.Alive;

    /// <summary> 
    /// 检查单位是否已死亡或正在播放死亡动画。
    /// 死亡状态的单位通常不参与碰撞和 AI 决策。
    /// </summary>
    public bool StateIsDead() => State == LifecycleState.Dead || State == LifecycleState.Dying;

    /// <summary> 
    /// 检查单位是否正处于复活倒计时流程中。
    /// </summary>
    public bool StateIsReviving() => State == LifecycleState.Reviving;



    // ================= HP 变化监听 =================

    /// <summary>
    /// 当 HealthComponent 的 HP 发生变化时的回调。
    /// 用于检测血量是否耗尽并触发死亡逻辑。
    /// </summary>
    private void IsUnitDeath(GameEventType.Data.HealthChangedEventData data)
    {
        // 如果 HP 降到 0 或以下且当前仍被视为存活，则触发 Kill
        if (data.NewHp <= 0 && StateIsAlive())
        {
            Kill(DeathType);
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
        _data?.Set(DataKey.LifecycleState, newState);

        _log.Debug($"状态变化: {oldState} -> {newState}");

        // 触发生命周期状态变化事件，方便其他系统（如 UI、动画、AI）响应
        _entity?.Events.Emit(GameEventType.Unit.StateChanged,
            new GameEventType.Unit.StateChangedEventData(
                "LifecycleState", oldState.ToString(), newState.ToString()));
    }

    // ================= 核心方法 =================

    /// <summary>
    /// 执行单位死亡逻辑。
    /// 负责进入 Dying/Dead 状态、标记 Data 属性、同步 HP 以及触发相关事件。
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
        ChangeState(LifecycleState.Dying);

        // 在 Data 容器中同步死亡标记，供无状态系统（如渲染器）查询
        _data?.Set(DataKey.IsDead, true);

        // 强制将 HP 归零，但不触发 HealthChanged 事件（避免死循环）
        _data?.Set(DataKey.CurrentHp, 0f);

        // TODO 暂时没用
        // 广播死亡事件，携带死亡类型信息
        // 具体的销毁或复活逻辑由 OnUnitDead 事件回调处理，实现逻辑解耦
        _entity?.Events.Emit(GameEventType.Unit.Dead,
            new GameEventType.Unit.DeadEventData(deathType));
        _log.Info($"单位死亡, 类型: {deathType}");

        // 根据死亡类型决定后续行为：是直接移除还是尝试复活
        switch (deathType)
        {
            case DeathType.Hero:
                // 英雄死亡：如果允许复活则进入复活流程
                if (CanRevive)
                {
                    StartRevive();
                }
                else
                {
                    ChangeState(LifecycleState.Dead);
                }
                break;

            case DeathType.Instant:
            case DeathType.Normal:
            case DeathType.Summon:
            default:
                DestroyEntity();
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
            EntityManager.Destroy(entityNode);
            _log.Debug($"实体已销毁，死亡类型：{DeathType}");
        }
    }

    /// <summary>
    /// 处理单位死亡事件。
    /// 根据死亡类型决定是进入复活流程还是直接销毁 Entity。
    /// </summary>
    private void OnUnitDead(GameEventType.Unit.DeadEventData data)
    {

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

        // TODO 复活逻辑
        // 启动复活计时器，每 0.1 秒执行一次进度回调
        // _reviveTimer = TimerManager.Instance?.Countdown(ReviveDuration, 0.1f)
        //     .OnCountdown((elapsed, progress) =>
        //     {
        //         // 随着复活进度增加，视觉上渐进恢复血量
        //         if (_health != null)
        //         {
        //             float maxHp = _health.MaxHp;
        //             // 使用 SetHp 直接设置，不触发事件（避免死亡判定循环）
        //             _health.SetHp(maxHp * progress, triggerEvent: false);
        //         }
        //     })
        //     .OnComplete(CompleteRevive); // 计时结束，完成复活
    }

    /// <summary>
    /// 完成复活。
    /// 重置各项状态、恢复满血、应用复活无敌并回到 Alive 状态。
    /// </summary>
    private void CompleteRevive()
    {
        // 1. 恢复至满血，这次触发事件以通知 UI 更新
        _data?.Set(DataKey.CurrentHp, _data.Get<float>(DataKey.FinalHp));

        // 2. 清除死亡标记
        _data?.Set(DataKey.IsDead, false);

        // 3. 应用复活后的短暂无敌保护
        // if (ReviveInvulnerabilityDuration > 0)
        // {
        //     _data?.Set(DataKey.IsInvulnerable, true);
        //     TimerManager.Instance?.Delay(ReviveInvulnerabilityDuration)
        //         .OnComplete(() => _data?.Set(DataKey.IsInvulnerable, false));
        // }

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
        // 只有处于死亡或死亡中状态的单位才能复活
        if (State != LifecycleState.Dead && State != LifecycleState.Dying) return;

        // 如果已经在复活中，取消当前的复活计时器
        _reviveTimer?.Cancel();
        _reviveTimer = null;

        CompleteRevive();
    }
}
