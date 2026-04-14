using Godot;
using System.Collections.Generic;

/// <summary>
/// MouseSelection 的拾取层。
/// <para>
/// 这个文件只负责“找得到谁”：屏幕 / 世界坐标换算、物理点选、距离兜底、世界矩形候选收集、实体回溯与语义过滤。
/// </para>
/// <para>
/// 交互层只决定什么时候调用这些方法，UI 层只负责展示框选预览，三者职责彼此分离。
/// </para>
/// </summary>
public partial class MouseSelectionSystem
{
    /// <summary>
    /// 把屏幕坐标转换成世界坐标。
    /// <para>这里使用当前 Viewport 的 Canvas 逆变换，保证 UI 坐标和世界坐标的换算一致。</para>
    /// </summary>
    private Vector2 GetWorldPosition(Vector2 screenPosition)
    {
        return GetViewport().GetCanvasTransform().AffineInverse() * screenPosition;
    }

    /// <summary>
    /// 使用物理查询拾取鼠标位置下的实体。
    /// <para>筛选有碰撞体的Entity</para>
    /// </summary>
    private IEntity? FindEntityByPhysics(Vector2 worldPosition, out GameEventType.Global.MouseSelectionHitKind hitKind)
    {
        hitKind = GameEventType.Global.MouseSelectionHitKind.None;
        var world2D = GetViewport().World2D;
        if (world2D == null)
        {
            return null;
        }

        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = DefaultCollisionMask
        };

        // 最多取少量结果即可，后面会做实体回溯和去重，不需要一次拿太多。
        var results = world2D.DirectSpaceState.IntersectPoint(query, 32);
        var visited = new HashSet<ulong>();

        foreach (Godot.Collections.Dictionary result in results)
        {
            // IntersectPoint 返回的是字典数组，这里先确认是否有 collider 字段。
            if (!result.ContainsKey("collider"))
            {
                continue;
            }

            // collider 可能是任意 Node，只要能回溯到 IEntity 即可。
            var collider = result["collider"].AsGodotObject() as Node;
            var entity = ResolveEntityFromNode(collider);
            if (entity is not Node entityNode || !PassEntityFilters(entity))
            {
                continue;
            }

            // 同一个实体可能通过多个碰撞体被命中，因此用 InstanceId 去重。
            var instanceId = entityNode.GetInstanceId();
            if (visited.Contains(instanceId))   // 已经处理过的实体，跳过
            {
                continue;
            }

            visited.Add(instanceId);
            hitKind = GameEventType.Global.MouseSelectionHitKind.PhysicsPoint;
            return entity;
        }

        return null;
    }

    /// <summary>
    /// 当物理拾取失败时，按距离兜底寻找最近实体。
    /// <para>这个逻辑不会依赖碰撞层，而是直接遍历实体列表，适合调试或碰撞形状较小的对象。</para>
    /// </summary>
    /// <param name="worldPosition">世界坐标</param>
    /// <param name="maxDistance">最大距离</param>
    /// <returns>最近的实体，如果没有任何实体在距离内则返回 null</returns>
    private IEntity? FindEntityByDistance(Vector2 worldPosition, float maxDistance)
    {
        IEntity? bestEntity = null;
        var bestDistanceSquared = maxDistance * maxDistance;

        // 这里使用全局实体集合做兜底搜索，因此必须尽量保守地应用过滤条件。
        foreach (var entity in EntityManager.GetAllEntities())
        {
            if (entity is not Node2D node2D)
            {
                continue;
            }

            // 先过滤掉不符合类型、阵营或生命周期要求的实体，再比较距离。
            if (!PassEntityFilters(entity))
            {
                continue;
            }

            var distanceSquared = node2D.GlobalPosition.DistanceSquaredTo(worldPosition);
            if (distanceSquared > bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared = distanceSquared;
            bestEntity = entity;
        }

        return bestEntity;
    }

    /// <summary>
    /// 按世界矩形收集框选候选实体。
    /// <para>框选逻辑只看实体位置是否落在矩形内部；如果需要更复杂的包围体判断，可在此基础上扩展。</para>
    /// </summary>
    private List<IEntity> FindEntitiesInWorldRect(Rect2 worldRect)
    {
        var entities = new List<IEntity>();
        foreach (var entity in EntityManager.GetAllEntities())
        {
            if (entity is not Node2D node2D)
            {
                continue;
            }

            if (!worldRect.HasPoint(node2D.GlobalPosition) || !PassEntityFilters(entity))
            {
                continue;
            }

            // 通过矩形与过滤条件的实体，才会进入后续排序与结果广播。
            entities.Add(entity);
        }

        return entities;
    }

    /// <summary>
    /// 从任意场景节点回溯所属实体。
    /// <para>物理拾取返回的可能是碰撞体、挂件节点或组件节点，因此需要沿父级链向上查找真正的实体宿主。</para>
    /// </summary>
    private static IEntity? ResolveEntityFromNode(Node? node)
    {
        var current = node;
        while (current != null)
        {
            // 如果当前节点本身就是实体，直接返回。
            if (current is IEntity entity)
            {
                return entity;
            }

            // 否则尝试从组件宿主反查实体，兼容“组件挂在实体子树中”的场景。
            var host = EntityManager.GetEntityByComponent(current);
            if (host is IEntity hostEntity)
            {
                return hostEntity;
            }

            // 继续向父节点回溯，直到根节点或找到实体为止。
            current = current.GetParent();
        }

        return null;
    }

    /// <summary>
    /// 执行通用实体过滤。
    /// <para>MouseSelection 只剔除生命周期上不可选的实体；类型、阵营和集合合并策略交给监听系统自行决定。</para>
    /// </summary>
    private static bool PassEntityFilters(IEntity entity)
    {
        if (entity.Data.Has(DataKey.LifecycleState))
        {
            var state = entity.Data.Get<LifecycleState>(DataKey.LifecycleState);
            if (state == LifecycleState.Dead || state == LifecycleState.Reviving)
            {
                return false;
            }
        }

        return true;
    }
}
