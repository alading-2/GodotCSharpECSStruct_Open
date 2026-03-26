# 实体状态管理与 AI 系统协调方案

> 文档类型：架构方案更新
> 适用版本：当前 `EntityMovementComponent + IMovementStrategy` 移动系统
> 状态：方案更新完成，尚未同步代码实现

---

## 1. 核心问题

当前项目缺少独立的**行为能力层**，导致多个系统各管一部分，但没有统一回答：

> 这个实体此刻还能不能思考、主动移动、攻击、施法？

现有问题：

| 层级 | 当前机制 | 问题 |
| ---- | ---- | ---- |
| 生命周期层 | `LifecycleState` | 只表达生死/复活，不表达行为约束 |
| 移动执行层 | `MoveMode` + `MovementStarted/Completed` | 只描述怎么移动，不描述能不能行动 |
| 速度物理层 | `IsMovementLocked` / `VelocityOverride` | 只描述速度合成，不描述 AI 是否暂停 |
| AI 层 | `AIEnabled` + `LifecycleState.Dead` | 不能覆盖 `Reviving`、击退、眩晕、剧情接管 |
| 攻击层 | `AttackState` | 只维护攻击状态，不参与统一协调 |

典型冲突：

- **击退**：`VelocityOverride` 生效，但 AI 仍在 Tick。
- **推开/冲锋**：轨迹切换了，AI 仍在索敌、攻击。
- **冻结/眩晕**：身体不能动，但脑子还在跑。
- **复活中**：`LifecycleState = Reviving`，AI 仍可能继续执行。

核心结论：**`MoveMode` 不能继续充当状态系统。**

---

## 2. 分层原则

| 层级 | 职责 | 代表数据 |
| ---- | ---- | ---- |
| 生命周期层 | 这个实体是否处于可存活流程 | `LifecycleState` |
| 行为能力层 | 这个实体当前还能做什么 | `EntityActionState` |
| 移动执行层 | 当前由哪种策略生成位移意图 | `MoveMode` / `DefaultMoveMode` |
| 速度物理层 | 多来源速度如何合成最终位移 | `VelocityResolver` 相关 DataKey |

统一原则：

- `MoveMode` 只负责**怎么移动**。
- `EntityActionState` 只负责**还能做什么**。
- `LifecycleState` 只负责**是否处于生死/复活流程**。
- `VelocityResolver` 只负责**最终速度怎么合成**。

不再允许：

- 用 `MoveMode != AIControlled` 推断 AI 是否暂停。
- 用 `VelocityOverride` / `IsMovementLocked` 推断实体是否还能攻击或施法。

---

## 3. 推荐方案：`EntityActionState`

新增 `EntityActionState` 作为**行为能力层**，统一表达实体当前是否允许思考、主动移动、攻击、施法。

```csharp
[Flags]
public enum EntityActionState
{
    Free = 0,

    MoveSuppressed = 1 << 0,
    ActionSuppressed = 1 << 1,
    AISuppressed = 1 << 2,
    FullStun = MoveSuppressed | ActionSuppressed | AISuppressed,
}
```

推荐语义：

- `MoveSuppressed`：禁止主动移动，但允许外力位移继续生效。
- `ActionSuppressed`：禁止攻击、施法、主动行为。
- `AISuppressed`：AI 行为树停止 Tick，控制权交给外部系统。
- `FullStun`：整体硬直。

职责边界：

- `LifecycleState`：只处理生死和复活流程。
- `EntityActionState`：只处理行为能力。
- `MoveMode`：只处理运动策略。
- `VelocityResolver`：只处理速度优先级。

---

## 4. 协作规则

### 4.1 默认自主移动链路

- `DefaultMoveMode = MoveMode.AIControlled`
- `AIComponent` 写 `AIMoveDirection`、`AIMoveSpeedMultiplier`
- `AIControlledStrategy` 读取意图并写 `Velocity`
- `EntityMovementComponent` 执行位移

### 4.2 外力控制但不需要特殊轨迹

例如击退、拉拽、冻结：

- 不必切 `MoveMode`
- 直接写 `VelocityOverride` / `IsMovementLocked`
- 同时写入 `EntityActionState`，禁止 AI / 攻击 / 施法继续执行

原则：

- **是否位移** → 速度层处理
- **是否还能思考/行动** → 状态层处理

### 4.3 外力控制且需要特殊轨迹

例如推开、冲锋、附着、回旋：

1. 发 `MovementStarted`
2. `EntityMovementComponent` 切入对应策略
3. 如需接管控制权，同时 `Push(EntityActionState.xxx)`
4. 轨迹结束后在 `MovementCompleted` 或定时器结束时 `Pop(EntityActionState.xxx)`

原则：

- `MovementStarted/Completed` 只负责**切换运动模式**
- `EntityActionState` 只负责**行为能力和控制权**
- 两者协作，不互相替代

### 4.4 AIComponent 判断规则

```csharp
if (!AIEnabled) return;

if (lifecycleState is Dead or Reviving)
    return;

if (actionState.HasFlag(EntityActionState.AISuppressed) ||
    actionState.HasFlag(EntityActionState.ActionSuppressed))
    return;

Runner.Tick(_context);
```

### 4.5 移动层判断规则

- `AIControlled` / `PlayerInput` 这类主动输入型策略应尊重 `MoveSuppressed`
- `TargetPoint` / `Boomerang` / `AttachToHost` 这类脚本轨迹型策略是否受 `MoveSuppressed` 影响，由业务定义

原则：`MoveSuppressed` 约束的是**主动控制**，不是机械地停止所有策略。

---

## 5. 落地步骤

1. **修正 AIComponent 判断**
   - 增加 `LifecycleState.Reviving` 屏蔽
   - 不再通过 `MoveMode` 直接暂停 AI

2. **引入 `EntityActionState`**
   - 新增 `DataKey.ActionState`
   - 提供 `Push/Pop` 或引用计数 Helper

3. **统一消费状态标记**
   - AI：读取 `AISuppressed` / `ActionSuppressed`
   - 攻击/技能：读取 `ActionSuppressed`
   - 主动输入型移动：读取 `MoveSuppressed`

4. **统一外部控制接入方式**
   - 轨迹切换走 `MovementStarted`
   - 行为抑制走 `EntityActionState`
   - 速度覆盖走 `VelocityOverride` / `IsMovementLocked`

5. **逐步迁移旧调用点**
   - 减少直接写 `MoveMode` 的调用
   - 不再把 `MoveMode` 当实体状态机使用

---

## 6. 最终结论

> `MoveMode` 负责“怎么移动”，`EntityActionState` 负责“还能做什么”，`LifecycleState` 负责“是否处于可存活流程”，`VelocityResolver` 负责“最终速度怎么合成”。

后续状态系统、AI 协调方案、控制效果接入，统一按这四层分工实现。
