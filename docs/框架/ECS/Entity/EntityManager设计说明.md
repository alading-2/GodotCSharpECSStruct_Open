# EntityManager 设计说明 - 统一的实体生命周期管理

## 1. 设计动机

### 1.1 原有问题

在传统架构中，Entity 的生成流程分散在多个模块：

```
SpawnSystem（决定何时生成）
    ↓
ObjectPool.Get()（获取实例）
    ↓
手动设置 Data（注入 Resource 配置）
    ↓
EntityManager.Register()（注册管理）
```

这种分散的流程导致：

1. **职责不清**：谁负责将 `EnemyResource` 的配置数据写入 `Entity.Data`？
2. **代码冗余**：每个 SpawnSystem 都要重复"获取 → 配置 → 注册"的逻辑
3. **易出错**：忘记注册、忘记注入数据、忘记设置位置等
4. **难维护**：新增 Entity 类型时需要修改多处代码

### 1.2 解决方案

将 EntityManager 升级为**统一入口**，整合 Factory 职责：

```
SpawnSystem
    ↓
EntityManager.Spawn(resource, position)
    ↓ (内部自动完成)
    ├─ ObjectPool.Get()
    ├─ InjectResourceData()
    ├─ Register()
    └─ 返回已配置好的实例
```

## 2. 核心设计

### 2.1 职责划分

| 模块              | 职责                             | 示例                            |
| ----------------- | -------------------------------- | ------------------------------- |
| **EntityManager** | 统一入口：生成、注册、查询、销毁 | `Spawn<Enemy>(resource, pos)`   |
| **ObjectPool**    | 内存管理：对象复用、生命周期     | `Get()`, `Release()`            |
| **Resource**      | 静态配置：HP、Speed、Damage 等   | `EnemyResource.tres`            |
| **Data**          | 运行时数据：动态键值对存储       | `node.GetData().Set("HP", 100)` |
| **Component**     | 逻辑模块：Health、Velocity、AI   | `HealthComponent.TakeDamage()`  |

### 2.2 数据流转

```
EnemyResource.tres (静态配置)
    ↓ (EntityManager.InjectResourceData)
Entity.Data (运行时数据)
    ↓ (Data.OnValueChanged 事件)
AttributeComponent (自动重算)
    ↓ (组件逻辑)
HealthComponent.TakeDamage()
```

**关键点**：

- `Resource` 是只读的静态配置（编辑器创建）
- `Data` 是可读写的运行时数据（代码动态修改）
- `Component` 监听 `Data` 的变化，自动响应

## 3. 核心方法详解

### 3.1 Spawn<T>() - 生成 Entity（方法重载）

EntityManager 提供了三个 `Spawn` 方法重载，适应不同类型 Entity 的需求：

#### 3.1.1 通用版本（无位置参数）

```csharp
public static T? Spawn<T>(string poolName, Resource resource, Node? parent = null) where T : Node
```

**功能**：生成不需要位置的 Entity。

**参数说明**：

- `poolName`：对象池名称（必须），明确指定从哪个池获取实例
- `resource`：静态配置 Resource
- `parent`：父节点（可选）

**适用场景**：

- **Buff**：纯逻辑节点，挂载在玩家/敌人下
- **Item**：背包物品，不在场景中显示
- **Ability**：技能实例，作为子节点管理

**流程**：

1. 验证池名称是否有效
2. 从指定的 `ObjectPool` 获取实例
3. 调用 `InjectResourceData()` 注入配置数据
4. 可选：重新挂载到指定父节点
5. 自动注册到 `EntityManager`
6. 返回已配置好的实例

**使用示例**：

```csharp
// 给玩家添加 Buff
var buff = EntityManager.Spawn<Buff>(PoolNames.BuffPool, buffResource, player);

// 生成背包物品
var item = EntityManager.Spawn<Item>(PoolNames.ItemPool, itemResource);
```

#### 3.1.2 带位置版本

```csharp
public static T? Spawn<T>(string poolName, Resource resource, Vector2 position, Node? parent = null)
    where T : Node2D
```

**功能**：生成需要位置的 Entity。

