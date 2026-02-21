using Godot;

/// <summary>
/// 玩家实体类（Scene 即 Entity）。
/// <para>
/// 职责：输入处理、升级系统、技能管理。
/// 架构：单例常驻，与 Enemy 逻辑分离，通过组件（Component）复用共享行为。
/// </para>
/// </summary>
public partial class PlayerEntity : CharacterBody2D, IUnit
{
    private static readonly Log _log = new(nameof(PlayerEntity));

    // ================= IEntity 实现 =================

    /// <summary>
    /// 动态数据容器
    /// </summary>
    public Data Data { get; private set; }

    public PlayerEntity()
    {
        Data = new Data(this);
    }

    /// <summary>
    /// 实体局部事件总线
    /// </summary>
    public EventBus Events { get; } = new EventBus();

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用 (Active)。
    /// 统一在此处订阅事件，确保对象池复用时事件正确绑定。
    /// </summary>
    public void OnPoolAcquire()
    {
        // 直接订阅即可（EntityManager 已自动清空事件）
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用 (Deactive)。
    /// 核心职责：清理状态、重置数据。
    /// </summary>
    public void OnPoolRelease()
    {
        // 1. 重置自身状态 (仅重置非 Data/Component 管理的状态)
        Velocity = Vector2.Zero;

        // 注意：Events.Clear(), Data.Clear(), Component.OnComponentUnregistered()
        // 均由 EntityManager.Destroy() -> UnregisterEntity() 统一处理
    }

    /// <summary>
    /// [IPoolable] 当归还池时重置
    /// </summary>
    public void OnPoolReset()
    {
        // 可以在这里移除所有动态添加的组件，如果需要的话
        // 但通常为了复用，我们保留组件结构，只重置数据
    }
}
