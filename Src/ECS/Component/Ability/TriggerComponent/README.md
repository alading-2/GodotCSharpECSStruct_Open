# TriggerComponent (触发组件)

## 概述
`TriggerComponent` 是技能系统的“扳机”，负责监听外部输入或游戏事件，决定何时尝试激活技能。它支持多种触发模式的组合。

## 核心功能
1.  **其于模式的触发**：根据 `AbilityTriggerMode` (Flags) 执行不同的触发逻辑。
2.  **事件监听**：订阅全局事件或特定对象的事件（如“受击时触发”）。
3.  **周期性触发**：处理光环或持续性技能的自动触发。
4.  **自动触发**：处理武器类技能的自动攻击尝试。
5.  **手动触发**：提供接口供玩家输入系统调用。

## 支持的触发模式 (AbilityTriggerMode)
*   **Manual (主动)**：玩家按下按键时触发。
*   **OnEvent (被动)**：监听如 `UnitHurt` (受击), `UnitKilled` (击杀) 等事件时触发。
*   **Periodic (被动/光环)**：每隔固定时间间隔触发一次。
*   **Auto (武器)**：类似于“吸血鬼幸存者”的自动武器，自动寻找目标触发。
*   **Permanent (被动)**：永久生效（属性加成），通常仅在添加时执行一次初始化逻辑。

**注意**：使用了 `[Flags]`，一个技能可以同时具备多种触发方式（例如既可以主动释放，也可以在濒死时自动触发）。

## 依赖的 DataKeys

| DataKey | 类型 | 描述 | 用途 |
| :--- | :--- | :--- | :--- |
| `AbilityTriggerMode` | `AbilityTriggerMode` | 触发模式掩码 | 决定组件行为 |
| `AbilityTriggerEvent` | `string` | 监听的事件名称 | OnEvent 模式必需 |
| `AbilityTriggerInterval` | `float` | 触发间隔 (秒) | Periodic 模式必需 |
| `AbilityTriggerChance` | `float` | 触发概率 (0-1) | OnEvent 模式用于概率触发 |

## 关键流程
*   **OnEvent**: 订阅 `GlobalEventBus` -> 收到事件 -> 概率检查 -> 冷却检查 -> `TriggerAbility()`。
*   **Periodic**: `_Process` 累加时间 -> 间隔检查 -> 冷却检查 -> `TriggerAbility()`。
*   **Auto**: `_Process` 轮询 -> 冷却检查 -> 发出 `TryActivate` 事件 (由 `TargetingComponent` 接手寻找目标)。

## 接口说明
```csharp
// 尝试手动触发（需检查是否包含 Manual 模式）
bool TryManualTrigger();

// 检查是否包含某种触发模式
bool HasTriggerMode(AbilityTriggerMode mode);
```

## 事件交互
当条件满足且通过内部冷却检查后，组件会发送 `GameEventType.Ability.Activated` 事件，通知 `AbilitySystem` 正式执行技能流程。
