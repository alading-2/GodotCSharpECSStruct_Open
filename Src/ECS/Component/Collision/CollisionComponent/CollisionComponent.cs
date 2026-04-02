using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 碰撞组件 - 统一桥接所有 Area2D 碰撞信号到 Entity.Events
/// <para>
/// 核心职责：
/// 1. 若 Entity 本身是 Area2D（如 Effect Entity），绑定其碰撞信号
/// 2. 在 Entity 下查找名为 "Collision" 的感应器容器节点（Node2D），扫描其下 Area2D 子节点并绑定信号
/// 3. 向实体局部的 EventBus 抛出 CollisionEntered / CollisionExited 事件
/// </para>
/// <para>
/// 注意：碰撞形状与 layer/mask 由 EntityManager.InjectVisualScene 在注入视觉场景时统一处理，
/// VisualRoot 下的 "CollisionShape2D" 碰撞模板数据会自动同步到 Entity 根节点并删除模板。
/// </para>
/// <para>
/// 节点结构约定：
/// Entity
/// ├── CollisionShape2D（layer/mask/shape 由 EntityManager.InjectVisualScene 同步）
/// └── Collision (Node2D，感应器容器，手动放置)
///     ├── PlayerHurtboxSensor (Area2D，预设场景实例)
///     │   └── CollisionShape2D (手动配置形状)
///     └── (其他感应器...)
/// </para>
/// <para>
/// CollisionType 语义：表达"哪个碰撞节点被触发了"
/// - 感应器容器下的 Area2D → 感应器类型（EnemyHurtboxSensor / PlayerHurtboxSensor 等）
/// - Area2D Entity 根节点 → 视觉体类型（EffectCollision 等）
/// 消费者在 On 订阅时通过 CollisionType 过滤，Emit 仅负责忠实传递。
/// </para>
/// </summary>
public partial class CollisionComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(CollisionComponent));

    /// <summary>Entity 下感应器容器节点的固定名称（Node2D，手动放置预设感应器场景）</summary>
    private const string SensorContainerName = "Collision";

    // ================= 组件依赖 =================

    private IEntity? _entity;

    /// <summary>每个绑定的 Area2D 对应一个解绑 Action，卸载时统一调用</summary>
    private readonly List<Action> _unbindActions = new();

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册：绑定 Area2D 碰撞信号
    /// <para>
    /// 注册流程：
    /// 1. 若 Entity 本身是 Area2D，绑定其碰撞信号（Effect Entity）
    /// 2. 扫描 Entity/Collision 感应器容器，绑定其下 Area2D 子节点信号
    /// </para>
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;
        _entity = iEntity;

        // 1. 若 Entity 本身是 Area2D，绑定其碰撞信号（Effect Entity 路径）
        if (entity is Area2D entityArea)
        {
            BindArea(entityArea);
            _log.Debug($"[{entity.Name}] 已绑定 Entity 根节点（Area2D，CollisionType={GetCollisionTypeFromTarget(entityArea)}）");
        }

        // 2. Entity/Collision 感应器容器下的 Area2D 节点绑定信号
        // 感应器位置已在 Entity 坐标系中设计（如受伤范围、技能范围等），无需额外偏移
        var sensorContainer = FindSensorContainer(entity);
        if (sensorContainer != null)
        {
            foreach (var child in sensorContainer.GetChildren())
            {
                if (!HasCollisionShapeChild(child)) continue;
                if (child is Area2D sensorArea)
                {
                    BindArea(sensorArea);
                    _log.Debug($"[{entity.Name}] 已绑定 Collision/{sensorArea.Name}（Area2D，CollisionType={GetCollisionTypeFromTarget(sensorArea)}）");
                }
            }
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
    /// 绑定一个 Area2D 的进入/离开信号
    /// <para>
    /// CollisionType 在绑定时由该节点自身的 layer/mask 反查确定，通过闭包捕获后传入 Emit。
    /// 这样事件中的 CollisionType 表达"哪个碰撞节点被触发了"，而非"进入的目标是什么"。
    /// </para>
    /// </summary>
    private void BindArea(Area2D area)
    {
        // 从Area2D节点的 collision_layer / collision_mask 反查对应的 CollisionType
        var CollisionType = GetCollisionTypeFromTarget(area);

        void BodyEntered(Node2D body) => EmitEntered(body, CollisionType);
        void BodyExited(Node2D body) => EmitExited(body, CollisionType);
        void AreaEntered(Area2D other) => EmitEntered(other, CollisionType);
        void AreaExited(Area2D other) => EmitExited(other, CollisionType);

        area.BodyEntered += BodyEntered;
        area.BodyExited += BodyExited;
        area.AreaEntered += AreaEntered;
        area.AreaExited += AreaExited;

        area.SetDeferred(Area2D.PropertyName.Monitoring, true);
        area.SetDeferred(Area2D.PropertyName.Monitorable, true);

        _unbindActions.Add(() =>
        {
            if (!IsInstanceValid(area)) return;
            area.BodyEntered -= BodyEntered;
            area.BodyExited -= BodyExited;
            area.AreaEntered -= AreaEntered;
            area.AreaExited -= AreaExited;
        });
    }

    // ================= 事件发射 =================

    /// <summary>
    /// 发射碰撞进入事件到实体的局部事件总线
    /// </summary>
    /// <param name="target">进入的节点（Body 或 Area）</param>
    /// <param name="collisionType">被触发的碰撞节点类型（绑定时预计算）</param>
    private void EmitEntered(Node2D target, CollisionType collisionType)
    {
        if (_entity == null || !IsInstanceValid(target)) return;
        _log.Trace($"[{(_entity as Node)?.Name}] 碰撞进入 target={target.Name} collisionType={collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionEntered,
            new GameEventType.Collision.CollisionEnteredEventData(_entity, target, collisionType));
    }

    /// <summary>
    /// 发射碰撞离开事件到实体的局部事件总线
    /// </summary>
    /// <param name="target">离开的节点（Body 或 Area）</param>
    /// <param name="collisionType">被触发的碰撞节点类型（绑定时预计算）</param>
    private void EmitExited(Node2D target, CollisionType collisionType)
    {
        if (_entity == null || !IsInstanceValid(target)) return;
        _log.Trace($"[{(_entity as Node)?.Name}] 碰撞离开 target={target.Name} collisionType={collisionType}");
        _entity.Events.Emit(GameEventType.Collision.CollisionExited,
            new GameEventType.Collision.CollisionExitedEventData(_entity, target, collisionType));
    }

    // ================= 反查工具 =================

    /// <summary>
    /// 从节点的 collision_layer / collision_mask 反查对应的 CollisionType [Flags]
    /// <para>
    /// 通过 CollisionTypeRegistry 反查：(layer, mask) → CollisionType（位标志）。
    /// 反查失败（未在注册表中找到）时返回 CollisionType.None。
    /// </para>
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
                return CollisionType.None;
        }
        // 通过 (layer, mask) 双条件反向查找 CollisionType
        CollisionTypeQuery.TryFromLayerMask(layer, mask, out var type);
        return type;
    }

    // ================= 查找逻辑 =================

    /// <summary>
    /// 查找感应器容器节点（节点名固定为 "Collision"，类型为 Node2D）
    /// <para>
    /// 直接在 Entity 根级查找名为 "Collision" 的 Node2D 容器节点。
    /// 类型检查：CollisionShape2D 物理节点 和 Area2D 不视为容器。
    /// </para>
    /// </summary>
    private static Node? FindSensorContainer(Node entity)
    {
        // 直接在 Entity 根级查找（Area2D 实体 / 无 Component 的实体）
        var direct = entity.GetNodeOrNull(SensorContainerName);
        if (direct is not null and not CollisionShape2D and not Area2D)
            return direct;

        return null;
    }

    /// <summary>
    /// 检查节点是否有 CollisionShape2D 或 CollisionPolygon2D 子节点
    /// <para>用于确认感应器预设场景已配置实际碰撞形状，未配置形状的节点不绑定信号。</para>
    /// </summary>
    private static bool HasCollisionShapeChild(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is CollisionShape2D or CollisionPolygon2D)
                return true;
        }
        return false;
    }

}
