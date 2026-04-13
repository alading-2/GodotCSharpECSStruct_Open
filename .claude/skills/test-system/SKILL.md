---
name: test-system
description: 开发或扩展运行时测试系统时使用。适用于：新增测试模块、接入实体选择、实现属性测试/技能测试、通过 FeatureDebugService 调试能力生命周期、在测试场景中主动指定选中实体。触发关键词：TestSystem、FeatureDebugService、AttributeTestModule、AbilityTestModule、运行时测试、测试模块、临时加成、调试面板。
---

# TestSystem 使用规范

## 系统定位

`TestSystem` 是项目里的运行时调试宿主。

它负责：

- 调试 UI 入口
- 选中实体
- 模块注册与切换
- 刷新当前测试模块

它不负责：

- 正式玩家 UI
- 技能主动执行前台
- 复制一套独立 Feature / Ability 生命周期

涉及真正的能力生命周期时，必须继续复用：

- `EntityManager`
- `FeatureSystem`
- `AbilitySystem`

## 什么时候该用本 Skill

以下场景应优先参考本 Skill：

- 新增一个运行时测试模块
- 修改 `TestSystem` 面板结构、实体选择、模块切换逻辑
- 给属性测试模块添加新的调试控件
- 给技能测试模块增加新的管理动作
- 希望在 TestSystem 中调试 Feature / Ability，但又不能绕开正式运行时链路
- 需要在测试场景启动后自动选中某个实体

如果你要处理的是：

- 通用 Feature 生命周期设计 → 看 `@feature-system`
- 技能执行链路 → 看 `@ability-system`
- Data 运行时读写规则 → 看 `@ecs-data`
- Data 目录配置与映射 → 看 `@data-authoring`

## 当前结构

### 宿主层

- `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- `Src/ECS/Base/System/TestSystem/TestSystem.tscn`
- `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`

### 模块层

- `Src/ECS/Base/System/TestSystem/AttributeTestModule.cs`
- `Src/ECS/Base/System/TestSystem/AttributeTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/AbilityTestModule.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestModule.tscn`

### 服务 / 适配层

- `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestService.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestViewModels.cs`

### 文档

- `Src/ECS/Base/System/TestSystem/README.md`
- `Docs/框架/ECS/System/TestSystem运行时测试系统说明.md`

## 核心原则

### 1. TestSystem 只做宿主

`TestSystem` 应只负责：

- 绑定调试面板场景骨架
- 注册模块
- 维护 `SelectedEntity`
- 处理模块切换与刷新

不要把具体业务测试逻辑继续堆进 `TestSystem.cs`。

### 2. UI 不直接承担系统生命周期

如果模块里出现以下需求：

- 添加 / 移除 Feature
- 启用 / 禁用 Feature
- 运行时构造临时 `FeatureDefinition`

不要把系统调用散落在 UI 控件回调里。

推荐做法：

- UI 模块收集输入
- Service / Adapter 封装业务
- 正式系统 API 执行生命周期

当前对应适配层就是：

- `FeatureDebugService`

### 3. 属性测试走双轨

当前属性测试不是单一路径，而是：

- **状态覆写**：直接 `entity.Data.Set(...)`
- **持续加成**：通过 `FeatureDebugService.ApplyTemporaryModifier(...)` 生成运行时 Feature

只有在以下条件下才应展示“临时加成”：

- `meta.IsNumeric == true`
- `meta.SupportModifiers == true`
- `meta.IsComputed == false`

### 4. 技能测试只做管理，不做执行

技能测试模块当前边界是：

- 添加技能
- 移除技能
- 启用 / 禁用技能

不要在 TestSystem 中新增：

- 主动触发技能按钮
- 技能执行验证器
- 测试版 AbilityExecutor / Registry

## 属性测试模块接入要点

### 分类来源

使用 `DataRegistry.GetCachedMetaByCategory(...)` 收集元数据。

### 编辑项筛选

只保留：

- `bool`
- 数值
- 枚举
- `string`
- `HasOptions`

排除：

- `IsComputed == true`
- 不适合在调试面板直接编辑的复杂类型

### 临时加成

当 `DataMeta` 满足：

- `IsNumeric == true`
- `SupportModifiers == true`
- `IsComputed == false`

应通过：

- `GetTemporaryModifierValue(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

来复用正式 Modifier 链路。

## 技能测试模块接入要点

### 数据来源

候选技能来自：

- `ResourcePaths.Resources[ResourceCategory.DataAbility]`
- `ResourceManagement.Load<AbilityConfig>(...)`

### 分组规则

优先使用：

- `AbilityConfig.FeatureGroupId`

兜底规则：

- 资源路径
- `AbilityType` 推导出的默认分组

### 事件刷新约定

监听：

- `GameEventType.Ability.Added`
- `GameEventType.Ability.Removed`
- `GameEventType.Feature.Enabled`
- `GameEventType.Feature.Disabled`

刷新应使用延迟方式，避免 `Tree` 交互中途重建：

- `RequestRefresh()`
- `CallDeferred(nameof(FlushRefresh))`

## FeatureDebugService 使用要点

当前服务负责：

- `GrantAbility(...)`
- `GrantFeature(...)`
- `RemoveAbility(...)`
- `SetFeatureEnabled(...)`
- `GetTemporaryModifierValue(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

临时 Feature 命名统一使用：

- `TestSystem.Modifier.{dataKey}`

底层必须继续复用：

- `EntityManager.AddAbility(owner, AbilityConfig)`
- `EntityManager.AddAbility(owner, FeatureDefinition)`
- `EntityManager.RemoveAbility(owner, ability)`
- `FeatureSystem.EnableFeature(feature, owner)`
- `FeatureSystem.DisableFeature(feature, owner)`

## 新增模块的标准步骤

### 第一步

新建：

- `public partial class MyTestModule : TestModuleBase`
- `MyTestModule.tscn`

### 第二步

实现：

- `DisplayName`
- `Initialize(...)`
- `Refresh()`
- 必要时实现 `OnSelectedEntityChanged / OnActivated / OnDeactivated`

并把固定 UI 骨架放进 `MyTestModule.tscn`。

### 第三步

在 `TestSystem.cs` / `TestSystem.tscn` 中注册：

- 在 `TestSystem.cs` 增加导出 `PackedScene` 字段
- 在 `TestSystem.tscn` 绑定 `MyTestModule.tscn`
- 在 `_Ready()` 中使用 `InstantiateModule<MyTestModule>(...)` 注册

### 第四步

如果模块要调用正式系统能力：

- 优先新增 Service / Adapter
- 不要把系统调用直接散落进按钮回调

## 禁止事项

- ❌ 不要把 TestSystem 做成正式玩家 UI
- ❌ 不要在 TestSystem 内新增技能主动触发前台
- ❌ 不要绕开 `FeatureSystem` / `EntityManager` 复制能力生命周期
- ❌ 不要直接编辑计算属性
- ❌ 不要让后台模块持续保留事件订阅
- ❌ 不要把复杂业务逻辑继续堆进 `TestSystem.cs`
