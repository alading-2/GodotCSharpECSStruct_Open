using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 管理器 - 统一的实体生命周期管理入口
/// 职责：生成、注册、查询、销毁
/// </summary>
public static class EntityManager
{
    private static readonly Log _log = new("EntityManager");

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
        // 1. 验证池名称
        if (string.IsNullOrEmpty(poolName))
        {
            _log.Error("池名称不能为空");
            return null;
        }

        // 2. 从 ObjectPool 获取实例（使用泛型方法，类型安全）
        var pool = ObjectPoolManager.GetPool<T>(poolName);
        if (pool == null)
        {
            _log.Error($"对象池 {poolName} 不存在，请检查 ObjectPoolInit");
            return null;
        }

        // 直接调用泛型方法，无需类型转换
        var entity = pool.Get();

        // 3. 数据注入（核心：将 Resource 配置写入 Data）
        InjectResourceData(entity, resource);


        // 4. 自动注册
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
    /// 数据注入：将 Resource 的静态配置写入 Entity 的 Data 容器
    /// 这是连接"静态配置"与"运行时数据"的桥梁
    /// </summary>
    private static void InjectResourceData(Node entity, Resource resource)
    {
        var data = entity.GetData();

        // 根据 Resource 类型分发注入逻辑
        switch (resource)
        {
            case EnemyResource enemyRes:
                // 基础属性（AttributeComponent 会监听这些值）
                data.Set("BaseMaxHp", enemyRes.MaxHp);
                data.Set("BaseSpeed", enemyRes.Speed);
                data.Set("BaseDamage", enemyRes.Damage);

                // 当前血量初始化为最大值
                data.Set("CurrentHp", enemyRes.MaxHp);

                // 其他数据
                data.Set("ExpReward", enemyRes.ExpReward);
                data.Set("EnemyName", enemyRes.EnemyName);
                data.Set("DefaultStrategy", enemyRes.DefaultStrategy);
                break;

            // 未来扩展：BulletResource, ItemResource, BuffResource 等
            // case BulletResource bulletRes:
            //     data.Set("BaseDamage", bulletRes.Damage);
            //     data.Set("BaseSpeed", bulletRes.Speed);
            //     data.Set("Lifetime", bulletRes.Lifetime);
            //     break;

            // case ItemResource itemRes:
            //     data.Set("ItemName", itemRes.ItemName);
            //     data.Set("ItemType", itemRes.ItemType);
            //     data.Set("Value", itemRes.Value);
            //     data.Set("Rarity", itemRes.Rarity);
            //     break;

            // case BuffResource buffRes:
            //     data.Set("BuffName", buffRes.BuffName);
            //     data.Set("Duration", buffRes.Duration);
            //     data.Set("StackCount", 1); // 初始层数
            //     data.Set("BuffType", buffRes.BuffType);
            //     break;

            default:
                _log.Warn($"未处理的 Resource 类型: {resource.GetType().Name}");
                break;
        }

        // 触发 Data 变化事件，AttributeComponent 会自动重算
        // 无需手动通知，Data.Set() 内部已实现事件机制
    }

    // ==================== 注册与注销 ====================

    /// <summary>
    /// 手动注册 Entity（通常由 Spawn 自动调用）
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
    /// </summary>
    public static void Unregister(Node entity)
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
    /// </summary>
    public static void Recycle(Node entity)
    {
        // 1. 注销
        Unregister(entity);

        // 2. 清理关系
        string id = entity.GetInstanceId().ToString();
        EntityRelationshipManager.RemoveAllRelationships(id);

        // 3. 归还到对象池
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
            Recycle(entity);
        }

        _log.Info($"已销毁所有 {entityType} 类型的 Entity，共 {entities.Count} 个");
    }

    /// <summary>
    /// 清理所有 Entity（场景切换时调用）
    /// </summary>
    public static void Clear()
    {
        _entities.Clear();
        _entitiesByType.Clear();
        _log.Info("EntityManager 已清空");
    }
}
