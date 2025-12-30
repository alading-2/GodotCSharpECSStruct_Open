using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 关系管理器
/// 负责管理 Entity 之间的关系，采用三索引结构实现高效查询
/// </summary>
public static class EntityRelationshipManager
{
    private static readonly Log _log = new("EntityRelationshipManager");

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
    }

    // 主存储：relationshipId -> RelationshipRecord
    private static readonly Dictionary<string, RelationshipRecord> _relationships = new();

    // 三索引结构
    private static readonly Dictionary<string, HashSet<string>> _parentIndex = new();  // parentEntityId -> Set<relationshipId>
    private static readonly Dictionary<string, HashSet<string>> _childIndex = new();   // childEntityId -> Set<relationshipId>
    private static readonly Dictionary<string, HashSet<string>> _typeIndex = new();    // relationType -> Set<relationshipId>

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
    /// <returns>是否成功添加</returns>
    public static bool AddRelationship(string parentId, string childId, string relationType, Dictionary<string, object>? data = null)
    {
        string relationshipId = GenerateRelationshipId(parentId, childId, relationType);

        // 检查重复
        if (_relationships.ContainsKey(relationshipId))
        {
            _log.Warn($"关系已存在: {parentId} -> {childId} ({relationType})");
            return false;
        }

        // 子 Entity 的关系不能重复（一个物品只能属于一个玩家）
        if (GetParentEntitiesByChildAndType(childId, relationType).Any())
        {
            _log.Warn($"子 Entity 已存在关系: {childId} ({relationType})");
            return false;
        }

        // 创建记录
        var record = new RelationshipRecord
        {
            ParentEntityId = parentId,
            ChildEntityId = childId,
            RelationType = relationType,
            Data = data ?? new()
        };

        // 添加到主存储
        _relationships[relationshipId] = record;

        // 更新三索引
        GetOrCreateSet(_parentIndex, parentId).Add(relationshipId);
        GetOrCreateSet(_childIndex, childId).Add(relationshipId);
        GetOrCreateSet(_typeIndex, relationType).Add(relationshipId);

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

    /// <summary>
    /// 获取父 Entity 的所有子 Entity（指定关系类型）
    /// 常用场景：获取玩家的所有物品
    /// </summary>
    public static IEnumerable<string> GetChildEntitiesByParentAndType(string parentId, string relationType)
    {
        if (!_parentIndex.TryGetValue(parentId, out var relationshipIds))
            return Enumerable.Empty<string>();

        return relationshipIds
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r?.RelationType == relationType)
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
            .Select(id => _relationships.GetValueOrDefault(id))
            .Where(r => r?.RelationType == relationType)
            .Select(r => r!.ParentEntityId);
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
            .Where(r => r != null)!;
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
            .Where(r => r != null)!;
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
            .Where(r => r != null)!;
    }

    // ==================== 批量操作 ====================

    /// <summary>
    /// 移除 Entity 的所有关系（Entity 销毁时调用）
    /// </summary>
    public static void RemoveAllRelationships(string entityId)
    {
        // 清理作为父 Entity 的关系
        if (_parentIndex.TryGetValue(entityId, out var parentRels))
        {
            foreach (var relId in parentRels.ToList())
            {
                var record = _relationships.GetValueOrDefault(relId);
                if (record != null)
                    RemoveRelationship(record.ParentEntityId, record.ChildEntityId, record.RelationType);
            }
        }

        // 清理作为子 Entity 的关系
        if (_childIndex.TryGetValue(entityId, out var childRels))
        {
            foreach (var relId in childRels.ToList())
            {
                var record = _relationships.GetValueOrDefault(relId);
                if (record != null)
                    RemoveRelationship(record.ParentEntityId, record.ChildEntityId, record.RelationType);
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
        var idsToRemove = relationshipIds.ToList();

        foreach (var id in idsToRemove)
        {
            var record = _relationships.GetValueOrDefault(id);
            if (record != null)
                RemoveRelationship(record.ParentEntityId, record.ChildEntityId, record.RelationType);
        }

        _log.Info($"已移除所有类型为 {relationType} 的关系，共 {idsToRemove.Count} 个");
    }

    // ==================== 工具方法 ====================

    /// <summary>
    /// 获取或创建 HashSet
    /// </summary>
    private static HashSet<string> GetOrCreateSet(Dictionary<string, HashSet<string>> dict, string key)
    {
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
}
