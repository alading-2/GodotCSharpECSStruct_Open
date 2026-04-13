# TestSystem 运行时测试系统说明

## 一、系统定位

`TestSystem` 是一个**仅面向开发调试阶段**的运行时测试系统。

它的目标不是替代正式 UI，也不是提供作弊面板，而是为开发者提供一套统一、可扩展、低接入成本的运行时调试入口，用于：

- 选择场景中的 `IEntity`
- 查看并修改实体的可编辑运行时数据
- 动态增删和启停实体技能
- 为后续更多测试模块提供统一宿主

当前系统位于：

- `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- `Src/ECS/Base/System/TestSystem/TestSystem.tscn`
- `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- `Src/ECS/Base/System/TestSystem/AttributeTestModule.cs`
- `Src/ECS/Base/System/TestSystem/AttributeTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestService.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestViewModels.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestModule.cs`
- `Src/ECS/Base/System/TestSystem/AbilityTestModule.tscn`
- `Src/ECS/Base/System/TestSystem/README.md`

---

## 二、设计目标

运行时测试系统围绕以下目标设计：

| 目标 | 说明 |
|------|------|
| **低侵入** | 不修改正式业务 UI，不要求实体额外挂测试专用组件 |
| **统一入口** | 由 `TestSystem` 统一承载测试面板、实体选择和模块切换 |
| **模块可扩展** | 新测试能力通过继承 `TestModuleBase` 接入，而不是改动核心流程 |
| **数据驱动** | 属性测试优先依赖 `DataMeta` 元数据，而非手写字段面板 |
| **复用正式链路** | 技能 / Feature 调试统一转发到 `EntityManager`、`FeatureSystem` 等正式运行时 API |
| **运行时同步** | 面板在实体数据或技能变化后可自动刷新，避免显示过期状态 |

---

## 三、整体架构

### 3.1 架构分层

| 层次 | 文件 | 职责 |
|------|------|------|
| **系统宿主** | `TestSystem.cs` + `TestSystem.tscn` | AutoLoad 注册、主界面骨架绑定、模块注册与切换、实体选择 |
| **模块基类** | `TestModuleBase.cs` | 统一生命周期与当前选中实体注入 |
| **属性模块** | `AttributeTestModule.cs` + `AttributeTestModule.tscn` | 按 `DataCategory` 展示并编辑实体的可编辑 `Data`，并为支持 Modifier 的属性提供临时加成入口 |
| **调试适配层** | `FeatureDebugService.cs` | 把调试动作转发到正式 `EntityManager` / `FeatureSystem` 链路 |
| **技能服务** | `AbilityTestService.cs` | 缓存技能目录、解析分组、构建视图模型、封装技能增删启停 |
| **技能视图模型** | `AbilityTestViewModels.cs` | 纯展示数据结构，承载分类分组与技能条目信息 |
| **技能模块** | `AbilityTestModule.cs` + `AbilityTestModule.tscn` | 渲染双树界面、处理点击 / 右键菜单，并把操作转发给技能服务 |

### 3.2 核心关系

```text
TestSystem
  ├── 负责 AutoLoad 初始化
  ├── 负责绑定 TestSystem.tscn 主界面骨架
  ├── 负责鼠标点击选择 IEntity
  ├── 负责注册/切换 TestModule
  │
  ├── AttributeTestModule
  │     ├── 基于 AttributeTestModule.tscn 提供固定布局
  │     ├── 基于 DataRegistry + DataMeta 生成属性编辑器
  │     └── 对支持 Modifier 的数值属性通过 FeatureDebugService 管理临时加成
  │
  ├── FeatureDebugService
  │     ├── GrantAbility / RemoveAbility
  │     ├── SetFeatureEnabled
  │     ├── GrantFeature
  │     └── ApplyTemporaryModifier / ClearTemporaryModifier
  │
  ├── AbilityTestService
  │     ├── 扫描 DataAbility 目录
  │     ├── 解析 FeatureGroupId / 资源路径兜底分组
  │     ├── 构建左侧技能库 / 右侧当前技能视图模型
  │     └── 通过 FeatureDebugService 封装 Add / Remove / Enable / Disable
  │
  └── AbilityTestModule
  │     ├── 基于 AbilityTestModule.tscn 提供固定布局
  │     └── 基于双 Tree + PopupMenu 展示并触发技能管理操作
```

