# TriggerComponent (触发组件)

## 概述
`TriggerComponent` 是技能的"扳机"，负责决定何时向 `AbilitySystem` 发起激活请求。

## 触发模式 (AbilityTriggerMode)
支持 `[Flags]` 位运算组合：
*   **Manual**：手动触发，响应 `TryManualTrigger` 调用。
*   **OnEvent**：监听 `GlobalEventBus` 上的特定游戏事件。
*   **Periodic**：基于 `_Process` 的固定频率循环触发。
*   **Auto**：武器专用，自动轮询并发出 `TryActivate` 激活请求。

## 核心 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityTriggerMode` | `int` | 触发模式掩码 |
| `AbilityTriggerEvent` | `string` | OnEvent 模式监听的事件名 |
| `AbilityTriggerInterval` | `float` | Periodic 模式的定时间隔 |
| `AbilityTriggerChance` | `float` | 触发概率 (0~1.0) |

## 触发流程 (事件化)
当满足触发条件时，组件会按以下步骤操作：
1. **就绪预检**：发送 `RequestCheckCanUse` 事件。
2. **数据注入**：若由事件触发，将 `eventData` 存入 Data 的 `_TriggerEventData` 以供 Effect 使用。
3. **发射激活**：
    - 主动/周期：直接发送 `Activated` 事件。
    - 自动/武器：发送 `TryActivate` 请求（由系统协调目标选择）。

## 生命周期管理
- **订阅**：在 `OnComponentRegistered` 时根据模式初始化（如订阅全局事件）。
- **清理**：在 `OnComponentUnregistered` 时必须 `UnsubscribeEvent` 以防止内存泄漏。

---

**维护者**：项目团队  
**文档版本**：v2.1  
**更新日期**：2026-01-19
