using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Brotato.Data.ResourceManagement;

/// <summary>
/// Entity 生成配置参数
/// 类似 TypeScript 的接口对象，支持命名参数初始化
/// </summary>
public readonly record struct EntitySpawnConfig
{
    /// <summary>单位配置资源（必填，如 EnemyConfig, PlayerConfig）</summary>
    public required Resource Config { get; init; }

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
/// var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
/// {
///     Config = enemyData,
///     UsingObjectPool = true,
///     PoolName = ObjectPoolNames.EnemyPool,
///     Position = new Vector2(100, 200)
/// });
/// 
/// // 生成 Entity (场景) - 类型安全，无需指定 SceneName
/// // 自动使用 typeof(T).Name (即 "Player") 查找 ResourceManagement
/// var player = EntityManager.Spawn<Player>(new EntitySpawnConfig
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
/// var healthComps = EntityManager.GetComponentsByType<HealthComponent>("HealthComponent");
/// 
/// // 通过 Component 反查 Entity
/// var entity = EntityManager.GetEntityByComponent(component);
/// </code>
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _log = new("EntityManager", LogLevel.Debug);

    // ==================== 实体生成（核心功能）====================

    /// <summary>
    /// 生成 Entity (统一参数版本)
    /// 支持对象池和场景两种创建模式，通过 EntitySpawnConfig 对象传递参数
    /// 
    /// 使用示例：
    /// <code>
    /// // 对象池 Entity
    /// var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
    /// {
    ///     Config = enemyData,
    ///     UsingObjectPool = true,
    ///     PoolName = ObjectPoolNames.EnemyPool,
    ///     Position = new Vector2(100, 200)
    /// });
    /// 
    /// // 场景 Entity - 类型安全，无需指定 SceneName
    /// var player = EntityManager.Spawn<Player>(new EntitySpawnConfig
    /// {
    ///     Config = playerData,
    ///     UsingObjectPool = false,  // 自动使用 "Player" 查找 ResourceManagement
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
            // 路径 2: 场景 Entity（通过 ResourceManagement 加载）
            // 强制使用类型名作为资源名称
            // 使用 ResourceManagement.Load 直接加载 PackedScene
            var scene = ResourceManagement.Load<PackedScene>(typeof(T).Name, ResourceCategory.Entity);
            if (scene == null)
            {
                _log.Error($"场景加载失败: {typeof(T).Name} (请检查 ResourceGenerator 是否运行)");
                return null;
            }
            entity = scene.Instantiate<T>();
            _log.Debug($"从场景实例化 Entity: {typeof(T).Name}");

            // 自动添加到场景树
            AddToSceneTree(entity);
        }

        string entityType = typeof(T).Name;
        string id = entity.GetInstanceId().ToString();
        entity.Data.Set(DataKey.Id, id);

        // 2. 数据注入 (从 Resource)
        entity.Data.LoadFromResource(config.Config);

        // 3. IUnitEntity 自动加载 VisualScene (如有)
        if (entity is IUnit)
        {
            InjectVisualScene(entity, config.Config);
        }

        // 4. 防止重复注册（对象池复用场景）
        if (!NodeLifecycleManager.IsRegistered(id))
        {
            Register(entity);
            RegisterComponents(entity);
        }

        // 5. 设置位置和旋转（仅对 Node2D 生效）
        if (entity is Node2D entity2D)
        {
            if (config.Position.HasValue) entity2D.GlobalPosition = config.Position.Value;
            if (config.Rotation.HasValue) entity2D.GlobalRotation = config.Rotation.Value;
        }

        GlobalEventBus.Global.Emit(GameEventType.Global.EntitySpawned, new GameEventType.Global.EntitySpawnedEventData(entity));

        return entity;
    }

    /// <summary>
    /// 将 Entity 添加到场景树（非对象池模式）
    /// </summary>
    private static void AddToSceneTree<T>(T entity) where T : Node, IEntity
    {
        // 如果已有父节点，则跳过
        if (entity.GetParent() != null) return;

        // 使用类型名称作为容器名称和路径
        // 例如：PlayerEntity -> ECS/Entity/PlayerEntity
        string typeName = typeof(T).Name;
        string path = $"ECS/Entity/{typeName}";

        // 自动创建并获取父节点
        var parent = ParentManager.GetOrRegister(typeName, path);

        // 添加到场景树
        parent.AddChild(entity);
        _log.Debug($"已将 Entity 添加到场景树: {typeName} -> {path}");
    }



    /// <summary>
    /// 自动加载 VisualScene
    /// </summary>
    private static void InjectVisualScene(Node entity, Resource config)
    {
        PackedScene? scene = null;

        // 尝试通过反射获取 VisualScenePath 属性 (UnitConfig)
        var prop = config.GetType().GetProperty(DataKey.VisualScenePath);
        if (prop != null)
        {
            var value = prop.GetValue(config);
            if (value is PackedScene ps) scene = ps;
            else if (value is string path && !string.IsNullOrEmpty(path)) scene = GD.Load<PackedScene>(path);
        }

        if (scene == null)
        {
            // 如果反射没找到，也可以尝试从 Data 中读取（因为 LoadFromResource 已经执行）
            // 但这需要 IEntity.Data 支持获取对象，目前 Data 主要存值类型和路径字符串
            return;
        }

        // 清理旧的
        var existingVisual = entity.GetNodeOrNull("VisualRoot");
        if (existingVisual != null) existingVisual.Free();

        // 实例化
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


    // ==================== 注册与注销 ====================

    /// <summary>
    /// 注册 Entity/Component 到 EntityManager
    /// </summary>
    /// <param name="node">要注册的节点（Entity 或 Component）</param>
    public static void Register(Node node)
    {
        // 委托给 NodeLifecycleManager
        NodeLifecycleManager.Register(node);
    }

    /// <summary>
    /// 注销 Entity（Entity._ExitTree 时调用）
    /// 同时注销其所有 Component 并清理关系
    /// </summary>
    public static void UnregisterEntity(Node entity)
    {
        string id = entity.GetInstanceId().ToString();

        // 检查是否已注册
        if (!NodeLifecycleManager.IsRegistered(id))
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

        // 3. 从 NodeLifecycleManager 注销
        NodeLifecycleManager.Unregister(entity);

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
        // 委托给 NodeLifecycleManager
        return NodeLifecycleManager.GetNodeById(id);
    }

    // ==================== 查询接口 ====================

    /// <summary>
    /// 按类型查询所有 Entity
    /// </summary>
    public static IEnumerable<T> GetEntitiesByType<T>() where T : Node
    {
        // 委托给 NodeLifecycleManager
        return NodeLifecycleManager.GetNodesByType<T>();
    }


    // ==================== 全局查询 ====================

    /// <summary>
    /// 获取所有已注册的 Entity（不含 Component）
    /// 常用场景：TargetSelector 的全局查询
    /// </summary>
    /// <returns>所有实现 IEntity 接口的节点</returns>
    public static IEnumerable<IEntity> GetAllEntities()
    {
        // 委托给 NodeLifecycleManager，过滤出 IEntity
        return NodeLifecycleManager.GetNodesByInterface<IEntity>();
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

        // 发送销毁事件（在注销前发送，以便监听者仍能访问实体的 Data/Id）
        if (entity is IEntity iEntity)
        {
            GlobalEventBus.Global.Emit(GameEventType.Unit.Destroyed, new GameEventType.Unit.DestroyedEventData(iEntity));
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
    /// 销毁所有指定类型的 Entity，比如所有Enemy
    /// </summary>
    public static void DestroyAllByType<T>() where T : Node
    {
        // 从 NodeLifecycleManager 获取该类型所有节点
        var entities = NodeLifecycleManager.GetNodesByType<T>().ToList();
        if (entities.Count == 0)
            return;

        // 复制列表避免迭代时修改
        foreach (var entity in entities)
        {
            Destroy(entity);
        }

        _log.Info($"已销毁所有 {typeof(T).Name} 类型的 Entity，共 {entities.Count} 个");
    }

    /// <summary>
    /// 清理所有 Entity（场景切换时调用）
    /// 会真正销毁所有实体并归还到对象池
    /// </summary>
    public static void Clear()
    {
        // 从 NodeLifecycleManager 获取所有节点
        var allNodes = NodeLifecycleManager.GetAllNodes().ToList();
        int count = allNodes.Count;

        // 使用 Destroy 统一处理，确保逻辑一致
        foreach (var node in allNodes)
        {
            // 只销毁 IEntity，Component 会随 Entity 一起清理
            if (node is IEntity)
            {
                Destroy(node);
            }
        }

        _log.Info($"EntityManager 已清空，共销毁 {count} 个节点");
    }
}
