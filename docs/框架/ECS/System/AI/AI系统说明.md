# AI 系统说明（当前实现）

## 1. 文档目标

本文档描述当前项目中敌人 AI 的**实际实现**，覆盖以下内容：

- 行为树核心运行机制（逐帧 Tick 如何驱动决策）
- `SequenceNode` / `SelectorNode` 的执行逻辑与返回值含义
- 完整的分支切换、打断、Reset 流程
- 常见组合模式的完整运行时例子
- ECS 组件集成（`AIComponent` / `EntityMovementComponent`）
- 攻击与动画协作（`AttackComponent` / `UnitAnimationComponent`）
- 关键数据键、事件流与扩展点

> 文档收敛说明：`AISystem_Prompt.md` 已移除，本文件作为 Docs 侧唯一 AI 说明。
>
> 源码目录说明：`Src/AI` 的基类"有什么用、怎么用"请看 `Src/ECS/AI/README.md`。

---

## 2. 总体架构

当前 AI 采用“**行为树决策 + 组件执行**”模式：

1. **AIComponent** 每帧构建 `AIContext` 并 Tick 行为树。
2. 行为树节点通过 `DataKey` 写入意图（如移动方向、状态）或发出事件（如攻击请求）。
3. 执行组件读取意图并执行：
   - `EntityMovementComponent`：在 `DefaultMoveMode = AIControlled` 时读取 AI 移动意图并执行移动/朝向；临时轨迹通过 `MovementStarted`（开始/切换事件）切换。
   - `AttackComponent`：执行攻击状态机与伤害判定。
   - `UnitAnimationComponent`：响应动画事件并驱动动画播放。

核心设计原则：

- **Data 是唯一运行时状态源**（`Entity.Data`）。
- AI 决策层不直接操作物理体和渲染节点。
- 组件之间通过 Entity 级 EventBus 通信，降低耦合。

---

## 3. 核心运行机制（重点）

> **一句话概括**：`SelectorNode` 每帧从头遍历找第一个不 Failure 的分支；`SequenceNode` 靠 `_currentIndex` 记住当前步，`continue` 推进，Failure/Running 立即 return 退出循环。行为树没有"卡住"概念——每帧 SelectorNode 都会重新评估所有高优先级分支。

### 3.1 三态含义

| 状态 | 含义 | SequenceNode 的反应 | SelectorNode 的反应 |
|------|------|---------------------|---------------------|
| `Success` | 这步完成 | `continue` 到下一步 | 立即 return，停止遍历 |
| `Failure` | 这步做不了 | 整条链 return Failure | `continue` 到下一分支 |
| `Running` | 这步进行中 | 记住索引，return Running | return Running |

### 3.2 SequenceNode for 循环逐行解析

```csharp
for (int i = _currentIndex; i < Children.Count; i++)
{
    var state = Children[i].Evaluate(ctx);
    switch (state)
    {
        case NodeState.Failure:
            _currentIndex = 0;
            return NodeState.Failure;   // 退出循环，整条链失败

        case NodeState.Running:
            _currentIndex = i;
            return NodeState.Running;   // 退出循环，记住当前步，下帧从这里继续

        case NodeState.Success:
            continue;                   // 唯一继续 for 循环的情况，推进到下一步
    }
}
_currentIndex = 0;
return NodeState.Success;               // 所有步都 Success，整体成功
```

**关键点**：`continue` 是推进步骤的唯一路径，Failure 和 Running 都是立即 `return`。下帧从 `_currentIndex` 续跑，不重跑前面已成功的步骤。

### 3.3 SelectorNode for 循环逐行解析

```csharp
for (int i = 0; i < Children.Count; i++)   // 每帧从 i=0 开始，不记忆上次的位置
{
    var state = Children[i].Evaluate(ctx);
    switch (state)
    {
        case NodeState.Success:
            if (_currentIndex != i) ResetChildrenExcept(i, ctx);  // 清理旧分支
            _currentIndex = -1;
            return NodeState.Success;   // 退出循环

        case NodeState.Running:
            if (_currentIndex != i) ResetChildrenExcept(i, ctx);  // 分支切换时清理
            _currentIndex = i;
            return NodeState.Running;   // 退出循环

        case NodeState.Failure:
            continue;                   // 继续尝试下一个分支
    }
}
_currentIndex = -1;
return NodeState.Failure;               // 所有分支都失败
```

**关键点**：SelectorNode 每帧从 `i=0` 重新遍历，这正是"高优先级随时抢占"的来源。分支切换时 `ResetChildrenExcept` 传入 `ctx`，清理旧分支的计时器和黑板数据。

