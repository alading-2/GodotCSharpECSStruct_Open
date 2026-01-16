using Godot;

/// <summary>
/// Entity 标准模板
/// 
/// ┌─────────────────────────────────────────────────────────────────┐
/// │ 【对象池版本】Enemy、Bullet、Item 等高频创建销毁的实体          │
/// │   - 实现 IPoolable 接口                                         │
/// │   - 配置 UsingObjectPool = true                                 │
/// │   - 生命周期：OnPoolAcquire → 业务逻辑 → OnPoolRelease          │
/// ├─────────────────────────────────────────────────────────────────┤
/// │ 【非对象池版本】Player、Boss 等单例或低频实体                   │
/// │   - 移除 IPoolable 接口和相关方法                               │
/// │   - 配置 UsingObjectPool = false                                │
/// │   - 统一使用 EntityManager.Spawn/Destroy 管理                   │
/// └─────────────────────────────────────────────────────────────────┘
/// </summary>
public partial class TemplateEntity : CharacterBody2D, IEntity, IPoolable
{
    private static readonly Log _log = new("TemplateEntity");

    #region ================= IEntity 实现 =================

    /// <summary>动态数据容器</summary>
    public Data Data { get; private set; }

    /// <summary>实体局部事件总线</summary>
    public EventBus Events { get; } = new EventBus();

    /// <summary>Entity 唯一标识符</summary>
    public string EntityId { get; private set; } = string.Empty;

    public TemplateEntity()
    {
        Data = new Data(this);
    }

    #endregion

    #region ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();

        // 注意: 通过 EntityManager.Spawn 创建的实体会自动注册
        // 只有直接放置在场景中的物体才需要手动调用 EntityManager.Register(this, "Type");

        _log.Debug($"{Name} 初始化完成");
    }

    public override void _ExitTree()
    {
        // 自动注销逻辑由 EntityManager.Destroy 处理
        // 如果是直接 QueueFree 的，建议调用 EntityManager.UnregisterEntity(this) 以策安全

        base._ExitTree();
    }

    #endregion

    #region ================= IPoolable 接口（对象池版本，非对象池版本删除此区域）=================

    /// <summary>
    /// 从对象池取出时调用
    /// ✅ 在此订阅事件(EntityManager已自动清空)
    /// </summary>
    public void OnPoolAcquire()
    {
        // 示例:订阅全局 Kill 事件（通过 Victim 筛选是否是自己）
        GlobalEventBus.Global.On<GameEventType.Global.UnitKilledEventData>(
            GameEventType.Global.UnitKilled, OnKilled);
        // 示例:订阅局部事件（仅在实体内部组件间通信）
        Events.On<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);
    }

    /// <summary>
    /// 归还对象池时调用
    /// ✅ 仅重置自身状态(非Data/Component管理的状态)
    /// ❌ 无需手动清理Events/Data/Component(EntityManager.Destroy已自动处理)
    /// </summary>
    public void OnPoolRelease()
    {
        // 取消全局事件订阅
        GlobalEventBus.Global.Off<GameEventType.Global.UnitKilledEventData>(
            GameEventType.Global.UnitKilled, OnKilled);
        // 示例:重置物理状态
        Velocity = Vector2.Zero;
    }

    /// <summary>
    /// 当归还池时重置数据重置
    /// 在 OnPoolRelease 之后调用,专门用于将数据恢复为默认值
    /// </summary>
    public void OnPoolReset()
    {
        // 通常留空
    }

    #endregion

    #region ================= 事件处理 =================

    private void OnKilled(GameEventType.Global.UnitKilledEventData evt)
    {
        // 全局事件筛选：只处理自己被击杀的事件
        if (evt.Victim as Node != this) return;
        _log.Info($"{Name} 死亡, 类型: {evt.DeathType}");
        // 处理死亡逻辑...
    }

    private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
    {
        _log.Info($"{Name} 受到 {evt.Amount} 点伤害");
        // 处理受伤逻辑
    }

    #endregion

    #region ================= 业务逻辑 =================

    // 在此添加业务逻辑

    /// <summary>
    /// Spawn 后的初始化示例(由 System 或其他逻辑调用)
    /// </summary>
    /// <param name="skillLevel">运行时数据: 技能等级</param>
    /// <param name="target">运行时数据: 目标</param>
    public void InitAfterSpawn(int skillLevel, IEntity target)
    {
        // ✅ Spawn 之后设置的运行时数据
        // 这些数据会触发 PropertyChanged 事件,Component 可以监听响应
        // 这种"Spawn -> Configure"模式是标准做法
        Data.Set("SkillLevel", skillLevel); // 建议使用 DataKey.SkillLevel
        Data.Set("Target", target);         // 建议使用 DataKey.Target
    }

    #endregion
}
