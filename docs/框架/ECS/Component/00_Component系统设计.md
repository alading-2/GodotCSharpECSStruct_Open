# Component 系统设计 - 伪 ECS 架构

## 📋 核心理念

### Component 在伪 ECS 中的职责

```
┌─────────────────────────────────────────────────────┐
│  Component 的职责边界                                │
├─────────────────────────────────────────────────────┤
│  ✅ 应该做的：                                       │
│  - 封装单一、原子化的功能（Health、Velocity）       │
│  - 管理自己的状态和数据                              │
│  - 通过 C# Event 对外发布状态变化                   │
│  - 从父节点的 Data 容器读取配置                     │
│  - 提供清晰的公开接口供 Entity 调用                 │
│                                                      │
│  ❌ 不应该做的：                                     │
│  - 直接引用其他 Component（通过 Entity 协调）       │
│  - 实现复杂的游戏逻辑（应该在 Entity 或 System）    │
│  - 直接操作 UI（通过 Event 通知）                   │
│  - 持有对 System 的引用（通过 EventBus）            │
└─────────────────────────────────────────────────────┘
```

---

## 🎮 Roguelike 割草游戏的 Component 需求分析

### 游戏特征

- **高密度战斗**：同屏 500+ 敌人
- **快节奏**：持续移动 + 自动攻击
- **成长系统**：经验升级 + 属性强化
- **简单操作**：仅移动，武器自动攻击

### 核心玩法循环

```
移动躲避 → 武器自动攻击 → 击杀敌人 → 获得经验 → 升级选择 → 变强 → 循环
```

---

## 📦 必需 Component 列表

### 1. 核心战斗组件（优先级：⭐⭐⭐⭐⭐）

| Component         | 职责         | 适用对象         |
| :---------------- | :----------- | :--------------- |
| HealthComponent   | 生命值管理   | 玩家、敌人、建筑 |
| HitboxComponent   | 攻击判定区域 | 武器、子弹、敌人 |
| HurtboxComponent  | 受击判定区域 | 玩家、敌人       |
| VelocityComponent | 物理移动控制 | 玩家、敌人、子弹 |

### 2. AI 与行为组件（优先级：⭐⭐⭐⭐）

| Component       | 职责                 | 适用对象     |
| :-------------- | :------------------- | :----------- |
| FollowComponent | 跟随目标（敌人 AI）  | 敌人         |
| PickupComponent | 拾取物品（磁铁效果） | 经验球、金币 |

### 3. 武器与攻击组件（优先级：⭐⭐⭐⭐）

| Component           | 职责                     | 适用对象   |
| :------------------ | :----------------------- | :--------- |
| WeaponComponent     | 武器控制（冷却、发射）   | 武器       |
| ProjectileComponent | 投射物行为（飞行、碰撞） | 子弹、飞刀 |

### 4. 视觉反馈组件（优先级：⭐⭐⭐）

| Component         | 职责               | 适用对象   |
| :---------------- | :----------------- | :--------- |
| FlashComponent    | 受击闪烁效果       | 玩家、敌人 |
| LifetimeComponent | 自动销毁（定时器） | 子弹、特效 |

### 5. 可选扩展组件（优先级：⭐⭐）

| Component           | 职责       | 适用对象   |
| :------------------ | :--------- | :--------- |
| KnockbackComponent  | 击退效果   | 玩家、敌人 |
| InvincibleComponent | 无敌帧     | 玩家       |
| LootComponent       | 掉落物生成 | 敌人       |

---

## 🏗️ Component 设计模式

### 模式 1：纯数据组件（推荐用于简单状态）

```csharp
/// <summary>
/// 纯数据组件 - 仅存储状态，无逻辑
/// </summary>
public partial class LifetimeComponent : Node
{
    [Export] public float Lifetime { get; set; } = 5.0f;

    private float _timer = 0;

    public override void _Process(double delta)
    {
        _timer += (float)delta;
        if (_timer >= Lifetime)
        {
            GetParent().QueueFree();
        }
    }
}
```

**特点**：

- ✅ 简单直接
- ✅ 易于理解
- ✅ 适合独立功能

---

### 模式 2：事件驱动组件（推荐用于核心功能）

```csharp
/// <summary>
/// 事件驱动组件 - 通过 Event 通知状态变化
/// </summary>
public partial class HealthComponent : Node
{
    // 事件定义
    public event Action<float>? Damaged;
    public event Action<float>? Healed;
    public event Action? Died;

    // 状态
    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }

    // 方法
    public void TakeDamage(float damage)
    {
        CurrentHp -= damage;
        Damaged?.Invoke(damage);

        if (CurrentHp <= 0)
        {
            Died?.Invoke();
        }
    }
}
```

**特点**：

- ✅ 解耦设计
- ✅ 易于扩展
- ✅ 适合核心系统

---

### 模式 3：Data 驱动组件（推荐用于可配置功能）

```csharp
/// <summary>
/// Data 驱动组件 - 从父节点 Data 容器读取配置
/// </summary>
public partial class VelocityComponent : Node
{
    private Data _data;

    // 从 Data 读取配置
    public float Speed => _data.Get<float>("Speed", 400f);
    public float MaxSpeed => _data.Get<float>("MaxSpeed", 1000f);
    public float Acceleration => _data.Get<float>("Acceleration", 10.0f);

    public override void _Ready()
    {
        _data = GetParent().GetData();
    }
}
```

**特点**：

- ✅ 运行时可调整
- ✅ 支持 Buff/Debuff
- ✅ 适合属性系统

---

