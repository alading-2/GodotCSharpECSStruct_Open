# Entity 系统 - 使用指南

**文档类型**：API 文档  
**目标受众**：开发者  
**最后更新**：2025-01-01

---

## 概述

Entity 系统提供统一的实体生命周期管理和关系管理。

**核心模块**：

- **EntityManager**：生命周期管理（生成、注册、查询、销毁）
- **EntityRelationshipManager**：关系管理（Entity-Component、Entity-Entity）

**设计理念**：详见 `Docs/框架/ECS/Entity/Entity架构设计理念.md`

---

## 快速开始

### 生成 Entity

```csharp
// 生成敌人（带位置）
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", enemyResource, spawnPosition);

// 生成 Buff（无位置）
var buff = EntityManager.Spawn<Buff>("BuffPool", buffResource);

// 发射子弹（带位置和方向）
var bullet = EntityManager.Spawn<Bullet>("BulletPool", bulletResource, position, angle);
```

### Entity 脚本模板

```csharp
public partial class Enemy : CharacterBody2D
{
    private static readonly Log _log = new("Enemy");

    // Spawn 已自动注册，无需手动调用
    public override void _Ready() { }

    // 必须：注销自己
    public override void _ExitTree()
    {
        EntityManager.UnregisterEntity(this);
    }

    // 死亡时回收到对象池
    private void Die()
    {
        EntityManager.Destroy(this);
    }
}
```

### Component 脚本模板

```csharp
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    // 可选：获取 Entity 引用
    public void OnComponentRegistered(Node entity)
    {
        _log.Debug($"注册到 Entity: {entity.Name}");
    }

    // 可选：清理资源
    public void OnComponentUnregistered()
    {
        _log.Debug("Component 注销");
    }

    // 通过 EntityManager 查找 Entity
    public void TakeDamage(float damage)
    {
        var entity = EntityManager.GetEntityByComponent(this);
        if (entity == null) return;

        var data = entity.GetData();
        float currentHp = data.Get<float>("CurrentHp");
        data.Set("CurrentHp", currentHp - damage);
    }
}
```

---

## API 文档

### EntityManager

#### Spawn<T>() - 生成 Entity

**三个重载**：

```csharp
// 1. 无位置（Buff、背包物品）
Spawn<T>(string poolName, Resource resource)

// 2. 带位置（Enemy、掉落物品）
Spawn<T>(string poolName, Resource resource, Vector2 position)

// 3. 带位置和旋转（Bullet、投射物）
Spawn<T>(string poolName, Resource resource, Vector2 position, float rotation)
```

**参数**：

- `poolName`：对象池名称（必须，如 "EnemyPool"）
- `resource`：静态配置 Resource
- `position`：初始位置（可选）
- `rotation`：初始旋转角度（可选）

**返回**：已配置好的 Entity 实例

**示例**：

```csharp
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", enemyResource, spawnPos);
```

#### 查询方法

```csharp
// 按类型查询所有 Entity
GetEntitiesByType<T>(string entityType)

// 按类型查询所有 Component
GetComponentsByType<T>(string componentType)

// 通过 Component 查找 Entity
GetEntityByComponent(Node component)

// 范围查询
GetEntitiesInRange<T>(Vector2 position, float range, string entityType)

// 获取最近的 Entity
GetNearestEntity<T>(Vector2 position, string entityType, float maxRange)
```

**示例**：

```csharp
// 查询所有敌人
var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");

// AI 寻敌
var nearbyEnemies = EntityManager.GetEntitiesInRange<Enemy>(
    playerPos, 500f, "Enemy"
);
```

#### 生命周期管理

```csharp
// 注册 Entity（通常由 Spawn 自动调用）
Register(Node entity, string entityType)

// 注销 Entity（必须在 _ExitTree 中调用）
UnregisterEntity(Node entity)

// 回收到对象池
Destroy(Node entity)

// 清理所有 Entity（场景切换时）
Clear()
```

### EntityRelationshipManager

#### 建立关系

```csharp
AddRelationship(
    string parentId,
    string childId,
    string relationType,
    Dictionary<string, object>? data = null,
    RelationshipConstraint constraint = RelationshipConstraint.None,
    int priority = 0
)
```

**参数**：

- `parentId`：父 Entity ID
- `childId`：子 Entity ID
- `relationType`：关系类型（如 `EntityRelationshipType.UNIT_TO_ITEM`）
- `data`：关系附加数据（可选）
- `constraint`：关系约束（None/OneToOne/OneToMany）
- `priority`：优先级（数值越小优先级越高）

