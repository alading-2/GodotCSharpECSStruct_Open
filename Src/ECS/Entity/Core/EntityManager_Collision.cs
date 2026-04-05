using Godot;

/// <summary>
/// EntityManager 碰撞处理扩展
/// <para>
/// 负责将 VisualRoot 中的碰撞模板同步到 Entity 根节点，
/// 确保 Entity 获得正确的物理碰撞形状。
/// </para>
/// </summary>
public static partial class EntityManager
{
    /// <summary>
    /// 同步 VisualRoot 下的碰撞形状模板到 Entity 根节点，然后删除模板
    /// <para>
    /// 碰撞模板可为 VisualRoot 下名为 "CollisionShape2D" 或 "CollisionPolygon2D" 的纯碰撞节点。
    /// 仅同步形状数据与局部变换，Entity 的 collision_layer / collision_mask 已直接在其 .tscn 根节点设置，无需此处传递。
    /// 同步完成后删除模板，VisualRoot 只保留视觉内容。
    /// </para>
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <param name="visualRoot">视觉根节点，包含碰撞模板</param>
    private static void SyncAndRemoveCollisionTemplate(Node entity, Node visualRoot)
    {
        // 查找碰撞模板节点（支持 CollisionShape2D 和 CollisionPolygon2D）
        Node? template = visualRoot.GetNodeOrNull<CollisionShape2D>("CollisionShape2D") as Node;
        template ??= visualRoot.GetNodeOrNull<CollisionPolygon2D>("CollisionPolygon2D") as Node;
        if (template == null) return;

        // 尝试同步碰撞模板
        if (!TrySyncCollisionTemplate(entity, template))
        {
            _log.Warn($"[{entity.Name}] 碰撞模板同步失败: {template.Name}");
        }

        _log.Debug($"[{entity.Name}] 已同步碰撞模板并删除 VisualRoot/{template.Name}");
        template.QueueFree();
    }

    /// <summary>
    /// 根据模板类型选择对应的同步方法
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <param name="template">碰撞模板节点</param>
    /// <returns>是否同步成功</returns>
    private static bool TrySyncCollisionTemplate(Node entity, Node template)
    {
        return template switch
        {
            CollisionShape2D sourceShape => SyncCollisionShapeTemplate(entity, sourceShape),
            CollisionPolygon2D sourcePolygon => SyncCollisionPolygonTemplate(entity, sourcePolygon),
            _ => false
        };
    }

    /// <summary>
    /// 同步 CollisionShape2D 碰撞模板
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <param name="sourceShape">源碰撞形状模板</param>
    /// <returns>是否同步成功</returns>
    private static bool SyncCollisionShapeTemplate(Node entity, CollisionShape2D sourceShape)
    {
        // 确保实体有对应的碰撞节点
        var entityShape = EnsureCollisionNode<CollisionShape2D>(entity, sourceShape.Name);
        if (entityShape == null) return false;

        // 同步碰撞形状属性
        entityShape.Shape = sourceShape.Shape;
        entityShape.Disabled = sourceShape.Disabled;
        entityShape.OneWayCollision = sourceShape.OneWayCollision;
        entityShape.OneWayCollisionMargin = sourceShape.OneWayCollisionMargin;

        // 同步变换信息
        CopyCollisionNodeTransform(entity, sourceShape, entityShape);
        return true;
    }

    /// <summary>
    /// 同步 CollisionPolygon2D 碰撞模板
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <param name="sourcePolygon">源碰撞多边形模板</param>
    /// <returns>是否同步成功</returns>
    private static bool SyncCollisionPolygonTemplate(Node entity, CollisionPolygon2D sourcePolygon)
    {
        // 确保实体有对应的碰撞节点
        var entityPolygon = EnsureCollisionNode<CollisionPolygon2D>(entity, sourcePolygon.Name);
        if (entityPolygon == null) return false;

        // 同步碰撞多边形属性
        entityPolygon.Polygon = sourcePolygon.Polygon;
        entityPolygon.BuildMode = sourcePolygon.BuildMode;
        entityPolygon.Disabled = sourcePolygon.Disabled;
        entityPolygon.OneWayCollision = sourcePolygon.OneWayCollision;
        entityPolygon.OneWayCollisionMargin = sourcePolygon.OneWayCollisionMargin;

        // 同步变换信息
        CopyCollisionNodeTransform(entity, sourcePolygon, entityPolygon);
        return true;
    }

    /// <summary>
    /// 确保实体拥有指定类型的碰撞节点
    /// <para>
    /// 如果实体已有碰撞节点，则复用并重命名；
    /// 如果没有或类型不匹配，则创建新的碰撞节点。
    /// </para>
    /// </summary>
    /// <typeparam name="T">碰撞节点类型</typeparam>
    /// <param name="entity">目标实体节点</param>
    /// <param name="nodeName">节点名称</param>
    /// <returns>碰撞节点实例，失败返回 null</returns>
    private static T? EnsureCollisionNode<T>(Node entity, string nodeName) where T : Node2D, new()
    {
        // 查找现有的碰撞节点
        var existingCollision = FindRootCollisionNode(entity);
        if (existingCollision is T typedCollision)
        {
            // 类型匹配，直接复用并重命名
            typedCollision.Name = nodeName;
            return typedCollision;
        }

        // 类型不匹配，删除现有碰撞节点
        if (existingCollision != null)
        {
            entity.RemoveChild(existingCollision);
            existingCollision.QueueFree();
        }

        // 创建新的碰撞节点
        var collisionNode = new T
        {
            Name = nodeName
        };
        entity.AddChild(collisionNode);
        return collisionNode;
    }

    /// <summary>
    /// 查找实体根节点下的碰撞节点
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <returns>找到的碰撞节点，未找到返回 null</returns>
    private static Node2D? FindRootCollisionNode(Node entity)
    {
        foreach (Node child in entity.GetChildren())
        {
            if (child is CollisionShape2D or CollisionPolygon2D)
            {
                return child as Node2D;
            }
        }

        return null;
    }

    /// <summary>
    /// 复制碰撞节点的变换信息
    /// <para>
    /// 将源节点的全局变换转换为相对于实体节点的局部变换。
    /// </para>
    /// </summary>
    /// <param name="entity">目标实体节点</param>
    /// <param name="source">源碰撞节点</param>
    /// <param name="target">目标碰撞节点</param>
    private static void CopyCollisionNodeTransform(Node entity, Node2D source, Node2D target)
    {
        if (entity is not Node2D entity2D) return;

        // 将源节点的全局变换转换为相对于实体的局部变换
        target.Transform = entity2D.GlobalTransform.AffineInverse() * source.GlobalTransform;
    }
}
