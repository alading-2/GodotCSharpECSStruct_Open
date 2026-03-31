using Godot;

/// <summary>
/// 视觉碰撞组件 - 桥接 VisualRoot 内碰撞节点的信号到 Entity.Events
/// <para>
/// 核心职责：
/// 1. 在 VisualRoot 下查找名为 "CollisionShape2D" 的碰撞节点（由 SpriteFramesGenerator 注入）
/// 2. 支持 Area2D（绑定 BodyEntered/AreaEntered 信号）和 CharacterBody2D（日志提示，运动碰撞由 EntityMovementComponent 负责）
/// 3. 向实体局部的 EventBus 抛出 VisualCollisionEntered / VisualCollisionExited 事件
/// </para>
/// <para>
/// 与 CollisionSensorComponent 的区别：
/// - CollisionComponent    = 视觉形状碰撞（单位/武器/子弹的实际形状，VisualRoot 内注入）
/// - CollisionSensorComponent = 固定范围感应器（受伤感应区、拾取感应区，独立 Area2D 组件）
/// 二者发布不同的事件 Key，业务组件按需订阅，互不干扰。
/// </para>
/// </summary>
public partial class CollisionComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(CollisionComponent));

    /// <summary>VisualRoot 内碰撞节点的固定名称（由 SpriteFramesGenerator 注入时统一命名）</summary>
    private const string CollisionNodeName = "CollisionShape2D";

    // ================= 组件依赖 =================

    private IEntity? _entity;

    /// <summary>若碰撞节点为 Area2D，缓存引用以便卸载时解绑信号</summary>
    private Area2D? _area;

    /// <summary>注册时从 VisualRoot 碰撞节点 layer 派生的 CollisionType（缓存，避免热路径 lookup）</summary>
    private CollisionType _collisionType = CollisionType.Custom;

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册：查找 VisualRoot 内碰撞节点并绑定信号
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        var collisionNode = FindCollisionNode(entity);
        if (collisionNode == null)
        {
            _log.Debug($"[{entity.Name}] VisualRoot 下未找到 {CollisionNodeName}，CollisionComponent 无效。");
            return;
        }

        if (collisionNode is Area2D area)
        {
            _area = area;
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
            area.AreaEntered += OnAreaEntered;
            area.AreaExited += OnAreaExited;
            area.SetDeferred(Area2D.PropertyName.Monitoring, true);
            area.SetDeferred(Area2D.PropertyName.Monitorable, true);
            // 注册时从 layer 反查 CollisionType（仅调用一次，结果缓存在字段中）
            _collisionType = CollisionTypeRegistry.FromLayer(area.CollisionLayer);
            _log.Debug($"[{entity.Name}] 已连接 VisualRoot/{CollisionNodeName}（Area2D, CollisionType={_collisionType}）的碰撞信号。");
        }
        else if (collisionNode is CharacterBody2D)
        {
            _log.Debug($"[{entity.Name}] VisualRoot/{CollisionNodeName} 为 CharacterBody2D，layer/mask 已预设，碰撞检测由 EntityMovementComponent 处理。");
        }
        else
        {
            _log.Warn($"[{entity.Name}] VisualRoot/{CollisionNodeName} 类型 {collisionNode.GetType().Name} 暂不支持，已跳过。");
        }
    }

    /// <summary>
    /// 组件卸载：解绑信号，清理引用
    /// </summary>
    public void OnComponentUnregistered()
    {
        if (_area != null)
        {
            _area.BodyEntered -= OnBodyEntered;
            _area.BodyExited -= OnBodyExited;
            _area.AreaEntered -= OnAreaEntered;
            _area.AreaExited -= OnAreaExited;
            _area = null;
        }

        _collisionType = CollisionType.Custom;
        _entity = null;
    }

    // ================= 物理信号回调 =================

    private void OnBodyEntered(Node2D body) => EmitEntered(body);
    private void OnBodyExited(Node2D body) => EmitExited(body);
    private void OnAreaEntered(Area2D area) => EmitEntered(area);
    private void OnAreaExited(Area2D area) => EmitExited(area);

    private void EmitEntered(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;
        _log.Trace($"[Visual: {(_entity as Node)?.Name}] 探测到 {node.Name} 进入，CollisionType={_collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionEntered,
            new GameEventType.Collision.CollisionEnteredEventData(_entity, node, _collisionType));
    }

    private void EmitExited(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;
        _log.Trace($"[Visual: {(_entity as Node)?.Name}] 探测到 {node.Name} 离开，CollisionType={_collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionExited,
            new GameEventType.Collision.CollisionExitedEventData(_entity, node, _collisionType));
    }

    // ================= 查找逻辑 =================

    /// <summary>
    /// 在 Entity 的 VisualRoot 下查找碰撞节点
    /// </summary>
    private static Node2D? FindCollisionNode(Node entity)
    {
        var visualRoot = entity.GetNodeOrNull("VisualRoot");
        if (visualRoot == null) return null;
        return visualRoot.GetNodeOrNull(CollisionNodeName) as Node2D;
    }
}
