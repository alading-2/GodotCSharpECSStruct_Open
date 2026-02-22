# AI 系统说明（当前实现）

## 1. 文档目标

本文档描述当前项目中敌人 AI 的**实际实现**，覆盖以下内容：

- 行为树核心框架（`Src/AI/Core`）
- 敌人行为树构建（`Src/AI/Nodes/EnemyBehaviorTreeBuilder.cs`）
- ECS 组件集成（`AIComponent` / `EnemyMovementComponent`）
- 攻击与动画协作（`AttackComponent` / `UnitAnimationComponent`）
- 关键数据键、事件流与扩展点

> 文档收敛说明：`AISystem_Prompt.md` 已移除，本文件作为 Docs 侧唯一 AI 说明。
>
> 源码目录说明：`Src/AI` 的基类“有什么用、怎么用”请看 `Src/AI/README.md`。

---

## 2. 总体架构

当前 AI 采用“**行为树决策 + 组件执行**”模式：

1. **AIComponent** 每帧构建 `AIContext` 并 Tick 行为树。
2. 行为树节点通过 `DataKey` 写入意图（如移动方向、状态）或发出事件（如攻击请求）。
3. 执行组件读取意图并执行：
   - `EnemyMovementComponent`：执行移动与朝向。
   - `AttackComponent`：执行攻击状态机与伤害判定。
   - `UnitAnimationComponent`：响应动画事件并驱动动画播放。

核心设计原则：

- **Data 是唯一运行时状态源**（`Entity.Data`）。
- AI 决策层不直接操作物理体和渲染节点。
- 组件之间通过 Entity 级 EventBus 通信，降低耦合。

---

## 3. 核心模块说明

## 3.1 行为树基础（`Src/AI/Core`）

### 3.1.1 `NodeState` 与 `BehaviorNode`

- `NodeState`：`Running / Success / Failure`
- `BehaviorNode`：所有节点基类，定义 `Evaluate(AIContext)` 与 `Reset()`

### 3.1.2 `AIContext`

每帧由 `AIComponent` 复用同一对象填充：

- `Entity`：当前 AI 实体
- `Data`：共享数据容器
- `Events`：实体事件总线
- `DeltaTime`：当前帧时间

### 3.1.3 组合节点

- `SequenceNode`（AND）：从 `_currentIndex` 继续执行，支持记忆。
- `SelectorNode`（OR/优先级）：每帧从 0 开始，支持高优抢占并重置旧分支。

### 3.1.4 叶子节点

- `ConditionNode`：读取上下文并返回成败。
- `ActionNode`：执行业务动作并返回状态。

### 3.1.5 装饰节点

- `InverterNode`：Success/Failure 反转。
- `AlwaysSucceedNode`：屏蔽失败（Running 保持）。
- `CooldownNode`：限制子节点成功后的触发频率。

---

## 3.2 行为树运行器（`BehaviorTreeRunner`）

职责：

- 持有根节点 `Root`
- 每帧调用 `Root.Evaluate(ctx)`
- 记录 `LastState` 与 `IsRunning`
- 支持 `Reset()` 与 `SetTree()` 热切换

---

## 3.3 AI 组件（`AIComponent`）

生命周期：

- `OnComponentRegistered`：
  - 缓存 `IEntity/Data`
  - 记录出生点 `DataKey.SpawnPosition`
  - 置 `DataKey.AIEnabled = true`
  - 默认装配近战树 `EnemyBehaviorTreeBuilder.BuildMeleeEnemyTree()`
- `_Process`：
  - 检查 Runner / AIEnabled / 生命周期（Dead 直接返回）
  - 填充 `_context`（避免每帧 new）
  - 调用 `Runner.Tick(_context)`
- `OnComponentUnregistered`：`Runner.Reset()` 并释放引用

---

## 3.4 敌人行为树（`EnemyBehaviorTreeBuilder`）

当前默认树结构：

```text
Selector(根)
├─ Sequence(攻击)
│  ├─ HasValidTarget
│  ├─ IsTargetInAttackRange
│  ├─ IsAttackReady
│  └─ ExecuteAttack
├─ Sequence(追逐)
│  ├─ HasValidTarget
│  └─ MoveToTarget
└─ Patrol
```

### 条件节点

- `HasValidTarget`
  - 调用 `FindNearestPlayer` 获取/维持目标
  - 过滤无效、死亡、Reviving 目标
- `IsTargetInAttackRange`
  - 使用实体位置与 `DataKey.AttackRange` 判定
- `IsAttackReady`
  - 读取 `DataKey.AttackState == Idle`

### 动作节点

- `ExecuteAttack`
  - 写入朝向 `MoveDirection`
  - 写入 `MoveSpeedMultiplier = 0`
  - 攻击中状态处理：
    - `WindUp` 返回 `Running`
    - `Recovery` 返回 `Failure`（允许 Selector 转追逐）
  - Idle 时发射 `GameEventType.Attack.Requested`
  - 写 `AIState = Attacking`，返回 `Running`

- `MoveToTarget`
  - 写 `MoveDirection` + `MoveSpeedMultiplier = 1`
  - 写 `AIState = Chasing`
  - 返回 `Running`

- `Patrol`
  - 在 `SpawnPosition` 周围随机点巡逻
  - 包含等待计时（`PatrolWaitTimer`）
  - 巡逻速度倍率 0.5
  - 写 `AIState = Patrolling`/`Idle`

### 索敌逻辑 `FindNearestPlayer`

- 若已有目标，仅检查是否超出 `LoseTargetRange`
- 无目标时通过 `TargetSelector.Query` 在 `DetectionRange` 内检索最近敌方单位
- 命中后缓存到 `DataKey.TargetNode`

