using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 管理器 - 统一的实体生命周期管理入口
/// 
/// 职责范围：
/// - Entity 管理：生成、注册、查询、销毁
/// - Component 管理：动态添加/移除、查询、生命周期
/// - 关系建立：自动建立 Entity-Component 关系（通过 EntityRelationshipManager）
/// 
/// 设计理念：
/// - Component 本质上也是 Entity（都是 Node，都注册到 _entities）
/// - EntityManager 是所有 Node 的统一管理入口
/// - 通过方法名区分操作对象（Spawn vs AddComponent）
/// 
/// 使用示例：
/// <code>
/// // 生成 Entity
/// var enemy = EntityManager.Spawn&lt;Enemy&gt;(poolName, resource, position);
/// 
/// // 动态添加 Component
/// EntityManager.AddComponent(enemy, buffComponent);
/// 
/// // 查询 Component
/// var healthComps = EntityManager.GetComponentsByType&lt;HealthComponent&gt;("HealthComponent");
/// </code>
/// </summary>
public static class EntityManager
{
    private static readonly Log _log = new("EntityManager", LogLevel.Warning);

    // ==================== 核心数据结构 ====================

    // 全局实体注册表：InstanceId -> Node
    private static readonly Dictionary<string, Node> _entities = new();

    // 分类索引：EntityType -> HashSet<Node>
    private static readonly Dictionary<string, HashSet<Node>> _entitiesByType = new();

    // ==================== 实体生成（核心功能）====================

    /// <summary>
    /// 生成 Entity（通用版本，适用于所有 Node 类型）
    /// 自动处理：ObjectPool 获取 → Resource 数据注入 → 注册管理
    /// 适用场景：Buff（纯 Node）、Item（可能无位置）等
    /// </summary>
    /// <typeparam name="T">Entity 类型（如 Buff, Item）</typeparam>
    /// <param name="poolName">对象池名称（必须）</param>
    /// <param name="resource">静态配置 Resource</param>
    /// <returns>已配置好的 Entity 实例</returns>
    public static T? Spawn<T>(string poolName, Resource resource) where T : Node
    {
        // 1. 从 ObjectPool 获取实例（使用泛型方法，类型安全）
        var pool = ObjectPoolManager.GetPool<T>(poolName);
        if (pool == null)
        {
            _log.Error($"对象池 {poolName} 不存在，请检查 ObjectPoolInit");
            return null;
        }
        // 直接调用泛型方法，无需类型转换
        var entity = pool.Get();

        // 2. 数据注入（核心：将 Resource 配置写入 Data）
        // 使用 Data 容器内置的 LoadFromResource 方法，替代原有的 InjectResourceData
        entity.GetData().LoadFromResource(resource);

        // 2.1 自动加载 VisualScene (如有)
        InjectVisualScene(entity, resource);

        // 2.2 自动注册 Entity 的所有 Component
        RegisterComponents(entity);

        // 3. 自动注册Entity
        string entityType = typeof(T).Name;
        Register(entity, entityType);

        _log.Debug($"生成 Entity: {entityType}");
        return entity;
    }

    /// <summary>
    /// 生成 Entity（带位置参数，适用于 Node2D 及其子类）
    /// 自动处理：ObjectPool 获取 → 位置初始化 → Resource 数据注入 → 注册管理
    /// 适用场景：Enemy、Bullet、掉落的 Item 等需要位置的实体
    /// </summary>
    /// <typeparam name="T">Entity 类型（如 Enemy, Bullet）</typeparam>
    /// <param name="poolName">对象池名称（必须）</param>
    /// <param name="resource">静态配置 Resource</param>
    /// <param name="position">初始位置</param>
    /// <returns>已配置好的 Entity 实例</returns>
    public static T? Spawn<T>(string poolName, Resource resource, Vector2 position) where T : Node2D
    {
        // 调用通用版本
        var entity = Spawn<T>(poolName, resource);

        // 额外设置位置
        if (entity != null)
        {
            entity.GlobalPosition = position;
            _log.Debug($"设置 Entity 位置: {typeof(T).Name} at {position}");
        }

        return entity;
    }

