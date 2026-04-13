using Godot;

/// <summary>
/// 投射物实体 - 通用运动碰撞实体（Area2D 类型）
/// 
/// 职责：
/// - 提供物理碰撞感知（通过 CollisionComponent 转发 Area2D 信号）
/// - 提供运动驱动（通过 EntityMovementComponent 执行各种轨迹策略）
/// - 不包含任何业务逻辑，由技能执行器通过事件订阅处理命中效果
/// 
/// 使用方式：
/// 1. EntityManager.Spawn 生成
/// 2. 订阅 MovementCollision 事件处理命中
/// 3. 发送 MovementStarted 事件启动轨迹
/// 4. 配置 DestroyOnCollision / DestroyOnComplete 自动回收
/// </summary>
public partial class ProjectileEntity : Area2D, IEntity, IPoolable
{
    private static readonly Log _log = new(nameof(ProjectileEntity));

    // ================= IEntity 实现 =================

    /// <summary>动态数据容器</summary>
    public Data Data { get; private set; }

    /// <summary>实体局部事件总线</summary>
    public EventBus Events { get; } = new EventBus();

    // ================= 构造函数 =================

    public ProjectileEntity()
    {
        Data = new Data(this);
        Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
    }

    public override void _ExitTree()
    {
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>从对象池取出时调用</summary>
    public void OnPoolAcquire()
    {
        Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
    }

    /// <summary>归还对象池时调用</summary>
    public void OnPoolRelease()
    {
    }

    /// <summary>归还对象池时重置视觉状态</summary>
    public void OnPoolReset()
    {
        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = Vector2.One;
        Visible = true;
    }
}
