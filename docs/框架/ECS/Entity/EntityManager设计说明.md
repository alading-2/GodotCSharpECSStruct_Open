# EntityManager 设计说明

**文档类型**：详细设计  
**目标受众**：开发者、维护者  
**最后更新**：2026-01-04

---

## 设计动机

EntityManager 是 Entity 系统的**统一入口**，整合了生成、注册、查询、销毁的完整流程。

### 解决的问题

传统架构中，Entity 生成流程分散且容易出错：

```csharp
// ❌ 传统做法：手动操作多步骤
var enemy = ObjectPoolManager.GetPool<Enemy>("EnemyPool").Get();  // 1. 获取实例
enemy.Data.Set(DataKey.MaxHp, 100);                                      // 2. 手动注入数据
enemy.Data.Set(DataKey.Speed, 200);
enemy.GlobalPosition = spawnPos;                                   // 3. 设置位置
EntityManager.Register(enemy, "Enemy");                            // 4. 手动注册

// 问题：
// - 代码冗余，每次生成都重复这些步骤
// - 易出错，忘记注册或漏设属性
// - 维护困难，修改流程需要改动多处
// - Component 注册需要手动处理
```

### 解决方案：统一入口

```csharp
// ✅ 新架构：一行代码完成
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Resource = enemyResource,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPos
});

// EntityManager 自动完成：
// 1. 从对象池获取实例
// 2. 反射注入 Resource 数据到 Data
// 3. 加载 VisualScene（如果配置了）
// 4. 递归注册所有 Component
// 5. 建立 Entity-Component 关系
// 6. 注册 Entity 到全局索引
// 7. 返回已完全配置好的实例
```

**优势**：

- 代码简洁，一行搞定
- 零出错，自动化所有步骤
- 易扩展，新增 Entity 类型无需修改框架
- 统一管理，所有 Entity/Component 通过同一入口

---

## 核心职责划分

| 模块                          | 职责                         | 示例                                      |
| ----------------------------- | ---------------------------- | ----------------------------------------- |
| **EntityManager**             | 生成、注册、查询、销毁       | `Spawn<Enemy>(poolName, res, pos)`        |
| **EntityRelationshipManager** | 管理关系（Entity-Component） | `AddRelationship(entityId, compId, type)` |
| **ObjectPoolManager**         | 内存管理、对象复用           | `GetPool<T>(poolName).Get()`              |
| **Resource**                  | 静态配置（编辑器设计）       | `EnemyResource.tres`                      |
| **Data**                      | 运行时数据（动态）           | `entity.Data.Set("HP", 100)`              |
| **Component**                 | 功能模块                     | `HealthComponent.TakeDamage()`            |

**职责边界**：

- EntityManager 只管理**生命周期**，不处理游戏逻辑
- Component 只实现**功能模块**，不关心注册流程
- Data 只存储**运行时数据**，不包含静态配置

---

## 数据流转设计

