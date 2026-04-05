using Godot;

/// <summary>
/// 运动技能实体 - 带碰撞的技能投射体（Area2D 类型）
/// 通过 MovementStarted 事件驱动运动，CollisionComponent 感知碰撞
/// </summary>
public partial class MovementAbilityEntity : Area2D, IEntity, IPoolable
{
    private static readonly Log _log = new(nameof(MovementAbilityEntity));

    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public MovementAbilityEntity()
    {
        Data = new Data(this);
        Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
    }

    public override void _Ready() { }
    public override void _ExitTree() { }

    public void OnPoolAcquire()
    {
        Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
    }

    public void OnPoolRelease() { }

    public void OnPoolReset()
    {
        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = Vector2.One;
        Visible = true;
    }
}
