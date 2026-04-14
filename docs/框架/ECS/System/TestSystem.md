# TestSystem 运行时测试系统

## 1. 系统定位

`TestSystem` 是一套仅面向开发调试阶段的运行时测试框架，不是正式游戏 UI，也不是临时作弊面板。

它的目标是：

- 为开发者提供统一的运行时测试入口
- 允许在游戏运行中选择目标实体并观察状态
- 以低侵入方式调试 Data / Ability / Feature / 其他系统
- 支撑后续持续增加测试模块，而不把宿主越写越乱

当前代码入口：

- `Src/ECS/Base/System/MouseSelection/README.md`
- `Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.cs`
- `Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.Interaction.cs`
- `Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.Picking.cs`
- `Src/ECS/Base/System/MouseSelection/MouseSelectionSystem.SelectionBoxUi.cs`
- `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- `Src/ECS/Base/System/TestSystem/TestModuleBase.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModule.cs`
- `Src/ECS/Base/System/TestSystem/Core/ITestModuleContext.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleDefinition.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestSelectionContext.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestRefreshScheduler.cs`
- `Src/ECS/Base/System/TestSystem/Core/TestModuleContext.cs`
- `Src/ECS/Base/System/TestSystem/Attribute/AttributeTestModule.cs`
- `Src/ECS/Base/System/TestSystem/Ability/AbilityTestModule.cs`
- `Src/ECS/Base/System/TestSystem/FeatureDebugService.cs`

---

## 2. 设计目标

`TestSystem` 的目标不是“先把面板做出来”，而是形成一套长期可维护的测试基础设施。

### 2.1 必须满足的目标

- **宿主稳定**：实体选择、模块切换、激活态管理必须统一收口
- **通用选择复用**：鼠标点选/框选实体能力要能被多个调试系统复用，而不是绑死在 `TestSystem`
- **模块解耦**：新增或删除测试模块时，不应频繁修改宿主核心代码
- **增量刷新**：数据变化后只更新受影响的 UI，不允许默认整页重建
- **事件驱动**：订阅来源必须清晰，激活时订阅，失活时解除
- **复用正式链路**：调试动作必须尽量复用 `EntityManager`、`FeatureSystem`、`AbilitySystem` 等正式运行时路径
- **可扩展目录结构**：后续新增 Buff / AI / Movement / Damage / EventTrace 等模块时，目录不会失控

### 2.2 反目标

以下做法不符合 `TestSystem` 目标：

- 把测试逻辑直接写死在 `TestSystem.cs`
- 任意数据变化都触发整页 `Refresh()`
- 模块隐藏后仍继续订阅和空转
- 为了“看起来模块化”把极薄的原生控件过度拆成大量零碎场景
- 新增测试模块必须改宿主注册逻辑或复制粘贴大段样板代码

---

## 3. 当前实现评估

## 3.1 做对了的部分

- `TestSystem` 作为统一宿主，由 `AutoLoad` 接入，方向是对的
- `TestModuleBase` 已经抽出了基础生命周期，方向是对的
- `FeatureDebugService` 把调试动作转发到正式运行时链路，方向是对的
- `AbilityTestService` 把 UI 与技能目录/业务操作做了初步隔离，方向也是对的

也就是说，当前问题不是“完全推翻重来”，而是**架构概念有雏形，但实现细节不成熟**。

## 3.2 当前主要问题

### 3.2.1 刷新模型错误

当前属性模块的默认策略是：

- 订阅选中实体的全部 `PropertyChanged`
- 任意键变化都直接 `Refresh()`
- `Refresh()` 先清空右侧所有行，再重新实例化全部编辑控件

这不是增量刷新，而是高频整页重建。  
对运行时工具来说，这是第一优先级要修掉的问题。

### 3.2.2 模块激活态不够严格

当前宿主与模块之间虽然有 `OnActivated / OnDeactivated`，但激活态管理不彻底：

- 非当前模块仍可能收到实体切换广播
- 面板整体隐藏后，模块未必真正停止响应
- `Visible` 被当作刷新门禁，但这不等于真正的运行态门禁

正确做法应该是：**以“当前激活模块 + 是否在树上可见 + 是否允许更新”为准**，而不是简单依赖 `Visible`。

### 3.2.3 UI 拆分粒度缺少统一标准

当前很多小 UI 都被拆成单独 `.tscn`，这不是绝对错误，但粒度需要收敛：

- 复合条目、卡片、区块，适合独立成场景
- 仅仅是一层很薄的 `SpinBox` / `LineEdit` 包装，不一定值得拆独立场景

如果拆分原则不统一，后面模块多了之后会出现：

- 文件数量膨胀
- 节点查找路径越来越碎
- 小场景之间命名和职责不一致
- 实例化开销和维护成本同时上升

### 3.2.4 宿主、模块、共享控件的边界还不够清晰

当前 `TestSystem` 已经有模块化趋势，但目录仍偏“实现拼接式”，还没有完全形成：

- 宿主核心
- 模块实现
- 共享复合控件
- 共享编辑器
- 调试服务

这 5 类边界。

---

## 4. 目标架构

## 4.1 分层结构

建议把 `TestSystem` 固化为以下 4 层：

| 层级 | 责任 | 典型内容 |
|------|------|----------|
| **Core** | 统一宿主、模块协议、模块定义、模块生命周期、选择上下文、刷新调度 | `TestSystem`、`ITestModule`、`ITestModuleContext`、`TestModuleDefinition`、`TestModuleBase`、`TestModuleContext`、`TestSelectionContext`、`TestRefreshScheduler` |
| **Modules** | 每个测试模块自己的状态、订阅、渲染与操作 | `Attribute`、`Ability`、未来的 `Buff` / `AI` / `Damage` 等 |
| **Shared UI** | 跨模块可复用的复合控件，不承载具体业务 | `SectionPanel`、`EmptyStateView`、`LabeledValueRow` |
| **Services** | 复用正式系统的调试适配层，不直接渲染 UI | `FeatureDebugService`、后续的 `TestSelectionService` 等 |

## 4.2 核心原则

### 4.2.1 宿主只做宿主的事

`TestSystem` 只负责：

- 当前选中对象
- 当前激活模块
- 面板显隐
- 模块注册与切换
- 统一调度刷新
- 按 `TestModuleDefinition` 维护稳定模块 Id 和排序
- 监听通用鼠标选择系统的全局结果事件，并在“选择实体”开关开启时消费 `PrimaryEntity / Entities`

`TestSystem` 不应该负责：

- 具体业务测试逻辑
- 模块内部控件创建
- 某个模块的数据过滤规则
- 鼠标拾取、物理查询和输入消费细节

通用鼠标选择系统的边界：

- `MouseSelectionSystem` 是独立 AutoLoad 系统，对外只暴露全局事件协议
- 输入入口使用 `_UnhandledInput`，确保 GUI 控件优先处理点击；不要用碰撞层解决 UI 点击拦截
- 选择事件支持单击和框选；`MouseSelectionSystem` 主动广播 `PreviewUpdated / Completed / Missed`，调用方不再通过请求占用普通选择入口
- 结果统一通过实体集合表达，调用方再决定替换、追加或切换选择
- 通用层只做宽松物理拾取与生命周期过滤，正式玩法监听方应自行按 `EntityType / Team / 当前输入状态` 做语义过滤
- `TestSystem` 只在调试面板可见且“选择实体”开关开启时消费结果，不拥有 MouseSelection 模式

### 4.2.2 模块只做本模块的事

每个模块只负责：

- 它关心哪些数据或事件
- 它如何构建自己的 UI
- 它如何把变化映射成局部更新

模块不应该：

- 越过宿主管理别的模块
- 自己决定全局实体选择逻辑
- 直接承担正式系统的业务生命周期实现

模块接入时必须给出稳定的 `TestModuleDefinition`：

- `Id` 是宿主切换、日志定位、后续模块状态持久化的稳定键
- `DisplayName` 只负责显示
- `SortOrder` 决定模块顺序，宿主不再依赖场景挂载顺序

### 4.2.3 刷新必须分层

推荐把刷新拆成 3 级：

- **Build**：首次构建 UI
- **Patch**：已有 UI 局部更新
- **Rebuild**：仅在结构变化时重建

`Rebuild` 只能用于真正的结构变化，例如：

- 分类切换
- 当前选中实体变化
- 技能列表增删导致条目数量变化

普通数值变化只允许走 `Patch`。

---

## 5. 生命周期规范

建议把模块生命周期明确成以下阶段：

| 阶段 | 责任 |
|------|------|
| `Initialize` | 缓存节点、接收 `TestModuleContext`、构建静态骨架、准备本地状态 |
| `OnSelectedEntityChanged` | 切换监听目标，标记需要同步 |
| `OnActivated` | 开始订阅、恢复刷新 |
| `OnDeactivated` | 解除订阅、停止刷新 |
| `OnSuspended` | 宿主面板隐藏时停止后台空转 |
| `OnResumed` | 宿主面板恢复显示时重新进入前台 |
| `BuildView` | 首次构建结构化 UI |
| `PatchView` | 局部更新已有 UI |
| `DisposeView` | 清理动态节点和临时状态 |

### 5.3 模块协议

当前宿主层已经收口为：

- `ITestModule`
- `ITestModuleContext`
- `TestModuleDefinition`

作用分别是：

- `ITestModule`：宿主只依赖模块协议，不依赖具体模块实现
- `ITestModuleContext`：模块初始化时只拿统一上下文，不直接耦合宿主内部字段
- `TestModuleDefinition`：模块提供稳定 `Id / DisplayName / SortOrder`

### 5.1 激活态规则

- 只有当前激活模块允许响应运行时事件
- 面板隐藏时，当前模块应进入休眠态
- 非激活模块不应保持高频订阅
- 不能用单一 `Visible` 替代完整激活态

### 5.2 刷新调度规则

统一建议：

- 模块接收事件后不立刻重建 UI
- 先记录“哪些键脏了”或“哪块结构脏了”
- 由调度器在同一帧末合并刷新一次

这套规则对于属性模块尤其重要，否则多目标命中时很容易被 `PropertyChanged` 打爆。

---

## 6. UI 拆分原则

## 6.1 什么应该拆成独立场景

满足以下任意两项，通常适合独立成 `.tscn`：

- 有稳定复用价值
- 有独立交互状态
- 有明确语义边界
- 有固定布局和样式
- 未来可能单独替换

适合独立场景的例子：

- `AttributeEditorRow`
- `AbilityCatalogItemControl`
- `AbilityOwnedItemControl`
- `AbilityGroupSection`

## 6.2 什么不应过度拆场景

以下情况通常不值得单独拆 `.tscn`：

- 只是极薄的一层原生控件包装
- 只在一个地方使用一次
- 没有独立行为，只有极轻布局

例如纯 `SpinBox` / `LineEdit` / `CheckButton` 包装，如果只是改几个属性，更适合：

- 直接在代码中创建
- 或统一放进一个轻量的编辑器工厂

## 6.3 推荐目录结构

建议把目录收敛为：

```text
Src/ECS/Base/System/TestSystem/
├── Core/
│   ├── TestSystem.cs
│   ├── TestModuleBase.cs
│   ├── TestSelectionContext.cs
│   └── TestRefreshScheduler.cs
├── Modules/
│   ├── Attribute/
│   ├── Ability/
│   ├── Buff/
│   ├── AI/
│   └── Damage/
├── Shared/
│   ├── Components/
│   └── Editors/
└── Services/
    └── FeatureDebugService.cs
