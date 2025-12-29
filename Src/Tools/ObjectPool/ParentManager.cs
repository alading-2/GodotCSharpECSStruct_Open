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
    private static readonly Dictionary<string, Node> _poolParents = new();
    private static ParentManager _instance = null!;

    /// <summary>
    /// 模块初始化：以核心优先级注册，确保在 System 级别的对象池初始化前就绪。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register("ParentManager", "res://Src/Tools/ObjectPool/ParentManager.cs", AutoLoad.Priority.Core);
    }

    public override void _Ready()
    {
        _instance = this;
    }

    /// <summary>
    /// 获取指定池名称对应的父节点
    /// </summary>
    public static Node? GetParent(string poolName)
    {
        var node = _poolParents.GetValueOrDefault(poolName);
        return IsInstanceValid(node) ? node : null;
    }

    /// <summary>
    /// 注册池的父节点路径
    /// 自动在 ParentManager 节点下创建层级
    /// </summary>
    public static void RegisterParent(string poolName, string path)
    {
        if (_instance == null)
        {
            _log.Error($"无法为 {poolName} 注册路径 {path}: ParentManager 尚未就绪。请检查 AutoLoad 优先级。");
            return;
        }

        Node current = _instance;
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

    private static bool IsInstanceValid(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
