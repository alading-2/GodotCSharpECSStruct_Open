---
name: ecs-entity
description: 创建新 Entity、管理 Entity 生命周期（Spawn/Register/Destroy）、实现 IEntity 接口、配置对象池时使用。适用于：新建敌人/子弹/玩家/技能等实体，处理 Entity 的生成销毁，实现 IPoolable 接口。触发关键词：新建实体、Entity、IEntity、IPoolable、对象池生成、EntityManager.Spawn。
---

# ECS Entity 规范

## 核心原则
- **Scene 即 Entity**：`.tscn` 场景文件就是 Entity，实现 `IEntity` 接口
- **统一生命周期**：必须通过 `EntityManager.Spawn/Register/Destroy`，禁止直接 `new` 或 `QueueFree()`
- **两种类型**：对象池版（高频：Enemy/Bullet/Item）和非对象池版（低频：Player/Boss）

## VisualRoot / Collision 约定（2026-03）
- `EntityManager.Spawn` 会在组件注册前按 `VisualScenePath` 注入 `VisualRoot`
- 生成器产物允许携带 `VisualRoot/Collision` 配置节点，用于描述 `Body / Sensor / Pickup` 的 shape、layer、mask
- 真实参与物理的仍然是 Entity 根节点或组件节点；`Collision` 只是配置源，不直接承担物理职责
- Entity 根场景只保留 `CollisionShape2D` 占位节点，不在根场景硬编码碰撞 shape 资源
- 依赖视觉或碰撞配置的组件，可以假设 `OnComponentRegistered` 执行时 `VisualRoot` 已注入完成

## IEntity 接口实现（必须）

```csharp
public partial class MyEntity : CharacterBody2D, IEntity, IPoolable  // 对象池版加 IPoolable
{
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public MyEntity() { Data = new Data(this); }
}
```

## 生命周期 API

```csharp
// ✅ 生成（对象池）
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPos
});

// ✅ 生成（非对象池）
var player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
{
    Config = playerData,
    UsingObjectPool = false,
    Position = Vector2.Zero
});

// ✅ 注册已存在于场景中的 Entity（如编辑器直接放置的）
EntityManager.Register(this);

// ✅ 销毁（自动判断归还对象池还是 QueueFree）
EntityManager.Destroy(enemy);

// ✅ 查询组件
var health = EntityManager.GetComponent<HealthComponent>(entity);
```

## IPoolable 接口（对象池版必须实现）

```csharp
public void OnPoolAcquire()
{
    // 从池中取出时：订阅事件
    GlobalEventBus.Global.On<GameEventType.Unit.KilledEventData>(
        GameEventType.Unit.Killed, OnKilled);
    Events.On<GameEventType.Unit.DamagedEventData>(
        GameEventType.Unit.Damaged, OnDamaged);
}

public void OnPoolRelease()
{
    // 归还池时：取消全局事件订阅，重置物理状态
    GlobalEventBus.Global.Off<GameEventType.Unit.KilledEventData>(
        GameEventType.Unit.Killed, OnKilled);
    Velocity = Vector2.Zero;
}

public void OnPoolReset()
{
    // 数据重置（通常留空，Data 由 EntityManager 自动 Clear）
}
```

## 事件处理模式

```csharp
// 全局事件：必须筛选是否是自己
private void OnKilled(GameEventType.Unit.KilledEventData evt)
{
    if (evt.Victim as Node != this) return;
    // 处理自己被击杀
}

// 局部事件（Entity.Events）：无需筛选，天然只属于本实体
private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
{
    // 直接处理
}
```

## 禁止事项
- ❌ 直接 `new EnemyEntity()` 创建实体
- ❌ 直接 `entity.QueueFree()` 销毁实体
- ❌ 在 `_Ready()` 中订阅 Entity.Events（应在 `OnPoolAcquire` 或 `OnComponentRegistered`）
- ❌ 在 Entity 中存储业务状态字段（如 `private float _hp`）→ 必须存 Data

## 关键文件路径
- **标准模板**（新建 Entity 从这里复制）→ `Src/ECS/Entity/TemplateEntity.cs`
- **接口定义** → `Src/ECS/Entity/IEntity.cs`
- **开发规范** → `Src/ECS/Entity/Entity规范.md`
- **API 手册** → `Src/ECS/Entity/Core/EntityManager.md`
- **核心实现** → `Src/ECS/Entity/Core/EntityManager.cs`
- **关系管理** → `Src/ECS/Entity/Core/EntityRelationshipManager.cs`
- **架构设计** → `Docs/框架/ECS/Entity/Entity架构设计理念.md`
- **对象池接口** → `Src/Tools/ObjectPool/IPoolable.cs`
- **对象池初始化** → 搜索 `ObjectPoolInit.cs`
- **特效实体参考** → `Src/ECS/Entity/Effect/EffectEntity.cs`
- **特效服务入口** → `Src/ECS/System/EffectSystem/EffectTool.cs`
