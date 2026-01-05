using Godot;

/// <summary>
/// 敌人实体类（Scene 即 Entity）。
/// <para>
/// 职责：AI 驱动、对象池管理（IPoolable）、掉落逻辑。
/// 架构：与 Player 逻辑分离，通过组件（Component）复用共享行为。
/// </para>
/// </summary>
public partial class Enemy : CharacterBody2D, IEntity, IPoolable
{
    private static readonly Log _log = new("Enemy", LogLevel.Info);

    // ================= IEntity 实现 =================

    /// <summary>
    /// 动态数据容器
    /// </summary>
    public Data Data { get; private set; } = new Data();

    /// <summary>
    /// Entity唯一标识符
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();
        _log.Debug($"敌人 {Name} 初始化完成。");
    }

    public override void _ExitTree()
    {
        // 确保解绑事件
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            health.Died -= OnDied;
        }
        base._ExitTree();
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 当敌人死亡时触发。
    /// </summary>
    private void OnDied()
    {
        _log.Info($"{Name} 死亡。归还对象池。");

        // 触发全局事件 (掉落、统计等)
        EventBus.TriggerEnemyDied(this, GlobalPosition);

        // 归还对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    /// <summary>
    /// [EntityFactory 回调] 当实体被生成并注入数据后调用。
    /// 可用于执行某些依赖于 Data 的立即初始化逻辑。
    /// </summary>
    public void OnSpawn(EnemyResource resource)
    {
        // 确保核心组件已挂载
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            // 防止重复绑定
            health.Died -= OnDied;
            health.Died += OnDied;
        }
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用 (Active)。
    /// </summary>
    public void OnPoolAcquire()
    {
        // 重新激活组件
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            // 确保事件绑定
            health.Died -= OnDied;
            health.Died += OnDied;
        }
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用 (Deactive)。
    /// 核心职责：清理状态、重置数据。
    /// </summary>
    public void OnPoolRelease()
    {
        // 1. 级联重置子组件 (Cascading Reset)
        EntityManager.GetComponent<HealthComponent>(this)?.Reset();

        // 2. 重置自身状态
        Velocity = Vector2.Zero;

        // 3. 清空 Data
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