```

如果暂时不改代码目录，文档和设计层面也建议按这个边界思考，避免继续把新内容平铺到当前目录。

---

## 7. 模块扩展规范

新增测试模块时，建议遵守以下规范：

### 7.1 结构规范

- 每个模块一个独立目录
- 模块目录内包含脚本、视图、私有小组件
- 只有跨模块复用的复合控件才进入 `Shared`

### 7.2 宿主接入规范

- 模块必须继承 `TestModuleBase`
- 模块通过统一宿主上下文拿到当前实体和刷新能力
- 模块接入不应要求修改宿主核心流程

### 7.3 刷新规范

- 数据变化优先走局部更新
- 结构变化才允许重建
- 同一帧多次变化必须合并
- 模块隐藏或失活时必须停止高频刷新

### 7.4 服务调用规范

- 涉及正式业务链路时，优先通过服务层转发
- UI 不直接承担正式系统逻辑
- 模块内部允许有 Presenter / ViewModel，但不要让 UI 节点直接拼业务流程

---

## 8. 现有模块的调整方向

## 8.1 AttributeTestModule

属性模块是当前最需要重构的部分。

建议方向：

- 从“任意属性变化整页刷新”改为“按 key 增量 patch”
- 缓存当前分类的行控件，不再反复 `QueueFree + Instantiate`
- 分类切换时才做结构重建
- 支持脏 key 合并刷新
- 把“编辑器类型选择”收敛为统一工厂，而不是继续拆更多薄场景

## 8.2 AbilityTestModule

技能模块比属性模块稍好，因为已经有 deferred refresh 思路，但还不够：

- 列表增删时允许重建
- 启用/禁用状态变化优先更新单条
- 共享条目控件可以保留场景化
- 节点查找和 warning 回退逻辑要进一步收敛

## 8.3 宿主 TestSystem

宿主需要从“当前能跑”升级成“长期稳定宿主”：

- 严格管理当前模块激活态
- 面板隐藏时统一停更
- 去掉重复刷新
- 让实体切换广播只通知必要对象

---

## 9. 推荐的长期形态

长期目标下，`TestSystem` 应该具备以下特征：

- 新增模块时主要新增目录和模块文件，而不是改宿主
- UI 不依赖整页重建维持正确性
- 高频运行中的模块仍能稳定工作
- 目录按职责分层，不因模块增多而失控
- 与 ECS 框架的事件驱动、数据驱动风格一致

换句话说，`TestSystem` 应当是项目的“运行时调试平台”，而不是一组临时拼起来的测试页面。

---

## 10. 相关文档

- [TestSystem 重构方案](./TestSystem重构方案.md)
- [`Src/ECS/Base/System/TestSystem/README.md`](../../../Src/ECS/Base/System/TestSystem/README.md)

如果后续对 `TestSystem` 的代码结构、模块接入方式、目录组织策略有改动，应优先同步本文件与重构方案文档，避免设计和实现再次脱节。
