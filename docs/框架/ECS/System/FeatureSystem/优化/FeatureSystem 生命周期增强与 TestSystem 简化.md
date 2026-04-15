# FeatureSystem 生命周期增强与 TestSystem 简化

## Context

两个核心问题：

1. **FeatureSystem 不够通用**：`IFeatureHandler.OnActivated` 是 `void` 通知型，无法回传执行结果。需要补充 `OnExecute` 和 `ExecuteResult`，让 Ability/Buff/Item 等子域都能通过同一接口执行效果并返回结果。
2. **TestSystem 太杂**：存在大量重复代码（`ResolveRequiredNode` 5 处、`InstantiateScene` 2 处、`ActionResult` 2 个定义、实体事件订阅模式 2 处），没有充分利用框架的事件/数据驱动能力。

---

## 任务一：FeatureSystem — 添加 `OnExecute` 生命周期阶段

### 核心思路

将 Feature 生命周期从 `Granted → Activated → Ended` 增强为 `Granted → Activated → Execute → Ended`。

`OnActivated` 是通知（“你被激活了”），`OnExecute` 是命令（“执行你的效果并返回结果”）。语义不同，不应混用。

### 改动清单

#### 1. `IFeatureHandler.cs` — 添加 `OnExecute`

```csharp
// 新增可选方法（默认返回 null）
object? OnExecute(FeatureContext context) => null;
```

现有 Handler 无需改动，默认实现返回 `null`。

#### 2. `FeatureContext.cs` — 添加 `ExecuteResult`

```csharp
// 新增属性，替代 ExtraData 字典的约定式通信
public object? ExecuteResult { get; set; }
```

#### 3. `FeatureSystem.cs` — 在 `OnFeatureActivated` 中编排 `OnExecute`

在 `OnActivated` 之后、事件发射之前，调用 `handler.OnExecute(ctx)` 并存入 `ctx.ExecuteResult`。

调用时序：`OnActivated`（通知）→ `OnExecute`（执行+结果）→ 累计次数 → 发射事件。

#### 4. Ability Handler — 直接实现 `IFeatureHandler`

- 不再保留 Ability 专属 FeatureHandler 基类。
- 具体处理器直接实现 `OnExecute(FeatureContext)`。
- 通过 `context.GetActivationData<CastContext>()` 提取施法上下文；AbilitySystem 负责在进入 FeatureSystem 前校验上下文类型。
- 返回 `AbilityExecutedResult`，由 FeatureSystem 写入 `FeatureContext.ExecuteResult`。

#### 5. `AbilitySystem.cs` — 直接读取 `ExecuteResult`

将 `EmitAbilityExecutedEvent` 中的 `featureCtx.ExtraData.TryGetValue(...)` 替换为 `featureCtx.ExecuteResult is AbilityExecutedResult`。

### 向后兼容

- **非 Ability Handler**：`OnExecute` 默认返回 `null`，无影响。
- **具体 Ability Handler（Slam/Dash/ArcShot 等）**：直接实现 `IFeatureHandler.OnExecute`，从 `ActivationData` 读取 `CastContext`。
- **FeatureSystem API**：签名不变。

---

## 任务二：TestSystem — 去冗余 + 利用框架能力

### 改动清单

#### 1. 新建 `Core/TestSceneHelper.cs` — 提取共享工具方法

- `ResolveRequiredNode<T>(this Node, uniquePath, fallbackPath, contextName)`：从 5 个控件中提取。
- `InstantiateScene<T>(PackedScene?, sceneName)`：从 2 个模块中提取。

涉及文件：`AbilityCatalogItemControl.cs`、`AbilityOwnedItemControl.cs`、`AbilityGroupSection.cs`、`AttributeEditorRow.cs`、`AttributeModifierEditor.cs`、`AttributeTestModule.cs`、`AbilityTestModule.cs`。删除各自的私有副本，改用扩展方法。

#### 2. 统一 `ActionResult`

新建 `Core/TestTypes.cs`，定义共享 `internal record struct ActionResult(bool Success, string Message)`。

- `FeatureDebugService.cs`：删除嵌套定义，改用共享类型。
- `AbilityTestViewModels.cs`：将 `AbilityActionResult` 替换为共享类型。

#### 3. `TestModuleBase.cs` — 添加实体事件绑定辅助方法

```csharp
protected void BindEntityEvents(IEntity entity, Action<IEntity> subscribe, Action<IEntity> unsubscribe);
protected void UnbindEntityEvents(Action<IEntity> unsubscribe);
```

- `AttributeTestModule.cs`：移除 `_subscribedEntity` 字段，使用基类方法。
- `AbilityTestModule.cs`：同上。

#### 4. 共享 `FeatureDebugService` 实例

- `TestModuleContext.cs`：添加 `FeatureDebugService` 字段。
- `AttributeTestModule.cs`：从 context 获取，不再 `new FeatureDebugService()`。
- `AbilityTestService.cs`：同上。

#### 5. 移除 `FeatureDebugService._temporaryModifierValues` 缓存

缓存可能与实际 Feature 状态脱节，且收益微乎其微。改为每次从 Feature 实体实时读取。

#### 6. 分类列表提取为声明式常量

新建 `Core/TestConstants.cs`，将 `BuildCategoryData` 的 11 个硬编码分类提取为 `static readonly` 数组。不改变功能，只改善可维护性。

---

## 实施顺序

| 阶段 | 内容 | 风险 |
|---|---|---|
| 阶段1 | FeatureSystem 增强（`OnExecute` + `ExecuteResult` + `AbilitySystem` 适配） | 低 — 默认实现，无破坏性 |
| 阶段2 | TestSystem 共享工具（`TestSceneHelper` + `TestTypes`） | 极低 — 纯提取 |
| 阶段3 | TestSystem 架构简化（基类事件绑定 + 共享 `FeatureDebugService` + 移除缓存） | 低 — 功能不变 |

## 验证

1. 编译通过（`dotnet build`）。
2. 所有具体 Ability Handler（Slam/Dash/ArcShot 等）无需改动，行为不变。
3. TestSystem 属性编辑器和技能面板功能不变。
4. 运行 TestSystem 场景验证 UI 交互。
