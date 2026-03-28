---
name: project-index
description: 查找项目任意模块的文档、源码、模板文件时使用。当需要了解项目整体架构、定位某个系统的实现文件、查找设计文档或 API 手册时自动触发。这是整个 Godot C# ECS 项目的导航地图。
---

# 项目导航地图 - Godot 复刻土豆兄弟

**架构**：Godot 伪 ECS（Scene Tree 处理渲染/物理 + 组件化逻辑 + 数据驱动状态）
**技术栈**：Godot 4.6 / C# / .NET 8.0

---

## 模块速查：我需要什么，去哪找

### Entity（实体生命周期）
- **接口定义** → `Src/ECS/Entity/IEntity.cs`
- **标准模板** → `Src/ECS/Entity/TemplateEntity.cs` ← 新建 Entity 从这里复制
- **生命周期管理 API** → `Src/ECS/Entity/Core/EntityManager.md`
- **核心实现** → `Src/ECS/Entity/Core/EntityManager.cs`
- **生成参数角度语义** → `EntitySpawnConfig.Rotation` 对外使用度，内部写入 `GlobalRotationDegrees`
- **关系管理** → `Src/ECS/Entity/Core/EntityRelationshipManager.cs`
- **开发规范** → `Src/ECS/Entity/Entity规范.md`
- **架构设计** → `Docs/框架/ECS/Entity/Entity架构设计理念.md`

### Component（组件）
- **接口定义** → `Src/ECS/Component/IComponent.cs`
- **标准模板** → `Src/ECS/Component/TemplateComponent.cs` ← 新建 Component 从这里复制
- **开发规范** → `Src/ECS/Component/Component规范.md`
- **设计理念** → `Docs/框架/ECS/Component/Component数据驱动设计理念.md`
- **现有组件目录**：
  - 通用单位组件 → `Src/ECS/Component/Unit/Common/`（HealthComponent、AttackComponent、UnitAnimationComponent 等）
  - 技能组件 → `Src/ECS/Component/Ability/`（CooldownComponent、ChargeComponent、TriggerComponent 等）
  - 玩家组件 → `Src/ECS/Component/Player/`

### Data（数据容器）
- **核心容器** → `Src/ECS/Data/Data.cs`
- **使用指南** → `Src/ECS/Data/README.md`
- **Data 顶层分工** → `Data/README.md`
- **Config / Resource 映射** → `Data/Data/README.md`
- **DataKey 定义规范** → `Data/DataKey/README.md`
- **DataKey 定义目录** → `Data/DataKey/`
  - 基础键 → `Data/DataKey/Base/`
  - 单位属性 → `Data/DataKey/Unit/`
  - 技能数据 → `Data/DataKey/Ability/`
  - 属性系统 → `Data/DataKey/Attribute/`
  - AI 数据 → `Data/DataKey/AI/`
- **架构设计** → `Docs/框架/ECS/Data/DataSystem_Design.md`
- **Skill 分工**：运行时容器问题看 `@ecs-data`，Data 目录配置与映射问题看 `@data-authoring`

### EventBus（事件系统）
- **核心引擎** → `Src/ECS/Event/EventBus.cs`
- **全局总线** → `Src/ECS/Event/GlobalEventBus.cs`
- **事件上下文** → `Src/ECS/Event/EventContext.cs`
- **最佳实践** → `Src/ECS/Event/README_EventBus.md`
- **事件类型定义目录** → `Data/EventType/`
  - 技能事件 → `Data/EventType/Ability/GameEventType_Ability.cs`
  - 单位事件 → `Data/EventType/Unit/`
  - 攻击事件 → `Data/EventType/Unit/Attack/GameEventType_Attack.cs`
  - 瞄准事件 → `Data/EventType/Unit/Targeting/GameEventType_Targeting.cs`

