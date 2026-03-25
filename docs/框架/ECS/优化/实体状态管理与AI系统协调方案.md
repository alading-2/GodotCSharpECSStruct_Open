# 实体状态管理与 AI 系统协调方案

> 文档类型：架构优化方案（深度分析 + 可选方案对比）
> 涉及系统：AI 系统、移动系统、VelocityResolver、LifecycleComponent
> 状态：**方案讨论阶段，未修改代码**

---

## 1. 问题陈述

### 1.1 表面问题

移动系统统一后，敌人的 `MoveMode` 从 `AIControlled` 切换到其他模式（如被击退时外部写入 `VelocityOverride`、被技能推开时临时改 `MoveMode`）时：

- **AI 行为树仍在每帧 Tick**，继续写入 `DataKey.AIMoveDirection` 和 `DataKey.AIMoveSpeedMultiplier`
- 这些写入虽然在 `MoveMode ≠ AIControlled` 时不会被 `AIControlledStrategy` 读取（因为策略调度器不会选中它），但 **AI 决策本身仍在运行**——索敌、追踪判定、攻击请求等逻辑全部照常执行
- 结果：AI 在"不该思考"的时候仍在思考，可能发出攻击事件、切换目标等，产生不可预期的行为

### 1.2 根源问题：缺乏统一的"实体行为状态"管理

当前项目中，**不同系统各自管理自己的"启停"状态，缺乏一个统一的协调层**：

| 系统 | 当前启停机制 | 问题 |
|------|-------------|------|
| **AI 系统** | `DataKey.AIEnabled` + `LifecycleState.Dead` 检查 | 只管"死没死"，不管"能不能行动" |
| **移动系统** | `DataKey.MoveMode` 策略切换 + `DataKey.IsMovementLocked` | 只管移动层，不通知 AI |
| **VelocityResolver** | `IsMovementLocked` → `VelocityOverride` → `Velocity` 分层 | 纯速度合成，不感知行为状态 |
| **LifecycleComponent** | `LifecycleState` 枚举 (Alive/Dead/Reviving) | 只管生死，粒度太粗 |
| **攻击系统** | `AttackState` (Idle/WindUp/Recovery) | 独立状态机，不与移动/AI 联动 |

**核心矛盾**：项目里没有一个地方回答 **"这个实体当前可以做什么"** 这个问题。每个系统只知道自己的状态，不知道其他系统对实体的约束。

### 1.3 具体冲突场景枚举

| 场景 | 触发方式 | 期望行为 | 当前实际行为 |
|------|---------|---------|-------------|
| **击退** | 外部写入 `VelocityOverride` | AI 暂停移动决策，击退结束后恢复 | AI 继续写 AIMoveDirection，VelocityOverride 覆盖了所以"看起来没事"，但 AI 可能在击退期间发出攻击请求 |
| **技能推开** | 临时改 `MoveMode` 为其他模式 | AI 完全暂停，推开结束后恢复 AIControlled | AI 继续 Tick，可能切换目标、重置巡逻点 |
| **眩晕/冻结** | 设置 `IsMovementLocked = true` | AI 暂停所有决策 | AI 继续决策，只是移动被锁定 |
| **死亡动画播放期间** | `LifecycleState = Dead` | AI 完全停止 | ✅ 已正确处理（AIComponent 检查 Dead） |
| **复活中** | `LifecycleState = Reviving` | AI 暂停 | ❌ 未处理，AI 继续 Tick |

---

## 2. 问题分层分析

### 2.1 第一层：AI 移动写入 vs 移动系统执行（已部分解决）

```
AI 动作节点 ──写入──→ AIMoveDirection / AIMoveSpeedMultiplier
                              │
                              ▼
AIControlledStrategy ──读取──→ 计算 Velocity
                              │
                              ▼
VelocityResolver ──合成──→ 最终速度（含击退/锁定覆盖）
```

当前架构的**移动执行层**已经通过 `VelocityResolver` 的分层合成解决了冲突——`VelocityOverride` 会覆盖 AI 写入的速度。但这只是**表象不冲突**，AI 的**决策层**仍在无意义地运行。

### 2.2 第二层：AI 决策 vs 行为约束（核心缺失）

AI 行为树不仅写移动数据，还会：

1. **索敌** (`FindEnemyAction`) → 切换 `DataKey.TargetNode`
2. **发起攻击** (`RequestAttackAction`) → 发射 `attack:requested` 事件
3. **施放技能** (`AutoCastAbilityAction`) → 触发 `TryTrigger` 流水线
4. **修改 AIState** → `Chasing / Attacking / Patrolling / Fleeing`

