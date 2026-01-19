# ChargeComponent (充能组件)

## 概述
`ChargeComponent` 负责管理技能的充能（Charges）系统。它允许技能像《DOTA2》中的“虚空假面-时间漫游”或《守望先锋》中的“猎空-闪现”一样，拥有多次使用次数，并在使用后随时间自动恢复。

## 核心功能
1.  **最大充能管理**：确保存储的充能次数不超过最大限制。
2.  **充能消耗**：在技能激活时扣除充能次数。
3.  **自动恢复**：当充能未满时，开启内部计时器，随时间自动恢复充能。
4.  **事件通知**：在充能恢复时发送 `ChargeRestored` 事件。
5.  **充能安全机制**：所有充能增加操作（自动恢复/外部触发）统一经过 `InternalAddCharges()` 方法，确保严格的上限检查，防止充能超过 `AbilityMaxCharges`。

## 适用场景
*   **主动技能**：绝大多数需要充能的技能都是主动技能。
*   **多段位移**：如累计 3 次的冲刺技能。
*   **储备型技能**：如可以连续投掷 2 枚的手雷。

## 依赖的 DataKeys
`ChargeComponent` 是无状态的，所有运行时数据和配置数据都存储在 `AbilityEntity.Data` 中。

| DataKey | 类型 | 描述 | 来源 |
| :--- | :--- | :--- | :--- |
| `AbilityMaxCharges` | `int` | 最大充能次数 (配置) | 静态配置 |
| `AbilityCurrentCharges` | `int` | 当前剩余充能 (运行时) | 运行时状态 |
| `AbilityChargeTime` | `float` | 恢复一次充能所需时间 (秒) | 静态配置 |

> **注意**：`AbilityChargeTimer` 已废弃，计时由 `TimerManager` 内部管理。

## 工作流程
1.  **初始化**：组件注册时，将 `AbilityCurrentCharges` 初始化为 `AbilityMaxCharges`。
2.  **激活检查**：`AbilitySystem` 在激活技能前会调用 `HasCharge()` 检查是否有可用充能。
3.  **消耗充能**：技能激活时，`AbilitySystem` 调用 `ConsumeCharge()` 减少一次充能。
4.  **启动恢复**：`ConsumeCharge()` 从满充能变为非满时，自动调用 `StartChargeRecovery()` 启动 `TimerManager.Loop()` 循环计时器。
5.  **自动恢复**：`TimerManager` 每隔 `AbilityChargeTime` 触发 `RecoverOneCharge()` 回调，内部调用 `InternalAddCharges(1, "自动恢复")` 安全地恢复一次充能。
6.  **外部增加**：道具、Buff等外部逻辑可通过事件触发 `AddCharges(amount)`，内部同样调用 `InternalAddCharges(amount, "外部触发")`。
7.  **统一保护**：`InternalAddCharges()` 确保所有充能增加都经过 `Math.Min(amount, maxCharges - currentCharges)` 边界检查，即使 `MaxCharges` 在运行时动态改变也不会超限。
8.  **停止恢复**：充能恢复到最大值时，自动停止计时器。

> **架构优势**：
> - 使用 `TimerManager` 而非手动 `_Process()` 累加，统一框架计时逻辑
> - 所有充能增加路径统一收敛到 `InternalAddCharges()`，消除重复代码
> - 日志自动区分充能来源（"自动恢复" / "外部触发"），便于调试

## 接口说明
```csharp
// 是否有可用充能
bool HasCharge();

// 消耗一次充能，成功返回 true
bool ConsumeCharge();

// 增加充能（道具/Buff使用），返回实际增加的数量
int AddCharges(int amount);

// 获取当前充能恢复进度 (0.0 - 1.0)
float GetChargeProgress();

// 立即恢复所有充能
void RestoreAllCharges();
```

## 事件驱动架构

`ChargeComponent` 遵循事件驱动设计，通过订阅 Entity 的事件来响应外部请求，实现高内聚低耦合。

### 订阅的事件

| 事件 | 响应行为 |
| :--- | :--- |
| `RequestCheckCanUse` | 检查充能状态（仅主动技能），若不足则调用 `context.SetBlocked("充能不足")` |
| `RequestConsumeResources` | 调用内部 `ConsumeCharge()` 消耗一次充能（仅主动技能） |

### 使用示例

```csharp
// 外部调用方（如 AbilitySystem）无需获取组件实例
// 只需发送事件，组件自动响应

// 消耗资源
ability.Events.Emit(GameEventType.Ability.RequestConsumeResources,
    new GameEventType.Ability.RequestConsumeResourcesEventData(ability));
```

> **设计优势**：调用者无需知道"谁"处理、"怎么"处理，只需发送事件即可。未来添加其他资源类型（如法力、能量）时，只需新组件订阅同一事件。
