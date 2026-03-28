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

## 文件结构

EntityManager 采用 `partial class` 模块化设计，各模块职责清晰：

| 文件 | 职责 |
|------|------|
| [EntityManager.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/Core/EntityManager.cs) | 核心生命周期管理（Spawn, Register, Destroy）、基础查询（GetEntitiesByType, GetEntityById）、全局查询（GetAllEntities, GetEntitiesByInterface） |
| [EntityManager_Component.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/Core/EntityManager_Component.cs) | Component 管理（RegisterComponents, AddComponent, GetComponent, RemoveComponent） |
| [EntityManager_Ability.cs](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/System/AbilitySystem/EntityManager_Ability.cs) | Ability 管理（AddAbility, RemoveAbility, GetAbilities） |

> [!NOTE]
> **设计原则**：核心方法（Register, UnregisterEntity）保留在主文件，因为它们同时服务于 Entity 和 Component。
>
> - Entity 管理：生成、注册、查询、销毁
> - Component 管理：动态添加/移除、查询、生命周期
> - 关系建立：自动建立 Entity-Component 关系（委托给 EntityRelationshipManager）

---

## 快速开始

### 1. 创建 Entity

#### 1.1 实现 IEntity 接口

```csharp
using Godot;

public partial class Enemy : CharacterBody2D, IEntity
{
    // ... (标准实现)
    
    // 无需在 _Ready/_ExitTree 中手动 Register/Unregister
    // 一切交给 EntityManager.Spawn 和 Destroy 管理
}
```

#### 1.2 使用 EntityManager 生成 Entity

```csharp
// 场景 1：SpawnSystem 批量生成敌人（使用对象池）
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = new Vector2(100, 200)
});

// 场景 2：游戏初始化生成玩家（不使用对象池）
// 类型安全：无需指定 SceneName，自动使用 typeof(Player).Name
var player = EntityManager.Spawn<Player>(new EntitySpawnConfig
{
    Config = playerData,
    UsingObjectPool = false,  // 自动使用 "Player" 查找 ResourceManagement
    Position = startPos
});

// 场景 3：生成子弹（使用位置和方向）
var bullet = EntityManager.Spawn<Bullet>(new EntitySpawnConfig
{
    Config = bulletData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.BulletPool,
    Position = muzzlePos,
    Rotation = shootAngle
});
```

**自动化操作**：

- ✅ **模式自适应**：根据 `UsingObjectPool` 自动选择对象池获取或场景实例化
- ✅ **数据注入**：将 Config 数据自动注入到 `Data` 容器
- ✅ **视觉加载**：自动加载 VisualScene（如果配置了 `VisualScenePath`）
- ✅ **组件管理**：自动注册所有 Component 并建立 Entity-Component 关系
- ✅ **生命周期注册**：将 Entity 注册到 EntityManager 进行统一管理

### 2. 创建 Component

#### 2.1 实现 IComponent 接口（推荐）

#### 数据初始化时序详解 (Critical Timing)

> [!IMPORTANT]
> **理解 Entity 数据初始化的两个阶段至关重要**

**阶段 1: 配置注入 (Spawn 内部)**
- **时间点**: `EntityManager.Spawn` 执行过程中
- **数据源**: `EntitySpawnConfig.Config` (通常来自 .tres 或字典配置)
- **可用性**: 在 `OnComponentRegistered` 中**可以访问**
- **典型数据**:MaxHp, MoveSpeed, BaseDamage

**阶段 2: 运行时初始化 (Spawn 之后)**
- **时间点**: `EntityManager.Spawn` 返回之后
- **数据源**: 代码动态设置 (如 `entity.Data.Set()`)
- **可用性**: 在 `OnComponentRegistered` 中**不可访问**
- **典型数据**:SkillLevel, TargetPlayer, Summoner

**代码对照**:
```csharp
// 1. 配置注入阶段
var enemy = EntityManager.Spawn<Enemy>(config); // 内部执行 Data.LoadFromConfig + OnComponentRegistered
// ↑ component.OnComponentRegistered 在此执行,只能读到 config 数据

// 2. 运行时初始化阶段
enemy.Data.Set(DataKey.SkillLevel, 5);         // 动态设置数据
// ↑ component 必须监听 PropertyChanged 才能响应此变化
```

**Component 最佳实践**:
```csharp
public void OnComponentRegistered(Node entity)
{
    // 1. 获取配置数据 (安全)
    float speed = _data.Get<float>(DataKey.MoveSpeed);
    
    // 2. 监听运行时数据 (必须)
    // 因为 SkillLevel 可能在 Spawn 之后才设置
    _entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
        GameEventType.Data.PropertyChanged, OnDataChanged);
}
```

