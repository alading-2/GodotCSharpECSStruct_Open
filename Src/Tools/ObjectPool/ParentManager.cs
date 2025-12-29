using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 对象池父节点管理器 (AutoLoad)
/// 负责管理对象池对应的场景父节点，以及自动创建层级结构。
/// 作为 AutoLoad 运行，确保在游戏启动初期就拥有稳定的场景树挂载点。
/// </summary>
public partial class ParentManager : Node
{
    private static readonly Log _log = new Log("ParentManager");
    private readonly Dictionary<string, Node> _poolParents = new();

    /// <summary>
    /// 全局单例引用
    /// </summary>
    public static ParentManager Instance { get; private set; } = null!;

    /// <summary>
    /// 模块初始化：以核心优先级注册，确保在 System 级别的对象池初始化前就绪。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register("ParentManager", "res://Src/Tools/ObjectPool/ParentManager.cs", AutoLoad.Priority.Core);
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>
    /// 获取指定池名称对应的父节点
    /// </summary>
    public Node? GetParent(string poolName)
    {
        var node = _poolParents.GetValueOrDefault(poolName);
        return IsInstanceValid(node) ? node : null;
    }

    /// <summary>
    /// 注册池的父节点路径
    /// 自动在场景树根节点 (Root) 下创建层级，确保对象池节点全局持久化且在远程调试中位置明确。
    /// </summary>
    public void RegisterParent(string poolName, string path)
    {
        if (!IsInsideTree())
        {
            _log.Error($"无法为 {poolName} 注册路径 {path}: ParentManager 尚未进入场景树。");
            return;
        }

        Node root = GetTree().Root;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Node current = root;

        foreach (var segment in segments)
        {
            Node? next = current.GetNodeOrNull(segment);

            if (next == null)
            {
                next = new Node { Name = segment };
                // 使用 CallDeferred 避免初始化期间的树锁定问题
                if (current == root)
                {
                    current.CallDeferred(Node.MethodName.AddChild, next);
                }
                else
                {
                    current.AddChild(next);
                }
            }
            current = next;
        }

        _poolParents[poolName] = current;
    }

    private bool IsInstanceValid(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
