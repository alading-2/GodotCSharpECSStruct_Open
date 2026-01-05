# Entity 系统 - 使用指南

**文档类型**：API 文档 + 使用指南  
**目标受众**：开发者  
**最后更新**：2026-01-04

---

## 概述

Entity 系统提供统一的实体生命周期管理和数据访问接口。核心设计理念是 **Scene 即 Entity**，每个游戏对象都是独立的 `.tscn` 场景文件。

**核心组件**：

- **EntityManager**：统一入口，管理 Entity 和 Component 的生命周期（生成、注册、查询、销毁）
- **IEntity 接口**：标记接口，为 Node 提供 `Data` 容器和 `EntityId` 属性
- **IComponent 接口**：标记接口，提供注册/注销回调
- **Data 容器**：动态数据存储，支持运行时属性管理

**设计理念**：详见 [`Docs/框架/ECS/Entity/Entity架构设计理念.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/Entity/Entity架构设计理念.md)

---

## 快速开始

### 1. 创建 Entity

#### 1.1 实现 IEntity 接口

```csharp
using Godot;

public partial class Enemy : CharacterBody2D, IEntity
{
    private static readonly Log _log = new("Enemy");

    // ================= IEntity 实现 =================

    /// <summary>
    /// 动态数据容器 - 存储运行时数据
    /// </summary>
    public Data Data { get; private set; } = new Data();

    /// <summary>
    /// Entity 唯一标识符
    /// </summary>
    public string EntityId { get; private set; } = string.Empty;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        // 初始化 EntityId
        EntityId = GetInstanceId().ToString();
    }

    public override void _ExitTree()
    {
        // 必须：清理 Data，避免内存泄漏
        Data.Clear();
        base._ExitTree();
    }
}
```

#### 1.2 使用 EntityManager 生成 Entity

```csharp
// 场景：SpawnSystem 生成敌人

// 1. 带位置（最常用）
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", enemyResource, spawnPosition);

// 2. 无位置（Buff、纯逻辑实体）
var buff = EntityManager.Spawn<Buff>("BuffPool", buffResource);

// 3. 带位置和方向（子弹、投射物）
var bullet = EntityManager.Spawn<Bullet>("BulletPool", bulletResource, position, rotation);
```

**自动化操作**：

- ✅ 从对象池获取实例
- ✅ 将 Resource 数据自动注入到 `Data` 容器
- ✅ 加载 VisualScene（如果配置了）
- ✅ 注册所有 Component 并建立关系
- ✅ 注册 Entity 到 EntityManager

### 2. 创建 Component

#### 2.1 实现 IComponent 接口（推荐）

```csharp
using Godot;

/// <summary>
/// 生命值组件 - 管理实体的生命值逻辑
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // 缓存 Entity 引用
    private IEntity? _entity;

    // 事件
    public event Action<float>? Damaged;
    public event Action? Died;

    // ================= IComponent 接口实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _log.Debug($"注册到 Entity: {entity.Name}");
        }
    }

    public void OnComponentUnregistered()
    {
        Damaged = null;
        Died = null;
    }

    // ================= 业务逻辑 =================

    public override void _Ready()
    {
        // 懒加载：如果 OnComponentRegistered 未被调用
        if (_entity == null)
        {
            var entity = EntityManager.GetEntityByComponent(this);
            if (entity is IEntity iEntity)
            {
                _entity = iEntity;
            }
        }
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_entity == null) return;

        var data = _entity.Data;
        float currentHp = data.Get<float>(DataKey.CurrentHp);
        currentHp -= amount;
        data.Set(DataKey.CurrentHp, currentHp);

        Damaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 重置逻辑（对象池复用时）
    /// </summary>
    public void Reset()
    {
        if (_entity == null) return;

        var data = _entity.Data;
        float maxHp = data.Get<float>(DataKey.MaxHp, 10f);
        data.Set(DataKey.CurrentHp, maxHp);
    }
}
```

#### 2.2 Component 访问 Entity 的方式

> [!IMPORTANT] > **禁止使用 `GetParent()`** 获取 Entity！所有 Component 必须通过 EntityManager 访问 Entity。

```csharp
// 方式 1：通过 IComponent 回调缓存 Data（推荐，性能最佳）
private Data? _data;

public void OnComponentRegistered(Node entity)
{
    if (entity is IEntity iEntity)
    {
        _data = iEntity.Data;  // 缓存 Data 引用
    }
}

// 方式 2：在 _Ready 中通过 EntityManager 获取（懒加载）
public override void _Ready()
{
    if (_data == null)
    {
        _data = EntityManager.GetEntityData(this);
    }
}