`TestSystem` 只负责调试宿主、实体选择和模块切换；真正的能力生命周期仍由正式 `EntityManager`、`FeatureSystem`、`AbilitySystem` 负责。

### 3.3 为什么采用模块化设计

如果把所有测试能力都堆到一个脚本中，后续很快会出现以下问题：

- 面板逻辑与具体业务测试逻辑耦合
- 新增一个测试页就要修改宿主系统内部大量代码
- 不同测试能力的刷新逻辑、订阅逻辑相互干扰

因此这里采用：

- `TestSystem` 只负责**宿主职责**
- 具体测试内容全部下沉到 `TestModuleBase` 子类

这使得系统天然适合继续扩展出：

- Buff 测试模块
- AI 测试模块
- Movement 测试模块
- Damage 测试模块
- Targeting 测试模块

---

## 四、TestSystem 核心机制

## 4.1 AutoLoad 注册

`TestSystem` 通过 `[ModuleInitializer]` + `AutoLoad.Register(...)` 自动接入启动流程。

核心意义：

- 不依赖主场景手动拖节点
- 调试系统可随游戏启动自动挂载
- 可以通过 `AutoLoad.Priority.Debug` 归入调试层级

创建时机上，`AutoLoad.Register(...)` 会：

- 通过 `ResourceManagement.Load<PackedScene>(nameof(TestSystem), ResourceCategory.System)` 加载宿主场景
- 使用 `AutoLoad.Priority.Debug` 把它挂到调试层级
- 通过 `ParentPath = "Debug"` 自动挂到 `Debug` 节点下

这意味着 `TestSystem` 现在是**场景骨架 + C# 绑定**的调试面板：固定结构放在 `.tscn`，动态模块与运行时数据刷新保留在代码中。

## 4.2 单例与生命周期

`TestSystem` 继承 `CanvasLayer`，在 `_EnterTree()` 中完成：

- 单例去重
- `Instance = this`
- `Layer = 100`
- `ProcessMode = Always`

这保证了：

- 调试 UI 永远显示在普通场景 UI 之上
- 在暂停或特殊流程下仍可工作
- 重复初始化时不会出现多个面板实例

## 4.3 UI 结构

`_Ready()` 中会先缓存 `TestSystem.tscn` 中的关键节点，再实例化两个模块场景：

- `AttributeTestModule`
- `AbilityTestModule`

其中：

- `TestSystem.tscn` 承载顶部工具栏、信息栏和模块宿主容器
- `AttributeTestModule.tscn` 提供左侧分类列表和右侧滚动编辑区
- `AbilityTestModule.tscn` 提供左右双树、状态栏和右键菜单节点
- `*.cs` 仍负责动态属性行、Tree 节点重建和事件转发

当前 UI 由以下部分组成：

| 区域 | 作用 |
|------|------|
| **测试按钮/面板显隐** | 控制整个调试面板开关 |
| **模块下拉框** | 在属性测试、技能测试等模块之间切换 |
| **实体显示区** | 展示当前选中的实体名称与标识 |
| **选择开关** | 控制是否允许鼠标点击拾取实体 |
| **清除选择按钮** | 清空当前选中实体 |
| **刷新按钮** | 手动刷新当前模块视图 |
| **模块宿主容器** | 当前模块 UI 的挂载区域 |

在全局测试场景中，如果希望技能测试页打开后就能直接管理玩家技能，测试代码应在生成玩家后主动调用 `TestSystem.Instance.SetSelectedEntity(player)`。
否则技能面板会处于“未选中实体”状态，左侧添加动作只能给出提示，不能真正执行。

## 4.4 实体选择

当面板可见且“选择实体”开关打开时，`TestSystem._UnhandledInput()` 会处理鼠标左键点击。

流程如下：

