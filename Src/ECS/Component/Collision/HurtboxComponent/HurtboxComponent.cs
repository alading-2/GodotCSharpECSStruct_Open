using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 受击区组件（Area2D）
/// <para>
/// 组件本身即 Area2D 传感器，注册时绑定自身 BodyEntered/BodyExited/AreaEntered/AreaExited 信号。
/// collision_layer / collision_mask 与 CollisionShape2D 直接在挂载实体的 .tscn 中配置。
/// </para>
/// </summary>
public partial class HurtboxComponent : Area2D, IComponent
{
    private static readonly Log _log = new(nameof(HurtboxComponent));

    private IEntity? _entity;
    private readonly List<Action> _unbindActions = new();

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册：绑定自身 Area2D 碰撞信号
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        BindSelf();
        _log.Debug($"[{entity.Name}] HurtboxComponent 注册（layer={CollisionLayer}, mask={CollisionMask}）");
    }

    /// <summary>
    /// 组件卸载：解除信号绑定，清理引用
    /// </summary>
    public void OnComponentUnregistered()
    {
        foreach (var unbind in _unbindActions)
            unbind.Invoke();
        _unbindActions.Clear();
        _entity = null;
    }

    // ================= 信号绑定 =================

    private void BindSelf()
    {
        void OnBodyEntered(Node2D body) => EmitEntered(body);
        void OnBodyExited(Node2D body) => EmitExited(body);
        void OnAreaEntered(Area2D other) => EmitEntered(other);
        void OnAreaExited(Area2D other) => EmitExited(other);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;

        _unbindActions.Add(() =>
        {
            if (!IsInstanceValid(this)) return;
            SetDeferred(Area2D.PropertyName.Monitoring, false);
            SetDeferred(Area2D.PropertyName.Monitorable, false);
            BodyEntered -= OnBodyEntered;
            BodyExited -= OnBodyExited;
            AreaEntered -= OnAreaEntered;
            AreaExited -= OnAreaExited;
        });
    }

    // ================= 事件发射 =================

    private void EmitEntered(Node2D target)
    {
        if (_entity == null || !IsInstanceValid(target)) return;
        var targetEntity = ResolveOwningEntity(target);
        _log.Debug($"[HurtboxEntered] source={FormatNodeDebug(_entity as Node)} target={FormatNodeDebug(target)} targetEntity={FormatEntityDebug(targetEntity)} distance={FormatDistance(_entity as Node, target)}");
        _entity.Events.Emit(GameEventType.Collision.HurtboxEntered,
            new GameEventType.Collision.HurtboxEnteredEventData(_entity, this, target, targetEntity));
    }

    private void EmitExited(Node2D target)
    {
        if (_entity == null || !IsInstanceValid(target)) return;
        var targetEntity = ResolveOwningEntity(target);
        _log.Debug($"[HurtboxExited] source={FormatNodeDebug(_entity as Node)} target={FormatNodeDebug(target)} targetEntity={FormatEntityDebug(targetEntity)} distance={FormatDistance(_entity as Node, target)}");
        _entity.Events.Emit(GameEventType.Collision.HurtboxExited,
            new GameEventType.Collision.HurtboxExitedEventData(_entity, this, target, targetEntity));
    }

    // ================= 工具 =================

    /// <summary>
    /// 解析节点的所属实体，向上遍历节点树查找第一个实现 IEntity 的节点
    /// </summary>
    private static IEntity? ResolveOwningEntity(Node node)
    {
        Node? current = node;
        while (current != null)
        {
            if (current is IEntity iEntity)
                return iEntity;
            current = current.GetParent();
        }
        return null;
    }

    /// <summary>
    /// 格式化节点调试信息，包含名称、类型、实例ID和位置（如果是Node2D）
    /// </summary>
    private static string FormatNodeDebug(Node? node)
    {
        if (node == null || !IsInstanceValid(node)) return "<invalid>";
        var name = node.Name.ToString();
        var type = node.GetType().Name;
        var instanceId = node.GetInstanceId();
        if (node is Node2D node2D)
            return $"{name}[{type}#{instanceId}] pos={node2D.GlobalPosition}";
        return $"{name}[{type}#{instanceId}]";
    }

    private static string FormatEntityDebug(IEntity? entity)
    {
        return entity is Node node ? FormatNodeDebug(node) : "<none>";
    }

    private static string FormatDistance(Node? source, Node? target)
    {
        if (source is not Node2D sourceNode2D || target is not Node2D targetNode2D) return "n/a";
        if (!IsInstanceValid(sourceNode2D) || !IsInstanceValid(targetNode2D)) return "n/a";
        return sourceNode2D.GlobalPosition.DistanceTo(targetNode2D.GlobalPosition).ToString("F2");
    }
}
