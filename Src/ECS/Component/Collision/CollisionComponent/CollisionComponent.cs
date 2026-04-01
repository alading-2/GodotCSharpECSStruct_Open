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

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册：查找 VisualRoot 内碰撞节点并绑定信号
    /// <para>
    /// 注册流程：
    /// 1. 验证实体是否实现 IEntity 接口
    /// 2. 在实体的 VisualRoot 节点下查找名为 "CollisionShape2D" 的碰撞节点
    /// 3. 根据碰撞节点类型（Area2D 或 CharacterBody2D）进行相应处理
    /// 4. 对于 Area2D 类型，绑定所有相关碰撞信号并启用监控
    /// </para>
    /// </summary>
    /// <param name="entity">要注册组件的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        // 验证实体是否实现了 IEntity 接口，否则无法使用事件系统
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        // 在实体的 VisualRoot 下查找预定义的碰撞节点
        var collisionNode = FindCollisionNode(entity);
        if (collisionNode == null)
        {
            _log.Debug($"[{entity.Name}] VisualRoot 下未找到 {CollisionNodeName}，CollisionComponent 无效。");
            return;
        }

        // 处理 Area2D 类型的碰撞节点（最常见的碰撞检测类型）
        if (collisionNode is Area2D area)
        {
            _area = area; // 缓存引用以便后续卸载时解绑信号

            // 绑定 Area2D 的所有碰撞进入/离开信号
            // BodyEntered/BodyExited：处理物理体（RigidBody2D、CharacterBody2D 等）的碰撞
            // AreaEntered/AreaExited：处理其他 Area2D 的碰撞
            area.BodyEntered += OnBodyEntered;
            area.BodyExited += OnBodyExited;
            area.AreaEntered += OnAreaEntered;
            area.AreaExited += OnAreaExited;

            // 启用 Area2D 的监控功能，使其能够接收碰撞事件
            // 使用 SetDeferred 确保在安全的时机修改属性
            area.SetDeferred(Area2D.PropertyName.Monitoring, true);
            area.SetDeferred(Area2D.PropertyName.Monitorable, true);

            _log.Debug($"[{entity.Name}] 已连接 VisualRoot/{CollisionNodeName}（Area2D）的碰撞信号。");
        }
        // 处理 CharacterBody2D 类型的碰撞节点
        else if (collisionNode is CharacterBody2D)
        {
            // CharacterBody2D 的碰撞检测由 EntityMovementComponent 通过 MoveAndSlide 处理
            // 这里只记录日志，不绑定信号，避免重复处理
            _log.Debug($"[{entity.Name}] VisualRoot/{CollisionNodeName} 为 CharacterBody2D，layer/mask 已预设，碰撞检测由 EntityMovementComponent 处理。");
        }
        // 处理不支持的碰撞节点类型
        else
        {
            // 记录警告信息，提示开发者当前节点类型不被支持
            _log.Warn($"[{entity.Name}] VisualRoot/{CollisionNodeName} 类型 {collisionNode.GetType().Name} 暂不支持，已跳过。");
        }
    }

    /// <summary>
    /// 组件卸载：解绑信号，清理引用
    /// <para>
    /// 清理流程：
    /// 1. 如果存在缓存的 Area2D 引用，解绑所有碰撞信号
    /// 2. 重置碰撞类型为默认值
    /// 3. 清空实体引用，防止内存泄漏
    /// </para>
    /// </summary>
    public void OnComponentUnregistered()
    {
        // 解绑 Area2D 的所有碰撞信号，防止组件卸载后继续触发回调
        if (_area != null)
        {
            _area.BodyEntered -= OnBodyEntered;
            _area.BodyExited -= OnBodyExited;
            _area.AreaEntered -= OnAreaEntered;
            _area.AreaExited -= OnAreaExited;
            _area = null; // 清空引用，帮助垃圾回收
        }

        _entity = null; // 清空实体引用，断开与实体的关联
    }

    // ================= 物理信号回调 =================
    // <summary>
    // 物理信号回调处理层：将 Godot 原生信号统一转发到事件发射器
    // <para>
    // 设计理念：
    // - 所有回调都统一调用 EmitEntered/EmitExited，避免重复代码
    // - 保持信号类型的一致性（Node2D），便于事件处理
    // - 这一层只是信号转发，具体的事件发射逻辑在 Emit 方法中实现
    // </para>
    // </summary>

    /// <summary>
    /// 处理物理体（Body）进入碰撞区域的信号
    /// </summary>
    /// <param name="body">进入的物理体节点</param>
    private void OnBodyEntered(Node2D body) => EmitEntered(body);

    /// <summary>
    /// 处理物理体（Body）离开碰撞区域的信号
    /// </summary>
    /// <param name="body">离开的物理体节点</param>
    private void OnBodyExited(Node2D body) => EmitExited(body);

    /// <summary>
    /// 处理区域（Area）进入碰撞区域的信号
    /// </summary>
    /// <param name="area">进入的区域节点</param>
    private void OnAreaEntered(Area2D area) => EmitEntered(area);

    /// <summary>
    /// 处理区域（Area）离开碰撞区域的信号
    /// </summary>
    /// <param name="area">离开的区域节点</param>
    private void OnAreaExited(Area2D area) => EmitExited(area);

    /// <summary>
    /// 发射碰撞进入事件到实体的局部事件总线
    /// <para>
    /// 事件发射流程：
    /// 1. 验证实体和目标节点的有效性
    /// 2. 记录详细的调试日志
    /// 3. 向实体的事件总线发射标准的碰撞进入事件
    /// </para>
    /// </summary>
    /// <param name="node">进入碰撞的节点（可能是 Body 或 Area）</param>
    private void EmitEntered(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;

        var collisionType = GetCollisionTypeFromTarget(node);
        _log.Trace($"[Visual: {(_entity as Node)?.Name}] 探测到 {node.Name} 进入，Target.CollisionType={collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionEntered,
            new GameEventType.Collision.CollisionEnteredEventData(_entity, node, collisionType));
    }

    /// <summary>
    /// 发射碰撞离开事件到实体的局部事件总线
    /// <para>
    /// 事件发射流程：
    /// 1. 验证实体和目标节点的有效性
    /// 2. 记录详细的调试日志
    /// 3. 向实体的事件总线发射标准的碰撞离开事件
    /// </para>
    /// </summary>
    /// <param name="node">离开碰撞的节点（可能是 Body 或 Area）</param>
    private void EmitExited(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;

        var collisionType = GetCollisionTypeFromTarget(node);
        _log.Trace($"[Visual: {(_entity as Node)?.Name}] 探测到 {node.Name} 离开，Target.CollisionType={collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionExited,
            new GameEventType.Collision.CollisionExitedEventData(_entity, node, collisionType));
    }

    /// <summary>
    /// 从碰撞目标节点的 collision_layer / collision_mask 反查对应的 CollisionType
    /// </summary>
    private static CollisionType GetCollisionTypeFromTarget(Node2D node)
    {
        uint layer, mask;
        switch (node)
        {
            case Area2D area:
                layer = area.CollisionLayer;
                mask = area.CollisionMask;
                break;
            case CharacterBody2D body:
                layer = body.CollisionLayer;
                mask = body.CollisionMask;
                break;
            default:
                return CollisionType.Custom;
        }
        CollisionTypeQuery.TryFromLayerMask(layer, mask, out var type);
        return type;
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