**参数说明**：

- `poolName`：对象池名称（必须）
- `resource`：静态配置 Resource
- `position`：初始位置
- `parent`：父节点（可选）

**适用场景**：

- **Enemy**：敌人生成在屏幕边缘
- **Bullet**：子弹从武器位置发射
- **掉落物品**：敌人死亡后掉落的物品

**额外操作**：

- 调用通用版本完成基础初始化
- 设置 `GlobalPosition`

**使用示例**：

```csharp
// 生成敌人
var enemy = EntityManager.Spawn<Enemy>(PoolNames.EnemyPool, enemyResource, spawnPos);

// 掉落物品
var dropItem = EntityManager.Spawn<Item>(PoolNames.ItemPool, itemResource, enemy.GlobalPosition);
```

#### 3.1.3 带位置和旋转版本

```csharp
public static T? Spawn<T>(string poolName, Resource resource, Vector2 position, float rotation, Node? parent = null)
    where T : Node2D
```

**功能**：生成需要初始方向的 Entity。

**参数说明**：

- `poolName`：对象池名称（必须）
- `resource`：静态配置 Resource
- `position`：初始位置
- `rotation`：初始旋转角度（弧度）
- `parent`：父节点（可选）

**适用场景**：

- **Bullet**：子弹朝向目标方向
- **Projectile**：投射物带有初始角度
- **Dash Effect**：冲刺特效跟随玩家方向

**额外操作**：

- 调用带位置版本完成位置初始化
- 设置 `GlobalRotation`

**使用示例**：

```csharp
// 发射子弹（朝向鼠标方向）
var direction = (mousePos - player.GlobalPosition).Normalized();
var angle = direction.Angle();
var bullet = EntityManager.Spawn<Bullet>(PoolNames.BulletPool, bulletResource, player.GlobalPosition, angle);
```

### 3.2 InjectResourceData() - 数据注入

```csharp
private static void InjectResourceData(Node entity, Resource resource)
```

**功能**：将 `Resource` 的静态配置写入 `Entity.Data`。

**设计要点**：

1. **类型分发**：使用 `switch (resource)` 根据类型分发注入逻辑
2. **命名约定**：Base 前缀表示基础值（如 `BaseMaxHp`），供 `AttributeComponent` 使用
3. **初始化**：设置 `CurrentHp = MaxHp`，确保实体满血生成
4. **自动触发**：`Data.Set()` 内部会触发 `OnValueChanged` 事件，`AttributeComponent` 自动重算

**扩展性**：

- 新增 Entity 类型时，只需在 `switch` 中添加一个 `case`
- 新增属性时，只需添加一行 `data.Set()`
- 池名称由调用方明确指定，无需维护 Resource 到 Pool 的映射表

### 3.3 Register() / Unregister() - 注册管理

```csharp
public static void Register(Node entity, string entityType)
public static void Unregister(Node entity)
```

**功能**：维护全局实体注册表和类型索引。

**数据结构**：

- `_entities`：`InstanceId -> Node`，用于快速查找
- `_entitiesByType`：`EntityType -> HashSet<Node>`，用于类型查询

**注意事项**：

- `Register` 通常由 `Spawn` 自动调用，无需手动注册
- `Unregister` 必须在 `Entity._ExitTree()` 中调用，防止内存泄漏

### 3.4 查询方法

```csharp
// 按类型查询
GetEntitiesByType<T>(string entityType)

// 范围查询
GetEntitiesInRange<T>(Vector2 position, float range, string entityType)

// 最近查询
GetNearestEntity<T>(Vector2 position, string entityType, float maxRange)
```

**性能优化**：

- 使用 `HashSet` 存储，查询效率 O(1)
- 范围查询使用 `DistanceTo()`，避免开方运算
- 最近查询使用线性扫描，适合小规模数据（< 1000）

**未来优化**：

- 大规模场景（> 1000 实体）可引入空间分区（QuadTree/Grid）
- 缓存查询结果，减少重复计算

### 3.5 Recycle() - 回收 Entity

```csharp
public static void Recycle(Node entity)
```

**功能**：回收 Entity 到对象池，而不是销毁。