// 方式 3：需要完整 Entity 引用时
var entity = EntityManager.GetEntityByComponent(this);
if (entity is IEntity iEntity)
{
    var data = iEntity.Data;
}
```

**核心规则**：

- ❌ 禁止：`GetParent()` 或 `GetParent<T>()`
- ✅ 推荐：`EntityManager.GetEntityData(this)` 或 `IComponent.OnComponentRegistered` 缓存
- ✅ 所有数据从 `Data` 容器增删改查

### 3. Entity 访问 Component

```csharp
// 在 Enemy.cs 中访问 HealthComponent

public partial class Enemy : CharacterBody2D, IEntity, IPoolable
{
    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();

        // 获取 Component
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            // 绑定事件
            health.Died += OnDied;
        }
    }

    private void OnDied()
    {
        _log.Info($"{Name} 死亡");
        // 触发全局事件
        EventBus.TriggerEnemyDied(this, GlobalPosition);
        // 归还对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    public override void _ExitTree()
    {
        // 解绑事件
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            health.Died -= OnDied;
        }
        Data.Clear();
        base._ExitTree();
    }
}
```

---

## API 文档

### EntityManager - Entity 生成

#### Spawn<T>() - 三个重载

```csharp
// 1. 无位置（Buff、背包物品）
T? Spawn<T>(string poolName, Resource resource) where T : Node, IEntity

// 2. 带位置（Enemy、掉落物品）
T? Spawn<T>(string poolName, Resource resource, Vector2 position) where T : Node2D, IEntity

// 3. 带位置和旋转（Bullet、投射物）
T? Spawn<T>(string poolName, Resource resource, Vector2 position, float rotation) where T : Node2D, IEntity
```

**参数说明**：

- `poolName`：对象池名称（必须在 [`ObjectPoolInit`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/Init/ObjectPoolInit.cs) 中注册）
- `resource`：静态配置 Resource（如 `EnemyResource`）
- `position`：初始位置
- `rotation`：初始旋转角度（弧度）

**示例**：

```csharp
// SpawnSystem 中生成敌人
var enemy = EntityManager.Spawn<Enemy>(
    "EnemyPool",
    enemyResource,
    new Vector2(100, 100)
);

// 生成子弹
var bullet = EntityManager.Spawn<Bullet>(
    "BulletPool",
    bulletResource,
    playerPos,
    shootAngle
);
```

### EntityManager - 查询接口

#### GetComponent<T>() - 从 Entity 获取 Component

```csharp
T? GetComponent<T>(Node entity) where T : Node
```

**示例**：

```csharp
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", resource, pos);

// 获取 HealthComponent
var health = EntityManager.GetComponent<HealthComponent>(enemy);
health?.TakeDamage(50f);

// 访问数据
var data = EntityManager.GetEntityData(enemy);
float damage = data?.Get<float>(DataKey.Damage) ?? 0f;
```

#### GetEntityByComponent() - 通过 Component 反查 Entity

```csharp
Node? GetEntityByComponent(Node component)
```

**示例**：

```csharp
// 在 HealthComponent 中反查所属 Entity
public void TakeDamage(float amount)
{
    var entity = EntityManager.GetEntityByComponent(this);
    if (entity is IEntity iEntity)
    {
        iEntity.Data.Add(DataKey.Damage, amount); // 示例：累加受到的总伤害，DataKey 中应有对应定义
    }
}
```

#### GetEntityData() - 直接获取 Entity 的 Data 容器

```csharp
Data? GetEntityData(Node component)
```

**示例**：

```csharp
// 在 VelocityComponent 中访问 Entity 数据
public override void _Ready()
{
    var data = EntityManager.GetEntityData(this);
    if (data != null)
    {
        float speed = data.Get<float>(DataKey.Speed, 400f);
    }
}
```

#### GetEntitiesByType<T>() - 查询所有指定类型的 Entity

```csharp
IEnumerable<T> GetEntitiesByType<T>(string entityType) where T : Node
```

**示例**：

```csharp
// 查询所有敌人
var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");
foreach (var enemy in enemies)
{
    // 处理逻辑
}
```

#### GetComponentsByType<T>() - 查询所有指定类型的 Component

```csharp
IEnumerable<T> GetComponentsByType<T>(string componentType) where T : Node
```

**示例**：

```csharp
// 查询所有 HealthComponent（如显示血条 UI）
var healthComps = EntityManager.GetComponentsByType<HealthComponent>("HealthComponent");
foreach (var health in healthComps)
{
    // 更新 UI
}
```

#### 范围查询

```csharp
// 范围内所有敌人（AI 寻敌）
IEnumerable<T> GetEntitiesInRange<T>(Vector2 position, float range, string entityType)
    where T : Node2D