#### 2.2 Component 访问 Entity 的方式

```csharp
using Godot;

/// <summary>
/// 生命值组件 - 管理实体的生命值逻辑
/// </summary>
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(HealthComponent));

    // ================= 标准字段 =================
    private Data? _data;
    private IEntity? _entity;

    // 事件
    public event Action<float>? Damaged;
    public event Action? Died;

    // ================= IComponent 接口实现 =================

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理事件和引用
        Damaged = null;
        Died = null;
        _data = null;
        _entity = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // ✅ 仅做日志输出，不做验证
        // ObjectPool 和 EntityManager 保证了初始化时序
        _log.Debug($"就绪, MaxHp: {_data?.Get<float>(DataKey.MaxHp, 100f)}");
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
    /// 重置逻辑（对象池复用时）
    /// </summary>
    public void Reset()
    {
        if (_data == null) return;

        float maxHp = _data.Get<float>(DataKey.MaxHp, 100f);
        _data.Set(DataKey.CurrentHp, maxHp);
    }
}
```

#### 2.2 Component 访问 Entity 的方式

> [!IMPORTANT] > **禁止使用 `GetParent()`** 获取 Entity！所有 Component 必须通过 `OnComponentRegistered` 缓存引用。

**标准模式**（2026-01-05 更新）：

```csharp
public partial class MyComponent : Node, IComponent
{
    // ✅ 标准字段：统一使用 _data 和 _entity
    private Data? _data;
    private IEntity? _entity;

    // ✅ 在 OnComponentRegistered 中初始化
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    // ✅ _Ready 仅做日志或信号连接，不做验证
    public override void _Ready()
    {
        Log.Debug("组件就绪");
    }

    // ✅ 需要特定类型时使用类型判断
    public override void _Process(double delta)
    {
        if (_entity is CharacterBody2D body)
        {
            // 使用 body...
        }
    }
}
```

**核心规则**：

- ❌ 禁止：`GetParent()` 或 `GetParent<T>()`
- ❌ 禁止：在 `_Ready` 中验证或懒加载
- ❌ 禁止：使用 `_owner`、`_body` 等多个引用字段
- ✅ 推荐：在 `OnComponentRegistered` 中缓存 `_data` 和 `_entity`
- ✅ 推荐：需要特定类型时使用 `is` 判断转换
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

#### Spawn<T>(EntitySpawnConfig config)

统一的实体生成入口，通过 `EntitySpawnConfig` 对象传递参数。

**参数配置 (`EntitySpawnConfig`)**：

- `Config`：**必填**。配置数据字典（`Dictionary<string, object>`）。
- `UsingObjectPool`：是否使用对象池。
  - `true`：从对象池获取（适用于 Enemy, Bullet, Effect）。
  - `false`：通过 ResourceManagement 加载场景实例化（适用于 Player, UniqueBoss）。
- `PoolName`：**必填** (当 `UsingObjectPool` 为 true 时)。对象池名称（如 `ObjectPoolNames.EnemyPool`）。
- `Position`：(可选) 初始位置 `Vector2`。
- `Rotation`：(可选) 初始旋转角度（角度）。

**代码示例**：

```csharp
// 1. 对象池模式 (Enemy)
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = new Vector2(100, 100)
});

// 2. 场景实例化模式 (Player) - 类型安全，无需指定 SceneName
var player = EntityManager.Spawn<Player>(new EntitySpawnConfig
{
    Config = playerData,
    UsingObjectPool = false,  // 自动使用 "Player" 查找 ResourceManagement
    Position = Vector2.Zero
});
```

### EntityManager - 查询接口

#### GetComponent<T>() - 从 Entity 获取 Component

```csharp
T? GetComponent<T>(Node entity) where T : Node
```

**示例**：

```csharp
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
     PoolName = ObjectPoolNames.EnemyPool,
    Position = pos
});

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
        float speed = data.Get<float>(DataKey.Speed);
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

#### GetAllEntities() - 获取所有 Entity

```csharp
IEnumerable<IEntity> GetAllEntities()
```

**功能**：获取所有已注册的 Entity（不含 Component）。

**示例**：

```csharp
// 获取所有实体
var allEntities = EntityManager.GetAllEntities();
foreach (var entity in allEntities)
{
    // 处理所有实体
}
```

#### GetEntitiesByInterface<T>() - 按接口/基类查询

```csharp
IEnumerable<T> GetEntitiesByInterface<T>() where T : class
```

**功能**：获取所有实现指定接口或基类的 Entity。

**示例**：

```csharp
// 获取所有 Node2D 类型的实体（用于空间查询）
var node2DEntities = EntityManager.GetEntitiesByInterface<Node2D>();

