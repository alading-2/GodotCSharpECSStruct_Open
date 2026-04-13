using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 通用父节点管理器 (静态工具类)
/// <para>负责管理命名父节点的注册与查询，实现自动化的层级结构创建。</para>
/// <para>适用场景：对象池、Entity容器、UI层级等任何需要统一父节点管理的场景。</para>
/// <para>通常由 AutoLoad 初始化，提供全局静态访问。</para>
/// </summary>
public static class ParentManager
{
    private static readonly Log _log = new Log("ParentManager");

    /// <summary> 存储命名父节点的映射表 </summary>
    private static readonly Dictionary<string, Node> _parents = new();

    /// <summary> 场景根节点引用（通常是 Main 场景或全局根节点） </summary>
    private static Node? _root;

    /// <summary> 
    /// 缓存本帧内创建的 Root 子节点。
    /// <para>由于 Root 节点的子节点是通过 CallDeferred 异步添加的，在同一帧内无法通过 GetNode 查找到新创建的节点。</para>
    /// <para>此缓存用于防止因异步延迟导致的重复创建问题。</para>
    /// </summary>
    private static readonly Dictionary<string, Node> _pendingRootNodes = new();

    /// <summary>
    /// 初始化管理器
    /// </summary>
    /// <param name="root">用于挂载所有父节点层级的根节点</param>
    public static void Init(Node root)
    {
        _root = root;
        _parents.Clear();
        _pendingRootNodes.Clear();
        _log.Info("ParentManager 已初始化");
    }

    /// <summary>
    /// 获取指定名称对应的父节点
    /// </summary>
    /// <param name="name">父节点的唯一标识名称</param>
    /// <returns>返回有效的节点引用，如果不存在或已销毁则返回 null</returns>
    public static Node? GetParent(string name)
    {
        var node = _parents.GetValueOrDefault(name);
        return IsInstanceValid(node) ? node : null;
    }

    /// <summary>
    /// 注册命名父节点路径
    /// <para>自动在 Root 节点下按路径创建层级，确保节点在场景中位置明确且易于调试。</para>
    /// </summary>
    /// <param name="name">父节点名称（用于后续查询）</param>
    /// <param name="path">相对路径，例如 "Pools/Bullets" 或 "ECS/Unit"</param>
    public static void Register(string name, string path)
    {
        if (_root == null)
        {
            _log.Error($"无法为 {name} 注册路径 {path}: ParentManager 尚未初始化 (Root 为空)。");
            return;
        }

        // 确保路径中的所有节点都已创建并获取末端节点
        Node targetNode = EnsurePath(_root, path);
        _parents[name] = targetNode;
        _log.Debug($"已注册父节点: {name} -> {path}");
    }

    /// <summary>
    /// 获取或注册父节点（便捷方法）
    /// <para>如果已注册则直接返回，否则先注册再返回</para>
    /// </summary>
    /// <param name="name">父节点名称</param>
    /// <param name="path">相对路径</param>
    /// <returns>父节点引用</returns>
    public static Node GetOrRegister(string name, string path)
    {
        var existing = GetParent(name);
        if (existing != null) return existing;

        Register(name, path);
        return GetParent(name) ?? throw new InvalidOperationException($"注册父节点失败: {name}");
    }

    /// <summary>
    /// 确保指定路径的节点链存在（通用层级创建工具）
    /// </summary>
    /// <param name="ancestor">起始祖先节点</param>
    /// <param name="relativePath">相对路径 (例如 "Zone/Unit")</param>
    /// <returns>返回路径末端的节点</returns>
    public static Node EnsurePath(Node ancestor, string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return ancestor;

        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Node current = ancestor;

        foreach (var segment in segments)
        {
            // 尝试查找现有节点
            Node? next = current.GetNodeOrNull(segment);

            // 特殊逻辑：如果是挂载到全局 Root 的第一级节点，需要检查待创建缓存
            if (next == null && current == _root)
            {
                next = _pendingRootNodes.GetValueOrDefault(segment);
            }

            // 如果节点不存在，则创建它
            if (next == null)
            {
                next = new Node { Name = segment };

                // 核心逻辑：如果是挂载到 Root，必须使用 CallDeferred
                // 因为 AutoLoad 初始化期间场景树可能处于“锁定”状态（正在加载子节点）
                if (current == _root)
                {
                    current.CallDeferred(Node.MethodName.AddChild, next);
                    // 立即记录到缓存，确保同一帧内后续的 EnsurePath 调用能识别到该节点已在创建队列中
                    _pendingRootNodes[segment] = next;
                }
                else
                {
                    // 普通节点可以直接同步添加
                    current.AddChild(next);
                }
            }
            current = next;
        }

        return current;
    }

    /// <summary>
    /// 检查节点是否有效（未销毁且未在删除队列中）
    /// </summary>
    private static bool IsInstanceValid(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
