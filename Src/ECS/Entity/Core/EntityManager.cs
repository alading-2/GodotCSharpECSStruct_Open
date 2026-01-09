using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 生成配置参数
/// 类似 TypeScript 的接口对象，支持命名参数初始化
/// </summary>
public readonly record struct EntitySpawnConfig
{
    /// <summary>Resource 配置数据（必填）</summary>
    public required Resource Resource { get; init; }

    /// <summary>是否使用对象池（默认 false）</summary>
    public bool UsingObjectPool { get; init; }

    /// <summary>对象池名称（UsingObjectPool=true 时必填，如 ObjectPoolNames.EnemyPool）</summary>
    public string? PoolName { get; init; }

    /// <summary>场景路径（UsingObjectPool=false 时必填，如 ECSIndex.Entity.EnemyEntity）</summary>
    public string? ScenePath { get; init; }

    /// <summary>初始位置（可选，仅对 Node2D 生效）</summary>
    public Vector2? Position { get; init; }

    /// <summary>初始旋转角度（可选，仅对 Node2D 生效）</summary>
    public float? Rotation { get; init; }
}

/// <summary>
/// Entity 管理器 - 伪 ECS 架构的统一节点生命周期管理入口
/// 
/// ==================== 设计理念 ====================
/// 
/// 1. 命名哲学：
///    - 在本项目的伪 ECS 架构中，Component 本质上也是 Entity（都是 Node）
///    - EntityManager 管理的是"所有需要生命周期管理的 Node"，而非狭义的"游戏实体"
///    - 这与 Unity ECS 的 EntityManager 设计理念一致（同时管理 Entity 和 Component）
/// 
/// 2. 职责边界：
///    - EntityManager：管理节点的 **生命周期**（生成、注册、查询、销毁）
///    - EntityRelationshipManager：管理节点的 **关系**（父子、依赖、组合）
///    - 两者协作构成完整的 ECS 管理体系
/// 
/// 3. 统一数据源：
///    - Entity 和 Component 都注册到 _entities 字典（InstanceId -> Node）
///    - 通过 _entitiesByType 索引实现高效的类型查询
///    - 通过方法名区分操作语义（Spawn vs AddComponent）
/// 
/// ==================== 职责范围 ====================
/// 
/// - Entity 管理：生成、注册、查询、销毁
/// - Component 管理：动态添加/移除、查询、生命周期
/// - 关系建立：自动建立 Entity-Component 关系（委托给 EntityRelationshipManager）
/// 
/// ==================== 使用示例 ====================
/// 
/// <code>
/// // 生成 Entity (对象池)
/// var enemy = EntityManager.Spawn&lt;Enemy&gt;(new EntitySpawnConfig
/// {
///     Resource = enemyData,
///     UsingObjectPool = true,
///     PoolName = ObjectPoolNames.EnemyPool,
///     Position = new Vector2(100, 200)
/// });
/// 
/// // 生成 Entity (场景)
/// var boss = EntityManager.Spawn&lt;Enemy&gt;(new EntitySpawnConfig
/// {
///     Resource = bossData,
///     UsingObjectPool = false,
/// ScenePath = ECSIndex.Entity.EnemyEntity,
///     Position = new Vector2(500, 300)
/// });
/// 
/// // 动态添加 Component
/// EntityManager.AddComponent(enemy, buffComponent);
/// 
/// // 查询 Component
/// var healthComps = EntityManager.GetComponentsByType&lt;HealthComponent&gt;("HealthComponent");
/// 
/// // 通过 Component 反查 Entity
/// var entity = EntityManager.GetEntityByComponent(component);
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
    /// 生成 Entity (统一参数版本)
    /// 支持对象池和场景两种创建模式，通过 EntitySpawnConfig 对象传递参数
    /// 
    /// 使用示例：
    /// <code>
    /// // 对象池 Entity
    /// var enemy = EntityManager.Spawn&lt;Enemy&gt;(new EntitySpawnConfig
    /// {
    ///     Resource = enemyData,
    ///     UsingObjectPool = true,
    ///     PoolName = ObjectPoolNames.EnemyPool,
    ///     Position = new Vector2(100, 200)
    /// });
    /// 
    /// // 场景 Entity
    /// var boss = EntityManager.Spawn&lt;Enemy&gt;(new EntitySpawnConfig
    /// {
    ///     Resource = bossData,
    ///     UsingObjectPool = false,
    ///     ScenePath = ECSIndex.Entity.EnemyEntity,
    ///     Position = new Vector2(500, 300),
    ///     Rotation = Mathf.Pi / 4
    /// });
    /// </code>
    /// </summary>
    /// <typeparam name="T">Entity 类型(如 Enemy, Player)</typeparam>
    /// <param name="config">生成配置参数</param>
    /// <returns>已配置好的 Entity 实例</returns>
    public static T? Spawn<T>(EntitySpawnConfig config) where T : Node, IEntity
    {
        T? entity;

        // 1. 根据模式创建 Entity
        if (config.UsingObjectPool)
        {
            // 路径 1: 对象池 Entity
            if (string.IsNullOrEmpty(config.PoolName))
            {
                _log.Error($"使用对象池模式但未提供 PoolName: {typeof(T).Name}");
                return null;
            }

            var pool = ObjectPoolManager.GetPool<T>(config.PoolName);
            if (pool == null)
            {
                _log.Error($"对象池不存在: 期望名称 '{config.PoolName}' (类型: {typeof(T).Name})");
                return null;
            }
            entity = pool.Get();
            _log.Debug($"从对象池获取 Entity: {typeof(T).Name} (池名: {config.PoolName})");
        }
        else
        {
            // 路径 2: 场景 Entity
            if (string.IsNullOrEmpty(config.ScenePath))
            {
                _log.Error($"场景路径为空，无法实例化 {typeof(T).Name}");
                return null;
            }

            var scene = GD.Load<PackedScene>(config.ScenePath);
            if (scene == null)
            {
                _log.Error($"场景加载失败: {config.ScenePath}");
                return null;
            }
            entity = scene.Instantiate<T>();
            _log.Debug($"从场景实例化 Entity: {typeof(T).Name} (场景: {config.ScenePath})");
        }

        string entityType = typeof(T).Name;
        string id = entity.GetInstanceId().ToString();

        // 2. 防止重复注册（对象池复用场景）
        if (!_entities.ContainsKey(id))
        {
            Register(entity, entityType);
            RegisterComponents(entity);
        }

        // 3. 数据注入（核心: 将 Resource 配置写入 Data）
        entity.Data.LoadFromResource(config.Resource);

        // 4. 自动加载 VisualScene (如有)
        InjectVisualScene(entity, config.Resource);

        // 5. 设置位置和旋转（仅对 Node2D 生效）
        if (entity is Node2D entity2D)
        {
            if (config.Position.HasValue)
            {
                entity2D.GlobalPosition = config.Position.Value;
                _log.Debug($"设置 Entity 位置: {typeof(T).Name} at {config.Position.Value}");
            }

            if (config.Rotation.HasValue)
            {
                entity2D.GlobalRotation = config.Rotation.Value;
                _log.Debug($"设置 Entity 旋转: {typeof(T).Name} rotation {config.Rotation.Value}");
            }
        }

        _log.Debug($"生成 Entity 完成: {typeof(T).Name}");
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
    /// 自动注册 Entity 的所有 Component（递归查找所有层级）
    /// 识别规则（按优先级）：
    /// 1. 实现了 IComponent 接口（最高优先级）
    /// 2. 类名以 "Component" 结尾（命名约定）
    /// 3. 在 ECSIndex 白名单中（特殊情况）
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
                // 1. 注册 Component
                Register(child, componentType);

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
                        _log.Debug($"触发 IComponent 回调: {componentType}");
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
                    }
                }

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
    /// 
    /// 注意：Entity 和 Component 都会注册到同一个 _entities 字典中
    /// 通过 nodeType 参数区分类型，便于后续按类型查询
    /// </summary>
    /// <param name="node">要注册的节点（Entity 或 Component）</param>
    /// <param name="nodeType">节点类型名称（如 "Enemy", "HealthComponent"）</param>
    public static void Register(Node node, string nodeType)
    {
        string id = node.GetInstanceId().ToString();

        // 防止重复注册
        if (_entities.ContainsKey(id))
        {
            _log.Warn($"Entity {id} 已注册，跳过");
            return;
        }

        _entities[id] = node;

        // 更新类型索引
        if (!_entitiesByType.ContainsKey(nodeType))
            _entitiesByType[nodeType] = new HashSet<Node>();
        _entitiesByType[nodeType].Add(node);
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
    /// 根据 ID 获取 Entity/Component
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
    /// <returns>Component 的 ID 列表</returns>
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
            _log.Debug($"为 Entity {entity.Name} 创建 Component 容器节点");
        }

        // 2. 挂载到 Component 容器下
        componentContainer.AddChild(component);

        // 3. 注册 Component
        string componentType = typeof(T).Name;
        Register(component, componentType);

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
                _log.Debug($"触发 IComponent 回调: {componentType}");
            }
            catch (Exception ex)
            {
                _log.Error($"Component 回调失败: {componentType}, 错误: {ex.Message}");
            }
        }

        _log.Info($"已动态添加 Component: {componentType} 到 Entity: {entity.Name}/Component");
    }

    /// <summary>
    /// 从 Entity 获取指定类型的 Component
    /// 常用场景：通过 ECSIndex.Component.HealthComponent 获取组件
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

        _log.Warn($"Entity {entity.Name} 未找到 Component: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 从 Entity 移除 Component（通过类型字符串）
    /// 自动处理：查找 Component → 触发回调 → 移除关系 → 注销 → 销毁节点
    /// 常用场景：通过 ECSIndex.Component.HealthComponent 移除组件
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

        _log.Warn($"Entity {entity.Name} 未找到 Component: {componentType}，无法移除");
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
            // 从类型索引中移除
            if (_entitiesByType.TryGetValue(componentType, out var set))
            {
                set.Remove(component);
                if (set.Count == 0)
                    _entitiesByType.Remove(componentType);
            }
        }

        // 4. 从节点树移除
        component.QueueFree();

        _log.Info($"已移除 Component: {componentType} 从 Entity: {entity.Name}");
    }

    // ==================== 范围查询工具 ====================
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