### 3.4 完整运行时例子：标准近战怪全程追踪

树结构（`BuildMeleeEnemyTree()` 构建）：

```text
SelectorNode("近战敌人")
├─[0] SequenceNode("攻击序列")   → AttackBranch()
│     ├─ FindEnemyAction
│     ├─ HasValidTargetCondition
│     ├─ IsInRangeCondition(AttackRange)
│     ├─ IsAttackReadyCondition
│     ├─ StopMovementAction
│     ├─ FaceTargetAction
│     └─ RequestAttackAction
├─[1] SequenceNode("追逐序列")   → ChaseBranch()
│     ├─ FindEnemyAction
│     ├─ HasValidTargetCondition
│     └─ MoveToTargetAction(AttackRange)
└─[2] SequenceNode("巡逻序列")   → PatrolBranch()
      ├─ RandomWanderAction
      └─ WaitIdleAction
```

---

#### 场景 A：无目标，敌人巡逻中

```
每帧 Tick：
  Selector i=0 攻击序列：FindEnemyAction → Failure（无目标）→ 整体 Failure
  Selector continue → i=1 追逐序列：FindEnemyAction → Failure → 整体 Failure
  Selector continue → i=2 巡逻序列：
    第一次进入：RandomWanderAction，PatrolTargetPoint==Zero
      → 选新目标点，写入 PatrolTargetPoint，return Success
      → SequenceNode continue → WaitIdleAction
      → WaitIdleAction：_timer==null，启动 1.5s 定时器，return Running
      → SequenceNode 记住 _currentIndex=1，return Running
  Selector _currentIndex=2，return Running

后续帧（等待中）：
  [0][1] 仍然 Failure（无目标）
  [2] 巡逻序列：_currentIndex=1，直接到 WaitIdleAction
      PatrolWaitDone==false → return Running（继续等）

1.5s 后定时器触发 PatrolWaitDone=true：
  [2] 巡逻序列：WaitIdleAction 读到 Done=true → 清除标志，return Success
      SequenceNode 所有步 Success → _currentIndex=0，return Success
  Selector return Success，_currentIndex=-1

下一帧（重新开始巡逻循环）：
  [2] 巡逻序列：_currentIndex=0，RandomWanderAction
      检查 PatrolTargetPoint 距离 < arrivalThreshold（刚才到达了）→ 选新点，return Success
      → WaitIdleAction：启动新定时器，return Running
```

---

#### 场景 B：发现目标，从巡逻切换到追逐

```
帧 N（无目标，巡逻 _currentIndex=1 在等待中）：
  Selector i=0：攻击序列 Failure（无目标）
  Selector i=1：追逐序列 Failure（无目标）
  Selector i=2：巡逻序列 Running（等待中）
  _currentIndex=2

帧 N+1（目标出现 100 格外）：
  Selector i=0：攻击序列
    FindEnemyAction → Success（找到目标，写入 TargetNode）
    HasValidTargetCondition → Success
    IsInRangeCondition(AttackRange=80) → Failure（距离100 > 80）
    → 攻击序列整体 Failure
  Selector i=1：追逐序列
    FindEnemyAction → Success
    HasValidTargetCondition → Success
    MoveToTargetAction → Running（开始移动，设 AIMoveDirection）
    → 追逐序列 Running，_currentIndex=2（在 MoveToTargetAction）
  Selector 发现 i=1 ≠ 旧 _currentIndex=2
    → ResetChildrenExcept(1, ctx)
      → [2] 巡逻序列 Reset(ctx)：
          RandomWanderAction.Reset(ctx)：PatrolTargetPoint = Zero
          WaitIdleAction.Reset(ctx)：_timer.Cancel()，PatrolWaitDone=false
  Selector _currentIndex=1，return Running
```

> 巡逻等待被立即打断：定时器取消，PatrolWaitDone 清零，下次回到巡逻时干净重启。

---

#### 场景 C：追逐到攻击范围内，切换为攻击

