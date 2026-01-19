# Entity 规范说明

## 概述

Entity 是 ECS 架构中的核心容器，代表一个游戏实体（玩家、敌人、子弹等）。在本项目的"伪 ECS"架构中，Entity 是 Godot Node，同时实现 `IEntity` 接口。

**核心文件参考**：
- [IEntity.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/IEntity.cs) - 接口定义
- [TemplateEntity.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/TemplateEntity.cs) - 标准模板

---

## IEntity 接口

每个 Entity 必须实现 `IEntity` 接口：

```csharp
public interface IEntity
{
    /// <summary>动态数据容器</summary>
    Data Data { get; }
    
    /// <summary>局部事件总线</summary>
    EventBus Events { get; }
    
    /// <summary>唯一标识符</summary>
    string EntityId { get; }
}
```

---

## Entity 设计原则

### 1. Entity 是纯容器 (Pure Container)

> [!IMPORTANT]
> **Entity 严禁包含业务逻辑**，它仅仅是一个"挂载点"：
> - **挂载数据**：持有 `Data` 容器
> - **挂载事件**：持有 `Events` 总线
> - **挂载组件**：作为 Component 的父节点
> - **提供标识**：提供 `EntityId` 和 `FactionId`

**核心准则**：
- 如果你在 Entity 类中写了 `_Process` 或 `_PhysicsProcess`，**你错了**。请移至 Component。
- 如果你在 Entity 类中写了 `TakeDamage()` 方法，**你错了**。请移至 HealthComponent 或 DamageSystem。
- Entity 类文件行数通常不超过 **50 行**。

**业务逻辑归属**：
- ❌ 死亡逻辑 → ✅ 放在 `LifecycleComponent`
- ❌ 移动逻辑 → ✅ 放在 `VelocityComponent`
- ❌ 攻击逻辑 → ✅ 放在 `AttackComponent` 或对应 System

### 2. 统一使用 EntityManager

> [!IMPORTANT]
> 所有 Entity 操作必须通过 `EntityManager` 进行

```csharp
// ✅ 正确：使用 EntityManager
var enemy = EntityManager.Spawn<Enemy>(config);
EntityManager.Destroy(enemy);
var health = EntityManager.GetComponent<HealthComponent>(enemy);

// ❌ 错误：直接操作节点树
entity.QueueFree();
entity.GetNode<HealthComponent>("HealthComponent");
```

### 3. 生命周期管理

**统一管理原则**:
- 无论是对象池 Entity 还是普通 Entity,**统一使用 `EntityManager.Spawn` 创建,使用 `EntityManager.Destroy` 销毁**。
- `EntityManager` 会根据配置自动处理对象池归还或节点销毁。

**对象池 Entity** (Enemy、Bullet 等高频对象):
- 实现 `IPoolable` 接口
- 配置 `UsingObjectPool = true`
- 销毁时自动归还对象池

**非对象池 Entity** (Player、Boss 等单例):
- 无需实现 `IPoolable`
- 配置 `UsingObjectPool = false`
- 销毁时自动执行 `QueueFree`并注销

### 4. Spawn 后的数据设置

> [!TIP]
> **推荐模式: Spawn -> Configure -> Use**

由于 `OnComponentRegistered` 早于外部数据设置执行,请遵循以下模式初始化 Entity:

```csharp
// 1. Spawn (注入基础配置)
var enemy = EntityManager.Spawn<Enemy>(config);

// 2. Configure (设置运行时数据)
// 这些操作会触发 PropertyChanged 事件,激活 Component 的逻辑
enemy.Data.Set(DataKey.SkillLevel, 10);
enemy.Data.Set(DataKey.Summoner, this);
enemy.Data.Set(DataKey.TargetPosition, targetPos);
```

### 5. 事件订阅

**Entity 使用局部事件总线 (`Entity.Events`)**:
- `Entity.Events` 是每个 Entity 的局部事件总线,用于实体内部的组件间通信
- 事件订阅在 `OnPoolAcquire` 中进行(对象池 Entity)
- 无需手动解绑,`EntityManager.Destroy` 会自动调用 `Events.Clear()`

```csharp
// ✅ 在 OnPoolAcquire 中订阅事件
public void OnPoolAcquire()
{
    // 订阅死亡事件
    Events.On<GameEventType.Unit.DeadEventData>(
        GameEventType.Unit.Dead, OnDied);
    
    // 订阅受伤事件
    Events.On<GameEventType.Unit.DamagedEventData>(
        GameEventType.Unit.Damaged, OnDamaged);
}

private void OnDied(GameEventType.Unit.DeadEventData evt)
{
    _log.Info($"{Name} 死亡");
    // 业务逻辑...
}

private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
{
    _log.Info($"{Name} 受到 {evt.Amount} 点伤害");
}

// ✅ 无需手动解绑(EntityManager.Destroy 自动调用 Events.Clear())
```

> [!IMPORTANT]
> **EventBus 自动防止死循环**: 当事件正在执行时,同类型的事件不会再次触发,避免事件死循环

---

## 标准模板

参考 [TemplateEntity.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/TemplateEntity.cs) 创建新 Entity。

```csharp
public partial class MyEntity : CharacterBody2D, IEntity, IPoolable
{
    private static readonly Log _log = new("MyEntity");

    // ================= IEntity 实现 =================

    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();
    public string EntityId { get; private set; } = string.Empty;

    public MyEntity()
    {
        Data = new Data(this);
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();
    }

    // ================= IPoolable 接口 =================

    public void OnPoolAcquire()
    {
        // ✅ 订阅事件(EntityManager已自动清空)
        Events.On<GameEventType.Unit.DeadEventData>(
            GameEventType.Unit.Dead, OnDied);
    }

    public void OnPoolRelease()
    {
        // ✅ 重置自身状态(非Data管理的)
        Velocity = Vector2.Zero;
    }

    public void OnPoolReset() { }

    // ================= 事件处理 =================

    private void OnDied(GameEventType.Unit.DeadEventData evt)
    {
        _log.Info($"{Name} 死亡");
        // 业务逻辑...
    }
}
```

---

## 更新记录

### 2026-01-12
- ✅ 创建 Entity 规范文档
- ✅ 明确 Entity 是纯容器，业务逻辑归 Component
- ✅ 强调统一使用 EntityManager