    /// <summary>
    /// 生成 Entity（带位置和旋转参数，适用于需要方向的 Entity）
    /// 适用场景：子弹、投射物等需要初始方向的实体
    /// </summary>
    /// <typeparam name="T">Entity 类型（如 Bullet, Projectile）</typeparam>
    /// <param name="poolName">对象池名称（必须）</param>
    /// <param name="resource">静态配置 Resource</param>
    /// <param name="position">初始位置</param>
    /// <param name="rotation">初始旋转角度（弧度）</param>
    /// <returns>已配置好的 Entity 实例</returns>
    public static T? Spawn<T>(string poolName, Resource resource, Vector2 position, float rotation) where T : Node2D
    {
        var entity = Spawn<T>(poolName, resource, position);

        if (entity != null)
        {
            entity.GlobalRotation = rotation;
        }

        return entity;
    }


    /// <summary>
    /// 自动加载 VisualScene (AnimatedSprite2D)
    /// 从 Resource 中读取 VisualScene 属性，实例化并挂载到 Entity 下
    /// 统一设置 ZIndex 以保证显示层级
    /// </summary>
    private static void InjectVisualScene(Node entity, Resource resource)
    {
        // 尝试获取 VisualScene 属性 (兼容 UnitResource, ItemResource 等)
        // 使用反射以支持任意 Resource 类型
        var prop = resource.GetType().GetProperty("VisualScene");
        if (prop == null) return;

        var scene = prop.GetValue(resource) as PackedScene;
        if (scene == null) return;

        // 1. 清理旧的 VisualRoot (对象池复用时)
        // 使用 Free() 立即释放，防止同帧内命名冲突
        var existingVisual = entity.GetNodeOrNull("VisualRoot");
        if (existingVisual != null)
        {
            existingVisual.Free();
        }

        // 2. 实例化并挂载
        var visual = scene.Instantiate();
        visual.Name = "VisualRoot";
        entity.AddChild(visual);

        // 3. 统一设置 ZIndex (如果是 Node2D)
        // 提高层级，确保显示在阴影或背景之上
        if (visual is Node2D visual2D)
        {
            visual2D.ZIndex = 10;
        }

        _log.Debug($"已加载 VisualScene: {scene.ResourcePath}");
    }

    /// <summary>
    /// 自动注册 Entity 的所有 Component
    /// 识别规则（按优先级）：
    /// 1. 实现了 IComponent 接口（最高优先级）
    /// 2. 类名以 "Component" 结尾（命名约定）
    /// 3. 在 ECSIndex 白名单中（特殊情况）
    /// 
    /// 自动建立 Entity-Component 关系（通过 EntityRelationshipManager）
    /// </summary>
    private static void RegisterComponents(Node entity)
    {
        int registeredCount = 0;
        string entityId = entity.GetInstanceId().ToString();

        foreach (Node child in entity.GetChildren())
        {
            bool isComponent = false;
            string componentType = child.GetType().Name;

            // 规则 1：实现了 IComponent 接口（优先级最高）
            if (child is IComponent component)
            {
                isComponent = true;

                // 触发回调，让 Component 获取 Entity 引用
                try
                {
                    component.OnComponentRegistered(entity);
                    _log.Debug($"触发 IComponent 回调: {componentType}");
                }
                catch (Exception ex)
                {
                    _log.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
                }
            }
            // 规则 2：类名以 "Component" 结尾（兼容旧代码）
            else if (componentType.EndsWith("Component"))
            {
                isComponent = true;
                _log.Debug($"通过命名约定识别 Component: {componentType}");
            }
            // 规则 3：在 ECSIndex 白名单中（特殊情况）
            else if (ECSIndex.IsComponentWhitelist(componentType))
            {
                isComponent = true;
                _log.Debug($"通过白名单识别 Component: {componentType}");
            }

            // 注册 Component 并建立关系
            if (isComponent)
            {
                // 注册Component
                Register(child, componentType);

                // 建立 Entity-Component 关系
                string componentId = child.GetInstanceId().ToString();
                EntityRelationshipManager.AddRelationship(
                    entityId,
                    componentId,
                    EntityRelationshipType.ENTITY_TO_COMPONENT
                );

                registeredCount++;
                _log.Info($"已注册 Component: {componentType} 到 Entity: {entity.Name}");
            }
        }

        if (registeredCount > 0)
        {
            _log.Debug($"Entity {entity.Name} 共注册 {registeredCount} 个 Component");
        }
    }

    // ==================== 注册与注销 ====================

