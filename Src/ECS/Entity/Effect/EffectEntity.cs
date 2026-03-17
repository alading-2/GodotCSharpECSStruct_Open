using Godot;

/// <summary>
/// 特效实体 - 统一管理所有视觉特效
/// 
/// 设计理念:
/// - 特效是 Entity，实现 IEntity 接口
/// - 业务逻辑归 EffectComponent（播放驱动、生命周期、附着跟随）
/// - 支持对象池复用（实现 IPoolable）
/// - 继承 Node2D 以支持 2D 空间变换（位置/旋转/缩放）
/// - 通过 EffectTool 统一生成和销毁
/// </summary>
public partial class EffectEntity : Node2D, IEntity, IPoolable
{
    private static readonly Log _log = new(nameof(EffectEntity));

    // ================= IEntity 实现 =================

    /// <inheritdoc/>
    public Data Data { get; private set; }
    /// <inheritdoc/>
    public EventBus Events { get; } = new EventBus();

    // ================= 构造函数 =================

    /// <summary>
    /// 构造函数，初始化 Data 容器
    /// </summary>
    public EffectEntity()
    {
        Data = new Data(this);
    }

    // ================= Godot 生命周期 =================

    /// <inheritdoc/>
    public override void _Ready()
    {
    }

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        // EntityManager.Destroy(this);
    }

    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用
    /// Data 和 Events 的清理/重置由 EntityManager.UnregisterEntity 处理
    /// </summary>
    public void OnPoolAcquire()
    {
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用
    /// Events.Clear(), Data.Clear() 均由 EntityManager.Destroy() -> UnregisterEntity() 统一处理
    /// </summary>
    public void OnPoolRelease()
    {
    }

    /// <summary>
    /// [IPoolable] 当归还池时重置视觉状态
    /// </summary>
    public void OnPoolReset()
    {
        // 清理动态加载的视觉子节点
        var visualRoot = GetNodeOrNull("VisualRoot");
        if (visualRoot != null)
        {
            RemoveChild(visualRoot);
            visualRoot.QueueFree();
        }

        // 重置 Node2D 变换
        Position = Vector2.Zero;
        Rotation = 0f;
        Scale = Vector2.One;
        Visible = true;
    }
}