// 最近的敌人
T? GetNearestEntity<T>(Vector2 position, string entityType, float maxRange = float.MaxValue)
    where T : Node2D
```

**示例**：

```csharp
// AI 系统：寻找范围内的敌人
var nearbyEnemies = EntityManager.GetEntitiesInRange<Enemy>(
    playerPos,
    500f,
    "Enemy"
);

// 获取最近的敌人
var nearest = EntityManager.GetNearestEntity<Enemy>(
    playerPos,
    "Enemy",
    maxRange: 1000f
);
```

### EntityManager - 生命周期管理

#### Register() - 注册 Entity/Component

```csharp
void Register(Node node, string nodeType)
```

> **注意**：`Spawn()` 会自动调用此方法，通常无需手动调用。特殊情况（如 Player 单例）才需要手动注册。

**示例**：

```csharp
// Player.cs（单例，不使用对象池）
public override void _Ready()
{
    EntityId = GetInstanceId().ToString();
    EntityManager.Register(this, "Player");  // 手动注册
}
```

#### UnregisterEntity() - 注销 Entity

```csharp
void UnregisterEntity(Node entity)
```

> **必须**：在 Entity 的 `_ExitTree()` 中调用，用于清理注册信息和关系。

**示例**：

```csharp
public override void _ExitTree()
{
    EntityManager.UnregisterEntity(this);  // 必须调用
    Data.Clear();  // 清理数据
    base._ExitTree();
}
```

#### Destroy() - 回收到对象池

```csharp
void Destroy(Node entity)
```

**功能**：注销 Entity 并归还到对象池。

**示例**：

```csharp
// 敌人死亡时回收
private void OnDied()
{
    EntityManager.Destroy(this);  // 自动注销并归还对象池
}
```

#### AddComponent<T>() - 动态添加 Component

```csharp
void AddComponent<T>(Node entity, T component) where T : Node
```

**功能**：运行时动态添加 Component（如 Buff）。

**示例**：

```csharp
// 添加速度 Buff
var speedBuff = new SpeedBuffComponent();
EntityManager.AddComponent(player, speedBuff);

// EntityManager 自动完成：
// 1. 挂载到 Entity/Component 路径下
// 2. 注册 Component
// 3. 建立 Entity-Component 关系
// 4. 触发 IComponent.OnComponentRegistered() 回调
```

#### RemoveComponent() - 移除 Component

```csharp
bool RemoveComponent(Node entity, string componentType)
void RemoveComponent(Node entity, Node component)
```

**示例**：

```csharp
// 通过类型移除
EntityManager.RemoveComponent(enemy, "HealthComponent");

// 通过实例移除
var health = EntityManager.GetComponent<HealthComponent>(enemy);
if (health != null)
{
    EntityManager.RemoveComponent(enemy, health);
}
```

---

## 完整使用示例

### 示例 1：生成敌人（SpawnSystem）

```csharp
public partial class SpawnSystem : Node
{
    private static readonly Log _log = new("SpawnSystem");

    [Export] private EnemyResource _enemyResource;

    private void SpawnEnemy(Vector2 position)
    {
        // 一行代码完成生成
        var enemy = EntityManager.Spawn<Enemy>("EnemyPool", _enemyResource, position);

        if (enemy == null)
        {
            _log.Error("生成敌人失败");
            return;
        }

        // 可选：额外配置
        enemy.Data.Set(DataKey.LuckBonus, 10f); // 示例：设置幸运加成
        enemy.Data.Set("SpawnTime", Time.GetTicksMsec()); // 某些非核心业务逻辑可以用字符串，但建议也走定义

        _log.Info($"生成敌人 {enemy.Name} at {position}");
    }
}
```

### 示例 2：实现完整的 Enemy Entity

```csharp
using Godot;

public partial class Enemy : CharacterBody2D, IEntity, IPoolable
{
    private static readonly Log _log = new("Enemy", LogLevel.Info);

    // ================= IEntity 实现 =================

