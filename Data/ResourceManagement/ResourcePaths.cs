//------------------------------------------------------------------------------
// <ResourceGenerator>
//     ResourceGenerator 资源路径生成器工具
//
//     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。
// </ResourceGenerator>
//------------------------------------------------------------------------------

using System.Collections.Generic;

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
    public static class Entity
    {
        public const string AbilityEntity = "AbilityEntity";
        public const string EnemyEntity = "EnemyEntity";
        public const string PlayerEntity = "PlayerEntity";
        public const string TargetingIndicatorEntity = "TargetingIndicatorEntity";
    }

    public static class Component
    {
        public const string AbilityPreset = "AbilityPreset";
        public const string AbilityTargetSelectionComponent = "AbilityTargetSelectionComponent";
        public const string ActiveSkillInputComponent = "ActiveSkillInputComponent";
        public const string AIComponent = "AIComponent";
        public const string AttackComponent = "AttackComponent";
        public const string ChargeComponent = "ChargeComponent";
        public const string CollisionSensorComponent = "CollisionSensorComponent";
        public const string ContactDamageComponent = "ContactDamageComponent";
        public const string CooldownComponent = "CooldownComponent";
        public const string CostComponent = "CostComponent";
        public const string DataInitComponent = "DataInitComponent";
        public const string EnemyMovementComponent = "EnemyMovementComponent";
        public const string EnemyPreset = "EnemyPreset";
        public const string HealthComponent = "HealthComponent";
        public const string LifecycleComponent = "LifecycleComponent";
        public const string PickupComponent = "PickupComponent";
        public const string PlayerPreset = "PlayerPreset";
        public const string RecoveryComponent = "RecoveryComponent";
        public const string TargetingIndicatorControlComponent = "TargetingIndicatorControlComponent";
        public const string TriggerComponent = "TriggerComponent";
        public const string UnitAnimationComponent = "UnitAnimationComponent";
        public const string UnitCorePreset = "UnitCorePreset";
        public const string UnitStateComponent = "UnitStateComponent";
        public const string VelocityComponent = "VelocityComponent";
    }

    public static class UI
    {
        public const string ActiveSkillBarUI = "ActiveSkillBarUI";
        public const string ActiveSkillSlotUI = "ActiveSkillSlotUI";
        public const string DamageNumberUI = "DamageNumberUI";
        public const string HealthBarUI = "HealthBarUI";
        public const string UIManager = "UIManager";
    }

    public static class Asset
    {
        public const string chailangren = "chailangren";
        public const string deluyi = "deluyi";
        public const string yuren = "yuren";
    }

    public static class System
    {
        public const string DamageService = "DamageService";
        public const string DamageStatisticsSystem = "DamageStatisticsSystem";
        public const string RecoverySystem = "RecoverySystem";
        public const string SpawnSystem = "SpawnSystem";
    }

    public static class Tools
    {
        public const string ObjectPoolInit = "ObjectPoolInit";
        public const string TimerManager = "TimerManager";
    }

    public static class EnemyConfig
    {
        public const string 豺狼人 = "豺狼人";
        public const string 鱼人 = "鱼人";
    }

    public static class PlayerConfig
    {
        public const string 德鲁伊 = "德鲁伊";
    }

    public static class Unit
    {
        public const string TargetingIndicatorConfig = "TargetingIndicatorConfig";
    }

    public static class AbilityConfig
    {
        public const string CircleDamageConfig = "CircleDamageConfig";
        public const string TargetEntitySkillConfig = "TargetEntitySkillConfig";
        public const string TargetPointSkillConfig = "TargetPointSkillConfig";
    }

    public static class ItemConfig
    {
    }

    public static class Test
    {
        public const string ActiveSkillInputTest = "ActiveSkillInputTest";
        public const string DamageSystemTest = "DamageSystemTest";
        public const string DataTestScene = "DataTestScene";
        public const string ECSTestScene = "ECSTestScene";
        public const string ExportTest = "ExportTest";
        public const string InputTest = "InputTest";
        public const string LogTest = "LogTest";
        public const string MainTest = "MainTest";
        public const string MyMathTest = "MyMathTest";
        public const string NodeExtensionsTest = "NodeExtensionsTest";
        public const string ObjectPoolManagerTest = "ObjectPoolManagerTest";
        public const string ObjectPoolVisualTest = "ObjectPoolVisualTest";
        public const string SpawnTestScene = "SpawnTestScene";
        public const string TargetSelectorTest = "TargetSelectorTest";
        public const string TestDataKeyMapping = "TestDataKeyMapping";
        public const string TestEntity = "TestEntity";
    }

    public static class Other
    {
    }

    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()
    {
        { ResourceCategory.Entity, new Dictionary<string, ResourceData>
            {
                { Entity.AbilityEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Ability/AbilityEntity.tscn") },
                { Entity.EnemyEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Enemy/EnemyEntity.tscn") },
                { Entity.PlayerEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Player/PlayerEntity.tscn") },
                { Entity.TargetingIndicatorEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn") },
            }
        },
        { ResourceCategory.Component, new Dictionary<string, ResourceData>
            {
                { Component.AbilityPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Ability/AbilityPreset.tscn") },
                { Component.AbilityTargetSelectionComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/AbilityTargetSelectionComponent/AbilityTargetSelectionComponent.tscn") },
                { Component.ActiveSkillInputComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/ActiveSkillInputComponent/ActiveSkillInputComponent.tscn") },
                { Component.AIComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Enemy/AI/AIComponent.tscn") },
                { Component.AttackComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/AttackComponent/AttackComponent.tscn") },
                { Component.ChargeComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/ChargeComponent/ChargeComponent.tscn") },
                { Component.CollisionSensorComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/CollisionSensorComponent/CollisionSensorComponent.tscn") },
                { Component.ContactDamageComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.tscn") },
                { Component.CooldownComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CooldownComponent/CooldownComponent.tscn") },
                { Component.CostComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CostComponent/CostComponent.tscn") },
                { Component.DataInitComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/DataInitComponent/DataInitComponent.tscn") },
                { Component.EnemyMovementComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Enemy/EnemyMovementComponent/EnemyMovementComponent.tscn") },
                { Component.EnemyPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/EnemyPreset.tscn") },
                { Component.HealthComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/HealthComponent/HealthComponent.tscn") },
                { Component.LifecycleComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/LifecycleComponent/LifecycleComponent.tscn") },
                { Component.PickupComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/PickupComponent/PickupComponent.tscn") },
                { Component.PlayerPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/PlayerPreset.tscn") },
                { Component.RecoveryComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/RecoveryComponent/RecoveryComponent.tscn") },
                { Component.TargetingIndicatorControlComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.tscn") },
                { Component.TriggerComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/TriggerComponent/TriggerComponent.tscn") },
                { Component.UnitAnimationComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.tscn") },
                { Component.UnitCorePreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/UnitCorePreset.tscn") },
                { Component.UnitStateComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitStateComponent/UnitStateComponent.tscn") },
                { Component.VelocityComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/VelocityComponent/VelocityComponent.tscn") },
            }
        },
        { ResourceCategory.UI, new Dictionary<string, ResourceData>
            {
                { UI.ActiveSkillBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillBarUI.tscn") },
                { UI.ActiveSkillSlotUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillSlotUI.tscn") },
                { UI.DamageNumberUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
                { UI.HealthBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/HealthBarUI/HealthBarUI.tscn") },
                { UI.UIManager, new ResourceData(ResourceCategory.UI, "res://Src/UI/Core/UIManager.tscn") },
            }
        },
        { ResourceCategory.Asset, new Dictionary<string, ResourceData>
            {
                { Asset.chailangren, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn") },
                { Asset.deluyi, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn") },
                { Asset.yuren, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn") },
            }
        },
        { ResourceCategory.System, new Dictionary<string, ResourceData>
            {
                { System.DamageService, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageService.tscn") },
                { System.DamageStatisticsSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageStatisticsSystem.tscn") },
                { System.RecoverySystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/RecoverySystem/RecoverySystem.tscn") },
                { System.SpawnSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/Spawn/SpawnSystem.tscn") },
            }
        },
        { ResourceCategory.Tools, new Dictionary<string, ResourceData>
            {
                { Tools.ObjectPoolInit, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/ObjectPool/ObjectPoolInit.tscn") },
                { Tools.TimerManager, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/Timer/TimerManager.tscn") },
            }
        },
        { ResourceCategory.EnemyConfig, new Dictionary<string, ResourceData>
            {
                { EnemyConfig.豺狼人, new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Unit/Enemy/Resource/豺狼人.tres") },
                { EnemyConfig.鱼人, new ResourceData(ResourceCategory.EnemyConfig, "res://Data/Data/Unit/Enemy/Resource/鱼人.tres") },
            }
        },
        { ResourceCategory.PlayerConfig, new Dictionary<string, ResourceData>
            {
                { PlayerConfig.德鲁伊, new ResourceData(ResourceCategory.PlayerConfig, "res://Data/Data/Unit/Player/Resource/德鲁伊.tres") },
            }
        },
        { ResourceCategory.Unit, new Dictionary<string, ResourceData>
            {
                { Unit.TargetingIndicatorConfig, new ResourceData(ResourceCategory.Unit, "res://Data/Data/Unit/Targeting/Resource/TargetingIndicatorConfig.tres") },
            }
        },
        { ResourceCategory.AbilityConfig, new Dictionary<string, ResourceData>
            {
                { AbilityConfig.CircleDamageConfig, new ResourceData(ResourceCategory.AbilityConfig, "res://Data/Data/Ability/Resource/CircleDamageConfig.tres") },
                { AbilityConfig.TargetEntitySkillConfig, new ResourceData(ResourceCategory.AbilityConfig, "res://Data/Data/Ability/Resource/TargetEntitySkillConfig.tres") },
                { AbilityConfig.TargetPointSkillConfig, new ResourceData(ResourceCategory.AbilityConfig, "res://Data/Data/Ability/Resource/TargetPointSkillConfig.tres") },
            }
        },
        { ResourceCategory.ItemConfig, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.Test, new Dictionary<string, ResourceData>
            {
                { Test.ActiveSkillInputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/ActiveSkillInputTest/ActiveSkillInputTest.tscn") },
                { Test.DamageSystemTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn") },
                { Test.DataTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/DataTestScene.tscn") },
                { Test.ECSTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn") },
                { Test.ExportTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Test/ExportTest/ExportTest.tscn") },
                { Test.InputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Input/InputTest.tscn") },
                { Test.LogTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Log/LogTest.tscn") },
                { Test.MainTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/GlobalTest/MainTest/MainTest.tscn") },
                { Test.MyMathTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Math/MyMathTest.tscn") },
                { Test.NodeExtensionsTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Test/NodeExtensionsTest.tscn") },
                { Test.ObjectPoolManagerTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn") },
                { Test.ObjectPoolVisualTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolVisualTest.tscn") },
                { Test.SpawnTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/Spawn/SpawnTestScene.tscn") },
                { Test.TargetSelectorTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.tscn") },
                { Test.TestDataKeyMapping, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn") },
                { Test.TestEntity, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/Entity/TestEntity.tscn") },
            }
        },
        { ResourceCategory.Other, new Dictionary<string, ResourceData>
            {
            }
        },
    };
}
