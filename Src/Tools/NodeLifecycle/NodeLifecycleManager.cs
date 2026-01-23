using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 通用 Node 生命周期管理器
/// 
/// 职责：提供底层的 Node 注册、查询、注销功能
/// 
/// 设计理念：
/// - EntityManager（管理 IEntity）和 UIManager（管理 UIBase）都基于此类构建
/// - 本类只负责"注册表"管理，不涉及具体业务逻辑
/// - 关系管理由 EntityRelationshipManager 负责
/// 
/// 使用示例：
/// <code>
/// 注册
/// NodeLifecycleManager.Register(node);
/// 
/// 查询
/// var node = NodeLifecycleManager.GetNodeById(id);
/// var enemies = NodeLifecycleManager.GetNodesByType<Enemy>();
/// 
/// 注销
/// NodeLifecycleManager.Unregister(node);
/// </code>
/// </summary>
public static class NodeLifecycleManager
{
    private static readonly Log _log = new("NodeLifecycleManager", LogLevel.Warning);

    // ==================== 核心数据结构 ====================

    /// <summary>
    /// 全局节点注册表：InstanceId -> Node
    /// </summary>
    private static readonly Dictionary<string, Node> _nodes = new();

    /// <summary>
    /// 分类索引：NodeType -> HashSet<Node>;
    /// </summary>
    private static readonly Dictionary<string, HashSet<Node>> _nodesByType = new();

    // ==================== 注册 ====================

    /// <summary>
    /// 注册 Node 到管理器
    /// </summary>
    /// <param name="node">要注册的节点</param>
    /// <returns>是否成功注册（false 表示已存在）</returns>
    public static bool Register(Node node)
    {
        string id = node.GetInstanceId().ToString();
        string nodeType = node.GetType().Name;

        // 防止重复注册
        if (_nodes.ContainsKey(id))
        {
            _log.Warn($"Node {id} ({nodeType}) 已注册，跳过");
            return false;
        }

        _nodes[id] = node;

        // 更新类型索引
        if (!_nodesByType.ContainsKey(nodeType))
            _nodesByType[nodeType] = new HashSet<Node>();
        _nodesByType[nodeType].Add(node);

        _log.Debug($"已注册 Node: {nodeType} (ID: {id})");
        return true;
    }

    /// <summary>
    /// 检查 Node 是否已注册
    /// </summary>
    public static bool IsRegistered(string nodeId)
    {
        return _nodes.ContainsKey(nodeId);
    }

    /// <summary>
    /// 检查 Node 是否已注册
    /// </summary>
    public static bool IsRegistered(Node node)
    {
        return IsRegistered(node.GetInstanceId().ToString());
    }

    // ==================== 注销 ====================

    /// <summary>
    /// 从管理器注销 Node
    /// </summary>
    /// <param name="node">要注销的节点</param>
    /// <returns>是否成功注销（false 表示不存在）</returns>
    public static bool Unregister(Node node)
    {
        return Unregister(node.GetInstanceId().ToString());
    }

    /// <summary>
    /// 从管理器注销 Node（通过 ID）
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <returns>是否成功注销</returns>
    public static bool Unregister(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
        {
            _log.Warn($"Node {nodeId} 未注册，无法注销");
            return false;
        }

        _nodes.Remove(nodeId);

        // 从类型索引中移除
        foreach (var set in _nodesByType.Values)
        {
            set.Remove(node);
        }

        // 清理空集合
        var emptyTypes = _nodesByType.Where(kvp => kvp.Value.Count == 0).Select(kvp => kvp.Key).ToList();
        foreach (var type in emptyTypes)
        {
            _nodesByType.Remove(type);
        }

        _log.Debug($"已注销 Node: {nodeId}");
        return true;
    }

    // ==================== 查询 ====================

    /// <summary>
    /// 根据 ID 获取 Node
    /// </summary>
    public static Node? GetNodeById(string nodeId)
    {
        return _nodes.GetValueOrDefault(nodeId);
    }

    /// <summary>
    /// 按类型查询所有 Node
    /// </summary>
    /// <typeparam name="T">Node 类型</typeparam>
    /// <returns>匹配的节点集合</returns>
    public static IEnumerable<T> GetNodesByType<T>() where T : Node
    {
        if (!_nodesByType.TryGetValue(typeof(T).Name, out var set))
            return Enumerable.Empty<T>();
        return set.OfType<T>();
    }

    /// <summary>
    /// 获取所有已注册的 Node
    /// </summary>
    public static IEnumerable<Node> GetAllNodes()
    {
        return _nodes.Values;
    }

    /// <summary>
    /// 获取所有实现指定接口/基类的 Node
    /// </summary>
    /// <typeparam name="T">接口或基类类型</typeparam>
    public static IEnumerable<T> GetNodesByInterface<T>() where T : class
    {
        return _nodes.Values.OfType<T>();
    }

    // ==================== 统计与清理 ====================

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public static (int TotalNodes, int TypeCount) GetStats()
    {
        return (_nodes.Count, _nodesByType.Count);
    }

    /// <summary>
    /// 清理所有注册
    /// </summary>
    public static void Clear()
    {
        int count = _nodes.Count;
        _nodes.Clear();
        _nodesByType.Clear();
        _log.Info($"NodeLifecycleManager 已清空，共清理 {count} 个 Node");
    }

    // ==================== 调试方法 ====================

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public static string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== NodeLifecycleManager 统计信息 ===");
        sb.AppendLine($"总节点数: {_nodes.Count}");
        sb.AppendLine($"类型数: {_nodesByType.Count}");
        sb.AppendLine();

        sb.AppendLine("=== 按类型统计 ===");
        foreach (var kvp in _nodesByType)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value.Count} 个");
        }

        return sb.ToString();
    }
}