```
帧 M（距离刚好 < 80，追逐 Running 中）：
  Selector i=0：攻击序列（每帧从头检查！）
    FindEnemyAction → Success
    HasValidTargetCondition → Success
    IsInRangeCondition → Success（距离75 < 80，进入范围！）
    IsAttackReadyCondition → Success（冷却就绪）
    StopMovementAction → Success（设 AIMoveDirection=Zero）
    FaceTargetAction → Success（设朝向）
    RequestAttackAction → 发 attack:requested 事件，AttackState=Idle → return Running
    → 攻击序列 Running，_currentIndex=6（停在 RequestAttackAction）
  Selector i=0 ≠ 旧 _currentIndex=1
    → ResetChildrenExcept(0, ctx) → 追逐序列 Reset（MoveToTargetAction 清理）
  Selector _currentIndex=0，return Running

帧 M+1（前摇中）：
  Selector i=0：攻击序列 _currentIndex=6，直接跳到 RequestAttackAction
    AttackState==WindUp（前摇中）→ return Running（保持阻塞，等待打出这刀）
    注意：前置条件 FindEnemyAction、IsInRangeCondition 不会再跑！
```

> **SequenceNode 记忆性的价值**：攻击前摇期间，序列直接从 `RequestAttackAction` 续跑，不重新跑前面的范围检测。所以即使敌人在前摇 0.2 秒内稍微移动导致"瞬间出范围"，这一刀照样打出去——逻辑符合直觉。

---

#### 场景 D：攻击后摇/冷却期，回到追逐

```
帧 M+K（攻击打出，AttackState=Recovery/冷却期）：
  Selector i=0：攻击序列 _currentIndex=6，直接到 RequestAttackAction
    AttackState==Recovery → return Failure（冷却中不允许新攻击）
    → SequenceNode Failure，_currentIndex=0 复位
  Selector continue → i=1：追逐序列（之前被 Reset，_currentIndex=0）
    FindEnemyAction → Success
    HasValidTargetCondition → Success
    MoveToTargetAction → Running（重新追逐）
  Selector _currentIndex=1，return Running
```

---

#### 场景 E：低血逃跑树（BuildFleeingMeleeTree）

```text
SelectorNode("逃跑近战敌人")
├─[0] SequenceNode("逃跑序列")   → FleeBranch(30f)
│     ├─ IsLowHpCondition(30f)
│     ├─ FindEnemyAction
│     ├─ HasValidTargetCondition
│     └─ FleeFromTargetAction    ← 持续 Running（反向移动）
├─[1] AttackBranch
├─[2] ChaseBranch
└─[3] PatrolBranch
```

```
血量 > 30%（正常战斗帧）：
  i=0 逃跑序列：IsLowHpCondition → Failure（血量充足）→ 整体 Failure
  i=1 攻击序列：正常流程...

血量降至 25%（触发逃跑）：
  i=0 逃跑序列：
    IsLowHpCondition → Success（25% < 30%）
    FindEnemyAction → Success
    HasValidTargetCondition → Success
    FleeFromTargetAction → Running（持续逃离，每帧写反向移动方向）
    → 逃跑序列 Running，_currentIndex=3（在 FleeFromTargetAction）
  Selector _currentIndex=0，旧 _currentIndex 可能是 1/2/3
    → ResetChildrenExcept(0, ctx) → 清理攻击/追逐/巡逻分支

后续每帧（血量仍 < 30%）：
  i=0 逃跑序列 Running（_currentIndex=3，直接到 FleeFromTargetAction）
  → 攻击/追逐/巡逻完全不会被评估，因为 Selector 在 i=0 就返回了
```

> **SelectorNode 返回值的意义**：SelectorNode 向父节点（或 Runner）报告 Running/Success/Failure，但在同一颗树内，它的返回值不会影响兄弟分支——兄弟分支的执行与否完全由 for 循环的 `continue` 控制，与返回值无关。

### 3.5 Reset 机制：分支切换时的状态清理

当 SelectorNode 检测到活跃分支切换时，调用 `ResetChildrenExcept(newIndex, ctx)`：

```text
例：从巡逻(i=2)切换到追逐(i=1)
ResetChildrenExcept(1, ctx) 遍历：
  Children[0].Reset(ctx) → 攻击序列：_currentIndex=0，子节点逐个 Reset
  Children[2].Reset(ctx) → 巡逻序列：
      RandomWanderAction.Reset(ctx) → PatrolTargetPoint = Vector2.Zero（清黑板）
      WaitIdleAction.Reset(ctx)     → _timer?.Cancel()，PatrolWaitDone=false（清黑板+取消计时）
```

**为什么 Reset 需要 `AIContext ctx` 参数**（此项目特有设计）：

- 旧版 `Reset()` 无参数，只能清 C# 私有字段（`_timer`、`_currentIndex`）。
- `WaitIdleAction` 的定时器回调会异步写 `PatrolWaitDone=true`。若不取消定时器且不清除 Data 标志，下次进入 WaitIdleAction 第一帧就 `Done=true`，直接 Success 跳过等待。
- `RandomWanderAction` 若不清 `PatrolTargetPoint`，下次进入巡逻序列时，距离检测认为"已到达旧目标"，直接 Success，WaitIdleAction 立刻启动而不先移动。
- 有 `ctx` 的 Reset 能直接操作 `Entity.Data` 黑板，保证分支重进时状态干净。