    public Data Data { get; private set; } = new Data();
    public string EntityId { get; private set; } = string.Empty;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();
        _log.Debug($"敌人 {Name} 初始化完成");
    }

    public override void _ExitTree()
    {
        // 解绑事件
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            health.Died -= OnDied;
        }

        // 必须：清理 Data
        Data.Clear();
        base._ExitTree();
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// EntityManager.Spawn() 后的自定义初始化
    /// </summary>
    public void OnSpawn(EnemyResource resource)
    {
        // 绑定事件
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            health.Died -= OnDied;  // 防止重复绑定
            health.Died += OnDied;
        }
    }

    private void OnDied()
    {
        _log.Info($"{Name} 死亡，归还对象池");

        // 触发全局事件
        EventBus.TriggerEnemyDied(this, GlobalPosition);

        // 归还对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    // ================= IPoolable 接口实现 =================

    public void OnPoolAcquire()
    {
        // 从池中取出时重新激活
        var health = EntityManager.GetComponent<HealthComponent>(this);
        if (health != null)
        {
            health.Died -= OnDied;
            health.Died += OnDied;
        }
    }

    public void OnPoolRelease()
    {
        // 归还池时重置状态
        // 重置所有 Component
        EntityManager.GetComponent<HealthComponent>(this)?.Reset();

        Velocity = Vector2.Zero;
        Data.Clear();
    }

    public void OnPoolReset() { }
}
```

### 示例 3：实现完整的 HealthComponent

```csharp
using Godot;
using System;

/// <summary>
/// 生命值组件 - 无状态设计，所有数据存储在 Entity.Data 中
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // 数据容器引用
    private Data? _data;

    // 事件
    public event Action<float>? Damaged;
    public event Action? Died;

    // ================= IComponent 接口 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        Damaged = null;
        Died = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // 懒加载：通过 EntityManager 直接获取 Data
        if (_data == null)
        {
            _data = EntityManager.GetEntityData(this);
        }

        if (_data == null)
        {
            _log.Error("无法获取 Data 容器！请确保该组件挂载在 IEntity 节点下。");
            return;
        }

        _log.Debug("HealthComponent 就绪");
    }

    // ================= 业务逻辑 =================

    /// <summary>
    /// 造成伤害
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (_data == null) return;

        float currentHp = _data.Get<float>(DataKey.CurrentHp);
        currentHp -= amount;
        _data.Set(DataKey.CurrentHp, currentHp);

        Damaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }

    /// <summary>
    /// 重置生命值（对象池复用时）
    /// </summary>
    public void Reset()
    {
        if (_data == null) return;

        // 直接从 Data 获取 MaxHp
        float maxHp = _data.Get<float>(DataKey.MaxHp, 100f);
        _data.Set(DataKey.CurrentHp, maxHp);
    }
}
```

---

## 注意事项

### 1. 必须注销（避免内存泄漏）

```csharp
public override void _ExitTree()
{
    EntityManager.UnregisterEntity(this);  // 必须调用
    Data.Clear();  // 清理 Data
    base._ExitTree();
}
```

### 2. 使用 Destroy 而不是 QueueFree

```csharp
// ❌ 错误：直接释放节点，无法归还对象池
QueueFree();

// ✅ 正确：使用 EntityManager.Destroy，自动注销并归还对象池
EntityManager.Destroy(this);
```

### 3. Component 识别优先级

1. **实现 IComponent 接口**（推荐）
2. **类名以 "Component" 结尾**（命名约定）
3. **在 ECSIndex 白名单中**（特殊情况）

### 4. 事件绑定和解绑

```csharp
// ✅ 正确：防止重复绑定
health.Died -= OnDied;
health.Died += OnDied;

// ✅ 正确：在 _ExitTree 解绑
public override void _ExitTree()
{
    health.Died -= OnDied;
}
```

### 5. 对象池名称规范

对象池名称建议使用常量，避免硬编码：

```csharp
public static class PoolNames
{
    public const string Enemy = "EnemyPool";
    public const string Bullet = "BulletPool";
}

// 使用
var enemy = EntityManager.Spawn<Enemy>(PoolNames.Enemy, resource, pos);
```

---

## 性能优化

### 缓存查询结果

```csharp
// ❌ 避免：每帧查询
public override void _Process(double delta)
{
    var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");
    // ...
}

// ✅ 推荐：定期更新缓存
private List<Enemy> _cachedEnemies = new();
private float _updateInterval = 0.5f;
private float _timer = 0f;

public override void _Process(double delta)
{
    _timer += (float)delta;
    if (_timer >= _updateInterval)
    {
        _timer = 0f;
        _cachedEnemies.Clear();
        _cachedEnemies.AddRange(
            EntityManager.GetEntitiesByType<Enemy>("Enemy")
        );
    }

    // 使用缓存的列表
    foreach (var enemy in _cachedEnemies)
    {
        // ...
    }
}
```

---

## 相关文档

- **架构设计**：[`Docs/框架/ECS/Entity/Entity架构设计理念.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/Entity/Entity架构设计理念.md)
- **详细设计**：[`Docs/框架/ECS/Entity/EntityManager设计说明.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/Entity/EntityManager设计说明.md)
- **项目规则**：[`.agent/rules/projectrules.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/.agent/rules/projectrules.md)

---

**维护者**：项目团队  
**文档版本**：v3.0  
**创建日期**：2026-01-04