```text
左键点击屏幕
  → TestSystem._UnhandledInput()
  → FindEntityAtScreenPosition(mousePosition)
  → 优先使用 2D 物理点查询获取命中对象
  → 尝试向上解析为 IEntity
  → 未命中时再按距离兜底找最近的 Node2D 实体
  → SetSelectedEntity(entity)
  → 广播给所有测试模块
  → 当前模块刷新显示
```

这里的关键点是：

- 选择目标不是直接遍历场景树，而是优先走**物理点击拾取**
- 纯视觉或无碰撞调试对象还能走最近距离兜底选择
- 真正被选中的对象必须能解析为 `IEntity`
- `TestSystem` 只维护一个当前选中实体：`SelectedEntity`

这种设计适合调试运行中真实参与物理和碰撞的单位。

## 4.5 模块切换

`TestSystem` 内部维护：

- `_modules: List<TestModuleBase>`
- `_modulesByIndex: Dictionary<int, TestModuleBase>`

模块注册后：

- 加入宿主容器
- 注入 `TestSystem` 引用
- 按 `DisplayName` 加入下拉菜单

切换模块时会执行：

- 旧模块 `OnDeactivated()`
- 新模块 `OnActivated()`
- 刷新新模块内容

这样每个模块都能安全管理自己的订阅和临时状态。

---

## 五、TestModuleBase 模块基类

`TestModuleBase` 是所有测试模块的统一基类。

它提供两类能力：

### 5.1 公共上下文

- `testSystem`：当前宿主系统引用
- `selectedEntity`：当前被选中的实体

### 5.2 生命周期钩子

| 方法 | 作用 |
|------|------|
| `Initialize(TestSystem system)` | 初始化模块，注入宿主，设置基础布局属性 |
| `OnSelectedEntityChanged(IEntity? entity)` | 当前选中实体改变时调用 |
| `OnActivated()` | 模块被切到前台时调用 |
| `OnDeactivated()` | 模块被切走时调用 |
| `Refresh()` | 刷新模块界面 |

设计上，`TestModuleBase` 不关心具体业务，只负责提供统一的模块协作协议。

---

## 六、AttributeTestModule 属性测试模块

## 6.1 模块定位

`AttributeTestModule` 用于调试选中实体的运行时属性，但当前已经形成**双轨调试模式**：

- **状态覆写**：直接调用 `entity.Data.Set(...)` 修改运行时值
- **临时加成**：对支持 Modifier 的数值属性，通过运行时 `FeatureDefinition` 挂载临时 Feature

其核心思想是：

- 不是手写“攻击力输入框”“血量输入框”
- 而是通过 `DataRegistry.GetCachedMetaByCategory(category)` 获取元数据
- 再根据 `DataMeta` 的类型与约束动态生成编辑控件

这使属性面板具备很强的适应能力：只要 `DataKey` 元数据定义正确，面板就能自动跟进。

## 6.2 分类来源

当前模块按以下分类组织：

- `DataCategory_Attribute.Health`
- `DataCategory_Attribute.Mana`
- `DataCategory_Attribute.Attack`
- `DataCategory_Attribute.Defense`
- `DataCategory_Attribute.Skill`
- `DataCategory_Attribute.Movement`
- `DataCategory_Attribute.Dodge`
- `DataCategory_Attribute.Crit`
- `DataCategory_Attribute.Resource`
- `DataCategory_Unit.State`
- `DataCategory_Unit.Recovery`

这样做的好处是：

- 面板结构与项目 Data 分类保持一致
- 方便开发者快速定位对应运行时字段
- 新增 DataKey 后，只要归类正确，通常无需大改 UI

## 6.3 可编辑项筛选规则

模块会过滤掉不适合直接编辑的字段，重点规则如下：

- `meta.IsComputed == true` 的计算属性不允许编辑
- 仅保留可布尔、数值、枚举、字符串或具备选项集的字段

这保证：

- 不会直接覆盖框架中的派生值
- 不会把只读计算链打断
- 面板聚焦于真实可操作的基础状态

## 6.4 控件生成方式

根据 `DataMeta` 元数据，模块会动态生成不同控件：