## 🎯 Component 设计原则

### 1. 单一职责原则

```csharp
// ❌ 不好：一个组件做太多事
public class PlayerComponent : Node
{
    public void Move() { }
    public void Attack() { }
    public void TakeDamage() { }
    public void Heal() { }
}

// ✅ 好：每个组件只负责一件事
public class VelocityComponent : Node { }
public class HealthComponent : Node { }
public class WeaponComponent : Node { }
```

### 2. 组件间通过 Entity 协调

```csharp
// ❌ 不好：组件直接引用其他组件
public class HurtboxComponent : Area2D
{
    private VelocityComponent _velocity;  // 不要这样
}

// ✅ 好：通过 Entity 协调
public class Player : CharacterBody2D
{
    private HealthComponent _health;
    private VelocityComponent _velocity;

    private void OnHit()
    {
        _health.TakeDamage(10);
        _velocity.Stop();  // Entity 协调两个组件
    }
}
```

### 3. 使用 Event 而非直接调用

```csharp
// ✅ 组件定义事件
public class HealthComponent : Node
{
    public event Action? Died;
}

// ✅ Entity 监听事件
public class Enemy : CharacterBody2D
{
    public override void _Ready()
    {
        _health.Died += OnDied;
    }

    private void OnDied()
    {
        // 处理死亡逻辑
        EventBus.EnemyDied?.Invoke(this, GlobalPosition);
    }
}
```

### 4. 避免在 Component 中实现游戏逻辑

```csharp
// ❌ 不好：在 Component 中实现复杂逻辑
public class HealthComponent : Node
{
    public void TakeDamage(float damage)
    {
        CurrentHp -= damage;

        // ❌ 不要在这里实现游戏逻辑
        if (CurrentHp <= 0)
        {
            SpawnLoot();  // 应该在 Entity 或 System 中
            PlayDeathAnimation();  // 应该在 Entity 中
            UpdateUI();  // 应该通过 Event
        }
    }
}

// ✅ 好：Component 只负责状态管理
public class HealthComponent : Node
{
    public event Action? Died;

    public void TakeDamage(float damage)
    {
        CurrentHp -= damage;

        if (CurrentHp <= 0)
        {
            Died?.Invoke();  // 只发布事件
        }
    }
}
```

---

## 📊 Component 性能优化

### 1. 缓存引用

```csharp
// ✅ 在 _Ready 中缓存
private CharacterBody2D _parent;

public override void _Ready()
{
    _parent = GetParent<CharacterBody2D>();
}

public override void _Process(double delta)
{
    _parent.Velocity = Velocity;  // 使用缓存
}
```

### 2. 避免频繁的 Data 查询

```csharp
// ❌ 不好：每帧查询
public override void _Process(double delta)
{
    float speed = _data.Get<float>("Speed", 400f);  // 每帧查询
}

// ✅ 好：缓存或使用属性
public float Speed => _data.Get<float>("Speed", 400f);  // 属性访问

public override void _Process(double delta)
{
    float speed = Speed;  // 只查询一次
}
```

### 3. 使用 ProcessMode 控制更新

```csharp
public override void _Ready()
{
    // 不需要每帧更新的组件
    ProcessMode = ProcessModeEnum.Disabled;
}
```

---

## 🔧 Component 测试策略

### 单元测试示例

```csharp
[TestFixture]
public class HealthComponentTests
{
    [Test]
    public void TakeDamage_ReducesHealth()
    {
        // Arrange
        var health = new HealthComponent();
        health.MaxHp = 100;
        health._Ready();

        // Act
        health.TakeDamage(30);

        // Assert
        Assert.AreEqual(70, health.CurrentHp);
    }

    [Test]
    public void TakeDamage_TriggersDeathEvent()
    {
        // Arrange
        var health = new HealthComponent();
        health.MaxHp = 100;
        health._Ready();

        bool diedTriggered = false;
        health.Died += () => diedTriggered = true;

        // Act
        health.TakeDamage(100);

        // Assert
        Assert.IsTrue(diedTriggered);
    }
}
```

---

## 📂 目录结构

```
Src/ECS/Component/
├── HealthComponent/
│   ├── HealthComponent.cs
│   ├── HealthComponent.tscn
│   └── HealthComponent.md
│
├── VelocityComponent/
│   ├── VelocityComponent.cs
│   ├── VelocityComponent.tscn
│   └── VelocityComponent.md
│
├── HitboxComponent/
│   ├── HitboxComponent.cs
│   ├── HitboxComponent.tscn
│   └── HitboxComponent.md
│
└── ...
```

---

## 📝 下一步

阅读各个 Component 的详细设计文档：

1. [VelocityComponent - 移动组件](./01_VelocityComponent.md) ✅
2. [HealthComponent - 生命值组件](./02_HealthComponent.md)
3. [HitboxComponent - 攻击判定组件](./03_HitboxComponent.md)
4. [HurtboxComponent - 受击判定组件](./04_HurtboxComponent.md)
5. [FollowComponent - 跟随组件](./05_FollowComponent.md)
6. [PickupComponent - 拾取组件](./06_PickupComponent.md)
7. [WeaponComponent - 武器组件](./07_WeaponComponent.md)
8. [ProjectileComponent - 投射物组件](./08_ProjectileComponent.md)
9. [FlashComponent - 闪烁组件](./09_FlashComponent.md)
10. [LifetimeComponent - 生命周期组件](./10_LifetimeComponent.md)

---

**文档版本**: v1.0  
**最后更新**: 2025-12-25  
**作者**: 架构设计团队
