using Godot;
using System.Linq;

/// <summary>
/// Component 标记接口
/// 所有 Component 应实现此接口，以便 EntityManager 自动识别和注册
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Component 注册到 Entity 时的回调
    /// 可用于获取 Entity 引用、初始化数据等
    /// 注意：此时 Entity-Component 关系已由 EntityManager 自动建立
    /// </summary>
    /// <param name="entity">所属的 Entity 节点</param>
    void OnComponentRegistered(Node entity) { }

    /// <summary>
    /// Component 从 Entity 注销时的回调
    /// 可用于清理资源等
    /// </summary>
    void OnComponentUnregistered() { }
}

