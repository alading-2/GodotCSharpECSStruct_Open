# TriggerComponent (触发组件)

## 概述
`TriggerComponent` 是技能的"扳机"，负责产生 **施法意图**，不负责具体执行。

## 核心职责
1.  **意图生成**：当条件满足时，发送 `TryTrigger` 事件。
2.  **多模式支持**：支持手动、事件、周期、自动等多种触发模式。
3.  **纯粹性**：不扣除消耗，不启动冷却，只负责"想不想放"。

## 触发模式 (AbilityTriggerMode)
*   **Manual**: 响应代码调用 `TryManualTrigger`。
*   **OnEvent**: 监听全局事件（如 `UnitKilled`），自动发起尝试。
*   **Periodic**: 定时器驱动，周期性发起尝试。
*   **Auto**: 自动攻击逻辑（如武器）。

## 事件交互
*   **OUT -> `TryTrigger`**: 发送给 `AbilitySystem`，请求激活技能。
*   **IN <- `GlobalEventBus`**: 监听外部事件（如果是 OnEvent 模式）。

## 依赖 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityTriggerMode` | `int` (Flags) | 触发模式掩码 |
| `AbilityTriggerEvent` | `string` | 监听的事件名 |
| `AbilityTriggerInterval` | `float` | 周期触发间隔 |

---

**维护者**：项目团队  
**文档版本**：v3.0  
**更新日期**：2026-01-20
