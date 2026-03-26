# Src/AI 说明文档

> 本文聚焦 `Src/AI` 源码目录：**架构设计、每个模块有什么用、怎么用**。

## 1. 目录结构

```text
Src/AI/
  Core/                                    # 行为树通用框架（不含业务逻辑）
    BehaviorNode.cs                        #   节点基类 + NodeState 枚举
    CompositeNode.cs                       #   Sequence / Selector
    DecoratorNode.cs                       #   Inverter / AlwaysSucceed / Cooldown
    LeafNode.cs                            #   ConditionNode / ActionNode（委托式叶子）
    AIContext.cs                           #   Tick 上下文
    BehaviorTreeRunner.cs                  #   行为树运行器
  Conditions/                              # 原子条件节点（每个文件一个条件）
    HasValidTargetCondition.cs             #   目标是否存活且有效
    IsInRangeCondition.cs                  #   目标是否在指定范围内（通用）
    IsAttackReadyCondition.cs              #   攻击冷却是否就绪
    IsAbilityReadyCondition.cs             #   技能冷却/充能是否就绪
    IsLowHpCondition.cs                    #   生命值是否低于阈值
  Actions/                                 # 原子动作节点（每个文件一个动作）
    Movement/
      MoveToTargetAction.cs                #   向目标移动（可配停止距离）
      RandomWanderAction.cs                #   圆环随机选点游荡
      WaitIdleAction.cs                    #   原地等待
      StopMovementAction.cs                #   停止移动
      FaceTargetAction.cs                  #   面向目标
      FleeFromTargetAction.cs              #   逃离目标（反向移动）
    Combat/
      FindEnemyAction.cs                   #   索敌（TargetSelector 查询）
      RequestAttackAction.cs               #   发起攻击请求（事件驱动）
      AutoCastAbilityAction.cs             #   自动施放指定技能（TryTrigger）
  Nodes/                                   # 行为树组装器（纯积木拼装，无业务逻辑）
    EnemyBehaviorTreeBuilder.cs            #   敌人行为树工厂
```

### 设计原则

- **原子节点**：每个 Condition / Action 只做一件事，独立文件、可复用。
- **积木组装**：Builder 只负责将原子节点组合成树，不含业务代码。
- **意图驱动**：Action 节点仅通过 `DataKey` 和 `EventBus` 表达意图，不直接操作物理/动画。

---

## 2. Core 基类怎么用（重点）

很多人第一次看 `Src/AI/Core` 会觉得“太学术”。
你可以把它想成一个**导演系统**：

- `BehaviorNode` = 演员要执行的一条指令
- `Sequence` = 按剧本顺序演（前一步失败就卡住）
- `Selector` = 先尝试高优方案，不行就换下一套方案
- `Runner` = 每帧喊“Action!”的人

下面不讲抽象定义，直接讲“你写新 AI 时怎么用”。

### 2.1 `BehaviorNode`：你真正要实现的接口

你只需要记两件事：

1. `Evaluate(AIContext ctx)`：本帧执行逻辑，返回状态
2. `Reset()`：节点被打断时，清掉内部运行态

返回值怎么选：

- `Success`：这步已经完成，可以走下一步
- `Failure`：这步做不了，交给上层切分支
- `Running`：这步没做完，下帧继续

### 2.2 `AIContext`：节点的“工具箱”

每个节点都从 `ctx` 取数据：

- `ctx.Entity`：我是谁（唯一入口）

当前实现中，`AIContext` 只保留 `Entity`，不再重复缓存 `Data/Events/DeltaTime`。

实战口诀：

- **读状态** → `ctx.Entity.Data.Get<T>(DataKey.xxx)`
- **写意图** → `ctx.Entity.Data.Set(DataKey.xxx, value)`
- **发命令** → `ctx.Entity.Events.Emit(...)`
- **做计时** → 优先 `TimerManager`，不要在节点里手动累加 `delta`

### 2.3 `SequenceNode` 和 `SelectorNode`：最常用组合

#### `SequenceNode`（串行）

适合“必须按顺序完成”的链路，例如：

`先看到目标 -> 再进攻击范围 -> 再发起攻击`

任一步失败就整条失败。

#### `SelectorNode`（优先级）

适合“多套方案择一”，例如：