**示例**：

```csharp
// 玩家拾取物品
EntityRelationshipManager.AddRelationship(
    playerId,
    itemId,
    EntityRelationshipType.UNIT_TO_ITEM,
    data: new() { { "Slot", 0 } },
    priority: 0
);
```

#### 查询关系

```csharp
// 查询父 Entity 的所有子 Entity
GetChildEntitiesByParentAndType(string parentId, string relationType)

// 查询子 Entity 的所有父 Entity
GetParentEntitiesByChildAndType(string childId, string relationType)

// 查询关系记录（支持优先级排序）
GetChildRelationshipsByParentAndType(
    string parentId,
    string relationType,
    bool sortByPriority = false
)
```

**示例**：

```csharp
// 查询玩家的所有物品
var itemIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
    playerId,
    EntityRelationshipType.UNIT_TO_ITEM
);

// 查询物品的拥有者
var ownerId = EntityRelationshipManager.GetParentEntitiesByChildAndType(
    itemId,
    EntityRelationshipType.UNIT_TO_ITEM
).FirstOrDefault();
```

#### 移除关系

```csharp
// 移除指定关系
RemoveRelationship(string parentId, string childId, string relationType)

// 移除 Entity 的所有关系
RemoveAllRelationships(string entityId)
```

---

## 使用场景

### SpawnSystem 中生成敌人

```csharp
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _enemyResource;

    private void SpawnEnemy(Vector2 position)
    {
        // 一行代码完成生成
        var enemy = EntityManager.Spawn<Enemy>("EnemyPool", _enemyResource, position);

        // 可选：额外配置
        enemy?.GetData().Set("IsElite", true);
    }
}
```

### 玩家拾取物品

```csharp
public partial class Player : CharacterBody2D
{
    private string _entityId;

    public override void _Ready()
    {
        _entityId = GetInstanceId().ToString();
        EntityManager.Register(this, "Player");
    }

    public void PickupItem(Node itemEntity)
    {
        string itemId = itemEntity.GetInstanceId().ToString();

        // 建立关系
        EntityRelationshipManager.AddRelationship(
            _entityId,
            itemId,
            EntityRelationshipType.UNIT_TO_ITEM
        );
    }

    public IEnumerable<Node> GetAllItems()
    {
        var itemIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            _entityId,
            EntityRelationshipType.UNIT_TO_ITEM
        );

        return itemIds
            .Select(id => EntityManager.GetEntityById(id))
            .Where(item => item != null);
    }
}
```

### 动态添加 Component

```csharp
// 运行时添加 Buff Component
public void ApplySpeedBuff(Node player, float duration)
{
    var buffComp = new SpeedBuffComponent();
    buffComp.Duration = duration;

    // 自动注册并建立关系
    EntityManager.AddComponent(player, buffComp);
}
```

---

## 扩展指南

### 新增 Entity 类型

**1. 创建 Resource**

```csharp
[GlobalClass]
public partial class ItemResource : Resource
{
    [Export] public string ItemName { get; set; }
    [Export] public int Value { get; set; }
}
```

**2. 注册对象池**

```csharp
new ObjectPool<Node>(
    () => itemScene.Instantiate(),
    new ObjectPoolConfig { Name = "ItemPool" }
);
```

**3. 无需修改 EntityManager**

反射自动注入所有 public 属性。

---

## 注意事项

1. **必须注销**：Entity 在 `_ExitTree()` 中必须调用 `EntityManager.UnregisterEntity(this)`
2. **使用 Destroy**：Entity 销毁时使用 `EntityManager.Destroy()` 而不是 `QueueFree()`
3. **关系自动清理**：`Destroy()` 会自动清理所有关系
4. **池名称**：必须明确指定对象池名称，避免硬编码
5. **Component 识别**：推荐实现 `IComponent` 接口，或类名以 "Component" 结尾

---

## 性能优化

### 缓存查询结果

```csharp
// ❌ 避免：每帧查询
public override void _Process(double delta)
{
    var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");
}

// ✅ 推荐：缓存查询
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

---

## 相关文档

- **架构设计**：`Docs/框架/ECS/Entity/Entity架构设计理念.md`
- **详细设计**：`Docs/框架/ECS/Entity/EntityManager设计说明.md`
- **项目规则**：`.trae/rules/project_rules.md`

---

**维护者**：项目团队  
**文档版本**：v2.0  
**创建日期**：2025-01-01