| 元数据类型 | 控件方向 |
|------|------|
| `bool` | `CheckButton` |
| 数值型 | `SpinBox` |
| 枚举 / Options | `OptionButton` |
| `string` | `LineEdit` |

并结合：

- `MinValue`
- `MaxValue`
- `DisplayName`
- `Options`

来生成更合理的编辑体验。

## 6.5 临时加成能力

当一个 `DataMeta` 同时满足以下条件时，属性行下方会额外出现“临时加成”控件：

- `meta.IsNumeric == true`
- `meta.SupportModifiers == true`
- `meta.IsComputed == false`

此时模块会通过 `FeatureDebugService` 提供三项能力：

- `GetTemporaryModifierValue(...)`：读取当前挂载的临时加成值
- `ApplyTemporaryModifier(...)`：用运行时 `FeatureDefinition` 给目标属性追加 `Additive` Modifier
- `ClearTemporaryModifier(...)`：移除当前临时 Feature

当当前临时 Feature 的命名规则为：

- `TestSystem.Modifier.{dataKey}`

这意味着 TestSystem 的属性调试已经不再是“所有东西都硬改 Data”，而是形成了：

- **直接状态改写**：适合瞬时验证、临时覆写
- **Feature 持续加成**：适合验证正式 Modifier / FeatureSystem 链路

## 6.6 特殊保护逻辑

属性测试模块额外做了两类安全保护：

### 当前生命值保护

当修改：

- `DataKey.CurrentHp`
- `DataKey.BaseHp`
- `DataKey.HpBonus`

时，会确保：

- `CurrentHp` 被限制在 `0 ~ FinalHp`

### 当前魔法值保护

当修改：

- `DataKey.CurrentMana`
- `DataKey.BaseMana`
- `DataKey.ManaBonus`

时，会确保：

- `CurrentMana` 被限制在 `0 ~ FinalMana`

这能避免测试面板把实体推入明显非法的资源状态。

## 6.7 实时刷新机制

模块会订阅当前实体的 `GameEventType.Data.PropertyChanged` 事件。

因此：

- 当外部系统修改该实体的 Data 时
- 当前模块可以自动刷新显示
- 不需要手动重开面板才能看到最新值

同时，在实体切换或模块切换时会主动取消旧订阅，避免事件泄漏。

---

## 七、AbilityTestModule 技能测试模块

## 7.1 模块定位

`AbilityTestModule` 用于在运行时直接管理实体技能，覆盖三个核心动作：

- 添加技能
- 移除技能
- 启用 / 禁用技能

它不是技能执行器，也不负责技能触发流程；它只是一个**运行时管理面板**。

## 7.2 数据来源

左侧“可用技能配置”列表来自：

- `ResourcePaths.Resources[ResourceCategory.DataAbility]`
- `ResourceManagement.Load<AbilityConfig>(...)`

`AbilityTestService` 会遍历所有 `DataAbility` 配置，并优先读取：

- `AbilityConfig.Name`
- `AbilityConfig.FeatureGroupId`
- `AbilityConfig.Description`
- `AbilityConfig.AbilityType`
- `AbilityConfig.AbilityTriggerMode`

如果旧资源尚未补齐 `FeatureGroupId`，则退回资源路径或 `AbilityType` 兜底分类。

这样做的意义是：

- 面板自动覆盖当前项目中所有已注册技能配置
- 不需要手写候选技能表
- 新增技能资源后，测试模块通常可自动感知

## 7.3 双树结构与职责拆分

当前实现不再把“资源扫描、分类、业务操作、UI 控件”堆在一个脚本里，而是拆为两层：

| 层次 | 作用 |
|------|------|
| `AbilityTestService` | 负责技能目录缓存、分类排序、条目视图构建、增删启停 |
| `AbilityTestModule` | 负责左右双 `Tree` 渲染、状态文本、右键 `PopupMenu`、事件订阅 |

界面分为左右两棵树：

| 区域 | 作用 |
|------|------|
| **左侧技能库 Tree** | 按分组路径显示项目内所有可添加技能；已拥有技能会灰显 |
| **右侧当前技能 Tree** | 按分组路径显示当前实体已拥有技能；禁用技能会灰显 |
| **右键菜单 PopupMenu** | 作用于右侧技能条目，提供启用 / 禁用 / 移除 |

