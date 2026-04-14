using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 关系约束类型
/// </summary>
public enum RelationshipConstraint
{
    /// <summary>无约束（多对多）</summary>
    None,
    /// <summary>一对一（子只能有一个父）</summary>
    OneToOne,
    /// <summary>一对多（父可以有多个子，子只能有一个父）</summary>
    OneToMany
}

/// <summary>
/// Entity 关系管理器
/// 
/// 职责范围：
/// - Entity-Component 关系：Entity 拥有哪些 Component
/// - Entity-Entity 关系：装备、Buff、父子关系等
/// - 高效查询：基于三索引结构（父索引、子索引、类型索引）
/// 
/// 核心关系类型：
/// - ENTITY_TO_COMPONENT：Entity 与 Component 的组合关系
/// - UNIT_TO_ITEM：单位装备物品
/// - UNIT_TO_BUFF：单位拥有 Buff
/// - PARENT：通用父子关系
/// 
/// 设计优势：
/// - 数据源唯一：所有关系都通过本管理器维护
/// - 支持任意层级：不依赖节点树结构
/// - 高性能查询：O(1) 索引查找
/// 
/// 使用示例：
/// <code>
/// // 查询 Entity 的所有 Component
/// var componentIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
///     entityId, 
///     EntityRelationshipType.ENTITY_TO_COMPONENT
/// );
/// 
/// // 查询 Component 所属的 Entity
/// var entityId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
///     componentId,
///     EntityRelationshipType.ENTITY_TO_COMPONENT
/// ).FirstOrDefault();
/// </code>
/// </summary>
public static class EntityRelationshipManager
{
    private static readonly Log _log = new("EntityRelationshipManager", LogLevel.Warning);

    // ==================== 数据结构 ====================

    /// <summary>
    /// 关系记录（公开以支持查询方法返回）
    /// </summary>
    public class RelationshipRecord
    {
        public string ParentEntityId { get; set; } = string.Empty;
        public string ChildEntityId { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public Dictionary<string, object> Data { get; set; } = new();
        /// <summary>优先级（数值越小优先级越高）</summary>
        public int Priority { get; set; } = 0;
    }

    // 主存储：relationshipId -> RelationshipRecord
    private static readonly Dictionary<string, RelationshipRecord> _relationships = new();

    // 三索引结构
    private static readonly Dictionary<string, HashSet<string>> _parentIndex = new();  // parentEntityId -> Set<relationshipId>
    private static readonly Dictionary<string, HashSet<string>> _childIndex = new();   // childEntityId -> Set<relationshipId>
    private static readonly Dictionary<string, HashSet<string>> _typeIndex = new();    // relationType -> Set<relationshipId>

    // 查询缓存（零 GC 优化）
    private static readonly List<string> _tempChildIds = new(32);
    private static readonly List<string> _tempParentIds = new(32);
    private static readonly List<RelationshipRecord> _tempRecords = new(32);

    // ==================== 核心方法 ====================

    /// <summary>
    /// 生成关系 ID
    /// </summary>
    private static string GenerateRelationshipId(string parentId, string childId, string type)
        => $"{parentId}:{childId}:{type}";

    /// <summary>
    /// 添加关系
    /// </summary>
    /// <param name="parentId">父 Entity ID</param>
    /// <param name="childId">子 Entity ID</param>
    /// <param name="relationType">关系类型</param>
    /// <param name="data">关系附加数据（可选）</param>
    /// <param name="constraint">关系约束类型（可选）</param>
    /// <param name="priority">优先级，数值越小优先级越高（可选）</param>
    /// <returns>是否成功添加</returns>
    public static bool AddRelationship(
        string parentId,
        string childId,
        string relationType,
        Dictionary<string, object>? data = null,
        RelationshipConstraint constraint = RelationshipConstraint.None,
        int priority = 0)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);

        // 检查重复
        if (_relationships.ContainsKey(relationshipId))
        {
            _log.Warn($"关系已存在: {parentId} -> {childId} ({relationType})");
            return false;
        }

        // 根据约束类型检查
        if (constraint == RelationshipConstraint.OneToOne || constraint == RelationshipConstraint.OneToMany)
        {
            // 检查子 Entity 是否已有父 Entity
            if (GetParentEntitiesByChildAndType(childId, relationType).Any())
            {
                _log.Warn($"子 Entity 已存在关系: {childId} ({relationType})，约束类型: {constraint}");
                return false;
            }
        }

