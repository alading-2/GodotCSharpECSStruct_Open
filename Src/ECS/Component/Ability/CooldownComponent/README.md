# CooldownComponent (冷却组件)

## 概述
`CooldownComponent` 利用 `TimerManager` 驱动高性能的技能冷却计时。

## 核心职责
1. **冷却状态管理**：判断技能是否处于 CD 中。
2. **冷却缩减应用**：自动结合 `CooldownReduction` 与基础 CD 计算最终时长。
3. **重置机制**：支持通过事件立即刷新技能。

## 依赖的 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityCooldown` | `float` | 基础冷却时间 (Static) |
| `CooldownReduction` | `float` | 全局冷却缩减百分比 (Modifiers) |

## 事件驱动响应
*   **`RequestCheckCanUse`**：若计时器存在，回复 "技能冷却中"。
*   **`RequestStartCooldown`**：启动 `TimerManager.Delay`。
*   **`RequestResetCooldown`**：立即取消计时器，使技能就绪。

## 计时器设计
- **无状态存储**：组件不记录 `RemainingTime` 到 DataKey，而是通过检查私有字段 `_timer` 是否为空来判断状态。
- **自动清理**：
    - `OnComplete` 回调中自动释放引用并发送 `Ready` 事件。
    - `OnComponentUnregistered` 时强制 `Cancel`。

---

**维护者**：项目团队  
**文档版本**：v2.2  
**更新日期**：2026-01-19
