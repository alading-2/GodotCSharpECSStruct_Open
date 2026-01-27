//------------------------------------------------------------------------------
// <ResourceGenerator>
//     ResourceGenerator 资源路径生成器工具
//
//     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。
// </ResourceGenerator>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using Brotato.Data.ResourceManagement;

public struct ResourceData
{
    public string Path;
    public ResourceCategory Category;
    public ResourceData(ResourceCategory category, string path)
    {
        Category = category;
        Path = path;
    }
}

public static class ResourcePaths
{
    public static readonly Dictionary<string, ResourceData> AbilityConfigs = new()
    {
        { "CircleDamageConfig", new ResourceData(ResourceCategory.AbilityConfig, "res://Data/Data/Resources/Abilities/CircleDamageConfig.tres") },
    };

    public static readonly Dictionary<string, ResourceData> Assets = new()
    {
        { "豺狼人", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/豺狼人/AnimatedSprite2D/豺狼人.tscn") },
        { "德鲁伊", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Player/德鲁伊/AnimatedSprite2D/德鲁伊.tscn") },
        { "鱼人", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/鱼人/AnimatedSprite2D/鱼人.tscn") },
    };

    public static readonly Dictionary<string, ResourceData> Components = new()
    {
        { "AbilityPreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Ability/AbilityPreset.tscn") },
        { "ChargeComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/ChargeComponent/ChargeComponent.tscn") },
        { "CombatPreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/CombatPreset.tscn") },
        { "CooldownComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CooldownComponent/CooldownComponent.tscn") },
        { "CostComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CostComponent/CostComponent.tscn") },
        { "EnemyPreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/EnemyPreset.tscn") },
        { "FollowComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/FollowComponent/FollowComponent.tscn") },
        { "FollowPreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/FollowPreset.tscn") },
        { "HealthComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/HealthComponent/HealthComponent.tscn") },
        { "HitboxComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/HitboxComponent/HitboxComponent.tscn") },
        { "HurtboxComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/HurtboxComponent/HurtboxComponent.tscn") },
        { "LifecycleComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/LifecycleComponent/LifecycleComponent.tscn") },
        { "PickupComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/PickupComponent/PickupComponent.tscn") },
        { "PlayerPreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/PlayerPreset.tscn") },
        { "RecoveryComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/RecoveryComponent/RecoveryComponent.tscn") },
        { "TriggerComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/TriggerComponent/TriggerComponent.tscn") },
        { "UnitCorePreset", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/UnitCorePreset.tscn") },
        { "UnitStateComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/UnitStateComponent/UnitStateComponent.tscn") },
        { "VelocityComponent", new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/VelocityComponent/VelocityComponent.tscn") },
    };

    public static readonly Dictionary<string, ResourceData> EnemyConfigs = new()
    {
        { "豺狼人Config", new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Resources/Enemies/豺狼人Config.tres") },
        { "鱼人Config", new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Resources/Enemies/鱼人Config.tres") },
    };

    public static readonly Dictionary<string, ResourceData> Entities = new()
    {
        { "AbilityEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Ability/AbilityEntity.tscn") },
        { "EnemyEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Enemy/EnemyEntity.tscn") },
        { "PlayerEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Player/PlayerEntity.tscn") },
    };

    public static readonly Dictionary<string, ResourceData> ItemConfigs = new()
    {
    };

    public static readonly Dictionary<string, ResourceData> Other = new()
    {
    };

    public static readonly Dictionary<string, ResourceData> PlayerConfigs = new()
    {
        { "德鲁伊Config", new ResourceData(ResourceCategory.PlayerConfig, "res://Data/Data/Resources/Players/德鲁伊Config.tres") },
    };

    public static readonly Dictionary<string, ResourceData> UI = new()
    {
        { "DamageNumberUI", new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
        { "HealthBarUI", new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/HealthBarUI/HealthBarUI.tscn") },
    };

    /// <summary>
    /// 所有资源的合并字典（兼容性保留，建议使用分类字典）
    /// </summary>
    public static readonly Dictionary<string, ResourceData> All = new();

    static ResourcePaths()
    {
        // 合并所有分类到 All 字典
        foreach (var dict in new[] { Entities, Components, UI, Assets, EnemyConfigs, PlayerConfigs, AbilityConfigs, ItemConfigs, Other })
        {
            foreach (var kvp in dict)
            {
                if (!All.ContainsKey(kvp.Key))
                    All[kvp.Key] = kvp.Value;
            }
        }
    }
}