        // 创建记录
        var record = new RelationshipRecord
        {
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationType = relationType,
            Data = data ?? new(),
            Priority = priority
        };

        // 添加到主存储
        _relationships[relationshipId] = record;

        // 更新三索引
        GetOrCreateSet(_parentIndex, parentId).Add(relationshipId);
        GetOrCreateSet(_childIndex, childId).Add(relationshipId);
        GetOrCreateSet(_typeIndex, relationType).Add(relationshipId);

        // 广播关系添加事件
        GlobalEventBus.Global.Emit(
            GameEventType.Global.RelationshipAdded,
            new GameEventType.Global.RelationshipAddedEventData(
                parentId, // 父实体Id
                childId, // 子实体Id
                relationType // 关系类型
            )
        );

        _log.Debug($"已添加关系: {parentId} -> {childId} ({relationType})");
        return true;
    }

    /// <summary>
    /// 移除关系
    /// </summary>
    public static bool RemoveRelationship(string parentId, string childId, string relationType)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);

        if (!_relationships.Remove(relationshipId))
        {
            _log.Warn($"关系不存在: {parentId} -> {childId} ({relationType})");
            return false;
        }

        // 从三索引移除
        _parentIndex.GetValueOrDefault(parentId)?.Remove(relationshipId);
        _childIndex.GetValueOrDefault(childId)?.Remove(relationshipId);
        _typeIndex.GetValueOrDefault(relationType)?.Remove(relationshipId);

        // 清理空集合
        CleanupEmptySet(_parentIndex, parentId);
        CleanupEmptySet(_childIndex, childId);
        CleanupEmptySet(_typeIndex, relationType);

        // 广播关系移除事件
        GlobalEventBus.Global.Emit(
            GameEventType.Global.RelationshipRemoved,
            new GameEventType.Global.RelationshipRemovedEventData(
                parentId, // 父实体Id
                childId, // 子实体Id
                relationType // 关系类型
            )
        );

        _log.Debug($"已移除关系: {parentId} -> {childId} ({relationType})");
        return true;
    }

    /// <summary>
    /// 设置关系数据
    /// </summary>
    public static bool SetRelationshipData(string parentId, string childId, string relationType, Dictionary<string, object> data)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);

        if (!_relationships.TryGetValue(relationshipId, out var record))
        {
            _log.Warn($"关系不存在: {parentId} -> {childId} ({relationType})");
            return false;
        }

        // 合并数据
        foreach (var kvp in data)
        {
            record.Data[kvp.Key] = kvp.Value;
        }

        return true;
    }

    /// <summary>
    /// 获取关系数据
    /// </summary>
    public static Dictionary<string, object>? GetRelationshipData(string parentId, string childId, string relationType)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);
        return _relationships.GetValueOrDefault(relationshipId)?.Data;
    }

    /// <summary>
    /// 检查关系是否存在
    /// </summary>
    public static bool HasRelationship(string parentId, string childId, string relationType)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);
        return _relationships.ContainsKey(relationshipId);
    }

    // ==================== 查询接口 ====================
    // 注意：根据性能测试，LINQ 在非热路径场景下影响微乎其微（0.004ms/次）
    // 优先考虑代码可读性，除非性能分析器显示瓶颈

    /// <summary>
    /// 获取父 Entity 的所有子 Entity（指定关系类型）
    /// 常用场景：获取玩家的所有物品
    /// <returns>子 Entity ID 列表</returns>
    /// </summary>
    public static IEnumerable<string> GetChildEntitiesByParentAndType(string parentId, string relationType)
    {
        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return Enumerable.Empty<string>();

        return relationshipIds
            // 将关系 ID 转换为实际的关系记录对象
            .Select(id => _relationships.GetValueOrDefault(id))
            // 筛选出符合指定关系类型的记录
            .Where(r => r?.RelationType == relationType)
            // 提取子实体 ID
            .Select(r => r!.ChildEntityId);
    }

    /// <summary>
    /// 获取子 Entity 的所有父 Entity（指定关系类型）
    /// 常用场景：获取物品的拥有者
    /// </summary>
    public static IEnumerable<string> GetParentEntitiesByChildAndType(string childId, string relationType)
    {
        if (!_childIndex.TryGetValue(childId, out var relationshipIds))
            return Enumerable.Empty<string>();

        return relationshipIds
            // 将关系 ID 转换为实际的关系记录对象
            .Select(id => _relationships.GetValueOrDefault(id))
            // 筛选出符合指定关系类型的记录
            .Where(r => r?.RelationType == relationType)
            // 仅提取父实体 ID
            .Select(r => r!.ParentEntityId);
    }

    /// <summary>
    /// 获取父 Entity 的所有关系记录（指定关系类型，支持优先级排序）
    /// 常用场景：获取玩家的所有武器（按槽位排序）
    /// </summary>
    public static IEnumerable<RelationshipRecord> GetChildRelationshipsByParentAndType(
        string parentId,
        string relationType,
        bool sortByPriority = false)
    {
        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return Enumerable.Empty<RelationshipRecord>();

        var records = relationshipIds
            // 将关系 ID 转换为实际的关系记录对象
            .Select(id => _relationships.GetValueOrDefault(id))
            // 筛选出符合指定关系类型的记录
            .Where(r => r?.RelationType == relationType)
            // 转换为非空记录序列
            .Select(r => r!);

        // 按优先级排序
        if (sortByPriority)
        {
            records = records.OrderBy(r => r.Priority);
        }

        return records;
    }

    /// <summary>
    /// 获取父 Entity 的所有关系
    /// </summary>
    public static IEnumerable<RelationshipRecord> GetRelationshipsByParent(string parentId)
    {
        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return Enumerable.Empty<RelationshipRecord>();

        return relationshipIds
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r != null)
            .Select(r => r!);
    }

    /// <summary>
    /// 获取子 Entity 的所有关系
    /// </summary>
    public static IEnumerable<RelationshipRecord> GetRelationshipsByChild(string childId)
    {
        if (!_childIndex.TryGetValue(childId, out var relationshipIds))
            return Enumerable.Empty<RelationshipRecord>();

        return relationshipIds
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r != null)
            .Select(r => r!);
    }

    /// <summary>
    /// 获取指定类型的所有关系
    /// </summary>
    public static IEnumerable<RelationshipRecord> GetRelationshipsByType(string relationType)
    {
        if (!_typeIndex.TryGetValue(relationType, out var relationshipIds))
            return Enumerable.Empty<RelationshipRecord>();

        return relationshipIds
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r != null)
            .Select(r => r!);
    }

    // ==================== 高性能查询接口（零 GC 优化）====================
    // 仅在性能分析器显示瓶颈时使用，适用于热路径场景

    /// <summary>
    /// 【高性能版本】获取父 Entity 的所有子 Entity（指定关系类型）
    /// 注意：返回的 List 会在下次调用时被清空，如需保留请复制
    /// 使用场景：每帧调用的热路径（如 AI 寻敌）
    /// </summary>
    public static List<string> GetChildEntitiesByParentAndTypeFast(string parentId, string relationType)
    {
        _tempChildIds.Clear();

        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return _tempChildIds;

        foreach (var id in relationshipIds)
        {
            if (_relationships.TryGetValue(id, out var record) &&
                record.RelationType == relationType)
            {
                _tempChildIds.Add(record.ChildEntityId);
            }
        }

        return _tempChildIds;
    }

    /// <summary>
    /// 【高性能版本】获取父 Entity 的所有关系记录（指定关系类型，支持优先级排序）
    /// 注意：返回的 List 会在下次调用时被清空，如需保留请复制
    /// 使用场景：每帧调用的热路径
    /// </summary>
    public static List<RelationshipRecord> GetChildRelationshipsByParentAndTypeFast(
        string parentId,
        string relationType,
        bool sortByPriority = false)
    {
        _tempRecords.Clear();

        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return _tempRecords;

        foreach (var id in relationshipIds)
        {
            if (_relationships.TryGetValue(id, out var record) &&
                record.RelationType == relationType)
            {
                _tempRecords.Add(record);
            }
        }

        // 按优先级排序
        if (sortByPriority && _tempRecords.Count > 1)
        {
            _tempRecords.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        return _tempRecords;
    }

    // ==================== 批量操作 ====================

    /// <summary>
    /// 移除 Entity 的所有关系（Entity 销毁时调用）
    /// </summary>
    public static void RemoveAllRelationships(string entityId)
    {
        // 清理作为父 Entity 的关系
        if (_parentIndex.TryGetValue(entityId, out var relationshipIds))
        {
            var parentRecords = relationshipIds
                .Select(relId => _relationships.GetValueOrDefault(relId))
                .Where(r => r != null)
                .ToList();

            foreach (var record in parentRecords)
            {
                RemoveRelationship(record!.ParentEntityId, record.ChildEntityId, record.RelationType);
            }
        }

        // 清理作为子 Entity 的关系
        if (_childIndex.TryGetValue(entityId, out var childRels))
        {
            var childRecords = childRels
                .Select(relId => _relationships.GetValueOrDefault(relId))
                .Where(r => r != null)
                .ToList();

            foreach (var record in childRecords)
            {
                RemoveRelationship(record!.ParentEntityId, record.ChildEntityId, record.RelationType);
            }
        }

        _log.Debug($"已清理 Entity 的所有关系: {entityId}");
    }

    /// <summary>
    /// 移除指定类型的所有关系
    /// </summary>
    public static void RemoveRelationshipsByType(string relationType)
    {
        if (!_typeIndex.TryGetValue(relationType, out var relationshipIds))
            return;

        // 复制列表避免迭代时修改
        var recordsToRemove = relationshipIds
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r != null)
            .ToList();

        foreach (var record in recordsToRemove)
        {
            RemoveRelationship(record!.ParentEntityId, record.ChildEntityId, record.RelationType);
        }

        _log.Info($"已移除所有类型为 {relationType} 的关系，共 {recordsToRemove.Count} 个");
    }

    // ==================== 所有权链查找 ====================
    /// <summary>
    /// 查找第一个符合类型的实体（包括自身或沿 PARENT 关系向上查找）
    /// <para>便捷重载，自动获取实体 ID。</para>
    /// <para>优先检查传入节点本身是否符合目标类型，如果是则直接返回。</para>
    /// <para>找不到时会自动打印警告日志。</para>
    /// </summary>
    public static T? FindAncestorOfType<T>(Godot.Node entity, int maxDepth = 10) where T : class
    {
        if (entity == null)
        {
            _log.Error($"FindAncestorOfType 传入节点为 null，无法查找 {typeof(T).Name}");
            return null;
        }

        // 1. 首先检查传入的节点本身是否符合目标类型
        if (entity is T typedEntity)
        {
            return typedEntity;
        }

        // 2. 沿 PARENT 关系向上查找
        return FindAncestorOfType<T>(entity.GetInstanceId().ToString(), maxDepth);
    }

    /// <summary>
    /// 查找第一个符合类型的实体（包括自身或沿 PARENT 关系向上查找）
    /// <para>优先检查传入节点本身是否符合目标类型，如果是则直接返回。</para>
    /// <para>常用场景：子弹→武器→角色，查找角色以进行统计归属/吸血/暴击等。</para>
    /// <para>找不到时会自动打印警告日志。</para>
    /// </summary>
    /// <typeparam name="T">目标类型（如 IUnit、IEntity）</typeparam>
    /// <param name="entityId">起始实体 ID</param>
    /// <param name="maxDepth">最大查找深度（防止无限循环，默认 10）</param>
    /// <returns>找到的目标类型实体，未找到返回 null</returns>
    private static T? FindAncestorOfType<T>(string entityId, int maxDepth = 10) where T : class
    {
        // 1. 首先检查传入的实体本身是否符合目标类型
        var startEntity = EntityManager.GetEntityById(entityId);
        if (startEntity is T startTypedEntity)
        {
            return startTypedEntity;
        }

        // 2. 沿 PARENT 关系向上查找
        string currentId = entityId;
        int depth = 0;

        while (depth < maxDepth)
        {
            // 查找当前实体的 PARENT
            var parentId = GetParentEntitiesByChildAndType(currentId, EntityRelationshipType.PARENT)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(parentId))
            {
                // 没有更多父级了
                break;
            }

            // 获取父级实体
            var parentEntity = EntityManager.GetEntityById(parentId);
            if (parentEntity == null)
            {
                _log.Warn($"父级实体 {parentId} 已不存在，终止向上查找");
                break;
            }

            // 检查是否符合目标类型
            if (parentEntity is T typedEntity)
            {
                return typedEntity;
            }

            // 继续向上
            currentId = parentId;
            depth++;
        }

        // 找不到时打印警告
        _log.Warn($"未能在实体 {entityId}({startEntity?.GetType().Name ?? "null"}) 的层级链上找到类型 {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 获取从 startNode 沿 PARENT 关系向上的所有实体链（包括自身，如果是 IEntity）
    /// <para>常用场景：伤害统计时遍历攻击链（子弹→武器→角色），为每个 IStatisticsTarget 累加数据。</para>
    /// </summary>
    /// <param name="startNode">起始节点</param>
    /// <param name="maxDepth">最大查找深度（防止无限循环，默认 10）</param>
    /// <returns>从起始节点到最顶层的所有 IEntity</returns>
    public static System.Collections.Generic.IEnumerable<IEntity> GetAncestorChain(Godot.Node startNode, int maxDepth = 10)
    {
        if (startNode == null) yield break;

        // 1. 检查起始节点自身
        if (startNode is IEntity startEntity)
            yield return startEntity;

        // 2. 沿 PARENT 关系向上遍历
        string currentId = startNode.GetInstanceId().ToString();
        int depth = 0;

        while (depth < maxDepth)
        {
            var parentId = GetParentEntitiesByChildAndType(currentId, EntityRelationshipType.PARENT)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(parentId)) break;

            var parentEntity = EntityManager.GetEntityById(parentId);
            if (parentEntity == null) break;

            if (parentEntity is IEntity entity)
                yield return entity;

            currentId = parentId;
            depth++;
        }
    }

    // ==================== 工具方法 ====================

    /// <summary>
    /// 获取或创建 HashSet
    /// </summary>
    private static HashSet<string> GetOrCreateSet(Dictionary<string, HashSet<string>> dict, string key)
    {
        // dict为空时，直接创建新集合
        if (!dict.TryGetValue(key, out var set))
        {
            set = new HashSet<string>();
            dict[key] = set;
        }
        return set;
    }

    /// <summary>
    /// 清理空集合
    /// </summary>
    private static void CleanupEmptySet(Dictionary<string, HashSet<string>> dict, string key)
    {
        if (dict.TryGetValue(key, out var set) && set.Count == 0)
        {
            dict.Remove(key);
        }
    }

    /// <summary>
    /// 清理所有数据
    /// </summary>
    public static void Clear()
    {
        _relationships.Clear();
        _parentIndex.Clear();
        _childIndex.Clear();
        _typeIndex.Clear();
        _tempChildIds.Clear();
        _tempParentIds.Clear();
        _tempRecords.Clear();
        _log.Info("EntityRelationshipManager 已清空");
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public static (int TotalRelationships, int ParentCount, int ChildCount, int TypeCount) GetStats()
    {
        return (
            _relationships.Count,
            _parentIndex.Count,
            _childIndex.Count,
            _typeIndex.Count
        );
    }

    // ==================== 调试方法 ====================

    /// <summary>
    /// 获取所有关系的统计信息（调试用）
    /// </summary>
    public static string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== EntityRelationshipManager 统计信息 ===");
        sb.AppendLine($"总关系数: {_relationships.Count}");
        sb.AppendLine($"父实体数: {_parentIndex.Count}");
        sb.AppendLine($"子实体数: {_childIndex.Count}");
        sb.AppendLine($"关系类型数: {_typeIndex.Count}");
        sb.AppendLine();

        // 按类型统计
        sb.AppendLine("=== 按类型统计 ===");
        foreach (var kvp in _typeIndex)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value.Count} 个关系");
        }
        sb.AppendLine();

        // 列出所有关系
        sb.AppendLine("=== 所有关系 ===");
        foreach (var kvp in _relationships)
        {
            var r = kvp.Value;
            sb.AppendLine($"[{r.RelationType}] {r.ParentEntityId} -> {r.ChildEntityId} (优先级: {r.Priority})");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取指定 Entity 的所有关系（调试用）
    /// </summary>
    public static string GetEntityDebugInfo(string entityId)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Entity {entityId} 的关系 ===");

        // 作为父实体的关系
        sb.AppendLine("作为父实体:");
        var parentRels = GetRelationshipsByParent(entityId);
        foreach (var r in parentRels)
        {
            sb.AppendLine($"  -> {r.ChildEntityId} ({r.RelationType}, 优先级: {r.Priority})");
        }

        // 作为子实体的关系
        sb.AppendLine("作为子实体:");
        var childRels = GetRelationshipsByChild(entityId);
        foreach (var r in childRels)
        {
            sb.AppendLine($"  <- {r.ParentEntityId} ({r.RelationType}, 优先级: {r.Priority})");
        }

        return sb.ToString();
    }
}