// 获取所有 IUnit 类型的实体
var units = EntityManager.GetEntitiesByInterface<IUnit>();
```

> [!NOTE]  
> **职责边界**：空间查询（范围查询、最近目标等）统一使用 `TargetSelector`，不再由 `EntityManager` 提供。

---

### EntityManager - 生命周期管理

#### Register() - 注册 Entity/Component (底层 API)

```csharp
void Register(Node node, string nodeType)
```

> [!NOTE]
> 通常无需手动调用此方法。`EntityManager.Spawn()` 会自动处理注册。
> 仅当你在编辑器中直接放置 Entity 且未使用 Spawn 生成时才需要手动调用。

#### UnregisterEntity() - 注销 Entity (底层 API)

```csharp
void UnregisterEntity(Node entity)
```

> [!NOTE]
> 通常无需手动调用。`EntityManager.Destroy()` 会自动处理注销。
> 仅在绕过 EntityManager 直接调用 `QueueFree` 时需要关注。

#### Destroy() - 销毁 Entity (推荐)

```csharp
void Destroy(Node entity)
```

**功能**：统一销毁 Entity，自动判断处理方式：
- **IPoolable Entity** (UsingObjectPool=true)：注销并归还对象池
- **普通 Entity** (UsingObjectPool=false)：注销并调用 `QueueFree()`

**所有 Entity 的销毁都应当调用此方法，而非直接调用 `QueueFree()`。**

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
    private static readonly Log _log = new(nameof(SpawnSystem));

    [Export] private EnemyResource _enemyResource;

    private void SpawnEnemy(Vector2 position)
    {
        // 使用参数对象，明确指定使用对象池
        var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
        {
            Config = enemyData,
            UsingObjectPool = true,
             PoolName = ObjectPoolNames.EnemyPool,
            Position = position
        });

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

    // ❌ 严禁在此处编写业务逻辑！
    // ❌ 死亡逻辑请放入 LifecycleComponent
    // ❌ 移动逻辑请放入 VelocityComponent

    // ================= IPoolable 接口实现 =================

    public void OnPoolAcquire()
    {
        // 仅做必要的重置或基础事件订阅（如调试日志）
    }

    public void OnPoolRelease()
    {
        // 归还池时重置状态
        Velocity = Vector2.Zero;
        // Data 和 Events 由 EntityManager 自动清理
    }

    public void OnPoolReset() { }
}
```

## 常见场景实战 (Scenarios)

### 场景 A: 生成敌人（基础属性 + 动态难度）

```csharp
// 1. Spawn: 注入基础属性 (MaxHp: 100, Speed: 50)
var enemy = EntityManager.Spawn<Enemy>(enemyConfig); 

// 2. Init: 注入动态数据 (难度系数: 1.5, 掉落倍率: 2.0)
enemy.Data.Set(DataKey.Difficulty, 1.5f);
enemy.Data.Set(DataKey.DropRate, 2.0f);

// 3. Component 响应
// DifficultyComponent 监听 Difficulty 变化，动态调整最终属性
// DropComponent 监听 DropRate 变化
```

### 场景 B: 装备武器（基础数值 + 强化等级）

```csharp
// 1. Spawn: 生成武器实体
var weapon = EntityManager.Spawn<Weapon>(weaponConfig);

// 2. Init: 设置强化等级和持有者
weapon.Data.Set(DataKey.Level, 5);      // 强化等级 +5
weapon.Data.Set(DataKey.Owner, player); // 归属玩家

// 3. Component 响应
// WeaponStatsComponent 监听 Level，重新计算攻击力
// AttackComponent 监听 Owner，用于伤害统计归属
```

### 场景 C: 召唤物（配置 + 召唤者关联）

```csharp
// 1. Spawn: 生成召唤物 (炮台)
var turret = EntityManager.Spawn<Turret>(turretConfig);

// 2. Init: 设置召唤者和持续时间
turret.Data.Set(DataKey.Summoner, player);
turret.Data.Set(DataKey.MaxLifeTime, 10f + player.SkillDurationBonus);

// 3. Component 响应
// LifecycleComponent 监听 MaxLifeTime，重置存活计时器
// DamageComponent 读取 Summoner，将击杀数算给玩家
```

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