### 3.6 AIContext — 节点的工具箱

每帧由 `AIComponent` 复用同一个实例（避免 new）：

- `ctx.Entity`：当前 AI 单位（唯一入口）
- 读状态：`ctx.Entity.Data.Get<T>(DataKey.xxx)`
- 写意图：`ctx.Entity.Data.Set(DataKey.xxx, value)`
- 发请求：`ctx.Entity.Events?.Emit(GameEventType.xxx, payload)`
- 计时：用 `TimerManager.Instance.Delay(t)`，禁止手动累加 delta

---

## 3.7 行为树运行器（`BehaviorTreeRunner`）

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

## 3.4 敌人行为树模块（`Src/ECS/AI/Nodes/`）

**职责分工**（两个文件）：

- `EnemyBehaviorBlocks.cs`：**积木块库**，定义所有可复用子树（`AttackBranch`、`ChaseBranch` 等）
- `EnemyBehaviorTreeBuilder.cs`：**预制树工厂**，仅调用积木块拼装完整预制树

```csharp
// 直接使用预制树
var tree = EnemyBehaviorTreeBuilder.BuildMeleeEnemyTree();

// 自由组合积木块
var tree = new SelectorNode("自定义Boss")
    .Add(EnemyBehaviorBlocks.FleeBranch(20f))
    .Add(EnemyBehaviorBlocks.SkillBranch("终极技能"))
    .Add(EnemyBehaviorBlocks.AttackBranch())
    .Add(EnemyBehaviorBlocks.ChaseBranch())
    .Add(EnemyBehaviorBlocks.PatrolBranch());
```

### 预制树（`EnemyBehaviorTreeBuilder`）

- `BuildMeleeEnemyTree()`：攻击 → 追逐 → 巡逻
- `BuildSkillMeleeTree(name)`：技能 → 攻击 → 追逐 → 巡逻
- `BuildFleeingMeleeTree(threshold?)`：逃跑 → 攻击 → 追逐 → 巡逻
- `BuildWandererTree()`：纯巡逻
- `BuildChaserTree()`：追逐 → 巡逻

### 积木块（`EnemyBehaviorBlocks`）

- `AttackBranch()`：FindEnemy → HasTarget → InRange → AttackReady → Stop → Face → Attack
- `SkillBranch(name, rangeKey?)`：FindEnemy → HasTarget → InRange → SkillReady → Stop → Face → Cast
- `FleeBranch(threshold?)`：LowHp → FindEnemy → HasTarget → FleeFrom
- `ChaseBranch()`：FindEnemy → HasTarget → MoveTo(AttackRange)
- `PatrolBranch()`：RandomWander → WaitIdle

### 原子条件节点（`Src/ECS/AI/Conditions/`）

- `HasValidTargetCondition`：校验 `DataKey.TargetNode` 存活且非 Dead/Reviving
- `IsInRangeCondition(dataKey)`：通用范围检测，传入不同 DataKey 可用于攻击/技能
- `IsAttackReadyCondition`：`DataKey.AttackState == Idle`
- `IsAbilityReadyCondition(name)`：技能冷却完毕或有充能可用
- `IsLowHpCondition(threshold)`：血量百分比低于阈值（0~100）

### 原子动作节点（`Src/ECS/AI/Actions/`）

- `FindEnemyAction`：`TargetSelector.Query` 索敌，含仇恨锁定/丢失机制
- `MoveToTargetAction(stopRangeKey?)`：向目标移动，到达 stopRangeKey 距离后返回 Success
- `RandomWanderAction(min, max)`：以当前位置为圆心，`[min,max]` 圆环内随机选点，到达返回 Success；**被打断时 Reset 清除 PatrolTargetPoint**
- `WaitIdleAction`：`TimerManager` 驱动，等待完成返回 Success；**被打断时 Reset 取消计时器并清除 PatrolWaitDone**
- `StopMovementAction`：停止移动（瞬时 Success）
- `FaceTargetAction`：面向目标（瞬时 Success）
- `FleeFromTargetAction`：反向全速逃离，持续 Running
- `RequestAttackAction`：发 `attack:requested`，前摇 Running，后摇/冷却 Failure
- `AutoCastAbilityAction(name)`：`TryTrigger` 流水线施法，成功 Success