`能攻击就攻击 -> 不能攻击就追逐 -> 再不行就巡逻`

每帧从高优先级开始检查，天然适合敌人 AI。

### 2.4 `ConditionNode` / `ActionNode`：什么时候用委托式叶子

Core 里有委托版叶子节点：

- `ConditionNode("名字", ctx => bool)`
- `ActionNode("名字", ctx => NodeState)`

它适合：

- 快速原型
- 一次性小逻辑

不适合：

- 需要复用
- 逻辑超过 20~30 行
- 需要独立测试

所以项目本次重构后，主流程都改成**独立类原子节点**（更像积木）。

### 2.5 装饰节点（Decorator）怎么下手

- `InverterNode`：把“失败”变“成功”（反之亦然）
  - 用途：`看不到目标` 这种反条件
- `AlwaysSucceedNode`：即使子节点失败也继续
  - 用途：记录日志、播放非关键表现，不阻塞主流程
- `CooldownNode`：限制触发频率
  - 用途：避免每帧触发重动作

### 2.6 `BehaviorTreeRunner`：组件层正确接法

在组件里通常这么用：

1. 初始化一次：`Runner = new BehaviorTreeRunner(root)`
2. 每帧复用 `AIContext` 后调用：`Runner.Tick(ctx)`
3. 切树（职业切换/Boss阶段切换）：`Runner.SetTree(newRoot)`
4. 销毁时：`Runner.Reset()`

### 2.7 一个“能跑”的最小例子（可直接套）

```csharp
// 逻辑：有目标就追，没有目标就站立
var tree = new SelectorNode("简单AI")
    .Add(new SequenceNode("追逐")
        .Add(new HasValidTargetCondition())
        .Add(new MoveToTargetAction())
    )
    .Add(new StopMovementAction());
```

如果你是新同学，建议按这个顺序学习：

1. 先只看 `Selector + Sequence + NodeState`
2. 再看 `AIContext` 怎么读写 Data
3. 最后看装饰节点和热切树

### 2.8 常见误区（本项目强约束）

- ❌ 在 AI 节点直接改 `CharacterBody2D.Velocity`
- ❌ 在 AI 节点直接操作动画节点
- ❌ 条件节点里做副作用（例如索敌 + 改状态）

推荐做法：

- ✅ AI 只写意图和事件
- ✅ 执行交给 Movement/Attack/Animation 组件
- ✅ 条件纯查询，动作纯执行

---

## 3. 原子节点速查

### 3.1 条件节点（Conditions）

| 节点 | 作用 | 返回 |
|------|------|------|
| `HasValidTargetCondition` | 目标存活且有效？ | Success/Failure |
| `IsInRangeCondition(dataKey)` | 目标在指定范围内？ | Success/Failure |
| `IsAttackReadyCondition` | 攻击冷却就绪？ | Success/Failure |
| `IsAbilityReadyCondition(name)` | 技能冷却/充能就绪？ | Success/Failure |
| `IsLowHpCondition(threshold)` | 血量低于阈值（0~100）？ | Success/Failure |

### 3.2 动作节点（Actions）

| 节点 | 作用 | 返回 |
|------|------|------|
| `FindEnemyAction` | 索敌（含仇恨锁定） | Success/Failure |
| `MoveToTargetAction(stopRangeKey?)` | 向目标移动，传入 stopRangeKey 可在指定距离停步 | Running/Success/Failure |
| `RandomWanderAction(min,max)` | 以当前位置为圆心在圆环内随机选点游荡 | Running/Success/Failure |
| `WaitIdleAction` | 原地等待（TimerManager 驱动） | Running/Success |
| `StopMovementAction` | 停止移动 | Success |
| `FaceTargetAction` | 面向目标 | Success/Failure |
| `FleeFromTargetAction` | 逃离目标（反向全速移动） | Running/Failure |
| `RequestAttackAction` | 发起普通攻击事件 | Running/Failure |
| `AutoCastAbilityAction(name)` | 自动施放指定技能（走 TryTrigger 流水线） | Success/Failure |

---

## 4. 搭积木：怎么组装敌人 AI

### 4.1 使用预制树

