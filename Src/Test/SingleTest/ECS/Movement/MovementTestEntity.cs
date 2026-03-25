using Godot;

namespace Slime.Test;

/// <summary>
/// 运动组件测试实体（Node2D + IEntity）
/// 用于在单测场景中承载 EntityMovementComponent
/// </summary>
public partial class MovementTestEntity : Node2D, IEntity, IPoolable
{
    private static readonly Log _log = new(nameof(MovementTestEntity));

    /// <summary>实体局部事件总线</summary>
    public EventBus Events { get; } = new();

    /// <summary>实体数据容器</summary>
    public Data Data { get; private set; } = new();

    /// <summary>Godot 节点就绪回调</summary>
    public override void _Ready()
    {
        _log.Debug("MovementTestEntity Ready");
    }

    /// <summary>绘制测试实体可视化圆形</summary>
    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 14f, Colors.OrangeRed);
        DrawArc(Vector2.Zero, 14f, 0f, Mathf.Tau, 32, Colors.White, 2f);
    }

    /// <summary>对象池取出回调</summary>
    public void OnPoolAcquire()
    {
    }

    /// <summary>对象池回收回调</summary>
    public void OnPoolRelease()
    {
        Data.Clear();
        Events.Clear();
    }

    /// <summary>对象池重置回调</summary>
    public void OnPoolReset()
    {
    }
}
