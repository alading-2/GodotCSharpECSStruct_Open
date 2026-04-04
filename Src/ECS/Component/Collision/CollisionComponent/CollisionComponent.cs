using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 碰撞组件 - 桥接 Area2D Entity 根节点碰撞信号到 Entity.Events
/// <para>
/// 核心职责：
/// 若 Entity 本身是 Area2D（如 Effect / Bullet Entity），绑定其 BodyEntered/BodyExited/AreaEntered/AreaExited 信号，
/// 向实体局部事件总线抛出 CollisionEntered / CollisionExited 事件。
/// </para>
/// <para>
/// 注意：仅处理 Entity 根节点为 Area2D 的情况。Hurtbox 传感器由 HurtboxComponent 独立处理。
/// collision_layer / collision_mask 直接在实体 .tscn 根节点设置。
/// 碰撞形状由 EntityManager.InjectVisualScene 从视觉场景同步（VisualRoot/CollisionShape2D → Entity 根节点 CollisionShape2D）。
/// </para>
/// </summary>
public partial class CollisionComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(CollisionComponent));

    private IEntity? _entity;

    /// <summary>每个绑定的 Area2D 对应一个解绑 Action，卸载时统一调用</summary>
    private readonly List<Action> _unbindActions = new();

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册：若 Entity 根节点是 Area2D，绑定其碰撞信号
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        if (entity is Area2D entityArea)
        {
            BindArea(entityArea);
            _log.Debug($"[{entity.Name}] 已绑定 Entity 根节点（Area2D）碰撞信号");
        }
    }

    /// <summary>
    /// 组件卸载：调用所有已注册的解绑操作，清理引用
    /// </summary>
    public void OnComponentUnregistered()
    {
        foreach (var unbind in _unbindActions)
            unbind.Invoke();
        _unbindActions.Clear();
        _entity = null;
    }

    // ================= 核心：绑定 Area2D =================

    /// <summary>
    /// 绑定 Area2D 的碰撞信号到事件发射器
    /// </summary>
    private void BindArea(Area2D area)
    {
        // 定义本地函数，将 Godot 信号转发到统一的事件发射器
        void BodyEntered(Node2D body) => EmitEntered(body);
        void BodyExited(Node2D body) => EmitExited(body);
        void AreaEntered(Area2D other) => EmitEntered(other);
        void AreaExited(Area2D other) => EmitExited(other);

        // 订阅 Area2D 的四个碰撞信号
        area.BodyEntered += BodyEntered;
        area.BodyExited += BodyExited;
        area.AreaEntered += AreaEntered;
        area.AreaExited += AreaExited;

        // 注册解绑操作，确保组件卸载时正确清理信号连接
        _unbindActions.Add(() =>
        {
            if (!IsInstanceValid(area)) return;
            // 先禁用监控，避免解绑过程中触发新事件
            area.SetDeferred(Area2D.PropertyName.Monitoring, false);
            area.SetDeferred(Area2D.PropertyName.Monitorable, false);
            // 取消信号订阅
            area.BodyEntered -= BodyEntered;
            area.BodyExited -= BodyExited;
            area.AreaEntered -= AreaEntered;
            area.AreaExited -= AreaExited;
        });
    }

    // ================= 事件发射 =================

    /// <summary>
    /// 发射碰撞进入事件
    /// </summary>
    private void EmitEntered(Node2D target)
    {
        // 安全性检查：确保实体存在且目标节点有效
        if (_entity == null || !IsInstanceValid(target)) return;

        // 记录调试信息，包含源实体、目标节点和距离
        _log.Debug($"[CollisionEntered] source={FormatNodeDebug(_entity as Node)} target={FormatNodeDebug(target)} distance={FormatDistance(_entity as Node, target)}");

        // 向实体局部事件总线发射碰撞进入事件
        _entity.Events.Emit(GameEventType.Collision.CollisionEntered,
            new GameEventType.Collision.CollisionEnteredEventData(_entity, target));
    }

    /// <summary>
    /// 发射碰撞退出事件
    /// </summary>
    private void EmitExited(Node2D target)
    {
        // 安全性检查：确保实体存在且目标节点有效
        if (_entity == null || !IsInstanceValid(target)) return;

        // 记录调试信息，包含源实体、目标节点和距离
        _log.Debug($"[CollisionExited] source={FormatNodeDebug(_entity as Node)} target={FormatNodeDebug(target)} distance={FormatDistance(_entity as Node, target)}");

        // 向实体局部事件总线发射碰撞退出事件
        _entity.Events.Emit(GameEventType.Collision.CollisionExited,
            new GameEventType.Collision.CollisionExitedEventData(_entity, target));
    }

    // ================= 调试工具 =================

    /// <summary>
    /// 格式化节点调试信息，包含名称、类型、实例ID和位置（如果是Node2D）
    /// </summary>
    private static string FormatNodeDebug(Node? node)
    {
        // 无效节点检查
        if (node == null || !IsInstanceValid(node)) return "<invalid>";

        // 提取节点基本信息
        var name = node.Name.ToString();
        var type = node.GetType().Name;
        var instanceId = node.GetInstanceId();

        // 如果是Node2D，额外显示全局位置
        if (node is Node2D node2D)
            return $"{name}[{type}#{instanceId}] pos={node2D.GlobalPosition}";

        return $"{name}[{type}#{instanceId}]";
    }

    /// <summary>
    /// 计算两个节点之间的距离，返回格式化字符串
    /// </summary>
    private static string FormatDistance(Node? source, Node? target)
    {
        // 类型检查：必须都是Node2D才能计算距离
        if (source is not Node2D sourceNode2D || target is not Node2D targetNode2D) return "n/a";

        // 有效性检查
        if (!IsInstanceValid(sourceNode2D) || !IsInstanceValid(targetNode2D)) return "n/a";

        // 计算并格式化距离（保留2位小数）
        return sourceNode2D.GlobalPosition.DistanceTo(targetNode2D.GlobalPosition).ToString("F2");
    }
}
