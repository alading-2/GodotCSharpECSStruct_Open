# Prefab 架构讨论总结

## 核心问题

1. **是否需要 Prefab 层？**
2. **是否应该合并 Player.cs 和 Enemy.cs 为 Unit.cs？**
3. **对象池应该实例化什么？**

---

## 1. 最初的设想（错误的）

**想法**：

- Prefab 搭积木，组装好组件（HealthComponent、VelocityComponent、Camera2D 等）
- Player 和 Enemy 都用 Unit.cs
- 通过配置区分行为

**致命问题**：

**问题 1：对象池实例化什么？**

```csharp
var pool = new ObjectPool<???>(
    () => ???.Instantiate(),  // Unit？Enemy？
    config
);
```

- 实例化 Unit → 如何区分 Player/Enemy？需要类型标记
- 实例化 Enemy → 那还需要 Enemy.tscn，Prefab 有什么意义？

**问题 2：GetParent() 层级混乱**

```
Enemy (Node) ← Prefab 容器
└── EnemyPrefab (CharacterBody2D)
    └── HealthComponent
```

- `HealthComponent.GetParent()` 返回 `EnemyPrefab`
- 但 Data 挂载在 `Enemy` 上
- 需要 `GetParent().GetParent()` 才能访问数据

**问题 3：Unit.cs 充斥 if-else**

```csharp
public override void _Process(double delta)
{
    if (Type == UnitType.Player)
        HandlePlayerInput();
    else if (Type == UnitType.Enemy)
        HandleEnemyAI();
    // 大量判断...
}
```

---

## 2. 为什么不能合并？（深度分析）

### 2.1 行为本质差异 > 数据共性

虽然 Player 和 Enemy 都有 HP、速度、位置，看起来数据结构相似，但它们的**行为本质**完全不同。在伪 ECS 架构中，我们应该根据“行为”而非“数据”来划分实体。

| 特性         | Player (玩家)                | Enemy (敌人)                   | 强行合并的后果 (Unit.cs)         |
| :----------- | :--------------------------- | :----------------------------- | :------------------------------- |
| **驱动方式** | 键盘/手柄输入 (InputManager) | 状态机/AI 逻辑 (\_Process)     | 充斥 `if (IsPlayer)` 的逻辑分支  |
| **生命周期** | 单例，随场景常驻             | 高频创建/销毁 (500+)，必须池化 | 复杂的池化与非池化判断逻辑       |
| **组件需求** | 需要 Camera2D, Inventory, XP | 不需要 Camera, 需要 LootTable  | 产生大量 null 检查或无用内存占用 |
| **数据流转** | 升级/装备改变属性            | 场景难度系数/波次改变属性      | 属性计算公式变得极其复杂         |

**结论**：这是“行为本质不同”，强行合并会导致上帝类（God Class）的产生，严重破坏单一职责原则。

### 2.2 Godot 的本质：Scene 就是 Prefab

Godot 的 design 哲学中，`.tscn` 文件本身就是功能完备的 Prefab。

- **可视化组装**：编辑器提供了直观的界面来挂载组件（Node）。
- **零成本实例化**：通过 `Instantiate()` 即可快速生成。
- **反模式警告**：在 Godot 之上再造一层 Prefab 系统（如 `UnitWrapper -> Enemy`）是在与引擎对抗，而非利用引擎。

---

## 3. 正确的架构：组件化复用

### 3.1 保持分离 + 组件复用

核心思想是：**复用的粒度应该是“组件”，而不是“实体”。**

```
Src/ECS/Entity/Unit/
├── Player.tscn + Player.cs      # 玩家逻辑（输入、升级、特有组件）
└── Enemy.tscn + Enemy.cs        # 敌人逻辑（AI、对象池、IPoolable）

共享逻辑通过组件实现：
├── HealthComponent              # 共有：生命值管理
└── VelocityComponent            # 共有：移动与物理同步
```

### 3.2 深度洞察：避免层级地狱与性能损耗

1. **简化数据访问**：如果不引入 Prefab 容器层，组件可以直接通过 `Owner.GetData()` 或 `GetParent().GetData()` 访问数据。多一层嵌套（如 `PrefabContainer -> Unit -> Component`）会导致访问路径变长，增加代码复杂度。
2. **物理层级清晰**：物理碰撞回调（Area2D/Body）会直接返回实体节点，无需通过 `GetParent()` 去寻找逻辑主体。
3. **性能优化**：在同屏 500+ 实体的 Survivor-like 场景下，减少 Node 树的深度可以显著降低 Transform 更新的开销。

---

## 4. 抽象建议：接口优于基类

如果某些系统（如“自动寻敌”、“伤害结算”）需要同时处理 Player 和 Enemy，请使用**接口**而不是**基类**：

```csharp
// 定义接口以支持跨类型操作
public interface ITargetable
{
    Vector2 GlobalPosition { get; }
    bool IsDead { get; }
    void TakeDamage(float amount);
}

// Player 和 Enemy 分别实现该接口
public partial class Player : CharacterBody2D, ITargetable { ... }
public partial class Enemy : CharacterBody2D, ITargetable, IPoolable { ... }
```

---

## 5. 对象池实例化什么？

**答案：实例化完整的、包含所有必要组件的具体实体场景（如 Enemy.tscn）。**

```csharp
// ObjectPoolInit.cs
var enemyScene = GD.Load<PackedScene>("res://Src/ECS/Entity/Unit/Enemy.tscn");
var pool = new ObjectPool<Enemy>(
    () => enemyScene.Instantiate<Enemy>(),
    config
);
```

---

## 6. 最终结论

**坚决不要引入额外的 Prefab 层，保持 Player.cs 和 Enemy.cs 的逻辑分离。**

理由：

1. **Godot 原生支持**：`.tscn` 本身就是最强大的 Prefab。
2. **单一职责**：分离确保了玩家和敌人逻辑的纯粹性，易于扩展。
3. **组件化复用**：通过 `HealthComponent` 等实现代码复用，而非通过继承。
4. **性能与维护性**：更浅的节点树意味着更好的性能和更低的数据访问复杂度。

---

**文档版本**：v1.1  
**更新日期**：2024-12-31  
**更新内容**：整合了关于行为差异、Godot 引擎特性及接口抽象的深度分析。