```
┌──────────────────────────────────────────────────────────┐
│ 1. 静态配置（编辑器，设计时）                            │
└──────────────────────────────────────────────────────────┘
   EnemyResource.tres
   ├── MaxHp = 100
   ├── Speed = 200
   ├── Damage = 10
   └── VisualScene = "res://Scenes/Enemy/EnemyVisual.tscn"
                     ↓
┌──────────────────────────────────────────────────────────┐
│ 2. 生成时注入（自动，运行时）                            │
└──────────────────────────────────────────────────────────┘
   EntityManager.Spawn<Enemy>(new EntitySpawnConfig
   {
       Resource = enemyResource,
       UsingObjectPool = true,
       PoolName = ObjectPoolNames.EnemyPool,
       Position = pos
   })
   │
   ├─ ObjectPoolManager.GetPool("EnemyPool").Get()   ①从池中获取
   ├─ Data.LoadFromResource(resource)                 ②反射注入所有属性
   ├─ InjectVisualScene(resource.VisualScene)         ③加载视觉场景
   ├─ RegisterComponents(entity)                      ④递归注册 Component
   ├─ Register(entity, "Enemy")                       ⑤注册 Entity
   └─ return enemy                                    ⑥返回已配置实例
                     ↓
┌──────────────────────────────────────────────────────────┐
│ 3. 运行时数据（动态，游戏逻辑）                          │
└──────────────────────────────────────────────────────────┘
   enemy.Data
    ├── Get<float>(DataKey.MaxHp)      → 100 (来自 Resource)
    ├── Set(DataKey.CurrentHp, 80)     → 受伤后更新
    ├── Get<float>(DataKey.MaxHp)      → 200 (来自 Resource)
    └── Add("TotalDamage", 50)   → 累计伤害（运行时数据）
                     ↓
┌──────────────────────────────────────────────────────────┐
│ 4. 组件响应（事件驱动）                                  │
└──────────────────────────────────────────────────────────┘
   HealthComponent
   └─ 监听 Data.OnValueChanged
      └─ 当 Hp 变化时 → 处理逻辑（如死亡、动画）
   UI 系统
   └─ 监听 Data.OnValueChanged
      └─ 当 CurrentHp 变化时 → 更新血条显示
```

**关键点**：

1. **Resource 是只读的静态配置**，在编辑器中设计
2. **Data 是可读写的运行时数据**，存储游戏过程中的变化
3. **自动注入**：EntityManager 自动将 Resource 复制到 Data
4. **事件驱动**：Data 变更时自动通知监听器

---

## 核心方法详解

### 1. Spawn<T>() - 生成 Entity

#### 统一方法签名

```csharp
public static T? Spawn<T>(EntitySpawnConfig config) where T : Node, IEntity
```

**参数配置 (EntitySpawnConfig)**：

- `Resource`：**必填**。静态配置资源（如 `EnemyResource`, `PlayerResource`）
- `UsingObjectPool`：是否使用对象池
  - `true`：从对象池获取（适用于 Enemy, Bullet, Effect）
  - `false`：从场景路径实例化（适用于 Player, UniqueBoss）
- `PoolName`：**必填**（当 `UsingObjectPool` 为 true 时）。对象池名称（如 `ObjectPoolNames.EnemyPool`）
- `ScenePath`：**必填**（当 `UsingObjectPool` 为 false 时）。ECSIndex 中定义的场景路径
- `Position`：（可选）初始位置 `Vector2`
- `Rotation`：（可选）初始旋转角度（弧度）

#### 内部流程

```csharp
public static T? Spawn<T>(EntitySpawnConfig config) where T : Node, IEntity
{
    T? entity;

    // ① 根据模式创建 Entity
    if (config.UsingObjectPool)
    {
        // 对象池模式
        var pool = ObjectPoolManager.GetPool<T>(config.PoolName);
        if (pool == null)
        {
            _log.Error($"对象池不存在: {config.PoolName}");
            return null;
        }
        entity = pool.Get();
    }
    else
    {
        // 场景实例化模式
        var scene = GD.Load<PackedScene>(config.ScenePath);
        entity = scene.Instantiate<T>();
        ParentManager.AddChild(entity);
    }

    // ② 数据注入（核心：Resource → Data）
    if (entity is IEntity iEntity)
    {
        iEntity.Data.LoadFromResource(config.Resource);
    }

    // ③ 自动加载 VisualScene
    InjectVisualScene(entity, config.Resource);

    // ④ 自动注册所有 Component
    RegisterComponents(entity);

    // ⑤ 注册 Entity
    string entityType = typeof(T).Name;
    Register(entity, entityType);

    // ⑥ 设置位置和旋转（如果提供）
    if (entity is Node2D entity2D)
    {
        if (config.Position.HasValue)
            entity2D.GlobalPosition = config.Position.Value;
        if (config.Rotation.HasValue)
            entity2D.GlobalRotation = config.Rotation.Value;
    }

    return entity;
}
```

#### 使用示例

