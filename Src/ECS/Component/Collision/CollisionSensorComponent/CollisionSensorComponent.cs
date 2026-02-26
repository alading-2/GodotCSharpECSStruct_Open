using Godot;

/// <summary>
/// 纯粹的通用物理碰撞感应器 (Sensor)
/// <para>
/// 核心职责：
/// 1. 作为 Area2D 容器，负责监听物理进入/离开事件
/// 2. 向实体局部的 EventBus 抛出标准的物理碰撞事件 (CollisionEntered / CollisionExited)
/// 3. 本组件绝不包含任何诸如“造成伤害”、“扣减穿透次数”等业务逻辑，它只是一个情报发送机。
/// </para>
/// </summary>
public partial class CollisionSensorComponent : Area2D, IComponent
{
    private static readonly Log _log = new(nameof(CollisionSensorComponent));

    // ================= 组件依赖 =================
    private IEntity? _entity;

    // ================= IComponent 实现 =================
    /// <summary>
    /// 组件注册
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        // 绑定 Godot 原生碰撞事件
        BodyEntered += OnNodeEntered;
        BodyExited += OnNodeExited;
        AreaEntered += OnNodeEntered;
        AreaExited += OnNodeExited;

        SetDeferred(Area2D.PropertyName.Monitoring, true);
        SetDeferred(Area2D.PropertyName.Monitorable, true);
        _log.Debug($"[{entity.Name}] 碰撞感应器注册完成，开始监听 Area/Body 进入与离开。");
    }

    /// <summary>
    /// 组件卸载
    /// </summary>
    public void OnComponentUnregistered()
    {
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        SetDeferred(Area2D.PropertyName.Monitorable, false);

        BodyEntered -= OnNodeEntered;
        BodyExited -= OnNodeExited;
        AreaEntered -= OnNodeEntered;
        AreaExited -= OnNodeExited;

        _entity = null;
    }

    // ================= 物理事件转发 =================

    /// <summary>
    /// 当节点（Body/Area）进入感应范围时的统一回调
    /// </summary>
    /// <param name="node">进入的 2D 节点</param>
    private void OnNodeEntered(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;

        var entityNode = _entity as Node;
        if (entityNode == null) return;

        _log.Trace($"[Sensor: {entityNode.Name}] 探测到 {node.Name} 进入。发送 CollisionEntered 事件。");

        _entity.Events.Emit(GameEventType.Collision.CollisionEntered, new GameEventType.Collision.CollisionEnteredEventData(
            _entity,
            node
        ));
    }

    /// <summary>
    /// 当节点（Body/Area）离开感应范围时的统一回调
    /// </summary>
    /// <param name="node">离开的 2D 节点</param>
    private void OnNodeExited(Node2D node)
    {
        if (_entity == null || !IsInstanceValid(node)) return;

        var entityNode = _entity as Node;
        if (entityNode == null) return;

        _log.Trace($"[Sensor: {entityNode.Name}] 探测到 {node.Name} 离开。发送 CollisionExited 事件。");

        _entity.Events.Emit(GameEventType.Collision.CollisionExited, new GameEventType.Collision.CollisionExitedEventData(
            _entity,
            node
        ));
    }
}
