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
- 按 `TestModuleDefinition` 维护稳定模块 Id 和排序

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
- `Src/ECS/Base/System/TestSystem/TestSystem.MouseSelection.cs`
- `Src/ECS/Base/System/TestSystem/TestSystem.tscn`
- `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModule.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModuleContext.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleDefinition.cs`

### 模块层

- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeEditorRow.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeCheckEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeOptionEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeNumericEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTextEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeModifierEditor.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityGroupSection.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityCatalogItem.tscn`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityOwnedItem.tscn`

### 服务 / 适配层

- `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestService.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestViewModels.cs`

### 文档

- `Src/ECS/Base/System/TestSystem/README.md`
- `Docs/框架/ECS/System/TestSystem.md`
- `Docs/框架/ECS/System/TestSystem重构方案.md`

## 核心原则

### 1. TestSystem 只做宿主

`TestSystem` 应只负责：

- 绑定调试面板场景骨架
- 扫描 `ModuleHost` 下的模块场景并注册
- 维护 `SelectedEntity`
- 处理模块切换与刷新
- 监听通用鼠标选择结果事件，并在面板可见且“选择实体”开关开启时消费 `PrimaryEntity / Entities`

不要把具体业务测试逻辑继续堆进 `TestSystem.cs`。

如果是“鼠标点选实体”这类可能被多个调试系统复用的能力：

- 不要继续写在 `TestSystem._UnhandledInput()`
- 应拆成独立 AutoLoad 系统
- `MouseSelectionSystem` 主动监听 `_UnhandledInput` 并广播 `PreviewUpdated / Completed / Missed`
- `TestSystem` 只监听选择完成事件，不发开始/取消请求，也不占用 MouseSelection 模式
- 单击用于选主目标，拖拽用于显示框选预览并返回实体集合
- 正式玩法系统监听同一事件后，应自行按 `EntityType / Team / 当前输入状态` 过滤和处理 `Replace / Add / Toggle`

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
- 条目骨架放到独立 `tscn`，模块只做实例化与数据绑定

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

### 3.1 属性测试刷新规则

属性模块默认应遵守：

- 分类切换、实体切换时才允许整页重建
- 普通属性变化只更新受影响行
- 高频属性变化必须先收集脏键，再在帧末统一 patch
- 不要恢复成“任意 `PropertyChanged` 就整页 `Refresh()`”

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

### UI 场景化约定

属性测试条目不要继续在代码中 `new Label / SpinBox / Button / LineEdit` 拼布局。

推荐拆分为：

- 词条容器场景
- 布尔编辑器场景
- 下拉编辑器场景
- 数值编辑器场景
- 文本编辑器场景
- 临时加成编辑器场景

### 节点查找日志约定

TestSystem UI 控件允许同时兼容：

- `unique-name` 路径
- 普通相对路径

但规则是：

- 回退成功时不要打印 `Warn`
- 只有两条路径都找不到时，才记录一次 `Log.Error` 并抛异常

目标是避免模块初始化时因兼容路径探测产生大批无效告警。

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

刷新应使用延迟方式，避免交互回调中途重建列表：

- `RequestStructureRefresh(...)` / `RequestPatch(...)`
- `RequestScheduledRefresh()`
- `TestRefreshScheduler`

同时必须遵守：

- 只有激活模块才允许持有高频事件订阅
- `OnActivated()` 恢复订阅
- `OnDeactivated()` 解除订阅
- `OnSuspended()` 停止后台空转
- `OnResumed()` 恢复前台运行
- 不要仅靠 `Visible` 作为后台停更门禁

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

- `Definition`
- `Initialize(...)`
- `Refresh()`
- 必要时实现 `OnSelectedEntityChanged / OnActivated / OnDeactivated`

并把固定 UI 骨架放进 `MyTestModule.tscn`。

### 第三步

在 `TestSystem.tscn` 的 `ModuleHost` 下挂载 `MyTestModule.tscn`，由 `TestSystem` 在 `_Ready()` 扫描模块、按 `TestModuleDefinition.SortOrder` 排序后完成注册。

模块必须提供：

- 稳定 `Id`
- 展示用 `DisplayName`
- 排序用 `SortOrder`

### 第四步

如果模块要调用正式系统能力：

- 优先新增 Service / Adapter
- 不要把系统调用直接散落进按钮回调

## DataKey 访问规范

在 TestSystem 内部访问 `Data.Get/Set` 时，**必须使用 `DataKey.XXX.Key` 显式取键名**：

```csharp
// ✅ 正确
ability.Data.Get<string>(DataKey.Name.Key);
entity.Data.Set(DataKey.CurrentHp.Key, value);

// ❌ 错误：依赖 DataMeta 的 implicit operator string
ability.Data.Get<string>(DataKey.Name.Key);
```

原因：规避不同工程上下文下 `DataMeta` 到 `string` 的编译兼容差异。早期代码曾使用反射 `ResolveDataKey` 方法绕过此问题，现已统一改为直接 `.Key` 访问，移除了 `using System.Reflection` 和相关反射方法。

## 日志级别规范

TestSystem UI 控件统一使用以下日志级别：

| 级别    | 用途                                 |
| ------- | ------------------------------------ |
| `Info`  | 用户操作确认（点击添加/移除/切换等） |
| `Warn`  | 节点回退查找、操作前置条件不满足     |
| `Error` | 场景实例化失败、分组渲染异常         |

**不要使用 `LogLevel.Debug`**。运行时测试系统的日志面向开发者调试，Debug 级别在测试面板中属于冗余输出。

## 禁止事项

- ❌ 不要把 TestSystem 做成正式玩家 UI
- ❌ 不要在 TestSystem 内新增技能主动触发前台
- ❌ 不要绕开 `FeatureSystem` / `EntityManager` 复制能力生命周期
- ❌ 不要直接编辑计算属性
- ❌ 不要让后台模块持续保留事件订阅
- ❌ 不要让属性模块回到“任意属性变化整页重建”的写法
- ❌ 不要把复杂业务逻辑继续堆进 `TestSystem.cs`
- ❌ 不要在 `Data.Get/Set` 中直接传 `DataKey` 对象，必须用 `DataKey.XXX.Key`
- ❌ 不要使用 `LogLevel.Debug` 或 `_log.Debug`，仅用 `Info / Warn / Error`
