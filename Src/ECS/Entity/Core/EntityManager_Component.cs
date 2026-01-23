using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// EntityManager 的 Component 扩展
/// 
/// 职责：管理 Component 的生命周期（注册、注销、增删查）
/// 注意：核心注册方法（Register, UnregisterEntity）保留在主文件，因为它们同时服务于 Entity 和 Component
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _componentLog = new("EntityManager_Component", LogLevel.Warning);

    // ==================== Component 注册 ====================

    /// <summary>
    /// 自动注册 Entity 的所有 Component（递归查找所有层级）
    /// 识别规则（按优先级）：
    /// 1. 实现了 IComponent 接口（最高优先级）
    /// 2. 类名以 "Component" 结尾（命名约定）
    /// 
    /// 自动建立 Entity-Component 关系（通过 EntityRelationshipManager）
    /// 
    /// 注意：使用 FindChildren() 递归查找，支持任意层级的 Component
    /// 注意：此方法现在为 public，可供 ObjectPoolInit 等外部模块调用
    /// </summary>
    public static void RegisterComponents(Node entity)
    {
        int registeredCount = 0;
        string entityId = entity.GetInstanceId().ToString();

        // 使用 FindChildren 递归查找所有层级的子节点
        // 参数: pattern="*" (匹配所有名字), type="" (所有类型), recursive=true (递归), owned=false (包括非拥有节点)
        var allChildren = entity.FindChildren("*", "Node", true, false);

        foreach (Node child in allChildren)
        {
            bool isComponent = false;
            string componentType = child.GetType().Name;

            // 规则 1：实现了 IComponent 接口（优先级最高）
            if (child is IComponent)
            {
                isComponent = true;
            }
            // 规则 2：类名以 "Component" 结尾（兼容旧代码）
            else if (componentType.EndsWith("Component"))
            {
                isComponent = true;
                _componentLog.Debug($"通过命名约定识别 Component: {componentType}");
            }

            // 注册 Component 并建立关系
            if (isComponent)
            {
                // 1. 注册 Component
                Register(child);

                // 2. 建立 Entity-Component 关系（必须在回调之前）
                string componentId = child.GetInstanceId().ToString();
                EntityRelationshipManager.AddRelationship(
                    entityId,
                    componentId,
                    EntityRelationshipType.ENTITY_TO_COMPONENT
                );

                // 3. 触发 IComponent 回调（此时关系已建立，可安全查询）
                if (child is IComponent component)
                {
                    try
                    {
                        component.OnComponentRegistered(entity);
                        _componentLog.Debug($"触发 IComponent 回调: {componentType}");
                    }
                    catch (Exception ex)
                    {
                        _componentLog.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
                    }
                }

                registeredCount++;
                _componentLog.Info($"已注册 Component: {componentType} 到 Entity: {entity.Name}");
            }
        }

        if (registeredCount > 0)
        {
            _componentLog.Debug($"Entity {entity.Name} 共注册 {registeredCount} 个 Component");
        }
    }

    /// <summary>
    /// 注销 Entity 的所有 Component（包括清理 Entity-Component 关系）
    /// 通过 EntityRelationshipManager 查询关系，而非依赖节点树结构
    /// 优势：支持任意层级、数据源唯一、与注册逻辑一致
    /// 
    /// 注意：此方法为 internal，由 UnregisterEntity 调用
    /// </summary>
    internal static void UnregisterComponents(Node entity)
    {
        int unregisteredCount = 0;
        string entityId = entity.GetInstanceId().ToString();

        // 通过关系管理器获取所有 Component ID（而非 GetChildren）
        var componentIds = EntityRelationshipManager
            .GetChildEntitiesByParentAndType(entityId, EntityRelationshipType.ENTITY_TO_COMPONENT)
            .ToList(); // 转为 List 避免迭代时修改集合

        foreach (var componentId in componentIds)
        {
            // 通过 ID 获取 Component 节点
            var component = GetEntityById(componentId);
            if (component == null)
            {
                _componentLog.Warn($"Component {componentId} 已不存在，跳过注销");
                continue;
            }

            // 从 NodeLifecycleManager 注销
            NodeLifecycleManager.Unregister(component);

            // 移除 Entity-Component 关系
            EntityRelationshipManager.RemoveRelationship(
                entityId,
                componentId,
                EntityRelationshipType.ENTITY_TO_COMPONENT
            );

            // 触发 IComponent 回调
            if (component is IComponent icomp)
            {
                try
                {
                    // 先重置组件状态（对象池复用前的清理）
                    icomp.OnComponentReset();
                    // 再触发注销回调
                    icomp.OnComponentUnregistered();
                    _componentLog.Debug($"触发 IComponent 回调: {component.GetType().Name}");
                }
                catch (Exception ex)
                {
                    _componentLog.Error($"Component 回调失败: {component.GetType().Name}, 错误: {ex.Message}");
                }
            }

            unregisteredCount++;
        }

        if (unregisteredCount > 0)
        {
            _componentLog.Debug($"Entity {entity.Name} 共注销 {unregisteredCount} 个 Component");
        }
    }

    // ==================== Component 查询 ====================

    /// <summary>
    /// 按类型查询所有 Component
    /// 常用场景：获取所有 HealthComponent 以显示血条
    /// </summary>
    public static IEnumerable<T> GetComponentsByType<T>() where T : Node
    {
        return GetEntitiesByType<T>();
    }

    /// <summary>
    /// 获取所有指定类型 Component 的 ID 列表
    /// 常用场景：配合 EntityRelationshipManager 进行反向查询
    /// </summary>
    /// <returns>Component 的 ID 列表</returns>
    public static IEnumerable<string> GetComponentIdsByType<T>()
    {
        // 委托给 NodeLifecycleManager
        return NodeLifecycleManager.GetNodesByType<Node>()
            .Select(c => c.GetInstanceId().ToString());
    }

    /// <summary>
    /// 通过 Component 查找所属 Entity
    /// 常用场景：Component 需要访问 Entity 数据
    /// </summary>
    public static Node? GetEntityByComponent(Node component)
    {
        string componentId = component.GetInstanceId().ToString();
        var entityId = EntityRelationshipManager
            .GetParentEntitiesByChildAndType(componentId, EntityRelationshipType.ENTITY_TO_COMPONENT)
            .FirstOrDefault();

        return entityId != null ? GetEntityById(entityId) : null;
    }

    /// <summary>
    /// 获取 Component 所属 Entity 的 Data 容器
    /// 常用于 Component 访问 Entity 的运行时数据
    /// </summary>
    /// <param name="component">Component 节点</param>
    /// <returns>Entity 的 Data 容器，如果 Entity 未找到或不是 IEntity 则返回 null</returns>
    public static Data? GetEntityData(Node component)
    {
        var entity = GetEntityByComponent(component);
        if (entity is IEntity iEntity)
            return iEntity.Data;
        return null;
    }

    // ==================== 动态 Component 管理 ====================

    /// <summary>
    /// 动态添加 Component 到 Entity
    /// 自动处理：挂载节点 → 注册 → 建立关系 → 触发回调
    /// 常用场景：运行时添加 Buff、技能等
    /// 
    /// 注意：Component 会被添加到 Entity/Component 路径下，如果 Component 节点不存在会自动创建
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要添加的 Component</param>
    public static void AddComponent<T>(Node entity, T component) where T : Node
    {
        // 1. 获取或创建 Component 容器节点
        Node componentContainer = entity.GetNodeOrNull("Component");
        if (componentContainer == null)
        {
            componentContainer = new Node();
            componentContainer.Name = "Component";
            entity.AddChild(componentContainer);
            _componentLog.Debug($"为 Entity {entity.Name} 创建 Component 容器节点");
        }

        // 2. 挂载到 Component 容器下
        componentContainer.AddChild(component);

        // 3. 注册 Component
        string componentType = typeof(T).Name;
        Register(component);

        // 4. 建立关系
        string entityId = entity.GetInstanceId().ToString();
        string componentId = component.GetInstanceId().ToString();
        EntityRelationshipManager.AddRelationship(
            entityId,
            componentId,
            EntityRelationshipType.ENTITY_TO_COMPONENT
        );

        // 5. 触发 IComponent 回调
        if (component is IComponent icomp)
        {
            try
            {
                icomp.OnComponentRegistered(entity);
                _componentLog.Debug($"触发 IComponent 回调: {componentType}");
            }
            catch (Exception ex)
            {
                _componentLog.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
            }
        }

        _componentLog.Info($"已动态添加 Component: {componentType} 到 Entity: {entity.Name}/Component");
    }

    /// <summary>
    /// 从 Entity 获取指定类型的 Component
    /// 常用场景：获取 Entity 上的特定组件（如 HealthComponent）
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    /// <param name="entity">目标 Entity</param>
    /// <returns>找到的 Component，如果不存在则返回 null</returns>
    public static T? GetComponent<T>(Node entity) where T : Node
    {
        string entityId = entity.GetInstanceId().ToString();

        // 通过关系管理器获取所有 Component ID
        var componentIds = EntityRelationshipManager
            .GetChildEntitiesByParentAndType(entityId, EntityRelationshipType.ENTITY_TO_COMPONENT);

        foreach (var componentId in componentIds)
        {
            var component = GetEntityById(componentId);
            if (component == null) continue;

            // 检查类型是否匹配
            if (component.GetType().Name == typeof(T).Name && component is T typedComponent)
            {
                return typedComponent;
            }
        }

        _componentLog.Warn($"Entity {entity.Name} 未找到 Component: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 从 Entity 移除 Component（通过类型字符串）
    /// 自动处理：查找 Component → 触发回调 → 移除关系 → 注销 → 销毁节点
    /// 常用场景：通过组件类型名称移除组件（如 "HealthComponent"）
    /// </summary>
    /// <param name="entity">目标 Entity</param>
    /// <param name="componentType">Component 类型名称（如 "HealthComponent"）</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveComponent(Node entity, string componentType)
    {
        string entityId = entity.GetInstanceId().ToString();

        // 通过关系管理器获取所有 Component ID
        var componentIds = EntityRelationshipManager
            .GetChildEntitiesByParentAndType(entityId, EntityRelationshipType.ENTITY_TO_COMPONENT)
            .ToList();

        foreach (var componentId in componentIds)
        {
            var component = GetEntityById(componentId);
            if (component == null) continue;

            // 检查类型是否匹配
            if (component.GetType().Name == componentType)
            {
                // 调用重载方法执行实际移除逻辑
                RemoveComponent(entity, component);
                return true;
            }
        }

        _componentLog.Warn($"Entity {entity.Name} 未找到 Component: {componentType}，无法移除");
        return false;
    }

    /// <summary>
    /// 从 Entity 移除 Component（通过 Component 实例）
    /// 自动处理：触发回调 → 移除关系 → 注销 → 销毁节点
    /// </summary>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要移除的 Component 实例</param>
    public static void RemoveComponent(Node entity, Node component)
    {
        string componentType = component.GetType().Name;

        // 1. 触发 IComponent 回调
        if (component is IComponent icomp)
        {
            try
            {
                icomp.OnComponentUnregistered();
                _componentLog.Debug($"触发 IComponent 注销回调: {componentType}");
            }
            catch (Exception ex)
            {
                _componentLog.Error($"Component 注销回调失败: {componentType}, 错误: {ex.Message}");
            }
        }

        // 2. 移除关系
        string entityId = entity.GetInstanceId().ToString();
        string componentId = component.GetInstanceId().ToString();
        EntityRelationshipManager.RemoveRelationship(
            entityId,
            componentId,
            EntityRelationshipType.ENTITY_TO_COMPONENT
        );

        // 3. 从 NodeLifecycleManager 注销
        NodeLifecycleManager.Unregister(component);

        // 4. 从节点树移除
        component.QueueFree();

        _componentLog.Info($"已移除 Component: {componentType} 从 Entity: {entity.Name}");
    }
}
