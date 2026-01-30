using Godot;

/// <summary>
/// 瞄准指示器实体 - 可视化的技能施法点标记（War3 的"马甲"）
/// 
/// Entity 规范：
/// - Entity 是纯容器，仅持有 Data 和 Events
/// - 所有业务逻辑由 TargetingIndicatorControlComponent 处理
/// </summary>
public partial class TargetingIndicatorEntity : Node2D, IEntity, IUnit
{
    private static readonly Log _log = new(nameof(TargetingIndicatorEntity));

    // ================= IEntity 实现 =================

    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    // ================= 构造函数 =================

    public TargetingIndicatorEntity()
    {
        Data = new Data(this);
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // 设置较高的 ZIndex 确保显示在最上层
        ZIndex = 100;

        _log.Debug("TargetingIndicatorEntity 初始化完成");
    }

    public override void _ExitTree()
    {
    }
}