```csharp
// SpawnSystem.cs
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _enemyResource;

    private void SpawnEnemy()
    {
        var pos = GetRandomSpawnPosition();

        // 使用 EntitySpawnConfig 生成敌人
        var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
        {
            Resource = _enemyResource,
            UsingObjectPool = true,
            PoolName = ObjectPoolNames.EnemyPool,
            Position = pos
        });

        if (enemy == null)
        {
            _log.Error("生成敌人失败");
            return;
        }

        // 可选：额外配置
        enemy.Data.Set("IsElite", true);
    }
}
```

### 2. InjectVisualScene() - 视觉场景加载

#### 功能

自动从 Resource 中读取 `VisualScene` 属性，实例化并挂载到 Entity 下。

#### 流程

```csharp
private static void InjectVisualScene(Node entity, Resource resource)
{
    // 1. 反射获取 VisualScene 属性（兼容任意 Resource 类型）
    var prop = resource.GetType().GetProperty("VisualScene");
    if (prop == null) return;

    var scene = prop.GetValue(resource) as PackedScene;
    if (scene == null) return;

    // 2. 清理旧的 VisualRoot（对象池复用时）
    var existingVisual = entity.GetNodeOrNull("VisualRoot");
    if (existingVisual != null)
    {
        existingVisual.Free();  // 立即释放，防止命名冲突
    }

    // 3. 实例化并挂载
    var visual = scene.Instantiate();
    visual.Name = "VisualRoot";
    entity.AddChild(visual);

    // 4. 设置 ZIndex（确保显示在背景之上）
    if (visual is Node2D visual2D)
    {
        visual2D.ZIndex = 10;
    }
}
```

#### 配置示例

```csharp
// EnemyResource.cs
[GlobalClass]
public partial class EnemyResource : Resource
{
    [Export] public float MaxHp { get; set; }
    [Export] public float Speed { get; set; }

    // EntityManager 会自动加载此场景
    [Export] public PackedScene VisualScene { get; set; }
}
```

### 3. RegisterComponents() - 自动注册 Component

#### 识别规则（按优先级）

1. **实现 IComponent 接口**（推荐）
2. **类名以 "Component" 结尾**（兼容旧代码）
3. **在 ECSIndex 白名单中**（特殊情况）

#### 流程

```csharp
private static void RegisterComponents(Node entity)
{
    int registeredCount = 0;
    string entityId = entity.GetInstanceId().ToString();

    // 使用 FindChildren 递归查找所有层级的子节点
    var allChildren = entity.FindChildren("*", "Node", true, false);

    foreach (Node child in allChildren)
    {
        bool isComponent = false;
        string componentType = child.GetType().Name;

        // 规则 1：IComponent 接口（优先级最高）
        if (child is IComponent component)
        {
            isComponent = true;

            // 触发回调，让 Component 获取 Entity 引用
            component.OnComponentRegistered(entity);
        }
        // 规则 2：命名约定
        else if (componentType.EndsWith("Component"))
        {
            isComponent = true;
        }
        // 规则 3：白名单
        else if (ECSIndex.IsComponentWhitelist(componentType))
        {
            isComponent = true;
        }

        if (isComponent)
        {
            // 注册 Component
            Register(child, componentType);

            // 建立 Entity-Component 关系
            string componentId = child.GetInstanceId().ToString();
            EntityRelationshipManager.AddRelationship(
                entityId,
                componentId,
                EntityRelationshipType.ENTITY_TO_COMPONENT
            );

            registeredCount++;
        }
    }
}
```

#### 示例：Component 实现

```csharp
// HealthComponent.cs
public partial class HealthComponent : Node, IComponent
{
    private Data? _data;

    // EntityManager 自动触发此回调
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _log.Debug($"注册到 Entity: {entity.Name}");
        }
    }

    public override void _Ready()
    {
        // 懒加载模式
        if (_data == null)
            _data = EntityManager.GetEntityData(this);
    }
}
}
```

### 4. UnregisterEntity() - 注销 Entity

#### 功能

Entity 销毁时的完整清理流程。

#### 流程