这些行为在"被控制"期间不应该发生，但当前没有机制阻止它们。

### 2.3 第三层：状态恢复（完全缺失）

当外部控制结束（击退结束、眩晕解除）后：

- 谁负责把 `MoveMode` 恢复为 `AIControlled`？
- 谁负责通知 AI "你可以继续思考了"？
- AI 恢复后，之前积累的决策残留数据（如过期的 TargetNode）是否需要清理？

当前答案：**没有人负责**。每个系统自扫门前雪。

---

## 3. 方案对比

### 方案 A：最小改动 —— AI 组件自检 MoveMode（推荐短期方案）

#### 核心思路

在 `AIComponent._Process` 中增加一行检查：**当 `MoveMode ≠ AIControlled` 时，跳过行为树 Tick**。

#### 改动范围

```
AIComponent.cs  → _Process 增加 MoveMode 检查（~5 行）
```

#### 伪代码

```csharp
// AIComponent._Process
public override void _Process(double delta)
{
    if (Runner == null || _data == null) return;
    if (!_data.Get<bool>(DataKey.AIEnabled)) return;

    var lifecycleState = _data.Get<LifecycleState>(DataKey.LifecycleState);
    if (lifecycleState == LifecycleState.Dead) return;

    // ★ 新增：非 AI 移动模式时暂停整个行为树
    var moveMode = _data.Get<MoveMode>(DataKey.MoveMode);
    if (moveMode != MoveMode.AIControlled && moveMode != MoveMode.None)
        return;

    _context.Entity = _entity;
    Runner.Tick(_context);
}
```

#### 优点

- **改动极小**，1 个文件 3 行代码
- 立即解决"被控制期间 AI 仍在决策"的问题
- 不引入新概念，不破坏现有架构

#### 缺点

- **耦合**：AI 组件需要知道 `MoveMode` 的语义（"哪些模式下我该停"）
- **粒度粗**：一刀切暂停整个行为树，无法做到"暂停移动但允许索敌"
- **不解决恢复问题**：MoveMode 恢复为 AIControlled 的责任仍在外部
- **扩展性差**：未来新增"允许 AI 部分运行"的场景时需要重构

#### 适用场景

项目早期、场景简单、只有"完全控制 or 完全自主"两种状态时。

---

### 方案 B：引入 EntityActionState（实体行为状态层）—— 推荐中期方案

#### 核心思路

在 `LifecycleState`（生死状态）之上，增加一个 **行为状态层** `EntityActionState`，统一回答"实体当前能做什么"。所有系统查询这个状态来决定自己的启停。

#### 新增枚举

```csharp
/// <summary>
/// 实体行为状态 - 描述实体当前的行为能力
/// <para>
/// 与 LifecycleState（生死）正交：活着的实体可能处于任何 ActionState。
/// 各系统根据此状态决定自己的启停逻辑。
/// </para>
/// </summary>
[Flags]
public enum EntityActionState
{
    /// <summary>完全自主行动（默认状态）</summary>
    Free        = 0,

    /// <summary>移动被抑制（击退/推拉/冻结路径期间）</summary>
    MoveSuppressed = 1 << 0,

    /// <summary>行动被抑制（眩晕/石化，不能移动也不能攻击/施法）</summary>
    ActionSuppressed = 1 << 1,

    /// <summary>AI 决策被抑制（外部完全接管，如剧情控制、过场动画）</summary>
    AISuppressed = 1 << 2,

    /// <summary>完全僵直（移动 + 行动 + AI 全部冻结）</summary>
    FullStun = MoveSuppressed | ActionSuppressed | AISuppressed,
}
```

#### 状态写入点（谁来设置）

| 触发源 | 写入的状态 | 恢复时机 |
|--------|-----------|---------|
| 击退效果 | `MoveSuppressed` | 击退 Timer 结束后清除标记 |
| 眩晕效果 | `FullStun` | 眩晕持续时间结束后清除 |
| 冻结效果 | `FullStun` | 冻结结束后清除 |
| 技能推开 | `MoveSuppressed` | 推开距离/时间达标后清除 |
| 剧情/过场 | `AISuppressed` | 过场结束后清除 |
| 正常状态 | `Free` | — |

#### 状态消费点（谁来读取）

