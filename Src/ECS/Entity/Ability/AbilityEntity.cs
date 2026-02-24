using Godot;

/// <summary>
/// 技能实体 - 每个技能都是独立的实体
/// 
/// 设计理念:
/// - 技能是 Entity，实现 IEntity 接口
/// - 业务逻辑归 Component (Cooldown, Trigger, Charge 等)
/// - 效果执行归 AbilityEffect 执行器
/// - 支持对象池复用（实现 IPoolable）
/// </summary>
public partial class AbilityEntity : Node2D, IEntity, IPoolable
{
    private static readonly Log _log = new(nameof(AbilityEntity));

    // ================= IEntity 实现 =================

    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();
    // EntityId 由 IEntity 默认实现（从 DataKey.Id 读取）

    // ================= 构造函数 =================

    public AbilityEntity()
    {
        Data = new Data(this);
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
    }

    public override void _ExitTree()
    {
        // 统一注销 (内部自动清理 Data 和 Events)
        EntityManager.UnregisterEntity(this);
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用 (Active)。
    /// 统一在此处订阅事件，确保对象池复用时事件正确绑定。
    /// </summary>
    public void OnPoolAcquire()
    {
        // 对象池复用时的初始化
        // Data 和 Events 的清理/重置由 EntityManager.UnregisterEntity 处理
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用 (Deactive)。
    /// 核心职责：清理状态、重置数据。
    /// </summary>
    public void OnPoolRelease()
    {
        // 注意：Events.Clear(), Data.Clear()
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