```csharp
public static void UnregisterEntity(Node entity)
{
    string id = entity.GetInstanceId().ToString();

    // 1. 从注册表移除
    if (!_entities.Remove(id)) return;

    // 2. 统一清理 IEntity 资源
    if (entity is IEntity iEntity)
    {
        iEntity.Events.Clear();
        iEntity.Data.Clear();
    }

    // 3. 从类型索引中移除
    foreach (var set in _entitiesByType.Values)
        set.Remove(entity);

    // 4. 注销所有 Component（触发 OnComponentReset + OnComponentUnregistered）
    UnregisterComponents(entity);

    // 5. 清理所有 Entity-Component 关系
    EntityRelationshipManager.RemoveAllRelationships(id);
}
```

#### 关系管理策略 (2026-01-11 更新)

> [!IMPORTANT]
> **核心设计决策**：每次 `Destroy` 会移除所有 Entity-Component 关系，每次 `Spawn` 会重新注册。

```
Spawn 流程:
  ObjectPool.Get() → Register() + RegisterComponents() → 建立关系

Destroy 流程:
  UnregisterEntity() → UnregisterComponents() → 移除关系 → ObjectPool.Return()
```

**设计优势**：
- ✅ 简单直接，无需维护复杂的关系状态
- ✅ 每次 Spawn 都是干净的状态
- ✅ 代码中的"防止重复注册"检查是安全保障（正常流程不触发）

**关于"防止重复注册"**：
- 代码 `if (!_entities.ContainsKey(id))` 是一道安全防线
- 正常流程下 `Destroy` 已移除所有关系，不会触发此检查
- 仅在异常场景（如 Entity 未正确销毁就复用）时生效

#### Component 回调顺序

```csharp
// UnregisterComponents 内部流程
foreach (var component in components)
{
    // 1. 先重置组件状态
    icomp.OnComponentReset();
    
    // 2. 再触发注销回调
    icomp.OnComponentUnregistered();
}
```

#### 使用示例

```csharp
// Player.cs - 非对象池 Entity
public override void _ExitTree()
{
    // 统一注销（自动清理 Data/Events/关系）
    EntityManager.UnregisterEntity(this);
}

// Enemy.cs - 对象池 Entity（通过 EntityManager.Destroy）
// 死亡时调用 EntityManager.Destroy(this)
// 内部自动处理: UnregisterEntity() → ObjectPool.Return()
```


### 5. GetComponent<T>() - 获取 Component

#### 实现

```csharp
public static T? GetComponent<T>(Node entity) where T : Node
{
    string entityId = entity.GetInstanceId().ToString();

    // 通过关系管理器获取所有 Component ID
    var componentIds = EntityRelationshipManager
        .GetChildEntitiesByParentAndType(entityId, EntityRelationshipType.ENTITY_TO_COMPONENT);

    foreach (var componentId in componentIds)
    {
        var component = GetEntityById(componentId);
        if (component == null) continue;

        // 检查类型是否匹配
        if (component.GetType().Name == typeof(T).Name && component is T typedComponent)
        {
            return typedComponent;
        }
    }

    return null;
}
```

#### 使用示例

```csharp
// Enemy.cs
public override void _Ready()
{
    // 获取 HealthComponent
    var health = EntityManager.GetComponent<HealthComponent>(this);
    if (health != null)
    {
        health.Died += OnDied;
    }
}
```

### 6. GetEntityData() - 获取 Entity 的 Data

#### 实现

```csharp
public static Data? GetEntityData(Node component)
{
    var entity = GetEntityByComponent(component);
    if (entity is IEntity iEntity)
        return iEntity.Data;
    return null;
}
```

#### 使用示例

```csharp
// VelocityComponent.cs
public override void _Ready()
{
    // 直接获取 Entity 的 Data 容器
    var data = EntityManager.GetEntityData(this);
    if (data != null)
    {
        float speed = data.Get<float>(DataKey.Speed);
    }
}
```

---

## 查询接口设计

### 按类型查询