    /// <summary>
    /// 注册 Entity/Component 到 EntityManager
    /// </summary>
    public static void Register(Node entity, string entityType)
    {
        string id = entity.GetInstanceId().ToString();

        // 防止重复注册
        if (_entities.ContainsKey(id))
        {
            _log.Warn($"Entity {id} 已注册，跳过");
            return;
        }

        _entities[id] = entity;

        // 更新类型索引
        if (!_entitiesByType.ContainsKey(entityType))
            _entitiesByType[entityType] = new HashSet<Node>();
        _entitiesByType[entityType].Add(entity);
    }

    /// <summary>
    /// 注销 Entity（Entity._ExitTree 时调用）
    /// 同时注销其所有 Component 并清理关系
    /// </summary>
    public static void UnregisterEntity(Node entity)
    {
        string id = entity.GetInstanceId().ToString();

        if (!_entities.Remove(id))
        {
            _log.Warn($"Entity {id} 未注册，无法注销");
            return;
        }

        // 从类型索引中移除
        foreach (var set in _entitiesByType.Values)
            set.Remove(entity);

        // 注销所有 Component（包括清理关系）
        UnregisterComponents(entity);

        // 清理 Entity 自身的所有关系（作为父或子）
        EntityRelationshipManager.RemoveAllRelationships(id);
    }

