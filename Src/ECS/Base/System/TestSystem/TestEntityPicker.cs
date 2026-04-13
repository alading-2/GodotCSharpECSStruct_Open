using Godot;
using System.Collections.Generic;

/// <summary>
/// TestSystem 实体拾取器。
/// <para>
/// 把“屏幕点击 -> IEntity 解析”的逻辑从宿主 UI 中拆出，避免宿主同时承担面板管理和拾取算法。
/// </para>
/// </summary>
internal sealed class TestEntityPicker
{
    /// <summary>
    /// 在屏幕坐标下查找实体。
    /// </summary>
    /// <param name="owner">调用拾取的宿主节点。</param>
    /// <param name="screenPosition">屏幕坐标。</param>
    /// <returns>命中的实体；未命中时返回 null。</returns>
    public IEntity? FindEntityAtScreenPosition(Node owner, Vector2 screenPosition)
    {
        var worldPosition = GetWorldMousePosition(owner, screenPosition);
        var entity = FindEntityByPhysics(owner, worldPosition);
        if (entity != null)
        {
            return entity;
        }

        return FindEntityByDistance(worldPosition, 56f);
    }

    /// <summary>
    /// 把屏幕坐标转换成世界坐标。
    /// </summary>
    private static Vector2 GetWorldMousePosition(Node owner, Vector2 screenPosition)
    {
        var camera = owner.GetViewport().GetCamera2D();
        if (camera != null)
        {
            return camera.GetGlobalMousePosition();
        }

        return screenPosition;
    }

    /// <summary>
    /// 使用物理空间查询命中实体。
    /// </summary>
    private static IEntity? FindEntityByPhysics(Node owner, Vector2 worldPosition)
    {
        var world2D = owner.GetViewport().World2D;
        if (world2D == null)
        {
            return null;
        }

        var query = new PhysicsPointQueryParameters2D
        {
            Position = worldPosition,
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = uint.MaxValue
        };

        var results = world2D.DirectSpaceState.IntersectPoint(query, 32);
        var visited = new HashSet<ulong>();

        foreach (Godot.Collections.Dictionary result in results)
        {
            if (!result.ContainsKey("collider"))
            {
                continue;
            }

            var collider = result["collider"].AsGodotObject() as Node;
            var entity = ResolveEntityFromNode(collider);
            if (entity is not Node entityNode)
            {
                continue;
            }

            var instanceId = entityNode.GetInstanceId();
            if (visited.Contains(instanceId))
            {
                continue;
            }

            visited.Add(instanceId);
            return entity;
        }

        return null;
    }

    /// <summary>
    /// 当物理拾取没有结果时，按距离兜底寻找最近实体。
    /// </summary>
    private static IEntity? FindEntityByDistance(Vector2 worldPosition, float maxDistance)
    {
        IEntity? bestEntity = null;
        var bestDistanceSquared = maxDistance * maxDistance;

        foreach (var entity in EntityManager.GetAllEntities())
        {
            if (entity is not Node2D node2D)
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
    /// 由任意场景节点向上回溯所属实体。
    /// </summary>
    private static IEntity? ResolveEntityFromNode(Node? node)
    {
        var current = node;
        while (current != null)
        {
            if (current is IEntity entity)
            {
                return entity;
            }

            var host = EntityManager.GetEntityByComponent(current);
            if (host is IEntity hostEntity)
            {
                return hostEntity;
            }

            current = current.GetParent();
        }

        return null;
    }
}
