//------------------------------------------------------------------------------
//* <ResourceGenerator>
//*     ResourceGenerator 资源路径生成器工具
//*
//*     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。
//* </ResourceGenerator>
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
    // --- Entity ---
    public const string Entity_AbilityEntity = "AbilityEntity";
    public const string Entity_EffectEntity = "EffectEntity";
    public const string Entity_EnemyEntity = "EnemyEntity";
    public const string Entity_LightningLineEffect = "LightningLineEffect";
    public const string Entity_PlayerEntity = "PlayerEntity";
    public const string Entity_TargetingIndicatorEntity = "TargetingIndicatorEntity";

    // --- Component ---
    public const string Component_AbilityPreset = "AbilityPreset";
    public const string Component_AbilityTargetSelectionComponent = "AbilityTargetSelectionComponent";
    public const string Component_ActiveSkillInputComponent = "ActiveSkillInputComponent";
    public const string Component_AIComponent = "AIComponent";
    public const string Component_AttackComponent = "AttackComponent";
    public const string Component_ChargeComponent = "ChargeComponent";
    public const string Component_CollisionComponent = "CollisionComponent";
    public const string Component_ContactDamageComponent = "ContactDamageComponent";
    public const string Component_CooldownComponent = "CooldownComponent";
    public const string Component_CostComponent = "CostComponent";
    public const string Component_DataInitComponent = "DataInitComponent";
    public const string Component_EffectComponent = "EffectComponent";
    public const string Component_EnemyPreset = "EnemyPreset";
    public const string Component_EntityMovementComponent = "EntityMovementComponent";
    public const string Component_HealthComponent = "HealthComponent";
    public const string Component_HurtboxComponent = "HurtboxComponent";
    public const string Component_LifecycleComponent = "LifecycleComponent";
    public const string Component_PickupComponent = "PickupComponent";
    public const string Component_PlayerPreset = "PlayerPreset";
    public const string Component_RecoveryComponent = "RecoveryComponent";
    public const string Component_TargetingIndicatorControlComponent = "TargetingIndicatorControlComponent";
    public const string Component_TriggerComponent = "TriggerComponent";
    public const string Component_UnitAnimationComponent = "UnitAnimationComponent";
    public const string Component_UnitCorePreset = "UnitCorePreset";
    public const string Component_UnitStateComponent = "UnitStateComponent";

    // --- UI ---
    public const string UI_ActiveSkillBarUI = "ActiveSkillBarUI";
    public const string UI_ActiveSkillSlotUI = "ActiveSkillSlotUI";
    public const string UI_DamageNumberUI = "DamageNumberUI";
    public const string UI_HealthBarUI = "HealthBarUI";
    public const string UI_UIManager = "UIManager";

    // --- Asset ---

    // --- AssetEffect ---
    public const string AssetEffect_003 = "003";
    public const string AssetEffect_004龙卷风 = "004龙卷风";
    public const string AssetEffect_020 = "020";
    public const string AssetEffect_lrsc3 = "lrsc3";

    // --- AssetUnit ---

    // --- AssetUnitEnemy ---
    public const string AssetUnitEnemy_chailangren = "chailangren";
    public const string AssetUnitEnemy_yuren = "yuren";

    // --- AssetUnitPlayer ---
    public const string AssetUnitPlayer_bubing = "bubing";
    public const string AssetUnitPlayer_deluyi = "deluyi";
    public const string AssetUnitPlayer_guangfa = "guangfa";

    // --- System ---
    public const string System_DamageService = "DamageService";
    public const string System_DamageStatisticsSystem = "DamageStatisticsSystem";
    public const string System_RecoverySystem = "RecoverySystem";
    public const string System_SpawnSystem = "SpawnSystem";

    // --- Tools ---
    public const string Tools_ObjectPoolInit = "ObjectPoolInit";
    public const string Tools_TimerManager = "TimerManager";

    // --- Data ---

    // --- DataAbility ---
    public const string DataAbility_ChainLightningConfig = "ChainLightningConfig";
    public const string DataAbility_CircleDamageConfig = "CircleDamageConfig";
    public const string DataAbility_DashConfig = "DashConfig";
    public const string DataAbility_SlamConfig = "SlamConfig";
    public const string DataAbility_TargetPointSkillConfig = "TargetPointSkillConfig";

    // --- DataUnit ---
    public const string DataUnit_chailangren = "chailangren";
    public const string DataUnit_deluyi = "deluyi";
    public const string DataUnit_TargetingIndicatorConfig = "TargetingIndicatorConfig";
    public const string DataUnit_yuren = "yuren";

    // --- DataCollision ---

    // --- Test ---
    public const string Test_ActiveSkillInputTest = "ActiveSkillInputTest";
    public const string Test_DamageSystemTest = "DamageSystemTest";
    public const string Test_DataTestScene = "DataTestScene";
    public const string Test_ECSTestScene = "ECSTestScene";
    public const string Test_ExportTest = "ExportTest";
    public const string Test_InputTest = "InputTest";
    public const string Test_LogTest = "LogTest";
    public const string Test_MainTest = "MainTest";
    public const string Test_MovementComponentTestScene = "MovementComponentTestScene";
    public const string Test_MyMathTest = "MyMathTest";
    public const string Test_ObjectPoolManagerTest = "ObjectPoolManagerTest";
    public const string Test_ObjectPoolVisualTest = "ObjectPoolVisualTest";
    public const string Test_SpawnTestScene = "SpawnTestScene";
    public const string Test_TargetSelectorTest = "TargetSelectorTest";
    public const string Test_TestDataKeyMapping = "TestDataKeyMapping";
    public const string Test_TestEntity = "TestEntity";

    // --- Other ---

    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()
    {
        { ResourceCategory.Entity, new Dictionary<string, ResourceData>
            {
                { Entity_AbilityEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Ability/AbilityEntity.tscn") },
                { Entity_EffectEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Effect/EffectEntity.tscn") },
                { Entity_EnemyEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Enemy/EnemyEntity.tscn") },
                { Entity_LightningLineEffect, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Effect/LightningLineEffect/LightningLineEffect.tscn") },
                { Entity_PlayerEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Player/PlayerEntity.tscn") },
                { Entity_TargetingIndicatorEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn") },
            }
        },
        { ResourceCategory.Component, new Dictionary<string, ResourceData>
            {
                { Component_AbilityPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Ability/AbilityPreset.tscn") },
                { Component_AbilityTargetSelectionComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/AbilityTargetSelectionComponent/AbilityTargetSelectionComponent.tscn") },
                { Component_ActiveSkillInputComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/ActiveSkillInputComponent/ActiveSkillInputComponent.tscn") },
                { Component_AIComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Enemy/AI/AIComponent.tscn") },
                { Component_AttackComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/AttackComponent/AttackComponent.tscn") },
                { Component_ChargeComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/ChargeComponent/ChargeComponent.tscn") },
                { Component_CollisionComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/CollisionComponent/CollisionComponent.tscn") },
                { Component_ContactDamageComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.tscn") },
                { Component_CooldownComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CooldownComponent/CooldownComponent.tscn") },
                { Component_CostComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CostComponent/CostComponent.tscn") },
                { Component_DataInitComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/DataInitComponent/DataInitComponent.tscn") },
                { Component_EffectComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Effect/EffectComponent/EffectComponent.tscn") },
                { Component_EnemyPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/EnemyPreset.tscn") },
                { Component_EntityMovementComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Movement/EntityMovementComponent.tscn") },
                { Component_HealthComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/HealthComponent/HealthComponent.tscn") },
                { Component_HurtboxComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/HurtboxComponent/HurtboxComponent.tscn") },
                { Component_LifecycleComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/LifecycleComponent/LifecycleComponent.tscn") },
                { Component_PickupComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/PickupComponent/PickupComponent.tscn") },
                { Component_PlayerPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/PlayerPreset.tscn") },
                { Component_RecoveryComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/RecoveryComponent/RecoveryComponent.tscn") },
                { Component_TargetingIndicatorControlComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.tscn") },
                { Component_TriggerComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/TriggerComponent/TriggerComponent.tscn") },
                { Component_UnitAnimationComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.tscn") },
                { Component_UnitCorePreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/UnitCorePreset.tscn") },
                { Component_UnitStateComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitStateComponent/UnitStateComponent.tscn") },
            }
        },
        { ResourceCategory.UI, new Dictionary<string, ResourceData>
            {
                { UI_ActiveSkillBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillBarUI.tscn") },
                { UI_ActiveSkillSlotUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillSlotUI.tscn") },
                { UI_DamageNumberUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
                { UI_HealthBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/HealthBarUI/HealthBarUI.tscn") },
                { UI_UIManager, new ResourceData(ResourceCategory.UI, "res://Src/UI/Core/UIManager.tscn") },
            }
        },
        { ResourceCategory.Asset, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.AssetEffect, new Dictionary<string, ResourceData>
            {
                { AssetEffect_003, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/003/AnimatedSprite2D/003.tscn") },
                { AssetEffect_004龙卷风, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn") },
                { AssetEffect_020, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/020/AnimatedSprite2D/020.tscn") },
                { AssetEffect_lrsc3, new ResourceData(ResourceCategory.AssetEffect, "res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn") },
            }
        },
        { ResourceCategory.AssetUnit, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.AssetUnitEnemy, new Dictionary<string, ResourceData>
            {
                { AssetUnitEnemy_chailangren, new ResourceData(ResourceCategory.AssetUnitEnemy, "res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn") },
                { AssetUnitEnemy_yuren, new ResourceData(ResourceCategory.AssetUnitEnemy, "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn") },
            }
        },
        { ResourceCategory.AssetUnitPlayer, new Dictionary<string, ResourceData>
            {
                { AssetUnitPlayer_bubing, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/bubing/AnimatedSprite2D/bubing.tscn") },
                { AssetUnitPlayer_deluyi, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn") },
                { AssetUnitPlayer_guangfa, new ResourceData(ResourceCategory.AssetUnitPlayer, "res://assets/Unit/Player/guangfa/AnimatedSprite2D/guangfa.tscn") },
            }
        },
        { ResourceCategory.System, new Dictionary<string, ResourceData>
            {
                { System_DamageService, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageService.tscn") },
                { System_DamageStatisticsSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageStatisticsSystem.tscn") },
                { System_RecoverySystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/RecoverySystem/RecoverySystem.tscn") },
                { System_SpawnSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/Spawn/SpawnSystem.tscn") },
            }
        },
        { ResourceCategory.Tools, new Dictionary<string, ResourceData>
            {
                { Tools_ObjectPoolInit, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/ObjectPool/ObjectPoolInit.tscn") },
                { Tools_TimerManager, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/Timer/TimerManager.tscn") },
            }
        },
        { ResourceCategory.Data, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.DataAbility, new Dictionary<string, ResourceData>
            {
                { DataAbility_ChainLightningConfig, new ResourceData(ResourceCategory.DataAbility, "res://Data/Data/Ability/Ability/ChainLightning/Data/ChainLightningConfig.tres") },
                { DataAbility_CircleDamageConfig, new ResourceData(ResourceCategory.DataAbility, "res://Data/Data/Ability/Resource/CircleDamageConfig.tres") },
                { DataAbility_DashConfig, new ResourceData(ResourceCategory.DataAbility, "res://Data/Data/Ability/Resource/DashConfig.tres") },
                { DataAbility_SlamConfig, new ResourceData(ResourceCategory.DataAbility, "res://Data/Data/Ability/Resource/SlamConfig.tres") },
                { DataAbility_TargetPointSkillConfig, new ResourceData(ResourceCategory.DataAbility, "res://Data/Data/Ability/Resource/TargetPointSkillConfig.tres") },
            }
        },
        { ResourceCategory.DataUnit, new Dictionary<string, ResourceData>
            {
                { DataUnit_chailangren, new ResourceData(ResourceCategory.DataUnit, "res://Data/Data/Unit/Enemy/Resource/chailangren.tres") },
                { DataUnit_deluyi, new ResourceData(ResourceCategory.DataUnit, "res://Data/Data/Unit/Player/Resource/deluyi.tres") },
                { DataUnit_TargetingIndicatorConfig, new ResourceData(ResourceCategory.DataUnit, "res://Data/Data/Unit/Targeting/Resource/TargetingIndicatorConfig.tres") },
                { DataUnit_yuren, new ResourceData(ResourceCategory.DataUnit, "res://Data/Data/Unit/Enemy/Resource/yuren.tres") },
            }
        },
        { ResourceCategory.DataCollision, new Dictionary<string, ResourceData>
            {
            }
        },
        { ResourceCategory.Test, new Dictionary<string, ResourceData>
            {
                { Test_ActiveSkillInputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/ActiveSkillInputTest/ActiveSkillInputTest.tscn") },
                { Test_DamageSystemTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn") },
                { Test_DataTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/DataTestScene.tscn") },
                { Test_ECSTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn") },
                { Test_ExportTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Test/ExportTest/ExportTest.tscn") },
                { Test_InputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Input/InputTest.tscn") },
                { Test_LogTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Log/LogTest.tscn") },
                { Test_MainTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/GlobalTest/MainTest/MainTest.tscn") },
                { Test_MovementComponentTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Movement/MovementComponentTestScene.tscn") },
                { Test_MyMathTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Math/MyMathTest.tscn") },
                { Test_ObjectPoolManagerTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn") },
                { Test_ObjectPoolVisualTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolVisualTest.tscn") },
                { Test_SpawnTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/Spawn/SpawnTestScene.tscn") },
                { Test_TargetSelectorTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.tscn") },
                { Test_TestDataKeyMapping, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn") },
                { Test_TestEntity, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/Entity/TestEntity.tscn") },
            }
        },
        { ResourceCategory.Other, new Dictionary<string, ResourceData>
            {
            }
        },
    };
}
