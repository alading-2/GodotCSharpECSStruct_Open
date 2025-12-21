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

    /// <summary>
    /// 获取节点关联的 Data 对象。
    /// 如果该节点尚未关联 Data，会自动创建一个新的 Data 实例并关联。
    /// </summary>
    /// <param name="node">目标 Godot 节点</param>
    /// <returns>关联的 Data 对象</returns>
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
}