## 7.4 技能操作方式

### 添加技能

点击左侧叶子节点时：

- `AbilityTestModule` 从树节点元数据拿到 `ResourceKey`
- 左侧树直接监听 `Tree.item_selected`，不再依赖 `GuiInput + GetItemAtPosition()` 的坐标命中
- 即使当前没有选中实体，左侧叶子仍可选中，模块会明确提示“请先选择一个实体”，而不是静默无响应
- `AbilityTestService.AddAbility(...)` 读取缓存配置
- 由 `FeatureDebugService.GrantAbility(...)` 转发到正式 `EntityManager.AddAbility(...)`

该流程遵循项目既有技能生命周期管理，不绕开框架。

### 移除技能

点击右侧叶子节点时：

- `AbilityTestModule` 从树节点元数据拿到技能实例 `Id`
- `AbilityTestService` 先解析出目标 `AbilityEntity`
- 再通过 `FeatureDebugService.RemoveAbility(...)` 转发到正式移除链路，按运行时实例精确移除

### 切换启用

右键右侧叶子节点时：

- 弹出 `PopupMenu`
- 根据当前 `FeatureEnabled` 状态显示“启用技能”或“禁用技能”
- 由 `AbilityTestService` 通过 `FeatureDebugService.SetFeatureEnabled(...)` 调用 `FeatureSystem.EnableFeature(...)` / `DisableFeature(...)`

这适合快速验证：

- 被动技能启停
- 主动技能临时屏蔽
- 技能栏/触发链是否正确响应启用状态

## 7.5 当前技能展示

右侧每个技能条目会展示：

- 启用状态（`启用 / 禁用`）
- 技能名称
- 技能类型 `AbilityType`
- 触发模式 `AbilityTriggerMode`
- 分组路径与描述提示

例如：

```text
[✓] 火球术 (Active)
[✗] 烈焰光环 (Passive)
```

这样在调试时可以快速识别：

- 当前实体到底拥有哪些技能
- 哪些技能只是存在但被禁用
- 这些技能属于主动还是被动类型

## 7.6 实时刷新机制

模块会监听当前选中实体上的：

- `GameEventType.Ability.Added`
- `GameEventType.Ability.Removed`
- `GameEventType.Feature.Enabled`
- `GameEventType.Feature.Disabled`

因此在技能被外部逻辑增删或启停后，左右两棵树都能自动刷新，避免面板与真实状态脱节。

同时，`AbilityTestService` 会把添加 / 移除 / 启用 / 禁用操作写入日志，至少包含：

- 当前宿主实体名
- 技能名
- 技能实例 ID

这样当测试面板出现“点了 A 却删了 B”这类问题时，可以直接从日志定位是 UI 命中错误、实例解析错误，还是生命周期调用错误。

同样地，模块在实体切换或模块失活时会取消订阅，防止重复监听。

---

## 八、典型工作流

## 8.1 调试属性

```text
打开调试面板
  → 切到“属性测试”
  → 开启实体选择
  → 鼠标点击一个单位
  → 左侧选择属性分类
  → 在右侧编辑对应属性
  → 需要直接覆写时使用基础控件
  → 需要验证持续加成时使用“临时加成”控件
```

适合验证：

- 护甲、攻击、暴击率等是否立即生效
- `DataMeta` 的上下限约束是否正确
- 某些状态标记是否能触发对应组件行为
- `SupportModifiers` 属性是否能正确走 FeatureSystem 加成链路

## 8.2 调试技能

```text
打开调试面板
  → 切到“技能测试”
  → 开启实体选择
  → 选择一个实体
  → 从左侧分类树点击添加技能
  → 在右侧分类树点击移除或右键切换启用状态
```

适合验证：

- `EntityManager_Ability` 增删逻辑是否正常
- 主动技能栏是否随技能变更更新
- `FeatureEnabled` 对触发链的影响是否正确
- 技能测试是否只承担管理职责，而不承担执行职责

