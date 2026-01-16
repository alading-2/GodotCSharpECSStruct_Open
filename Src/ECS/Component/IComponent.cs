using Godot;
using System.Linq;

/// <summary>
/// Component 标记接口
/// 所有 Component 应实现此接口，以便 EntityManager 自动识别和注册
/// 
/// 标准实现模式：
/// <code>
/// public partial class MyComponent : Node, IComponent
/// {
///     private Data? _data;
///     private IEntity? _entity;
///     
///     public void OnComponentRegistered(Node entity)
///     {
///         if (entity is IEntity iEntity)
///         {
///             _data = iEntity.Data;
///             _entity = iEntity;
///         }
///     }
///     
///     public void OnComponentUnregistered()
///     {
///         _data = null;
///         _entity = null;
///     }
/// }
/// </code>
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Component 注册到 Entity 时的回调
    /// 在此方法中缓存 Entity 引用和 Data 容器,并订阅事件
    /// 
    /// ⚠️  数据访问时序说明:
    ///     - 此时可以访问"配置数据"(如敌人基础属性: HP, Speed, Damage)
    ///     - "运行时初始数据"(如 SkillLevel, Target)通常在 Spawn 之后才设置
    ///     - 如需响应后续数据设置,应在此方法中监听 PropertyChanged 事件
    /// 
    /// 注意：此时 Entity-Component 关系已由 EntityManager 自动建立
    /// </summary>
    /// <param name="entity">所属的 Entity 节点</param>
    void OnComponentRegistered(Node entity);

    /// <summary>
    /// Component 从 Entity 注销时的回调
    /// 可用于清理资源等
    /// </summary>
    void OnComponentUnregistered();

    /// <summary>
    /// Entity OnPoolRelease 归还池时调用
    /// </summary>
    void OnComponentReset() { }
}