```
AIComponent._Process:
    if (actionState.HasFlag(AISuppressed) || actionState.HasFlag(ActionSuppressed))
        → 跳过 Tick

EntityMovementComponent.RunMovementLogic:
    if (actionState.HasFlag(MoveSuppressed))
        → 跳过策略执行（但 VelocityResolver 仍处理 VelocityOverride）

AttackComponent:
    if (actionState.HasFlag(ActionSuppressed))
        → 拒绝攻击请求

AbilitySystem:
    if (actionState.HasFlag(ActionSuppressed))
        → TryTrigger 返回 Failed
```

#### 与现有系统的关系

```
                    ┌─────────────────────────┐
                    │   EntityActionState      │  ← 新增：行为能力层
                    │   (Flag 枚举，存 Data)    │
                    └──────────┬──────────────┘
                               │ 各系统读取
              ┌────────────────┼────────────────┐
              ▼                ▼                 ▼
        ┌──────────┐   ┌──────────────┐   ┌──────────┐
        │ AI 系统   │   │  移动系统     │   │ 攻击系统  │
        │ 查 AI/    │   │ 查 Move      │   │ 查 Action │
        │ Action    │   │ Suppressed   │   │ Suppressed│
        │ Suppressed│   │              │   │           │
        └──────────┘   └──────────────┘   └──────────┘

已有的 VelocityResolver 分层合成保持不变：
    IsMovementLocked → VelocityOverride → Velocity → Impulse
    （EntityActionState 是更上层的"行为意图"控制，
      VelocityResolver 是更下层的"速度物理"控制）
```

#### 与 IsMovementLocked / VelocityOverride 的关系

这是一个关键设计决策：

- `EntityActionState.MoveSuppressed` = **行为层**："你不应该主动移动"
- `IsMovementLocked` = **物理层**："你的物理体不能动"
- `VelocityOverride` = **速度层**："你的速度被外力覆盖"

三者可以独立存在，也可以组合：

| 场景 | MoveSuppressed | IsMovementLocked | VelocityOverride |
|------|:-:|:-:|:-:|
| 普通击退 | ✅ | ✗ | ✅ (击退速度) |
| 眩晕 | ✅ | ✅ | ✗ |
| 冰冻（可被推动）| ✅ | ✗ | ✗ |
| 技能推开 | ✅ | ✗ | ✅ (推开速度) |
| 正常 AI 移动 | ✗ | ✗ | ✗ |

**关键点**：`MoveSuppressed` 告诉 AI 和移动策略"别自作主张"，但不阻止 `VelocityOverride` 生效（击退仍然能推动你）。

#### 改动范围

```
新增：
  Data/DataKey/ 下新增 EntityActionState 枚举 + DataKey
  
修改（每处 3-5 行）：
  AIComponent.cs             → _Process 检查 ActionState
  EntityMovementComponent.cs → RunMovementLogic 检查 ActionState
  
外部触发点（逐步接入）：
  击退效果处理器   → 设置 MoveSuppressed，Timer 恢复
  眩晕效果处理器   → 设置 FullStun，Timer 恢复
  ...后续效果按需接入
```

#### 优点

- **语义清晰**：统一回答"实体能做什么"，消除各系统的猜测
- **Flags 枚举灵活**：可细粒度控制（只禁移动 / 只禁行动 / 全禁）
- **设置和恢复自包含**：效果自己设置标记 + 自己用 Timer 清除，不需要"中央调度器"
- **向后兼容**：现有 `IsMovementLocked` / `VelocityOverride` 逻辑完全保留，新层叠加在上面

#### 缺点

- 需要给每个"效果触发点"补上设置/清除逻辑
- Flag 叠加时需要引用计数（两个效果同时 Suppress 移动，一个结束不能把另一个也清了）
- 比方案 A 改动量大

#### 引用计数问题的解决

```csharp
// 而不是直接 Set，使用引用计数
public static class ActionStateHelper
{
    public static void Push(Data data, EntityActionState flag)
    {
        var current = data.Get<EntityActionState>(DataKey.ActionState);
        data.Set(DataKey.ActionState, current | flag);
        // 引用计数 +1
        var refKey = $"ActionStateRef_{flag}";
        data.Set(refKey, data.Get<int>(refKey) + 1);
    }

    public static void Pop(Data data, EntityActionState flag)
    {
        var refKey = $"ActionStateRef_{flag}";
        int count = data.Get<int>(refKey) - 1;
        data.Set(refKey, Mathf.Max(0, count));
        if (count <= 0)
        {
            var current = data.Get<EntityActionState>(DataKey.ActionState);
            data.Set(DataKey.ActionState, current & ~flag);
        }
    }
}
```

---

### 方案 C：状态机系统（EntityStateMachine）—— 远期大改方案

#### 核心思路

