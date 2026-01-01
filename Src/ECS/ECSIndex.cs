using Godot;
using System.Collections.Generic;

/// <summary>
/// ECS 索引类：负责维护实体 (Entity)、组件 (Component) 与场景资源路径的映射关系。
/// 提供统一的路径查询入口，方便 Factory 或系统动态加载。
/// </summary>
public static class ECSIndex
{
    // ================= 实体 (Entity) 路径 =================

    public static class Entity
    {
        public const string PlayerEntity = "res://Src/ECS/Entity/Player/Player.tscn";
        public const string EnemyEntity = "res://Src/ECS/Entity/Enemy/Enemy.tscn";
    }

    // ================= 组件 (Component) 路径 =================

    public static class Component
    {
        public const string AttributeComponent = "AttributeComponent";
        public const string HealthComponent = "HealthComponent";
        public const string VelocityComponent = "VelocityComponent";
        public const string HitboxComponent = "HitboxComponent";
        public const string HurtboxComponent = "HurtboxComponent";
        public const string FollowComponent = "FollowComponent";
        public const string PickupComponent = "PickupComponent";
    }

    private static readonly Dictionary<string, string> _nameToPathMap = new()
    {
        { Component.AttributeComponent, "res://Src/ECS/Component/AttributeComponent/AttributeComponent.tscn" },
        { Component.HealthComponent, "res://Src/ECS/Component/HealthComponent/HealthComponent.tscn" },
        { Component.VelocityComponent, "res://Src/ECS/Component/VelocityComponent/VelocityComponent.tscn" },
        { Component.HitboxComponent, "res://Src/ECS/Component/HitboxComponent/HitboxComponent.tscn" },
        { Component.HurtboxComponent, "res://Src/ECS/Component/HurtboxComponent/HurtboxComponent.tscn" },
        { Component.FollowComponent, "res://Src/ECS/Component/FollowComponent/FollowComponent.tscn" },
        { Component.PickupComponent, "res://Src/ECS/Component/PickupComponent/PickupComponent.tscn" },

        { Entity.PlayerEntity, "res://Src/ECS/Entity/Player/Player.tscn" },
        { Entity.EnemyEntity, "res://Src/ECS/Entity/Enemy/Enemy.tscn" }
    };

    // ================= Component 类型识别（白名单）=================
    // 用于识别不符合命名约定的特殊 Component（如 Hitbox, Hurtbox）
    private static readonly HashSet<string> _componentWhitelist = new()
    {
        // "Hitbox",
        // "Hurtbox",
        // "CollisionShape2D",  // 特殊情况：物理组件
        // "AnimatedSprite2D",  // 特殊情况：视觉组件
    };

    /// <summary>
    /// 判断节点类型名是否为 Component
    /// 识别规则（按优先级）：
    /// 1. 实现了 IComponent 接口（由 EntityManager 检查）
    /// 2. 类名以 "Component" 结尾
    /// 3. 在白名单中
    /// </summary>
    public static bool IsComponentWhitelist(string typeName)
    {
        if (ECSIndex._componentWhitelist.Contains(typeName))
            return true;
        return false;
    }

    /// <summary>
    /// 添加自定义 Component 类型到白名单
    /// 用于运行时动态注册特殊 Component
    /// </summary>
    public static void RegisterComponentType(string typeName)
    {
        _componentWhitelist.Add(typeName);
    }

    /// <summary>
    public static string Get(string name)
    {
        if (_nameToPathMap.TryGetValue(name, out var path))
        {
            return path;
        }

        return string.Empty;
    }

    /// <summary>
    /// 获取所有已注册的映射（用于调试）。
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetAll() => _nameToPathMap;
}
