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
    /// <summary>单位配置数据（必填）</summary>
    public required Dictionary<string, object> Config { get; init; }

    /// <summary>是否使用对象池（默认 false）</summary>
    public bool UsingObjectPool { get; init; }

    /// <summary>对象池名称（UsingObjectPool=true 时必填，如 ObjectPoolNames.EnemyPool）</summary>
    public string? PoolName { get; init; }

    /// <summary>初始位置（可选，仅对 Node2D 生效）</summary>
    public Vector2? Position { get; init; }

    /// <summary>初始旋转角度（可选，仅对 Node2D 生效）</summary>
    public float? Rotation { get; init; }
}

/// <summary>
/// Entity 管理器 - 伪 ECS 架构的统一节点生命周期管理入口
/// 
/// ==================== 模块化设计 ====================
/// 
/// 本类采用 partial class 设计，分为以下模块：
/// 1. [EntityManager.cs]（本文件）- 核心层
///    - 职责：生命周期管理（Spawn, Register, Destroy）、核心数据结构、基础查询
/// 
/// 2. [EntityManager_Component.cs] - 组件层
///    - 职责：Component 管理（RegisterComponents, AddComponent, GetComponent, RemoveComponent）
/// 
/// 3. [EntityManager_Ability.cs] - 技能层
///    - 职责：Ability 管理（AddAbility, RemoveAbility, GetAbilities）
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
/// ==================== 职责范围 (Core) ====================
/// 
/// - Entity 管理：生成、注册、查询、销毁
/// - 核心查询：按类型查询、全局遍历
/// - 关系建立：自动建立 Entity-Component 关系（委托给 EntityRelationshipManager）
/// 
/// ==================== 使用示例 ====================
/// 
/// <code>
/// // 生成 Entity (对象池)
/// var enemy = EntityManager.Spawn&lt;Enemy&gt;(new EntitySpawnConfig
/// {
///     Config = enemyData,
///     UsingObjectPool = true,
///     PoolName = ObjectPoolNames.EnemyPool,
///     Position = new Vector2(100, 200)
/// });
/// 
/// // 生成 Entity (场景) - 类型安全，无需指定 SceneName
/// // 自动使用 typeof(T).Name (即 "Player") 查找 ResourceRegistry
/// var player = EntityManager.Spawn&lt;Player&gt;(new EntitySpawnConfig
/// {
///     Config = playerData,
///     UsingObjectPool = false,
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
public static partial class EntityManager
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
    ///     Config = enemyData,
    ///     UsingObjectPool = true,
    ///     PoolName = ObjectPoolNames.EnemyPool,
    ///     Position = new Vector2(100, 200)
    /// });
    /// 
    /// // 场景 Entity - 类型安全，无需指定 SceneName
    /// var player = EntityManager.Spawn&lt;Player&gt;(new EntitySpawnConfig
    /// {
    ///     Config = playerData,
    ///     UsingObjectPool = false,  // 自动使用 "Player" 查找 ResourceRegistry
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
            // 路径 2: 场景 Entity（通过 ResourceRegistry 加载）
            // 强制使用类型名作为资源名称
            var scene = ResourceRegistry.LoadScene<T>();
            if (scene == null)
            {
                _log.Error($"场景加载失败: {typeof(T).Name} (请检查 ResourceRegistry.tscn 配置)");
                return null;
            }
            entity = scene.Instantiate<T>();
            _log.Debug($"从场景实例化 Entity: {typeof(T).Name}");
        }

        string entityType = typeof(T).Name;
        string id = entity.GetInstanceId().ToString();
        entity.Data.Set(DataKey.Id, id);
        // 2. 数据注入（核心: 将配置字典写入 Data），需要放在Component注册之前，因为可能包含Component初始数据，不过初始数据一般是spawn后再修改
        // 2. 数据注入（核心: 将配置字典写入 Data）
        // ⚠️  重要时序说明:
        //    - 此时注入的是"预设配置数据"(如敌人基础属性: HP, Speed, Damage 等)
        //    - OnComponentRegistered 会在步骤 4 中执行,此时 Component 可以访问这些配置数据
        //    - 实际的"运行时初始数据"(如SkillLevel, Target)通常是 Spawn 之后才设置
        //    - Component 如需响应后续数据变化,应在 OnComponentRegistered 中监听 PropertyChanged 事件
        //
        // 典型场景示例:
        //   var enemy = EntityManager.Spawn<Enemy>(config);  // ← config 包含 HP, Speed 等
        //   enemy.Data.Set(DataKey.SkillLevel, 5);          // ← 这才是"初始数据"
        entity.Data.LoadFromConfig(config.Config);

        // 3. 自动加载 VisualScene (如有)
        InjectVisualScene(entity, config.Config);

        // 4. 防止重复注册（对象池复用场景）
        if (!_entities.ContainsKey(id))
        {
            Register(entity, entityType);
            RegisterComponents(entity);
        }

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
    /// 从配置字典中读取 VisualScenePath，加载并挂载到 Entity 下
    /// 统一设置 ZIndex 以保证显示层级
    /// </summary>
    private static void InjectVisualScene(Node entity, Dictionary<string, object> config)
    {
        // 检查 VisualScenePath 是否存在
        var visualPath = config.GetValueOrDefault(DataKey.VisualScenePath) as string;
        var name = config.GetValueOrDefault(DataKey.Name) as string ?? "Unknown";

        if (string.IsNullOrEmpty(visualPath))
        {
            _log.Warn($"配置 {name} 未设置 VisualScenePath，跳过加载视觉场景");
            return;
        }

        // 加载场景
        var scene = GD.Load<PackedScene>(visualPath);
        if (scene == null)
        {
            _log.Error($"无法加载 VisualScene: {visualPath}");
            return;
        }

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

        _log.Debug($"已加载 VisualScene: {visualPath}");
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

        // 1. 注销所有 Component（包括清理关系）
        // 必须先于 Data/Events 清理，以便 Component 在 OnComponentUnregistered/OnComponentReset 中仍能访问 Entity 数据
        UnregisterComponents(entity);

        // 2. 统一清理 IEntity 资源
        if (entity is IEntity iEntity)
        {
            // 清空事件
            iEntity.Events.Clear();
            // 清空数据
            iEntity.Data.Clear();
        }

        // 3. 从类型索引中移除
        foreach (var set in _entitiesByType.Values)
            set.Remove(entity);

        // 4. 清理 Entity 自身的所有关系（作为父或子）
        EntityRelationshipManager.RemoveAllRelationships(id);
    }

    /// <summary>
    /// 根据 ID 获取 Entity/Component
    /// <param name="id">Entity/Component 的 节点ID</param>
    /// <returns>Entity/Component 的节点</returns>
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


    // ==================== 全局查询 ====================

    /// <summary>
    /// 获取所有已注册的 Entity（不含 Component）
    /// 常用场景：TargetSelector 的全局查询
    /// </summary>
    /// <returns>所有实现 IEntity 接口的节点</returns>
    public static IEnumerable<IEntity> GetAllEntities()
    {
        // 遍历所有类型索引，过滤出 IEntity
        foreach (var set in _entitiesByType.Values)
        {
            foreach (var node in set)
            {
                if (node is IEntity entity)
                    yield return entity;
            }
        }
    }

    /// <summary>
    /// 获取所有实现指定接口/基类的 Entity
    /// 常用场景：获取所有 Node2D（用于空间查询）、所有 IUnit
    /// </summary>
    /// <typeparam name="T">接口或基类类型（如 Node2D、IUnit）</typeparam>
    /// <returns>所有实现该接口且也是 IEntity 的节点</returns>
    public static IEnumerable<T> GetEntitiesByInterface<T>() where T : class
    {
        foreach (var set in _entitiesByType.Values)
        {
            foreach (var node in set)
            {
                if (node is T typed && node is IEntity)
                    yield return typed;
            }
        }
    }




    // ==================== 生命周期管理 ====================

    /// <summary>
    /// 销毁 Entity（兼容对象池和非对象池）
    /// - 对象池 Entity：归还到对象池
    /// - 非对象池 Entity：调用 QueueFree 销毁
    /// </summary>
    public static void Destroy(Node entity)
    {
        if (!GodotObject.IsInstanceValid(entity))
        {
            // 如果节点已经无效（已被引擎释放），仅执行注销逻辑
            UnregisterEntity(entity);
            return;
        }

        // 1. 注销（内部已清理 Component、关系、Data、Events）
        UnregisterEntity(entity);

        // 2. 根据类型决定销毁方式
        if (entity is IPoolable)
        {
            // 对象池 Entity：归还到池中
            ObjectPoolManager.ReturnToPool(entity);
        }
        else
        {
            // 非对象池 Entity：直接销毁
            entity.QueueFree();
        }
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