```csharp
// 查询所有 Enemy
var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");

// 查询所有 HealthComponent
var healthComps = EntityManager.GetComponentsByType<HealthComponent>("HealthComponent");

// 内部实现：基于类型索引（O(1) 查询）
private static readonly Dictionary<string, HashSet<Node>> _entitiesByType = new();
```

### 范围查询

```csharp
// AI 寻敌：范围内所有敌人
var nearbyEnemies = EntityManager.GetEntitiesInRange<Enemy>(
    playerPos,
    range: 500f,
    entityType: "Enemy"
);

// 获取最近的敌人
var nearest = EntityManager.GetNearestEntity<Enemy>(
    playerPos,
    "Enemy",
    maxRange: 1000f
);

// 实现：先类型查询，再距离过滤
public static IEnumerable<T> GetEntitiesInRange<T>(
    Vector2 position,
    float range,
    string entityType) where T : Node2D
{
    return GetEntitiesByType<T>(entityType)
        .Where(e => e.GlobalPosition.DistanceTo(position) <= range);
}
```

### 关系查询

```csharp
// 通过 Component 反查 Entity
var entity = EntityManager.GetEntityByComponent(component);

// 从 Entity 获取 Component
var health = EntityManager.GetComponent<HealthComponent>(entity);

// 内部实现：通过 EntityRelationshipManager
var entityId = EntityRelationshipManager
    .GetParentEntitiesByChildAndType(componentId, EntityRelationshipType.ENTITY_TO_COMPONENT)
    .FirstOrDefault();
```

---

## 系统协作

### 与 ObjectPoolManager 协作

```csharp
// ObjectPoolInit.cs - 初始化对象池
public override void _Ready()
{
    new ObjectPool<Enemy>(
        () => EnemyScene.Instantiate<Enemy>(),
        new ObjectPoolConfig {
            Name = "EnemyPool",
            InitialSize = 50,
            MaxSize = 200
        }
    );
}

// EntityManager 自动使用对象池
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Resource = resource,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = pos
});

// 销毁 Entity（兼容对象池和非对象池）
EntityManager.Destroy(enemy);
// - IPoolable Entity → ObjectPoolManager.ReturnToPool()
// - 非 IPoolable Entity → QueueFree()
```

### 与 Data 容器协作

EntityManager 负责将 Resource 中的静态数据反射注入到 Entity 的 Data 容器中。Component 则可以监听 Data 的变更来实现自恰的逻辑。

```csharp
// EntityManager 自动注入
entity.Data.Set(DataKey.MaxHp, 100);

// Component 响应式更新
public override void _Ready()
{
    _data = EntityManager.GetEntityData(this);
    _data.On(DataKey.CurrentHp, (oldVal, newVal) => {
        UpdateVisual(newVal);
    });
}
```

### 与 DataInit 协作

```csharp
// DataInit.cs - 注册计算属性
DataRegistry.RegisterComputed(
    "CurrentHp",
    entity => {
        var health = EntityManager.GetComponent<HealthComponent>(entity);
        return health?.CurrentHp ?? 0f;
    }
);

// 业务代码直接访问
float currentHp = entity.Data.Get<float>("CurrentHp");  // 自动计算
```

---

## 扩展指南

### 新增 Entity 类型

**需要做的**：

1. 创建 Resource 类

```csharp
[GlobalClass]
public partial class ItemResource : Resource
{
    [Export] public string ItemName { get; set; }
    [Export] public int Value { get; set; }
}
```

2. 注册对象池

```csharp
new ObjectPool<Item>(
    () => itemScene.Instantiate<Item>(),
    new ObjectPoolConfig { Name = "ItemPool" }
);
```

**不需要做的**：

- ❌ 修改 EntityManager（反射自动处理）
- ❌ 添加 case 分支
- ❌ 手动注入数据

### 新增 Component 类型

**推荐做法**：

```csharp
public partial class NewComponent : Node, IComponent
{
    private Data? _data;

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理资源
    }
}
```

**自动完成**：

