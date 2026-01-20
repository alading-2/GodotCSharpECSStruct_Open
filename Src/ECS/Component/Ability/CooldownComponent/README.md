# CooldownComponent (冷却组件)

## 概述
`CooldownComponent` 管理技能的冷却状态 (CD)。

## 架构特性
*   **精准计时**：利用 `TimerManager` 进行毫秒级冷却管理。
*   **属性封装**：提供 `IsOnCooldown` 属性供快速访问。
*   **自动处理**：支持全局冷却缩减 (Cooldown Reduction) 统计属性。

## 事件交互
12: 1.  **`CheckCanUse` (IN)**:
13:     *   检查 `IsOnCooldown`。
14:     *   若为 true -> `Context.SetFailed("冷却中")`。
15: 2.  **`StartCooldown` (IN)**:
16:     *   读取 `AbilityCooldown` 和 `CooldownReduction`。
17:     *   计算最终 CD 并启动计时器。
18: 3.  **`ResetCooldown` (IN)**:
19:     *   立即 Cancel 计时器，恢复就绪状态。

## 依赖 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityCooldown` | `float` (Stat) | 基础冷却时间 |
| `CooldownReduction` | `float` (Stat) | 缩减百分比 (0.0 - 1.0) |

---

**维护者**：项目团队  
**文档版本**：v3.0  
**更新日期**：2026-01-20