引入正式的 **有限状态机**，实体在明确的状态之间转换，每个状态定义允许的行为集合。

#### 状态定义

```
                    ┌─────────┐
            ┌──────→│  Free   │◄──────┐
            │       └────┬────┘       │
            │            │            │
       恢复  │      击退/推开    眩晕解除
            │            │            │
            │       ┌────▼────┐       │
            │       │Displaced│───────┘  （被位移中：AI暂停，移动被外部控制）
            │       └─────────┘
            │
            │       ┌─────────┐
            └───────│ Stunned │  （眩晕/石化：全部暂停）
                    └─────────┘
                    
                    ┌─────────┐
                    │Channeling│  （引导施法中：移动暂停，AI部分暂停）
                    └─────────┘
```

#### 状态转换矩阵

| 当前状态 → | Free | Displaced | Stunned | Channeling |
|-----------|:----:|:---------:|:-------:|:----------:|
| **Free** | - | ✅ | ✅ | ✅ |
| **Displaced** | ✅(自动) | 可叠加 | ✅(覆盖) | ✗ |
| **Stunned** | ✅(Timer) | ✗ | 可刷新 | ✗ |
| **Channeling** | ✅(完成/打断) | ✅(打断) | ✅(打断) | ✗ |

#### 各状态下系统行为

| 系统 | Free | Displaced | Stunned | Channeling |
|------|:----:|:---------:|:-------:|:----------:|
| AI 决策 | ✅ | ❌ | ❌ | ⚠️(仅维持) |
| AI 移动写入 | ✅ | ❌ | ❌ | ❌ |
| 移动策略执行 | ✅ | ❌(外部控制) | ❌ | ❌ |
| VelocityOverride | - | ✅ | - | - |
| 攻击 | ✅ | ❌ | ❌ | ❌ |
| 技能施放 | ✅ | ❌ | ❌ | ⚠️(当前技能) |
| 受击判定 | ✅ | ✅ | ✅ | ✅ |

#### 优点

- **最完整的状态管理**：所有行为约束在状态定义中一目了然
- **状态转换可审计**：可以打日志追踪每次状态变化
- **支持复杂游戏玩法**：引导施法、控制技能免疫等高级需求天然支持

#### 缺点

- **改动量最大**：需要新增 StateMachine 组件，所有系统都要改为读状态机
- **状态爆炸风险**：随着游戏玩法增加，状态数量可能失控
- **与行为树存在概念重叠**：行为树本身就是一种决策状态管理，再加状态机需要理清边界
- **过度设计风险**：对于 Brotato-like 的简单肉鸽，可能远超实际需求

---

## 4. 方案对比总结

| 维度 | 方案 A (AI 自检) | 方案 B (ActionState) | 方案 C (状态机) |
|------|:-:|:-:|:-:|
| 改动量 | ★☆☆ (~5行) | ★★☆ (~50行+效果接入) | ★★★ (200+行) |
| 解决完整度 | 60% | 90% | 100% |
| AI 暂停 | ✅ | ✅ | ✅ |
| 细粒度控制 | ✗ | ✅ (Flags) | ✅ (状态表) |
| 恢复机制 | ✗ 外部负责 | ✅ Push/Pop | ✅ 状态转换 |
| 多效果叠加 | ✗ | ✅ 引用计数 | ✅ 优先级覆盖 |
| 引入新概念 | 无 | 1个枚举+1个Helper | 1个完整子系统 |
| 过度设计风险 | 无 | 低 | 中高 |
| 推荐时机 | **现在** | **效果系统完善时** | **玩法复杂化时** |

---

## 5. 推荐实施路径（渐进式）

### 第一步：方案 A（立即可做，5 分钟）

在 `AIComponent._Process` 中加一行 MoveMode 检查，解决最紧迫的"被控制时 AI 仍在跑"的问题。

同时补上 `LifecycleState.Reviving` 的检查（当前遗漏）。

### 第二步：方案 B 基础版（效果系统开发时）

当项目开始实现击退、眩晕、冰冻等控制效果时，引入 `EntityActionState` Flags 枚举。

此时需要：
1. 定义枚举 + DataKey
2. AI/移动/攻击系统各加 2-3 行检查
3. 每个效果处理器负责 Push/Pop 自己的状态标记

### 第三步：评估是否需要方案 C

当出现以下情况时再考虑：
- 需要"引导施法期间可被打断"等复杂状态转换
- 需要"控制免疫"（部分状态转换被拒绝）
- 状态之间有复杂的优先级覆盖关系