### AbilitySystem（技能系统）
- **核心系统** → `Src/ECS/System/AbilitySystem/AbilitySystem.cs`
- **技能 CRUD** → `Src/ECS/System/AbilitySystem/EntityManager_Ability.cs`
- **模块说明** → `Src/ECS/System/AbilitySystem/README.md`
- **技能实体** → `Src/ECS/Entity/Ability/AbilityEntity.cs`
- **施法上下文** → `Data/EventType/Ability/CastContext.cs`
- **枚举定义** → `Data/DataKey/Ability/AbilityEnums.cs`
- **架构设计（唯一概念文档）** → `Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- **内置技能组件**：
  - 触发 → `Src/ECS/Component/Ability/TriggerComponent/`
  - 冷却 → `Src/ECS/Component/Ability/CooldownComponent/`
  - 充能 → `Src/ECS/Component/Ability/ChargeComponent/`
  - 消耗 → `Src/ECS/Component/Ability/CostComponent/`
  - 目标选择 → `Src/ECS/Component/Ability/AbilityTargetSelectionComponent/`

### EffectSystem（特效系统）
- **核心工具** → `Src/ECS/System/EffectSystem/EffectTool.cs`（统一入口 `EffectTool.Spawn`，Host? 可选参数区分独立/附着；不走 `EntityManager.Spawn`，而是内部执行 Effect 专用 Spawn 编排；`EffectSpawnOptions.Rotation` 对外使用度）
- **特效实体** → `Src/ECS/Entity/Effect/EffectEntity.cs`（Node2D + IEntity + IPoolable）
- **特效组件** → `Src/ECS/Component/Effect/EffectComponent/EffectComponent.cs`（完整特效管理：附着跟随、MaxLifeTime 计时器、直接控制 AnimatedSprite2D 播放、宿主销毁监听）
- **参数入口** → `EffectTool.Spawn(position, new EffectSpawnOptions(visualScene, Host: host))`
- **宿主销毁监听** → EffectComponent 监听 `GameEventType.Global.EntityDestroyed`（通用 Entity 销毁事件）
- **特效 DataKey** → `Data/DataKey/Effect/DataKey_Effect.cs`
- **设计提示词** → `Docs/框架/ECS/Entity/特效系统_Entity生成提示词.md`

### DamageSystem（伤害系统）
- **核心服务** → `Src/ECS/System/DamageSystem/DamageService.cs`
- **伤害信息** → `Src/ECS/System/DamageSystem/DamageInfo.cs`
- **处理器接口** → `Src/ECS/System/DamageSystem/IDamageProcessor.cs`
- **扩展指南** → `Src/ECS/System/DamageSystem/README.md`
- **内置处理器目录** → `Src/ECS/System/DamageSystem/Processors/`
- **设计理念** → `Docs/框架/ECS/System/伤害系统设计理念.md`

### UI System（UI 系统）
- **核心基类** → `Src/UI/Core/UIBase.cs`
- **管理器** → `Src/UI/Core/UIManager.cs`
- **开发指南** → `Src/UI/README.md`
- **架构设计** → `Docs/框架/UI/UI架构设计理念.md`
- **现有 UI 组件**：
  - 血条 → `Src/UI/UI/HealthBarUI/HealthBarUI.cs`
  - 伤害数字 → `Src/UI/UI/DamageNumberUI/DamageNumberUI.cs`
  - 技能栏 → `Src/UI/UI/SkillUI/ActiveSkillBarUI.cs`
  - 技能槽 → `Src/UI/UI/SkillUI/ActiveSkillSlotUI.cs`

### AI System（AI 系统）
- **运动策略调度器** → `Src/ECS/Component/Movement/EntityMovementComponent.cs`（统一 Node2D/CharacterBody2D；含 PlayerInput/AIControlled）
- **角度语义约定** → `MovementParams` 对外角度输入统一使用“度”（`Angle / Orbit* / WavePhase`），策略内部仅在三角函数/旋转计算时转弧度
- **运动通用工具** → `Src/ECS/System/Movement/MovementHelper.cs`（跨策略通用能力）
- **Orbit 专用工具（partial）** → `Src/ECS/System/Movement/Strategies/Orbit/MovementHelper.Orbit.cs`
- **行为树运行器** → `Src/AI/Core/BehaviorTreeRunner.cs`
- **节点基类** → `Src/AI/Core/BehaviorNode.cs`
- **运行时上下文** → `Src/AI/Core/AIContext.cs`
- **敌人行为树** → `Src/AI/Nodes/EnemyBehaviorTreeBuilder.cs`
- **AI DataKey** → `Data/DataKey/AI/DataKey_AI.cs`
- **架构说明** → `Docs/框架/ECS/System/AI/AI系统说明.md`
- **源码说明** → `Src/AI/README.md`

### Tools（工具类）
- **TimerManager** → `Src/Tools/Timer/TimerManager.cs` | 文档 → `Src/Tools/Timer/TimerManager.md`
- **ObjectPool** → `Src/Tools/ObjectPool/ObjectPool.cs` | 文档 → `Src/Tools/ObjectPool/ObjectPool.md`
- **TargetSelector** → `Src/Tools/TargetSelector/TargetSelector.cs` | 文档 → `Src/Tools/TargetSelector/README.md`
- **ResourceManagement** → `Data/ResourceManagement/ResourceManagement.cs` | 文档 → `Data/ResourceManagement/ResourceManagement.md`
- **Log** → `Src/Tools/Logger/Log.cs`
- **InputManager** → `Src/Tools/Input/InputManager.cs`
- **ObjectPoolInit（初始化配置）** → 搜索 `ObjectPoolInit.cs`

### 数据与配置
- **ResourceManagement（资源加载）** → `Data/ResourceManagement/ResourceManagement.cs`
- **ResourcePaths（自动生成路径索引）** → `Data/ResourceManagement/ResourcePaths.cs`
- **DataForge 插件（可视化数据编辑）** → `addons/DataForge/`
- **AutoLoad（全局单例管理）** → `Src/Autoload/AutoLoad.cs`

---

## 如何使用这份地图

1. **新建某类型文件** → 找对应的"标准模板"文件复制
2. **查 API 用法** → 找对应的 `.md` 文档
3. **理解设计决策** → 找 `Docs/框架/` 下的设计理念文档
4. **找事件类型定义** → `Data/EventType/` 目录
5. **找 DataKey / DataMeta 定义** → `Data/DataKey/` 目录
6. **找现有实现参考** → 对应模块的 `Src/` 目录

---

## 完整文档索引

详细架构文档：`Docs/框架/项目索引.md`（人类可读的完整导航）

