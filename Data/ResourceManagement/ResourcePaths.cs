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
    public const string Entity_Ability_AbilityEntity = "Ability_AbilityEntity";
    public const string Entity_Effect_EffectEntity = "Effect_EffectEntity";
    public const string Entity_Unit_Enemy_EnemyEntity = "Unit_Enemy_EnemyEntity";
    public const string Entity_Unit_Player_PlayerEntity = "Unit_Player_PlayerEntity";
    public const string Entity_Unit_TargetingIndicator_TargetingIndicatorEntity = "Unit_TargetingIndicator_TargetingIndicatorEntity";

    // --- Component ---
    public const string Component_Ability_AbilityTargetSelectionComponent_AbilityTargetSelectionComponent = "Ability_AbilityTargetSelectionComponent_AbilityTargetSelectionComponent";
    public const string Component_Ability_ChargeComponent_ChargeComponent = "Ability_ChargeComponent_ChargeComponent";
    public const string Component_Ability_CooldownComponent_CooldownComponent = "Ability_CooldownComponent_CooldownComponent";
    public const string Component_Ability_CostComponent_CostComponent = "Ability_CostComponent_CostComponent";
    public const string Component_Ability_TriggerComponent_TriggerComponent = "Ability_TriggerComponent_TriggerComponent";
    public const string Component_Collision_CollisionSensorComponent_CollisionSensorComponent = "Collision_CollisionSensorComponent_CollisionSensorComponent";
    public const string Component_Collision_ContactDamageComponent_ContactDamageComponent = "Collision_ContactDamageComponent_ContactDamageComponent";
    public const string Component_Effect_EffectComponent_EffectComponent = "Effect_EffectComponent_EffectComponent";
    public const string Component_Player_ActiveSkillInputComponent_ActiveSkillInputComponent = "Player_ActiveSkillInputComponent_ActiveSkillInputComponent";
    public const string Component_Player_PickupComponent_PickupComponent = "Player_PickupComponent_PickupComponent";
    public const string Component_Player_VelocityComponent_VelocityComponent = "Player_VelocityComponent_VelocityComponent";
    public const string Component_Presets_Ability_AbilityPreset = "Presets_Ability_AbilityPreset";
    public const string Component_Presets_Unit_EnemyPreset = "Presets_Unit_EnemyPreset";
    public const string Component_Presets_Unit_PlayerPreset = "Presets_Unit_PlayerPreset";
    public const string Component_Presets_Unit_UnitCorePreset = "Presets_Unit_UnitCorePreset";
    public const string Component_Unit_Common_AttackComponent_AttackComponent = "Unit_Common_AttackComponent_AttackComponent";
    public const string Component_Unit_Common_DataInitComponent_DataInitComponent = "Unit_Common_DataInitComponent_DataInitComponent";
    public const string Component_Unit_Common_HealthComponent_HealthComponent = "Unit_Common_HealthComponent_HealthComponent";
    public const string Component_Unit_Common_LifecycleComponent_LifecycleComponent = "Unit_Common_LifecycleComponent_LifecycleComponent";
    public const string Component_Unit_Common_RecoveryComponent_RecoveryComponent = "Unit_Common_RecoveryComponent_RecoveryComponent";
    public const string Component_Unit_Common_UnitAnimationComponent_UnitAnimationComponent = "Unit_Common_UnitAnimationComponent_UnitAnimationComponent";
    public const string Component_Unit_Common_UnitStateComponent_UnitStateComponent = "Unit_Common_UnitStateComponent_UnitStateComponent";
    public const string Component_Unit_Enemy_AI_AIComponent = "Unit_Enemy_AI_AIComponent";
    public const string Component_Unit_Enemy_EnemyMovementComponent_EnemyMovementComponent = "Unit_Enemy_EnemyMovementComponent_EnemyMovementComponent";
    public const string Component_Unit_TargetingIndicatorControlComponent_TargetingIndicatorControlComponent = "Unit_TargetingIndicatorControlComponent_TargetingIndicatorControlComponent";

    // --- UI ---
    public const string UI_Core_UIManager = "Core_UIManager";
    public const string UI_UI_DamageNumberUI_DamageNumberUI = "UI_DamageNumberUI_DamageNumberUI";
    public const string UI_UI_HealthBarUI_HealthBarUI = "UI_HealthBarUI_HealthBarUI";
    public const string UI_UI_SkillUI_ActiveSkillBarUI = "UI_SkillUI_ActiveSkillBarUI";
    public const string UI_UI_SkillUI_ActiveSkillSlotUI = "UI_SkillUI_ActiveSkillSlotUI";

    // --- Asset ---
    public const string Asset_Effect_003 = "Effect_003";
    public const string Asset_Effect_004龙卷风 = "Effect_004龙卷风";
    public const string Asset_Effect_020 = "Effect_020";
    public const string Asset_Effect_lrsc3 = "Effect_lrsc3";
    public const string Asset_Unit_Enemy_chailangren = "Unit_Enemy_chailangren";
    public const string Asset_Unit_Enemy_yuren = "Unit_Enemy_yuren";
    public const string Asset_Unit_Player_deluyi = "Unit_Player_deluyi";

    // --- System ---
    public const string System_DamageSystem_DamageService = "DamageSystem_DamageService";
    public const string System_DamageSystem_DamageStatisticsSystem = "DamageSystem_DamageStatisticsSystem";
    public const string System_RecoverySystem_RecoverySystem = "RecoverySystem_RecoverySystem";
    public const string System_Spawn_SpawnSystem = "Spawn_SpawnSystem";

    // --- Tools ---
    public const string Tools_ObjectPool_ObjectPoolInit = "ObjectPool_ObjectPoolInit";
    public const string Tools_Timer_TimerManager = "Timer_TimerManager";

    // --- Data ---
    public const string Data_Ability_Resource_ChainLightningConfig = "Ability_Resource_ChainLightningConfig";
    public const string Data_Ability_Resource_CircleDamageConfig = "Ability_Resource_CircleDamageConfig";
    public const string Data_Ability_Resource_DashConfig = "Ability_Resource_DashConfig";
    public const string Data_Ability_Resource_SlamConfig = "Ability_Resource_SlamConfig";
    public const string Data_Ability_Resource_TargetEntitySkillConfig = "Ability_Resource_TargetEntitySkillConfig";
    public const string Data_Ability_Resource_TargetPointSkillConfig = "Ability_Resource_TargetPointSkillConfig";
    public const string Data_Unit_Enemy_Resource_chailangren = "Unit_Enemy_Resource_chailangren";
    public const string Data_Unit_Enemy_Resource_yuren = "Unit_Enemy_Resource_yuren";
    public const string Data_Unit_Player_Resource_deluyi = "Unit_Player_Resource_deluyi";
    public const string Data_Unit_Targeting_Resource_TargetingIndicatorConfig = "Unit_Targeting_Resource_TargetingIndicatorConfig";

    // --- Test ---
    public const string Test_GlobalTest_MainTest_MainTest = "GlobalTest_MainTest_MainTest";
    public const string Test_SingleTest_ECS_Data_DataTestScene = "SingleTest_ECS_Data_DataTestScene";
    public const string Test_SingleTest_ECS_Data_TestDataKeyMapping = "SingleTest_ECS_Data_TestDataKeyMapping";
    public const string Test_SingleTest_ECS_ECSTest_ECSTestScene = "SingleTest_ECS_ECSTest_ECSTestScene";
    public const string Test_SingleTest_ECS_ECSTest_Entity_TestEntity = "SingleTest_ECS_ECSTest_Entity_TestEntity";
    public const string Test_SingleTest_ECS_System_ActiveSkillInputTest_ActiveSkillInputTest = "SingleTest_ECS_System_ActiveSkillInputTest_ActiveSkillInputTest";
    public const string Test_SingleTest_ECS_System_DamageSystemTest_DamageSystemTest = "SingleTest_ECS_System_DamageSystemTest_DamageSystemTest";
    public const string Test_SingleTest_ECS_System_Spawn_SpawnTestScene = "SingleTest_ECS_System_Spawn_SpawnTestScene";
    public const string Test_SingleTest_Test_ExportTest_ExportTest = "SingleTest_Test_ExportTest_ExportTest";
    public const string Test_SingleTest_Tools_Input_InputTest = "SingleTest_Tools_Input_InputTest";
    public const string Test_SingleTest_Tools_Log_LogTest = "SingleTest_Tools_Log_LogTest";
    public const string Test_SingleTest_Tools_Math_MyMathTest = "SingleTest_Tools_Math_MyMathTest";
    public const string Test_SingleTest_Tools_ObjectPool_ObjectPoolManagerTest = "SingleTest_Tools_ObjectPool_ObjectPoolManagerTest";
    public const string Test_SingleTest_Tools_ObjectPool_ObjectPoolVisualTest = "SingleTest_Tools_ObjectPool_ObjectPoolVisualTest";
    public const string Test_SingleTest_Tools_TargetSelector_TargetSelectorTest = "SingleTest_Tools_TargetSelector_TargetSelectorTest";

    // --- Other ---

    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()
    {
        { ResourceCategory.Entity, new Dictionary<string, ResourceData>
            {
                { Entity_Ability_AbilityEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Ability/AbilityEntity.tscn") },
                { Entity_Effect_EffectEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Effect/EffectEntity.tscn") },
                { Entity_Unit_Enemy_EnemyEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Enemy/EnemyEntity.tscn") },
                { Entity_Unit_Player_PlayerEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/Player/PlayerEntity.tscn") },
                { Entity_Unit_TargetingIndicator_TargetingIndicatorEntity, new ResourceData(ResourceCategory.Entity, "res://Src/ECS/Entity/Unit/TargetingIndicator/TargetingIndicatorEntity.tscn") },
            }
        },
        { ResourceCategory.Component, new Dictionary<string, ResourceData>
            {
                { Component_Ability_AbilityTargetSelectionComponent_AbilityTargetSelectionComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/AbilityTargetSelectionComponent/AbilityTargetSelectionComponent.tscn") },
                { Component_Ability_ChargeComponent_ChargeComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/ChargeComponent/ChargeComponent.tscn") },
                { Component_Ability_CooldownComponent_CooldownComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CooldownComponent/CooldownComponent.tscn") },
                { Component_Ability_CostComponent_CostComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/CostComponent/CostComponent.tscn") },
                { Component_Ability_TriggerComponent_TriggerComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Ability/TriggerComponent/TriggerComponent.tscn") },
                { Component_Collision_CollisionSensorComponent_CollisionSensorComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/CollisionSensorComponent/CollisionSensorComponent.tscn") },
                { Component_Collision_ContactDamageComponent_ContactDamageComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Collision/ContactDamageComponent/ContactDamageComponent.tscn") },
                { Component_Effect_EffectComponent_EffectComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Effect/EffectComponent/EffectComponent.tscn") },
                { Component_Player_ActiveSkillInputComponent_ActiveSkillInputComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/ActiveSkillInputComponent/ActiveSkillInputComponent.tscn") },
                { Component_Player_PickupComponent_PickupComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/PickupComponent/PickupComponent.tscn") },
                { Component_Player_VelocityComponent_VelocityComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Player/VelocityComponent/VelocityComponent.tscn") },
                { Component_Presets_Ability_AbilityPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Ability/AbilityPreset.tscn") },
                { Component_Presets_Unit_EnemyPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/EnemyPreset.tscn") },
                { Component_Presets_Unit_PlayerPreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/PlayerPreset.tscn") },
                { Component_Presets_Unit_UnitCorePreset, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Presets/Unit/UnitCorePreset.tscn") },
                { Component_Unit_Common_AttackComponent_AttackComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/AttackComponent/AttackComponent.tscn") },
                { Component_Unit_Common_DataInitComponent_DataInitComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/DataInitComponent/DataInitComponent.tscn") },
                { Component_Unit_Common_HealthComponent_HealthComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/HealthComponent/HealthComponent.tscn") },
                { Component_Unit_Common_LifecycleComponent_LifecycleComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/LifecycleComponent/LifecycleComponent.tscn") },
                { Component_Unit_Common_RecoveryComponent_RecoveryComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/RecoveryComponent/RecoveryComponent.tscn") },
                { Component_Unit_Common_UnitAnimationComponent_UnitAnimationComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.tscn") },
                { Component_Unit_Common_UnitStateComponent_UnitStateComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Common/UnitStateComponent/UnitStateComponent.tscn") },
                { Component_Unit_Enemy_AI_AIComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Enemy/AI/AIComponent.tscn") },
                { Component_Unit_Enemy_EnemyMovementComponent_EnemyMovementComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/Enemy/EnemyMovementComponent/EnemyMovementComponent.tscn") },
                { Component_Unit_TargetingIndicatorControlComponent_TargetingIndicatorControlComponent, new ResourceData(ResourceCategory.Component, "res://Src/ECS/Component/Unit/TargetingIndicatorControlComponent/TargetingIndicatorControlComponent.tscn") },
            }
        },
        { ResourceCategory.UI, new Dictionary<string, ResourceData>
            {
                { UI_Core_UIManager, new ResourceData(ResourceCategory.UI, "res://Src/UI/Core/UIManager.tscn") },
                { UI_UI_DamageNumberUI_DamageNumberUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/DamageNumberUI/DamageNumberUI.tscn") },
                { UI_UI_HealthBarUI_HealthBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/HealthBarUI/HealthBarUI.tscn") },
                { UI_UI_SkillUI_ActiveSkillBarUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillBarUI.tscn") },
                { UI_UI_SkillUI_ActiveSkillSlotUI, new ResourceData(ResourceCategory.UI, "res://Src/UI/UI/SkillUI/ActiveSkillSlotUI.tscn") },
            }
        },
        { ResourceCategory.Asset, new Dictionary<string, ResourceData>
            {
                { Asset_Effect_003, new ResourceData(ResourceCategory.Asset, "res://assets/Effect/003/AnimatedSprite2D/003.tscn") },
                { Asset_Effect_004龙卷风, new ResourceData(ResourceCategory.Asset, "res://assets/Effect/004龙卷风/AnimatedSprite2D/004龙卷风.tscn") },
                { Asset_Effect_020, new ResourceData(ResourceCategory.Asset, "res://assets/Effect/020/AnimatedSprite2D/020.tscn") },
                { Asset_Effect_lrsc3, new ResourceData(ResourceCategory.Asset, "res://assets/Effect/lrsc3/AnimatedSprite2D/lrsc3.tscn") },
                { Asset_Unit_Enemy_chailangren, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/chailangren/AnimatedSprite2D/chailangren.tscn") },
                { Asset_Unit_Enemy_yuren, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Enemy/yuren/AnimatedSprite2D/yuren.tscn") },
                { Asset_Unit_Player_deluyi, new ResourceData(ResourceCategory.Asset, "res://assets/Unit/Player/deluyi/AnimatedSprite2D/deluyi.tscn") },
            }
        },
        { ResourceCategory.System, new Dictionary<string, ResourceData>
            {
                { System_DamageSystem_DamageService, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageService.tscn") },
                { System_DamageSystem_DamageStatisticsSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/DamageSystem/DamageStatisticsSystem.tscn") },
                { System_RecoverySystem_RecoverySystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/RecoverySystem/RecoverySystem.tscn") },
                { System_Spawn_SpawnSystem, new ResourceData(ResourceCategory.System, "res://Src/ECS/System/Spawn/SpawnSystem.tscn") },
            }
        },
        { ResourceCategory.Tools, new Dictionary<string, ResourceData>
            {
                { Tools_ObjectPool_ObjectPoolInit, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/ObjectPool/ObjectPoolInit.tscn") },
                { Tools_Timer_TimerManager, new ResourceData(ResourceCategory.Tools, "res://Src/Tools/Timer/TimerManager.tscn") },
            }
        },
        { ResourceCategory.Data, new Dictionary<string, ResourceData>
            {
                { Data_Ability_Resource_ChainLightningConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/ChainLightningConfig.tres") },
                { Data_Ability_Resource_CircleDamageConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/CircleDamageConfig.tres") },
                { Data_Ability_Resource_DashConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/DashConfig.tres") },
                { Data_Ability_Resource_SlamConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/SlamConfig.tres") },
                { Data_Ability_Resource_TargetEntitySkillConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/TargetEntitySkillConfig.tres") },
                { Data_Ability_Resource_TargetPointSkillConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Ability/Resource/TargetPointSkillConfig.tres") },
                { Data_Unit_Enemy_Resource_chailangren, new ResourceData(ResourceCategory.Data, "res://Data/Data/Unit/Enemy/Resource/chailangren.tres") },
                { Data_Unit_Enemy_Resource_yuren, new ResourceData(ResourceCategory.Data, "res://Data/Data/Unit/Enemy/Resource/yuren.tres") },
                { Data_Unit_Player_Resource_deluyi, new ResourceData(ResourceCategory.Data, "res://Data/Data/Unit/Player/Resource/deluyi.tres") },
                { Data_Unit_Targeting_Resource_TargetingIndicatorConfig, new ResourceData(ResourceCategory.Data, "res://Data/Data/Unit/Targeting/Resource/TargetingIndicatorConfig.tres") },
            }
        },
        { ResourceCategory.Test, new Dictionary<string, ResourceData>
            {
                { Test_GlobalTest_MainTest_MainTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/GlobalTest/MainTest/MainTest.tscn") },
                { Test_SingleTest_ECS_Data_DataTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/DataTestScene.tscn") },
                { Test_SingleTest_ECS_Data_TestDataKeyMapping, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn") },
                { Test_SingleTest_ECS_ECSTest_ECSTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/ECSTestScene.tscn") },
                { Test_SingleTest_ECS_ECSTest_Entity_TestEntity, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/ECSTest/Entity/TestEntity.tscn") },
                { Test_SingleTest_ECS_System_ActiveSkillInputTest_ActiveSkillInputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/ActiveSkillInputTest/ActiveSkillInputTest.tscn") },
                { Test_SingleTest_ECS_System_DamageSystemTest_DamageSystemTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn") },
                { Test_SingleTest_ECS_System_Spawn_SpawnTestScene, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/ECS/System/Spawn/SpawnTestScene.tscn") },
                { Test_SingleTest_Test_ExportTest_ExportTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Test/ExportTest/ExportTest.tscn") },
                { Test_SingleTest_Tools_Input_InputTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Input/InputTest.tscn") },
                { Test_SingleTest_Tools_Log_LogTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Log/LogTest.tscn") },
                { Test_SingleTest_Tools_Math_MyMathTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/Math/MyMathTest.tscn") },
                { Test_SingleTest_Tools_ObjectPool_ObjectPoolManagerTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn") },
                { Test_SingleTest_Tools_ObjectPool_ObjectPoolVisualTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/ObjectPool/ObjectPoolVisualTest.tscn") },
                { Test_SingleTest_Tools_TargetSelector_TargetSelectorTest, new ResourceData(ResourceCategory.Test, "res://Src/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.tscn") },
            }
        },
        { ResourceCategory.Other, new Dictionary<string, ResourceData>
            {
            }
        },
    };
}
