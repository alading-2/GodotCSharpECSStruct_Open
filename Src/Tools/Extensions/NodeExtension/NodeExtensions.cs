using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Node 扩展方法工具类
/// 核心职责：
/// 1. 为 Godot Node 提供类似于 ECS Entity 的数据挂载能力 (Data Component)
/// 2. 提供其他常用的 Node 操作扩展
/// 
/// 核心原理：
/// 1. **扩展方法 (Extension Methods)**: 
///    - 允许向现有类型 (Node) "添加" 方法，而无需创建新的派生类型、重新编译或以其他方式修改原始类型。
///    - 使得调用代码看起来像是 `node.GetData()`，实际上是编译器转换为 `NodeExtensions.GetData(node)`。
/// 
/// 2. **条件弱表 (ConditionalWeakTable)**:
///    - 这是一个线程安全的键值对集合，专门用于动态地将附加状态与对象关联。
///    - **弱引用键 (Weak Reference Key)**: 它持有键 (Node) 的弱引用，不会阻止垃圾回收器 (GC) 回收该 Node。
///    - **自动生命周期管理**: 一旦 Node 被 GC 回收，表中对应的 Data 也会自动从表中移除并被回收。
/// </summary>
public static class NodeExtensions
{
    private static readonly ConditionalWeakTable<Node, Data> _nodeDataMap = new();
    private const string COMPONENT_KEY = "Component";

    /// <summary>
    /// 获取节点关联的 Data 对象。
    /// 如果该节点尚未关联 Data，会自动创建一个新的 Data 实例并关联。
    /// </summary>
    /// <param name="node">目标 Godot 节点</param>
    /// <returns>关联 of Data 对象</returns>
    public static Data GetData(this Node node)
    {
        return _nodeDataMap.GetValue(node, _ => new Data());
    }

    /// <summary>
    /// 检查节点当前是否已关联 Data 对象。
    /// 不会创建新对象。
    /// </summary>
    /// <param name="node">目标 Godot 节点</param>
    /// <returns>如果已存在 Data 返回 true，否则返回 false</returns>
    public static bool HasData(this Node node)
    {
        return _nodeDataMap.TryGetValue(node, out _);
    }

    /// <summary>
    /// 尝试获取节点关联的 Data 对象。
    /// 不会创建新对象。
    /// </summary>
    /// <param name="node">目标 Godot 节点</param>
    /// <param name="data">输出的 Data 对象，如果不存在则为 null</param>
    /// <returns>如果获取成功返回 true</returns>
    public static bool TryGetData(this Node node, out Data? data)
    {
        return _nodeDataMap.TryGetValue(node, out data);
    }

    // ================= 组件管理 (Component Management) =================

    /// <summary>
    /// 获取组件字典。
    /// 组件存储在 Data 容器的 "Component" 键下。
    /// </summary>
    private static Dictionary<string, Node> GetComponentsDict(this Node node)
    {
        var data = node.GetData();
        if (!data.TryGetValue<Dictionary<string, Node>>(COMPONENT_KEY, out var dict))
        {
            dict = new Dictionary<string, Node>();
            data.Set(COMPONENT_KEY, dict);
        }
        return dict;
    }

    /// <summary>
    /// 根据名称获取组件。
    /// 索引使用 string 映射。
    /// </summary>
    /// <typeparam name="T">期望的组件类型</typeparam>
    /// <param name="node">目标节点</param>
    /// <param name="name">组件在 ECSIndex 中定义的名称</param>
    /// <returns>找到的组件或 null</returns>
    public static T? GetComponent<T>(this Node node, string name) where T : Node
    {
        var dict = node.GetComponentsDict();

        if (dict.TryGetValue(name, out var comp))
        {
            if (GodotObject.IsInstanceValid(comp))
            {
                return comp as T;
            }
            else
            {
                // 实例已失效，移除
                dict.Remove(name);
            }
        }

        // 备选逻辑：查找直接子节点（如果名称匹配且类型匹配）
        foreach (var child in node.GetChildren())
        {
            if (child.Name == name && child is T t)
            {
                dict[name] = child;
                return t;
            }
        }

        return null;
    }

    /// <summary>
    /// 添加组件（通过场景实例化）。
    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="node">目标节点</param>
    /// <param name="name">组件在 ECSIndex 中定义的名称</param>
    /// <returns>新创建或已存在的组件</returns>
    public static T AddComponent<T>(this Node node, string name) where T : Node
    {
        var existing = node.GetComponent<T>(name);
        if (existing != null) return existing;

        var path = ECSIndex.Get(name);
        if (string.IsNullOrEmpty(path))
        {
            GD.PrintErr($"[NodeExtensions] AddComponent 失败：找不到组件 '{name}' 的路径。请在 ECSIndex 中注册。");
            return null!;
        }

        var scene = GD.Load<PackedScene>(path);
        if (scene == null)
        {
            GD.PrintErr($"[NodeExtensions] AddComponent 失败：无法加载组件场景 '{path}'");
            return null!;
        }

        var comp = scene.Instantiate<T>();
        comp.Name = name;
        node.AddChild(comp);

        var dict = node.GetComponentsDict();
        dict[name] = comp;

        return comp;
    }

    /// <summary>
    /// 确保组件存在，不存在则添加。
    /// </summary>
    public static T EnsureComponent<T>(this Node node, string name) where T : Node
    {
        var comp = node.GetComponent<T>(name);
        return comp ?? node.AddComponent<T>(name);
    }

    /// <summary>
    /// 移除组件。
    /// </summary>
    public static void RemoveComponent(this Node node, string name)
    {
        var dict = node.GetComponentsDict();
        if (dict.TryGetValue(name, out var comp))
        {
            if (GodotObject.IsInstanceValid(comp))
            {
                comp.QueueFree();
            }
            dict.Remove(name);
        }
    }

    /// <summary>
    /// 手动注册组件。
    /// </summary>
    public static void RegisterComponent(this Node node, string name, Node component)
    {
        if (component == null) return;
        var dict = node.GetComponentsDict();
        dict[name] = component;
    }

    /// <summary>
    /// 获取组件快捷访问代理。
    /// 用法：node.Component().Health
    /// </summary>
    public static ComponentAccessor Component(this Node node) => new ComponentAccessor(node);
}

/// <summary>
/// 组件访问代理结构体，提供强类型快捷访问。
/// </summary>
public readonly struct ComponentAccessor
{
    private readonly Node _node;
    public ComponentAccessor(Node node) => _node = node;

    // 基础组件
    public AttributeComponent? AttributeComponent => _node.GetComponent<AttributeComponent>(ECSIndex.Component.AttributeComponent);
    public HealthComponent? HealthComponent => _node.GetComponent<HealthComponent>(ECSIndex.Component.HealthComponent);
    public VelocityComponent? VelocityComponent => _node.GetComponent<VelocityComponent>(ECSIndex.Component.VelocityComponent);
    public HitboxComponent? HitboxComponent => _node.GetComponent<HitboxComponent>(ECSIndex.Component.HitboxComponent);
    public HurtboxComponent? HurtboxComponent => _node.GetComponent<HurtboxComponent>(ECSIndex.Component.HurtboxComponent);
    public FollowComponent? FollowComponent => _node.GetComponent<FollowComponent>(ECSIndex.Component.FollowComponent);
    public PickupComponent? PickupComponent => _node.GetComponent<PickupComponent>(ECSIndex.Component.PickupComponent);

    /// <summary>
    /// 通用获取方法
    /// </summary>
    public T? Get<T>(string name) where T : Node => _node.GetComponent<T>(name);
}
