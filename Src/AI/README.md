# Src/AI 说明文档（精简版）

> 本文聚焦 `Src/AI` 源码目录：**每个基类有什么用、什么时候用、怎么用**。

## 1. 目录结构

```text
Src/AI/
  Core/
    BehaviorNode.cs        # 节点基类 + NodeState
    CompositeNode.cs       # Sequence / Selector
    DecoratorNode.cs       # Inverter / AlwaysSucceed / Cooldown
    LeafNode.cs            # ConditionNode / ActionNode
    AIContext.cs           # Tick 上下文
    BehaviorTreeRunner.cs  # 行为树运行器
  Nodes/
    EnemyBehaviorTreeBuilder.cs  # 敌人行为树组装工厂
```

---

## 2. Core 基类速览（有什么用）

### 2.1 `BehaviorNode`（所有节点父类）

- **作用**：统一节点接口。
- **你要实现/调用的核心**：
  - `Evaluate(AIContext ctx)`：执行节点逻辑并返回 `NodeState`
  - `Reset()`：切分支或切树时清理运行态

### 2.2 `NodeState`

- `Success`：本节点本次完成
- `Failure`：本节点本次失败
- `Running`：跨帧执行，下一帧继续 Tick

### 2.3 `AIContext`

- **作用**：给节点传运行时信息。
- 常用字段：
  - `Entity`：当前单位
  - `Data`：共享数据容器（黑板）
  - `Events`：实体事件总线
  - `DeltaTime`：帧间隔

### 2.4 `SequenceNode` / `SelectorNode`

- `Sequence`（AND）：子节点按顺序跑，任何一步失败就失败。
- `Selector`（OR）：按优先级尝试，命中一个成功/运行即返回。
- **项目当前行为树核心模式**：`Selector(攻击 -> 追逐 -> 巡逻)`。

### 2.5 `ConditionNode` / `ActionNode`

- `ConditionNode`：做判断，只返回成败（不应做重逻辑）。
- `ActionNode`：做动作，可返回 `Running` 形成跨帧行为。

### 2.6 Decorator（装饰节点）

- `InverterNode`：反转成功与失败。
- `AlwaysSucceedNode`：吞掉失败，保持流程继续。
- `CooldownNode`：限制子节点成功后的再次触发频率。

### 2.7 `BehaviorTreeRunner`

- **作用**：行为树 Tick 驱动器。
- 能力：
  - `Tick(ctx)`：执行根节点
  - `Reset()`：重置整棵树
  - `SetTree(newRoot)`：热切换行为树

---

## 3. 怎么用（最小流程）

1. 用 `ConditionNode` / `ActionNode` 写叶子逻辑。  
2. 用 `SequenceNode` / `SelectorNode` 组合成树。  
3. 在组件中创建 `BehaviorTreeRunner(root)`。  
4. 每帧构建/复用 `AIContext`，调用 `Runner.Tick(ctx)`。  
5. 动作节点通过 `DataKey` 和 `Events` 发意图，不直接操控物理/动画。  

---

## 4. 项目内推荐写法（避免耦合）

- ✅ 推荐：
  - `ActionNode` 写 `DataKey.MoveDirection`、`DataKey.MoveSpeedMultiplier`
  - 发 `GameEventType.Attack.Requested`
- ❌ 不推荐：
  - 在行为树节点里直接改 `CharacterBody2D.Velocity`
  - 在行为树节点里直接操作 `AnimatedSprite2D`

执行层应交给：

- 移动：`EnemyMovementComponent`
- 攻击：`AttackComponent`
- 动画：`UnitAnimationComponent`

---

## 5. 示例：新增一个“技能攻击”分支

思路：

1. 新增 `ConditionNode("技能可用", IsSkillReady)`
2. 新增 `ActionNode("释放技能", CastSkill)`
3. 把该 Sequence 插到根 Selector 的“普通攻击”前面（更高优先级）
4. `CastSkill` 内仅发事件，不直接做技能效果

这样可以保持：**决策可扩展，执行可复用**。