    /// <summary>
    /// 注销 Entity 的所有 Component（包括清理 Entity-Component 关系）
    /// 通过 EntityRelationshipManager 查询关系，而非依赖节点树结构
    /// 优势：支持任意层级、数据源唯一、与注册逻辑一致
    /// </summary>
    private static void UnregisterComponents(Node entity)
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
                _log.Warn($"Component {componentId} 已不存在，跳过注销");
                continue;
            }

            // 从注册表移除
            if (_entities.Remove(componentId))
            {
                // 从类型索引中移除
                string componentType = component.GetType().Name;
                if (_entitiesByType.TryGetValue(componentType, out var set))
                {
                    set.Remove(component);
                    if (set.Count == 0)
                        _entitiesByType.Remove(componentType);
                }

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
                        icomp.OnComponentUnregistered();
                        _log.Debug($"触发 IComponent 注销回调: {component.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Component 注销回调失败: {component.GetType().Name}, 错误: {ex.Message}");
                    }
                }

                unregisteredCount++;
            }
        }

        if (unregisteredCount > 0)
        {
            _log.Debug($"Entity {entity.Name} 共注销 {unregisteredCount} 个 Component");
        }
    }

    /// <summary>
    /// 根据 ID 获取 Entity
    /// </summary>
    public static Node? GetEntityById(string id)
    {
        return _entities.GetValueOrDefault(id);
    }

    // ==================== 查询接口 ====================

    /// <summary>
    /// 按类型查询所有 Entity
    /// </summary>
    public static IEnumerable<T> GetEntitiesByType<T>(string entityType) where T : Node
    {
        if (!_entitiesByType.TryGetValue(entityType, out var set))
            return Enumerable.Empty<T>();
        return set.OfType<T>();
    }

    /// <summary>
    /// 按类型查询所有 Component
    /// 常用场景：获取所有 HealthComponent 以显示血条
    /// </summary>
    public static IEnumerable<T> GetComponentsByType<T>(string componentType) where T : Node
    {
        return GetEntitiesByType<T>(componentType);
    }

    /// <summary>
    /// 获取所有指定类型 Component 的 ID 列表
    /// 常用场景：配合 EntityRelationshipManager 进行反向查询
    /// </summary>
    public static IEnumerable<string> GetComponentIdsByType(string componentType)
    {
        if (!_entitiesByType.TryGetValue(componentType, out var set))
            return Enumerable.Empty<string>();
        return set.Select(c => c.GetInstanceId().ToString());
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
    /// <returns>Entity 的 Data 容器，如果 Entity 未找到则返回 null</returns>
    public static Data? GetEntityData(Node component)
    {
        var entity = GetEntityByComponent(component);
        return entity?.GetData();
    }

    /// <summary>
    /// 范围查询（常用于 AI 寻敌、AOE 伤害）
    /// </summary>
    public static IEnumerable<T> GetEntitiesInRange<T>(Vector2 position, float range, string entityType)
        where T : Node2D
    {
        return GetEntitiesByType<T>(entityType)
            .Where(e => e.GlobalPosition.DistanceTo(position) <= range);
    }

    /// <summary>
    /// 获取最近的 Entity（常用于 AI 锁定目标）
    /// </summary>
    public static T? GetNearestEntity<T>(Vector2 position, string entityType, float maxRange = float.MaxValue)
        where T : Node2D
    {
        T? nearest = null;
        float minDistance = maxRange;

        foreach (var entity in GetEntitiesByType<T>(entityType))
        {
            float distance = entity.GlobalPosition.DistanceTo(position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = entity;
            }
        }

        return nearest;
    }

    // ==================== 动态 Component 管理 ====================

    /// <summary>
    /// 动态添加 Component 到 Entity
    /// 自动处理：挂载节点 → 注册 → 建立关系 → 触发回调
    /// 常用场景：运行时添加 Buff、技能等
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要添加的 Component</param>
    public static void AddComponent<T>(Node entity, T component) where T : Node
    {
        // 1. 挂载到 Entity
        entity.AddChild(component);

        // 2. 注册 Component
        string componentType = typeof(T).Name;
        Register(component, componentType);

        // 3. 建立关系
        string entityId = entity.GetInstanceId().ToString();
        string componentId = component.GetInstanceId().ToString();
        EntityRelationshipManager.AddRelationship(
            entityId,
            componentId,
            EntityRelationshipType.ENTITY_TO_COMPONENT
        );

        // 4. 触发 IComponent 回调
        if (component is IComponent icomp)
        {
            try
            {
                icomp.OnComponentRegistered(entity);
                _log.Debug($"触发 IComponent 回调: {componentType}");
            }
            catch (Exception ex)
            {
                _log.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
            }
        }

        _log.Info($"已动态添加 Component: {componentType} 到 Entity: {entity.Name}");
    }

    /// <summary>
    /// 从 Entity 移除 Component
    /// 自动处理：触发回调 → 移除关系 → 注销 → 销毁节点
    /// </summary>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要移除的 Component</param>
    public static void RemoveComponent(Node entity, Node component)
    {
        string componentType = component.GetType().Name;

        // 1. 触发 IComponent 回调
        if (component is IComponent icomp)
        {
            try
            {
                icomp.OnComponentUnregistered();
                _log.Debug($"触发 IComponent 注销回调: {componentType}");
            }
            catch (Exception ex)
            {
                _log.Error($"Component 注销回调失败: {componentType}, 错误: {ex.Message}");
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

        // 3. 从注册表移除
        if (_entities.Remove(componentId))
        {
            foreach (var set in _entitiesByType.Values)
                set.Remove(component);
        }

        // 4. 从节点树移除
        component.QueueFree();

        _log.Info($"已移除 Component: {componentType} 从 Entity: {entity.Name}");
    }

    // ==================== 生命周期管理 ====================

    /// <summary>
    /// 回收 Entity（归还到对象池）
    /// 注意：Unregister 内部已处理关系清理，无需重复调用
    /// </summary>
    public static void Destroy(Node entity)
    {
        // 1. 注销（内部已清理 Component 和关系）
        UnregisterEntity(entity);

        // 2. 归还到对象池
        ObjectPoolManager.ReturnToPool(entity);
    }

    /// <summary>
    /// 销毁所有指定类型的 Entity
    /// </summary>
    public static void DestroyAllByType(string entityType)
    {
        if (!_entitiesByType.TryGetValue(entityType, out var set))
            return;

        // 复制列表避免迭代时修改
        var entities = set.ToList();
        foreach (var entity in entities)
        {
            Destroy(entity);
        }

        _log.Info($"已销毁所有 {entityType} 类型的 Entity，共 {entities.Count} 个");
    }

    /// <summary>
    /// 清理所有 Entity（场景切换时调用）
    /// 会真正销毁所有实体并归还到对象池
    /// </summary>
    public static void Clear()
    {
        // 复制列表避免迭代时修改
        var allEntities = _entities.Values.ToList();
        int count = allEntities.Count;

        // 使用 Destroy 统一处理，确保逻辑一致
        foreach (var entity in allEntities)
        {
            Destroy(entity);
        }

        // 确保字典已清空（Destroy 内部会逐个移除）
        _entities.Clear();
        _entitiesByType.Clear();

        _log.Info($"EntityManager 已清空，共销毁 {count} 个 Entity");
    }
}
