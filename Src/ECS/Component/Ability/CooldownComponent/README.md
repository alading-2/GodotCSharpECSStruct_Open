# CooldownComponent (冷却组件)

## 概述
`CooldownComponent` 负责管理技能的冷却时间（Cooldown）。它使用高性能的 `TimerManager` 来处理计时逻辑，确保在大量技能并发时依然保持高效。

## 核心特性
1.  **高性能计时**：不使用每帧 `_Process` 轮询，而是通过 `TimerManager` 注册回调。
2.  **单一数据源**：直接操作 `DataKey` 存储状态，支持 ECS 数据驱动。
3.  **冷却缩减支持**：计算冷却时间时自动应用 `CooldownReduction` 属性修改。
4.  **UI 同步**：提供剩余时间和进度的查询接口，方便 UI 展示。

## 依赖的 DataKeys

| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityCooldown` | `float` | 基础冷却时间 (静态配置) |
| `CooldownReduction` | `float` | 冷却缩减百分比 (0-0.8)，通常来自 Role 或 Item |
| `AbilityCooldownRemaining` | `float` | **[已移除]** 旧版 Key，现已废弃，状态由 Timer 内部管理 |
| `AbilityIsCoolingDown` | `bool` | **[已移除]** 旧版 Key，现已废弃，状态由 Timer 内部管理 |

**注意**：在最新的重构中，组件直接持有 `GameTimer` 引用来判断是否处于冷却中，不再依赖 `AbilityIsCoolingDown` 等中间状态 Key，以保证数据的一致性。

## 工作原理
1.  **开始冷却**：技能激活后，调用 `StartCooldown()`。
2.  **创建定时器**：组件向 `TimerManager` 申请一个 `Delay` 定时器。
3.  **状态管理**：`IsReady()` 通过检查 `_timer` 是否存在来判断是否可用。
4.  **完成回调**：定时器结束后，触发 `onComplete` 回调，发出 `Ability.Ready` 事件，并销毁定时器引用。

## 接口说明
```csharp
// 检查技能是否就绪（非冷却中）
bool IsReady();

// 开始冷却（自动计算冷却缩减）
// 冷却时间 = BaseCooldown * (1 - clamp(Reduction, 0, 0.8))
void StartCooldown();

// 立即重置冷却（变为就绪状态）
void ResetCooldown();

// 获取冷却进度 (0.0=刚开始, 1.0=完成)
float GetCooldownProgress();

// 获取剩余冷却时间 (秒)
float GetRemainingCooldown();
```

## 事件驱动架构

`CooldownComponent` 遵循事件驱动设计，通过订阅 Entity 的事件来响应外部请求，实现高内聚低耦合。

### 订阅的事件

| 事件 | 响应行为 |
| :--- | :--- |
| `RequestCheckCanUse` | 检查冷却状态，若冷却中则调用 `context.SetBlocked("技能冷却中")` |
| `RequestStartCooldown` | 调用内部 `StartCooldown()` 启动冷却计时 |
| `RequestResetCooldown` | 调用内部 `ResetCooldown()` 立即重置冷却 |

### 使用示例

```csharp
// 外部调用方（如 TriggerComponent、AbilitySystem）无需获取组件实例
// 只需发送事件，组件自动响应

// 检查可用性
var context = new AbilityCanUseCheckContext(ability);
ability.Events.Emit(GameEventType.Ability.RequestCheckCanUse, 
    new GameEventType.Ability.RequestCheckCanUseEventData(context));

if (!context.CanUse) 
{
    // 被阻止：context.BlockReason 包含原因
}

// 启动冷却
ability.Events.Emit(GameEventType.Ability.RequestStartCooldown,
    new GameEventType.Ability.RequestStartCooldownEventData(ability));
```