- ✅ EntityManager 自动识别（IComponent 接口）
- ✅ 自动注册
- ✅ 自动建立关系
- ✅ 自动触发回调

---

## 最佳实践

### Entity 脚本模板

```csharp
using Godot;

public partial class MyEntity : CharacterBody2D, IEntity, IPoolable
{
    private static readonly Log _log = new("MyEntity");

    // ================= IEntity 实现 =================

    public Data Data { get; private set; } = new Data();
    public string EntityId { get; private set; } = string.Empty;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        EntityId = GetInstanceId().ToString();
    }

    public override void _ExitTree()
    {
        // 统一注销 (自动处理 Data/Events 清理)
        EntityManager.UnregisterEntity(this);
    }

    // ================= IPoolable 实现 =================

    public void OnPoolAcquire()
    {
        // 从池中取出时重新激活
        // 注意：Events 需在此处重新订阅 (因为 OnPoolRelease/UnregisterEntity 已清空)
    }

    public void OnPoolRelease()
    {
        // 归还池时仅需重置非 Data/Component 管理的状态
        // Data.Clear() 和 Events.Clear() 已由 EntityManager.Destroy() 统一处理
    }

    public void OnPoolReset() { }
}
```

### Component 脚本模板

```csharp
using Godot;

public partial class MyComponent : Node, IComponent
{
    private static readonly Log _log = new("MyComponent");

    // 缓存数据引用
    private Data? _data;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _log.Debug($"注册到 Entity: {entity.Name}");
        }
    }

    public void OnComponentUnregistered()
    {
        _log.Debug("Component 注销");
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // ✅ 仅做日志或 Godot 内置信号连接
        // ❌ 不要在此处订阅 Data 或 Entity.Events 事件（应在 OnComponentRegistered）
    }

    // ================= 业务逻辑 =================

    public void DoSomething()
    {
        if (_data == null) return;

        float value = _data.Get<float>(DataKey.Damage, 0f);
        _data.Set(DataKey.Damage, value + 1);
    }
}
```

---

## 性能优化

### 1. 缓存查询结果

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
}
```

### 2. 对象池配置

```csharp
// 根据实际使用量调整
new ObjectPoolConfig
{
    Name = "EnemyPool",
    InitialSize = 50,      // 平均使用量
    MaxSize = 200,         // 峰值 × 1.5
    EnableAutoCleanup = true,
    CleanupInterval = 60f  // 60 秒清理一次闲置对象
}
```

### 3. 场景切换清理

```csharp
// SceneManager.cs
public void ChangeScene(string scenePath)
{
    // 清理所有 Entity
    EntityManager.Clear();

    // 清理对象池
    ObjectPoolManager.CleanupAll();

    // 切换场景
    GetTree().ChangeSceneToFile(scenePath);
}
```

---

## 总结

EntityManager 实现了以下目标：

1. **统一入口**：所有 Entity/Component 操作通过一个接口
2. **自动化流程**：Spawn() 自动完成获取、注入、注册等步骤
3. **零配置扩展**：新增 Entity 类型无需修改框架代码
4. **反射注入**：自动处理所有 Resource 属性
5. **自动识别**：Component 通过接口/命名/白名单自动注册
6. **关系管理**：集成 EntityRelationshipManager，支持复杂查询
7. **高性能**：基于索引查询（O(1)）+ 对象池集成

**设计哲学**：

- 简单易用：开发者只需关心业务逻辑
- 自动化：框架自动处理繁琐的管理工作
- 可扩展：通过接口和反射实现零配置扩展

---

## 相关文档

- **架构理念**：[`Docs/框架/ECS/Entity/Entity架构设计理念.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/Entity/Entity架构设计理念.md)
- **API 使用**：[`Src/ECS/Entity/Core/README.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/Core/README.md)
- **项目规则**：[`.agent/rules/projectrules.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/.agent/rules/projectrules.md)

---

**维护者**：项目团队  
**文档版本**：v3.0  
**创建日期**：2026-01-04