**流程**：

1. 从 `EntityManager` 注销
2. 清理 `EntityRelationshipManager` 中的所有关系
3. 归还到 `ObjectPool`（触发 `IPoolable.OnPoolRelease`）

**使用场景**：

- 敌人死亡
- 子弹超出屏幕
- 特效播放完毕

## 4. 与其他系统的协作

### 4.1 与 ObjectPool 的协作

```csharp
// ObjectPoolInit.cs - 初始化对象池
new ObjectPool<Node>(
    () => enemyScene.Instantiate(),
    new ObjectPoolConfig { Name = PoolNames.EnemyPool, InitialSize = 100 }
);

// EntityManager.cs - 使用对象池（通过池名称）
var enemy = EntityManager.Spawn<Enemy>(PoolNames.EnemyPool, enemyResource, position);
```

**关键点**：

- `ObjectPool` 只负责内存管理，不关心业务逻辑
- `EntityManager` 负责业务逻辑，通过池名称明确指定使用哪个对象池
- 池名称统一在 `PoolNames` 常量类中定义，避免硬编码

### 4.2 与 AttributeComponent 的协作

```csharp
// EntityManager 注入数据
data.Set("BaseMaxHp", 100);

// AttributeComponent 监听变化（内部实现）
data.On("BaseMaxHp", (oldVal, newVal) => {
    _isDirty = true; // 标记需要重算
});

// 下次访问时自动重算
float finalHp = attributeComponent.MaxHp; // 触发 RecalculateIfDirty()
```

**关键点**：

- `EntityManager` 只负责写入 `Data`，不直接操作 `Component`
- `AttributeComponent` 通过事件监听自动响应，解耦合

### 4.3 与 SpawnSystem 的协作

```csharp
// SpawnSystem.cs - 简洁的生成逻辑
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _basicEnemy;
    [Export] private EnemyResource _eliteEnemy;

    private void SpawnWave(int waveNumber)
    {
        for (int i = 0; i < waveNumber * 5; i++)
        {
            var pos = GetRandomSpawnPosition();

            // 明确指定池名称，一行代码完成生成
            var enemy = EntityManager.Spawn<Enemy>(PoolNames.EnemyPool, _basicEnemy, pos);

            // 可选：额外配置
            if (waveNumber > 5)
            {
                enemy?.GetData().Set("IsElite", true);
            }
        }
    }
}
```

**优势**：

- SpawnSystem 只关心"何时生成"、"生成什么"和"从哪个池获取"
- 池名称明确，避免自动推断带来的不确定性
- 不需要关心"如何获取实例"和"如何配置数据"的细节

## 5. 扩展性设计

### 5.1 新增 Entity 类型

只需三步：

1. **创建 Resource**：

```csharp
[GlobalClass]
public partial class ItemResource : Resource
{
    [Export] public string ItemName { get; set; }
    [Export] public int Value { get; set; }
}
```

2. **注册对象池**（在 `ObjectPoolInit.cs`）：

```csharp
new ObjectPool<Node>(
    () => itemScene.Instantiate(),
    new ObjectPoolConfig { Name = PoolNames.ItemPool }
);
```

3. **添加注入逻辑**（在 `EntityManager.InjectResourceData`）：

```csharp
case ItemResource itemRes:
    data.Set("ItemName", itemRes.ItemName);
    data.Set("Value", itemRes.Value);
    break;
```

### 5.2 新增查询方法

```csharp
// 示例：查询所有精英敌人
public static IEnumerable<Enemy> GetEliteEnemies()
{
    return GetEntitiesByType<Enemy>("Enemy")
        .Where(e => e.GetData().Get<bool>("IsElite", false));
}

// 示例：查询血量低于 30% 的敌人
public static IEnumerable<Enemy> GetLowHealthEnemies(float threshold = 0.3f)
{
    return GetEntitiesByType<Enemy>("Enemy")
        .Where(e => {
            var data = e.GetData();
            float current = data.Get<float>("CurrentHp");
            float max = data.Get<float>("BaseMaxHp");
            return current / max < threshold;
        });
}
```

