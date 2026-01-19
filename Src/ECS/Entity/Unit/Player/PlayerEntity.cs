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
    private static readonly Log _log = new("Player");

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

    // EntityId 由 IEntity 默认实现（从 DataKey.Id 读取）

    // 0: Player
    public int FactionId => 0;

    public override void _Ready()
    {
        base._Ready();

        // 注册到 EntityManager（如果需要全局查询）
        EntityManager.Register(this, "Player");

        _log.Info("玩家实体初始化完成。");
    }

    public override void _ExitTree()
    {
        // 统一注销 (内部自动清理 Data 和 Events)
        EntityManager.UnregisterEntity(this);
    }
}
