# ChargeComponent (充能组件)

## 概述
`ChargeComponent` 管理技能的充能（Charges）系统，支持多次使用及随时间自动恢复机制。

## 架构特性
1.  **无状态设计**：核心数据 (`MaxCharges`, `ChargeTime`) 存储于 `Entity.Data`。
2.  **选择性封装**：高频运行时数据 (`CurrentCharges`, `Timer`) 通过 C# 属性封装，内部读写 Data，外部调用更简洁。
3.  **事件驱动**：完全通过 `EventContext` 响应系统请求。

## 核心功能
1.  **多段充能**：支持 1 到 N 次充能。
2.  **高性能恢复**：使用 `TimerManager` 驱动恢复，若 `ChargeTime < 0` 则禁用自动恢复。
3.  **事件交互**：
    *   **Check**: 响应 `RequestCheckCanUse` -> 检查 `CurrentCharges > 0`。
    *   **Consume**: 响应 `ConsumeCharge` -> 扣除 1 层充能。
    *   **Add**: 响应 `AddCharge` -> 外部逻辑增加充能。

## 依赖 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityMaxCharges` | `int` (Stat) | 最大充能次数 |
| `AbilityCurrentCharges` | `int` (Runtime)| 当前充能次数（属性封装） |
| `AbilityChargeTime` | `float` (Stat) | 恢复时间 (<0 禁用自动恢复) |

---

**维护者**：项目团队  
**文档版本**：v3.0  
**更新日期**：2026-01-20