```csharp
// 标准近战怪：攻击 + 追逐 + 巡逻
var tree = EnemyBehaviorTreeBuilder.BuildMeleeEnemyTree();

// 纯巡逻怪：只会随机游荡
var tree = EnemyBehaviorTreeBuilder.BuildWandererTree();

// 纯追逐怪：追到就停，不攻击
var tree = EnemyBehaviorTreeBuilder.BuildChaserTree();

// 技能近战怪：技能优先 → 普通攻击 → 追逐 → 巡逻
var tree = EnemyBehaviorTreeBuilder.BuildSkillMeleeTree("火球术");

// 逃跑近战怪：低于 30% 血量逃跑 → 普通攻击 → 追逐 → 巡逻
var tree = EnemyBehaviorTreeBuilder.BuildFleeingMeleeTree(30f);
```

### 4.2 使用子树积木块自由组合

```csharp
// 自定义：追逐 + 巡逻（不攻击）
var tree = new SelectorNode("自定义")
    .Add(EnemyBehaviorTreeBuilder.ChaseBranch())
    .Add(EnemyBehaviorTreeBuilder.PatrolBranch());

// 自定义：只攻击不追逐（炮台型）
var tree = new SelectorNode("炮台")
    .Add(EnemyBehaviorTreeBuilder.AttackBranch())
    .Add(new WaitIdleAction());

// 自定义：技能 + 攻击 + 逃跑 + 追逐 + 巡逻（Boss 复杂树）
var tree = new SelectorNode("Boss")
    .Add(EnemyBehaviorTreeBuilder.FleeBranch(20f))           // 低血优先逃
    .Add(EnemyBehaviorTreeBuilder.SkillBranch("终极技能"))    // 技能次之
    .Add(EnemyBehaviorTreeBuilder.AttackBranch())             // 普攻兜底
    .Add(EnemyBehaviorTreeBuilder.ChaseBranch())
    .Add(EnemyBehaviorTreeBuilder.PatrolBranch());
```

### 4.3 新增原子节点扩展

1. 在 `Src/AI/Conditions/` 或 `Src/AI/Actions/` 下新建类文件
2. 继承 `BehaviorNode`，实现 `Evaluate(AIContext ctx)`
3. 在 Builder 中组合使用

---

## 5. 解耦约束

- ✅ 推荐：
  - Action 节点写 `DataKey.AIMoveDirection`、`DataKey.AIMoveSpeedMultiplier`
  - 发 `GameEventType.Attack.Requested` 事件
- ❌ 禁止：
  - 在行为树节点里直接改 `CharacterBody2D.Velocity`
  - 在行为树节点里直接操作 `AnimatedSprite2D`

执行层交给：

- 移动：`EntityMovementComponent`（默认使用 `DefaultMoveMode = AIControlled`，AI 只写 `AIMoveDirection` / `AIMoveSpeedMultiplier`；临时轨迹通过 `MovementStarted` 切换）
- 攻击：`AttackComponent`
- 动画：`UnitAnimationComponent`

---

## 6. 示例：给敌人加技能 / 低血逃跑

### 6.1 带自动施法的敌人

前提：已通过 `EntityManager.AddAbility(enemy, config)` 给敌人添加了名为 "火球术" 的技能。

```csharp
// 方式一：直接用预制树
var tree = EnemyBehaviorTreeBuilder.BuildSkillMeleeTree("火球术");

// 方式二：自定义优先级（技能范围与攻击范围不同时）
var tree = new SelectorNode("法术近战")
    .Add(EnemyBehaviorTreeBuilder.SkillBranch("火球术", DataKey.AbilityRange))
    .Add(EnemyBehaviorTreeBuilder.AttackBranch())
    .Add(EnemyBehaviorTreeBuilder.ChaseBranch())
    .Add(EnemyBehaviorTreeBuilder.PatrolBranch());
```

### 6.2 低血量逃跑的敌人

```csharp
// 血量低于 25% 开始逃跑
var tree = EnemyBehaviorTreeBuilder.BuildFleeingMeleeTree(25f);
```

### 6.3 新增自定义原子节点

1. 在 `Src/AI/Conditions/` 或 `Src/AI/Actions/` 下新建类文件
2. 继承 `BehaviorNode`，实现 `Evaluate(AIContext ctx)`
3. 在 `EnemyBehaviorTreeBuilder` 中添加积木块方法组合使用
