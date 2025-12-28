using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 对象池父节点管理器
/// 负责管理对象池对应的场景父节点，以及自动创建层级结构
/// </summary>
public static class ParentManager
{
    private static readonly Log _log = new Log("ParentManager");
    private static readonly Dictionary<string, Node> _poolParents = new();

    /// <summary>
    /// 获取指定池名称对应的父节点
    /// </summary>
    public static Node? GetParent(string poolName)
    {
        return _poolParents.GetValueOrDefault(poolName);
    }

    /// <summary>
    /// 注册池的父节点路径
    /// 自动从 CurrentScene 或 Root 开始查找/创建路径
    /// </summary>
    public static void RegisterParent(string poolName, string path)
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;

        if (root == null)
        {
            _log.Error($"无法为 {poolName} 注册路径 {path}: 未找到场景根节点");
            return;
        }

        Node current = root;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            var next = current.GetNodeOrNull(segment);
            if (next == null)
            {
                next = new Node { Name = segment };
                current.AddChild(next);
            }
            current = next;
        }

        _poolParents[poolName] = current;
    }

}