对于 Brotato-like 游戏，**大概率不需要方案 C**。

---

## 6. 关于"AI 是否需要完全暂停"的分析

你提到的核心疑问：**被击退时，是否需要把 AI 所有逻辑都停掉？**

### 分场景分析

| AI 子行为 | 击退期间是否应该运行 | 理由 |
|----------|:-:|------|
| 移动决策（写 AIMoveDirection） | ❌ | 被外力控制中，写了也白写 |
| 索敌（FindEnemy） | ⚠️ 可选 | 击退期间发现新目标其实合理，但实现复杂 |
| 攻击请求 | ❌ | 被击退时不应该能攻击 |
| 技能施放 | ❌ | 被击退时不应该能施法 |
| AIState 更新 | ❌ | 会污染状态标记 |

### 结论

对于 Brotato-like 游戏的典型场景：

> **击退/眩晕/冻结期间，AI 行为树整体暂停是最安全、最简单的选择。**

理由：
1. 击退持续时间通常很短（0.1~0.5 秒），暂停 AI 完全不影响体验
2. 让 AI "部分运行"的收益极低（击退期间提前索敌？省不了几帧）
3. 部分运行的实现复杂度远高于整体暂停（需要给每个动作节点分类标记）

如果未来确实需要"被控制期间允许索敌"，可以在方案 B 基础上扩展：让 `AISuppressed` 区分"完全暂停"和"仅暂停输出"两种模式。但建议 **YAGNI（You Aren't Gonna Need It）**，等需求真实出现再做。

---

## 7. 附录：当前系统数据流全景图

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Entity (CharacterBody2D)                      │
│                                                                      │
│  ┌──────────────┐     ┌──────────────────────┐     ┌──────────────┐ │
│  │ AIComponent   │     │ EntityMovementComp.  │     │ AttackComp.  │ │
│  │               │     │                      │     │              │ │
│  │ _Process:     │     │ _PhysicsProcess:     │     │              │ │
│  │  Tick(行为树) │     │  RunMovementLogic()  │     │              │ │
│  └───────┬───────┘     └──────────┬───────────┘     └──────────────┘ │
│          │                        │                                   │
│          │ 写入                    │ 读取                              │
│          ▼                        ▼                                   │
│  ┌────────────────────────────────────────────────────────────┐      │
│  │                     Data 容器                               │      │
│  │                                                             │      │
│  │  AI 意图层:                                                  │      │
│  │    AIMoveDirection     ← AI 动作节点写                       │      │
│  │    AIMoveSpeedMultiplier ← AI 动作节点写                     │      │
│  │    AIState             ← AI 动作节点写                       │      │
│  │    TargetNode          ← FindEnemyAction 写                  │      │
│  │                                                             │      │
│  │  移动控制层:                                                 │      │
│  │    MoveMode            ← 初始化/外部效果写                    │      │
│  │    MoveSpeed           ← 配置初始化                          │      │
│  │    Velocity            ← Strategy 写 → Resolver 读          │      │
│  │                                                             │      │
│  │  速度覆盖层 (VelocityResolver 消费):                         │      │
│  │    IsMovementLocked    ← 眩晕/冻结效果写                     │      │
│  │    VelocityOverride    ← 击退/推拉效果写                     │      │
│  │    VelocityImpulse     ← 爆炸/弹射效果写（单帧清零）          │      │
│  │                                                             │      │
│  │  ★ 缺失层: EntityActionState  ← 应统一协调上述所有层 ★       │      │
│  │                                                             │      │
│  └────────────────────────────────────────────────────────────┘      │
│                                                                      │
│  数据消费流：                                                         │
│                                                                      │
│  AI 意图 → AIControlledStrategy → Velocity                           │
│                                      │                                │
│                                      ▼                                │
│                              VelocityResolver                         │
│                     IsMovementLocked? → Zero                          │
│                     VelocityOverride? → 覆盖                          │
│                     否则 → Velocity                                   │
│                     + VelocityImpulse                                 │
│                                      │                                │
│                                      ▼                                │
│                              MoveAndSlide()                           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 8. 决策记录

| 决策项 | 结论 | 理由 |
|--------|------|------|
| 是否立即引入状态系统 | 否，先用方案 A 过渡 | YAGNI，当前效果系统尚未完善 |
| 被控制时 AI 是否整体暂停 | 是 | 收益/复杂度比最优 |
| 是否需要状态机 | 大概率不需要 | Brotato 玩法复杂度有限 |
| 方案 B 何时实施 | 开发第一个控制效果（击退/眩晕）时 | 有真实场景驱动 |