---

## 3.5 移动执行组件（`EntityMovementComponent`）

AI 默认移动链路如下：

1. `AIComponent` 驱动行为树，写入 `DataKey.AIMoveDirection` 与 `DataKey.AIMoveSpeedMultiplier`
2. `EntityMovementComponent` 在注册时按 `DataKey.DefaultMoveMode` 进入默认策略
3. 默认策略为 `MoveMode.AIControlled` 时，`AIControlledStrategy` 读取 AI 意图并写入 `DataKey.Velocity`
4. `EntityMovementComponent` 在 `_PhysicsProcess` 中调用 `VelocityResolver.Resolve()` + `MoveAndSlide()`
5. 组件再根据速度更新朝向与翻转表现

临时轨迹移动不再由 AI 直接接管，而是通过 `GameEventType.Unit.MovementStarted`（开始/切换事件）切入其它策略；完成后由 `MovementCompleted` 回退到 `DefaultMoveMode`。
切换门禁规则为：当前模式等于 `DefaultMoveMode` 时允许直接切换，当前为非默认临时模式时需满足 `CanBeInterrupted=true`。

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

位于 `Data/DataKey/AI/DataKey_AI.cs`：

- 状态与目标：`AIState`、`TargetNode`、`AIEnabled`
- 感知：`DetectionRange`、`LoseTargetRange`
- 巡逻：`SpawnPosition`、`PatrolRadius`、`PatrolTargetPoint`、`PatrolWaitTime`、`PatrolWaitDone`
- 移动意图：`AIMoveDirection`、`AIMoveSpeedMultiplier`
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
    -> Selector(近战敌人)
      -> Sequence(攻击序列)
        -> FindEnemyAction    写入 DataKey.TargetNode
        -> HasValidTargetCondition   校验目标有效
        -> IsInRangeCondition        检查攻击范围
        -> IsAttackReadyCondition    检查冷却
        -> StopMovementAction        停步
        -> FaceTargetAction          面向目标
        -> RequestAttackAction       发 attack:requested
          -> AttackComponent 启动前摇/校验
            -> 命中时 ExecuteDamage -> DamageService.Process
            -> 结束发 attack:finished
          -> UnitAnimationComponent 响应动画事件
```

---

## 6. 扩展指南

### 6.1 新增敌人类型（搭积木）

```csharp
// 方式一：在 EnemyBehaviorTreeBuilder 添加预制树
public static BehaviorNode BuildRangedTree()
{
    return new SelectorNode("远程敌人")
        .Add(RangedAttackBranch())              // 在 EnemyBehaviorBlocks 新增积木块
        .Add(EnemyBehaviorBlocks.ChaseBranch()) // 复用已有积木块
        .Add(EnemyBehaviorBlocks.PatrolBranch());
}

// 方式二：AIComponent 直接按配置热切换树
_runner.SetTree(EnemyBehaviorTreeBuilder.BuildRangedTree());
```

### 6.2 新增积木块

1. 在 `EnemyBehaviorBlocks.cs` 中添加新的 `public static SequenceNode XxxBranch()` 方法
2. 积木块内部使用原子节点组合，不包含业务逻辑
3. 在 `EnemyBehaviorTreeBuilder` 或直接在代码中引用

### 6.3 新增原子节点

1. 在 `Src/ECS/AI/Conditions/` 或 `Src/ECS/AI/Actions/` 下新建类文件
2. 继承 `BehaviorNode`，实现 `Evaluate(AIContext ctx)`
3. 若节点有内部状态（计时/缓存），同时实现 `Reset(AIContext? ctx)` 并清理 Data 黑板
4. 共性频控需求用 `CooldownNode` 装饰器包装

### 6.3 增强调试能力

- 输出当前 Running 节点名与 `AIState`
- 在 Editor/Gizmo 中可视化 DetectionRange / LoseTargetRange / AttackRange

### 6.4 攻击表现增强

- 在 `attack:started/finished/cancelled` 上接 VFX/SFX/UI
- 继续保持"AI 发命令、组件执行"的边界

---

## 7. 维护约定

- 若新增原子节点，需更新 `Src/ECS/AI/README.md` 的节点速查表。
- 若修改行为树结构或节点返回语义，需同步更新本说明第 3.4 节。
- 若新增 AI 关键 DataKey/Event，需补充到本文第 4 节。
- 若更改攻击状态机时序（WindUp/Recovery/CD 语义），需验证：
  - `IsAttackReadyCondition` 判定是否仍正确
  - `RequestAttackAction` 的状态处理是否仍符合预期
