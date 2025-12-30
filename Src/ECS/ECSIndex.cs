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

        { "PlayerEntity", "res://Src/ECS/Entity/Player/Player.tscn" },
        { "EnemyEntity", "res://Src/ECS/Entity/Enemy/Enemy.tscn" }
    };

    /// <summary>
    /// 根据定义的常量名获取路径。
    /// 例如：ECSIndex.Get("PlayerEntity") -> "res://Src/ECS/Entity/Player/Player.tscn"
    /// </summary>
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
