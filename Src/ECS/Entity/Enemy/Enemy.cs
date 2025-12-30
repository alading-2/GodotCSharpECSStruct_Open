using Godot;
using System;

/// <summary>
/// 敌人实体类。
/// <para>
/// 核心职责：
/// 1. 继承 Entity 基类，获得组件管理能力。
/// 2. 实现 IPoolable 接口，管理对象池生命周期。
/// 3. 负责在重置时级联重置子组件。
/// </para>
/// </summary>
public partial class Enemy : CharacterBody2D, IPoolable
{
    private static readonly Log _log = new("Enemy", LogLevel.Info);

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        _log.Debug($"敌人 {Name} 初始化完成。");
    }

    public override void _ExitTree()
    {
        // 确保解绑事件
        var health = this.Component().HealthComponent;
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
        // 确保核心组件已挂载（双重保险，通常由 Factory 负责添加）
        // 如果是代码动态添加，这里可以进行组件的事件绑定
        var health = this.GetComponent<HealthComponent>(ECSIndex.Component.HealthComponent);
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
        var health = this.GetComponent<HealthComponent>(ECSIndex.Component.HealthComponent);
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
        // 使用 GetComponent 动态获取，不再依赖字段缓存
        this.Component().HealthComponent?.Reset();
        this.Component().AttributeComponent?.Reset();

        // 2. 重置自身状态
        Velocity = Vector2.Zero;

        // 3. 清空 Data
        this.GetData().Clear();
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