---

## 九、扩展新测试模块

新增模块的推荐流程如下：

### 第一步：创建模块类

```csharp
public partial class MyTestModule : TestModuleBase
{
    internal override string DisplayName => "我的测试";

    internal override void Initialize(TestSystem system)
    {
        base.Initialize(system);
    }

    internal override void OnSelectedEntityChanged(IEntity? entity)
    {
        base.OnSelectedEntityChanged(entity);
    }

    internal override void Refresh()
    {
    }
}
```

### 第二步：创建模块场景

创建：

- `MyTestModule.tscn`

把固定 UI 骨架放进场景，把脚本挂到根节点。

### 第三步：在 `TestSystem` 中注册

```csharp
RegisterModule(InstantiateModule<MyTestModule>(_myTestModuleScene, nameof(MyTestModule)));
```

同时需要：

- 在 `TestSystem.cs` 增加导出 `PackedScene` 字段
- 在 `TestSystem.tscn` 给该字段绑定 `MyTestModule.tscn`

### 第四步：遵循模块边界

建议遵守以下原则：

- **宿主负责切换，模块负责内容**
- **模块只操作自己负责的系统能力**
- **如有事件订阅，必须在 `OnDeactivated()` 或实体切换时解除**
- **优先复用现有系统 API，不要直接破坏框架封装**

---

## 十、边界与约束

当前 `TestSystem` 有以下明确边界：

| 约束 | 说明 |
|------|------|
| **仅用于调试** | 不应作为正式玩家 UI 使用 |
| **属性测试不改计算属性** | 计算属性应继续由 Data 计算链维护 |
| **技能测试不直接执行技能** | 执行仍应走 `TryTrigger` / `AbilitySystem` |
| **属性调试分双轨** | 状态覆写可直写 `Data`，持续数值加成优先走 `FeatureSystem` |
| **依赖可点击实体** | 鼠标拾取优先基于物理查询；纯视觉对象仅能走最近距离兜底选择 |
| **模块自行管理订阅** | 宿主不统一清理模块内部事件 |

---

## 十一、相关文档与后续可扩展方向

如果你要继续扩展或维护 TestSystem，建议优先阅读：

- `Src/ECS/Base/System/TestSystem/README.md` - 源码目录使用说明
- `Docs/框架/项目索引.md` - 项目级导航入口
- `.windsurf/skills/test-system/SKILL.md` - TestSystem 专用 skill
- `.windsurf/skills/feature-system/SKILL.md` - FeatureSystem 边界与生命周期规范

当前系统已经具备稳定的宿主结构，后续推荐优先扩展：

1. **Buff / Debuff 测试模块**
   - 查看当前修改器
   - 手动添加/移除 Buff
   - 验证 DataModifier 叠加结果

2. **Movement 测试模块**
   - 动态切换 `MoveMode`
   - 注入 `MovementParams`
   - 快速验证轨迹、结束条件和碰撞行为

3. **Damage 测试模块**
   - 指定伤害类型、标签、数值
   - 对选中实体造成伤害 / 治疗
   - 观察 DamageProcessor 链路输出

4. **Targeting / AI 测试模块**
   - 显示 AI 当前目标
   - 手动触发目标选择或施法请求
   - 可视化关键状态

---

## 十二、总结

`TestSystem` 的价值不在于“做了一个调试面板”，而在于它为项目建立了一套**可持续演进的运行时测试基础设施**：

- 它统一了运行时调试入口
- 它把实体选择、模块切换和调试 UI 宿主职责集中管理
- 它通过 `TestModuleBase` 形成稳定的扩展协议
- 它通过 `FeatureDebugService` 保证调试动作继续复用正式运行时链路
- 它通过 `AttributeTestModule` 的双轨调试模式，兼顾状态覆写与正式 Modifier 流程验证
- 它通过 `AbilityTestModule` 和 `AbilityTestService` 验证了“数据驱动 + 模块化 + 服务适配层”的设计可行性

未来只要继续遵守这套结构，就可以把更多系统级调试能力持续沉淀到同一套测试框架中。
