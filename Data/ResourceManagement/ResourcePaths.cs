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
    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()
    {
        { ResourceCategory.Entity, new Dictionary<string, ResourceData>
            {
                { "AbilityEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Ability/AbilityEntity.tscn") },
                { "EnemyEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Enemy/EnemyEntity.tscn") },
                { "PlayerEntity", new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Player/PlayerEntity.tscn") },
            }
        },
        { ResourceCategory.Component, new Dictionary<string, ResourceData>
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
            }
        },
        { ResourceCategory.UI, new Dictionary<string, ResourceData>
            {
                { "DamageNumberUI", new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
                { "HealthBarUI", new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/HealthBarUI/HealthBarUI.tscn") },
            }
        },
        { ResourceCategory.Asset, new Dictionary<string, ResourceData>
            {
                { "豺狼人", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/豺狼人/AnimatedSprite2D/豺狼人.tscn") },
                { "德鲁伊", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Player/德鲁伊/AnimatedSprite2D/德鲁伊.tscn") },
                { "鱼人", new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/鱼人/AnimatedSprite2D/鱼人.tscn") },
            }
        },
        { ResourceCategory.EnemyConfig, new Dictionary<string, ResourceData>
            {
                { "豺狼人", new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Resources/Enemies/豺狼人.tres") },
                { "鱼人", new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Resources/Enemies/鱼人.tres") },
            }
        },
        { ResourceCategory.PlayerConfig, new Dictionary<string, ResourceData>
            {
                { "德鲁伊", new ResourceData(ResourceCategory.PlayerConfig, "res://Data/Data/Resources/Players/德鲁伊.tres") },
            }
        },
        { ResourceCategory.AbilityConfig, new Dictionary<string, ResourceData>
            {
                { "CircleDamageConfig", new ResourceData(ResourceCategory.AbilityConfig, "res://Data/Data/Resources/Abilities/CircleDamageConfig.tres") },
            }
        },
        { ResourceCategory.ItemConfig, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.Other, new Dictionary<string, ResourceData>
            {
            }
        },
    };
}