## 6. 性能考量

### 6.1 热路径优化

**避免 GC**：

- 使用 `HashSet` 而非 `List`，避免频繁扩容
- 查询方法返回 `IEnumerable`，延迟执行（LINQ 惰性求值）
- 缓存常用查询结果（如 `GetNearestEntity`）

**避免重复计算**：

- `AttributeComponent` 使用脏标记 `_isDirty`
- 范围查询使用 `DistanceSquaredTo()` 避免开方

### 6.2 内存管理

**对象池配置**：

- `InitialSize`：设置为场景平均使用量（如 100 个敌人）
- `MaxSize`：设置为峰值的 1.5 倍（如 500 个敌人）

**清理策略**：

- 场景切换时调用 `EntityManager.Clear()`
- 定期调用 `ObjectPool.Cleanup()` 清理闲置对象

## 7. 最佳实践

### 7.1 Entity 脚本模板

```csharp
public partial class Enemy : CharacterBody2D
{
    private static readonly Log _log = new Log("Enemy");

    // 不需要在 _Ready 中注册（Spawn 已自动注册）
    public override void _Ready()
    {
        // 仅用于编辑器预览的手动实例化
        if (!Engine.IsEditorHint()) return;
        EntityManager.Register(this, "Enemy");
    }

    // 必须在 _ExitTree 中注销
    public override void _ExitTree()
    {
        EntityManager.Unregister(this);
    }

    // 死亡时回收而非销毁
    private void Die()
    {
        EntityManager.Recycle(this);
    }
}
```

### 7.2 SpawnSystem 模板

```csharp
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _enemyResource;
    [Export] private float _spawnInterval = 2.0f;

    private float _timer = 0f;

    public override void _Process(double delta)
    {
        _timer += (float)delta;
        if (_timer >= _spawnInterval)
        {
            _timer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        var pos = GetRandomPosition();

        // 明确指定池名称，带位置的生成
        var enemy = EntityManager.Spawn<Enemy>(PoolNames.EnemyPool, _enemyResource, pos);

        // 可选：额外配置
        if (enemy != null)
        {
            enemy.SetTarget(GetPlayer());
        }
    }
}
```

### 7.3 不同 Entity 类型的生成示例

```csharp
// 1. 生成敌人（带位置）
var enemy = EntityManager.Spawn<Enemy>(PoolNames.EnemyPool, enemyResource, spawnPosition);

// 2. 发射子弹（带位置和方向）
var direction = (target - player.GlobalPosition).Normalized();
var angle = direction.Angle();
var bullet = EntityManager.Spawn<Bullet>(PoolNames.BulletPool, bulletResource, player.GlobalPosition, angle);

// 3. 给玩家添加 Buff（无位置，指定父节点）
var buff = EntityManager.Spawn<Buff>(PoolNames.BuffPool, buffResource, player);

// 4. 生成背包物品（无位置）
var item = EntityManager.Spawn<Item>(PoolNames.ItemPool, itemResource);

// 5. 掉落物品（带位置）
var dropItem = EntityManager.Spawn<Item>(PoolNames.ItemPool, itemResource, enemy.GlobalPosition);

// 6. 生成特效（带位置和旋转）
var effect = EntityManager.Spawn<Effect>(PoolNames.EffectPool, effectResource, hitPosition, hitAngle);
```

## 8. 总结

EntityManager 的增强设计实现了：

1. **统一入口**：所有 Entity 生成都通过 `Spawn()` 方法
2. **明确池名称**：通过参数明确指定对象池，避免自动推断的不确定性
3. **灵活重载**：三个 Spawn 重载适应不同 Entity 类型（纯 Node、Node2D、带方向）
4. **自动化流程**：从获取实例到数据注入到注册管理，一气呵成
5. **解耦合**：SpawnSystem 不需要知道 ObjectPool 和 Data 的细节
6. **易扩展**：新增 Entity 类型只需添加一个 `case` 分支，无需维护映射表
7. **高性能**：基于索引的查询系统，支持大规模实体管理

这种设计符合"单一职责"和"开闭原则"，为项目的长期维护奠定了坚实基础。
