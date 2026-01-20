# ChargeComponent (充能组件)

## 概述
`ChargeComponent` 管理技能的充能（Charges）系统，支持多次使用及随时间自动恢复机制。

## 核心功能
1.  **多段充能**：最大次数由 `AbilityMaxCharges` 定义。
2.  **事件驱动消耗**：响应 `ConsumeCharge` 事件。
3.  **高性能恢复**：使用 `TimerManager.Loop` 驱动自动恢复。
4.  **按需恢复控制**：若 `AbilityChargeTime < 0`，则禁用自动恢复，仅响应 `AddCharge` 事件。

## 依赖的 DataKeys
| DataKey | 类型 | 描述 |
| :--- | :--- | :--- |
| `AbilityMaxCharges` | `int` | 最大充能次数 |
| `AbilityCurrentCharges` | `int` | 运行时当前充能 |
| `AbilityChargeTime` | `float` | 恢复一次所需时间（秒） |
| `IsAbilityUsesCharges` | `bool` | 是否启用该组件逻辑 |

## 事件交互 (Context 模式)

### 1. 响应 `RequestCheckCanUse`
检查充能是否大于 0。若不足，调用 `eventData.Context.SetFailed("充能不足")`。

### 2. 响应 `ConsumeCharge`
扣除一次充能并标记成功。若执行中充能不足，通过 `EventContext` 返回失败原因。

### 3. 响应 `AddCharge`
由外部系统（道具/Buff）增加充能层数，受 `MaxCharges` 保护。

## 工作逻辑
- **初始化**：通过 `OnComponentRegistered` 订阅实体事件。
- **计时器管理**：
    - 充能未满且 `AbilityChargeTime > 0` 时，启动 `TimerManager.Loop`。
    - 充能满时自动 `Cancel` 计时器。
    - 组件注销 (`OnComponentUnregistered`) 时强制清理计时器。

---

**维护者**：项目团队  
**文档版本**：v2.6  
**更新日期**：2026-01-19