---

## 3.5 移动执行组件（`EnemyMovementComponent`）

在 `_PhysicsProcess` 中执行：

1. 读取 `DataKey.MoveDirection`
2. 读取 `DataKey.MoveSpeedMultiplier`
3. 读取 `DataKey.MoveSpeed`
4. 计算 `_body.Velocity` 并 `MoveAndSlide()`
5. 用方向更新 `VisualRoot.Scale.X` 实现朝向翻转

这使 AI 只“决策”，移动组件只“执行”。

---

## 3.6 攻击执行组件（`AttackComponent`）

`AttackComponent` 是攻击状态机 + 双计时器驱动：

- 状态：`Idle -> WindUp -> Recovery -> Idle`
- 计时器：
  - `_phaseTimer`：阶段推进（前摇、后摇、剩余冷却）
  - `_validationTimer`：每 0.2s 校验上下文合法性

### 事件输入

- 监听 `attack:requested`：尝试启动攻击
- 监听 `attack:cancel_requested`：外部中断

### 关键流程

1. `OnAttackRequested`
   - 仅 `Idle` 可进入
   - 通过 `ValidateCanAttack` 校验自身/目标/距离
   - 发 `attack:started`
   - 请求播放攻击动画 `unit:play_animation_requested`
   - 进入 `EnterWindUp`

2. `OnWindUpComplete`
   - 再次 `ValidateTargetForStrike`
   - 执行 `ExecuteDamage`

3. `ExecuteStrikeAndProceed`
   - 若有后摇进入 `EnterRecovery`
   - 否则直接 `FinishAttack`

4. `FinishAttack`
   - 计算剩余冷却：`AttackInterval - WindUp - Recovery`
   - 若剩余 > 0，复用 `Recovery` 状态延迟结束
   - 最终 `CompleteFinishAttack`：置 `AttackState = Idle` 并发 `attack:finished`

5. `CancelAttack`
   - 清理计时器并置 Idle
   - 发 `unit:stop_animation_requested`
   - 发 `attack:cancelled`

### 与行为树的耦合点

- 行为树以 `DataKey.AttackState == Idle` 判断“可开新攻击”。
- 攻击组件维护整个攻击占用窗口（前摇/后摇/剩余间隔），自然限制攻击频率。

---

## 3.7 动画组件（`UnitAnimationComponent`）

职责：

- 监听 `unit:play_animation_requested` 播放指定动画
- 监听 `unit:stop_animation_requested` 强制回 Idle
- 监听 `unit:damaged` 播放受击动画
- 监听 `unit:killed` 播放死亡动画
- 在 `_Process` 中根据速度在 `idle/run` 间自动切换

攻击协作点：

- `AttackComponent` 在前摇开始时请求播放攻击动画（支持按攻击间隔拉伸时长）
- 攻击取消时请求停止动画，避免表现残留

---

## 4. 关键 DataKey 与 Event

## 4.1 DataKey（AI 相关）

位于 `Data/DataKeyRegister/AI/DataKey_AI.cs`：

- 状态与目标：`AIState`、`TargetNode`、`AIEnabled`
- 感知：`DetectionRange`、`LoseTargetRange`
- 巡逻：`SpawnPosition`、`PatrolRadius`、`PatrolTargetPoint`、`PatrolWaitTime`、`PatrolWaitTimer`
- 移动意图：`MoveDirection`、`MoveSpeedMultiplier`
- 攻击动画：`AttackAnimName`

## 4.2 关键事件

- 攻击事件（`Data/EventType/Unit/Attack/GameEventType_Attack.cs`）
  - 命令：`attack:requested`、`attack:cancel_requested`
  - 通知：`attack:started`、`attack:finished`、`attack:cancelled`
- 单位事件（`Data/EventType/Unit/GameEventType_Unit.cs`）
  - 动画：`unit:play_animation_requested`、`unit:stop_animation_requested`
  - 生命周期/受击：`unit:killed`、`unit:damaged`

---

## 5. 运行时主链路（从 Tick 到伤害）

```text
AIComponent._Process
  -> Runner.Tick(ctx)
    -> EnemyBehaviorTreeBuilder 的树
      -> ExecuteAttack 发 attack:requested
        -> AttackComponent 启动前摇/校验
          -> 命中时 ExecuteDamage -> DamageService.Process
          -> 结束发 attack:finished
        -> UnitAnimationComponent 响应动画事件
```

---

## 6. 扩展建议（按当前架构）

1. **新增敌人类型**
   - 为不同敌人提供新的 Builder（如远程、突进、召唤）。
   - 在 `AIComponent.SetBehaviorTree()` 中按配置切树。

2. **新增行为节点**
   - 优先新增 Condition/Action 叶子节点并复用现有 Composite。
   - 共性频控需求优先用 `CooldownNode` 包装。

3. **增强调试能力**
   - 输出当前 Running 节点名与 `AIState`。
   - 在 Editor/Gizmo 中可视化 DetectionRange / LoseTargetRange / AttackRange。

4. **攻击表现增强**
   - 在 `attack:started/finished/cancelled` 上接 VFX/SFX/UI。
   - 继续保持“AI 发命令、组件执行”的边界，不在行为树内直接做表现逻辑。

---

## 7. 维护约定

- 若修改行为树结构或节点返回语义，需同步更新本说明。
- 若新增 AI 关键 DataKey/Event，需补充到本文第 4 节。
- 若更改攻击状态机时序（WindUp/Recovery/CD 语义），需验证：
  - `IsAttackReady` 判定是否仍正确
  - 追逐/攻击切换是否仍符合预期
