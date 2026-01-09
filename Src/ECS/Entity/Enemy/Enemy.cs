using Godot;

/// <summary>
/// 敌人实体类（Scene 即 Entity）。
/// <para>
/// 职责：AI 驱动、对象池管理（IPoolable）、掉落逻辑。
/// 架构：与 Player 逻辑分离，通过组件（Component）复用共享行为。
/// </para>
/// </summary>
public partial class Enemy : CharacterBody2D, IPoolable, IUnit
{
    private static readonly Log _log = new("Enemy", LogLevel.Info);

    // ================= IEntity 实现 =================

    /// <summary>
    /// 动态数据容器
    /// </summary>
    public Data Data { get; private set; }

    public Enemy()
    {
        Data = new Data(this);
    }

    /// <summary>
    /// 实体局部事件总线
    /// </summary>
    public EventBus Events { get; } = new EventBus();

    /// <summary>
    /// Entity唯一标识符
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    // 1: Enemy
    public int FactionId => 1;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();

        _log.Debug($"敌人 {Name} 初始化完成。");
    }

    public override void _ExitTree()
    {
        Events.Clear();
        base._ExitTree();
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 当敌人死亡时触发。
    /// </summary>
    private void OnDied(GameEventType.Unit.DeadEventData evt)
    {
        _log.Info($"{Name} 死亡。归还对象池。");

        // 触发全局事件 (掉落、统计等)
        GlobalEventBus.TriggerEnemyDied(this, GlobalPosition);

        // 归还对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    /// <summary>
    /// [EntityFactory 回调] 当实体被生成并注入数据后调用。
    /// 可用于执行某些依赖于 Data 的立即初始化逻辑。
    /// </summary>
    public void OnSpawn(EnemyResource resource)
    {
        // OnSpawn 不需要订阅事件，OnPoolAcquire 已统一处理
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用 (Active)。
    /// 统一在此处订阅事件，确保对象池复用时事件正确绑定。
    /// </summary>
    public void OnPoolAcquire()
    {
        // 先解绑再订阅，避免重复订阅
        Events.Off<GameEventType.Unit.DeadEventData>(GameEventType.Unit.Dead, OnDied);
        Events.On<GameEventType.Unit.DeadEventData>(GameEventType.Unit.Dead, OnDied);
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用 (Deactive)。
    /// 核心职责：清理状态、重置数据。
    /// </summary>
    public void OnPoolRelease()
    {
        // 1. 清理事件
        Events.Clear();

        // 2. 级联重置子组件 (Cascading Reset)
        EntityManager.GetComponent<HealthComponent>(this)?.Reset();

        // 3. 重置自身状态
        Velocity = Vector2.Zero;

        // 4. 清空 Data
        Data.Clear();
    }

    /// <summary>
    /// [IPoolable] 彻底重置
    /// </summary>
    public void OnPoolReset()
    {
        // 可以在这里移除所有动态添加的组件，如果需要的话
        // 但通常为了复用，我们保留组件结构，只重置数据
    }
}
