# EntityManager 设计说明

**文档类型**：详细设计  
**目标受众**：开发者、维护者  
**最后更新**：2025-01-01

---

## 设计动机

EntityManager 是 Entity 系统的统一入口，整合了生成、注册、查询、销毁的完整流程。

### 解决的问题

传统架构中，Entity 生成流程分散：

```
SpawnSystem → ObjectPool.Get() → 手动注入数据 → 手动注册
```

**问题**：职责不清、代码冗余、易出错、难维护

### 解决方案

```
SpawnSystem → EntityManager.Spawn(poolName, resource, position)
                ↓ (内部自动完成)
                ├─ ObjectPool.Get()
                ├─ InjectResourceData()
                ├─ InjectVisualScene()
                ├─ RegisterComponents()
                ├─ Register()
                └─ 返回已配置好的实例
```

---

## 核心职责

| 模块              | 职责                   | 示例                            |
| ----------------- | ---------------------- | ------------------------------- |
| **EntityManager** | 生成、注册、查询、销毁 | `Spawn<Enemy>(poolName, res)`   |
| **ObjectPool**    | 内存管理、对象复用     | `Get()`, `Release()`            |
| **Resource**      | 静态配置（编辑器）     | `EnemyResource.tres`            |
| **Data**          | 运行时数据（动态）     | `node.GetData().Set("HP", 100)` |
| **Component**     | 逻辑模块               | `HealthComponent.TakeDamage()`  |

---

## 数据流转

```
1. 静态配置（编辑器）
   EnemyResource.tres
   ├── MaxHp = 100
   ├── Speed = 200
   └── VisualScene = "res://..."

2. 生成时注入（自动）
   EntityManager.Spawn()
   ├── InjectResourceData()  // 反射注入所有属性
   └── InjectVisualScene()   // 加载视觉场景

3. 运行时数据（动态）
   enemy.GetData()
   ├── Set("CurrentHp", 80)
   └── Get<float>("Speed")

4. 组件响应（事件驱动）
   AttributeComponent
   └── 监听 Data.OnValueChanged
       └── 自动重算最终属性
```

---

## 核心方法

### 1. Spawn<T>() - 三个重载

#### 通用版本（无位置）

```csharp
Spawn<T>(string poolName, Resource resource)
```

**适用**：Buff、背包物品、技能实例

```csharp
var buff = EntityManager.Spawn<Buff>("BuffPool", buffResource);
```

#### 带位置版本

```csharp
Spawn<T>(string poolName, Resource resource, Vector2 position)
```

**适用**：Enemy、掉落物品

```csharp
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", enemyResource, spawnPos);
```

#### 带位置和旋转版本

```csharp
Spawn<T>(string poolName, Resource resource, Vector2 position, float rotation)
```

**适用**：Bullet、投射物

```csharp
var bullet = EntityManager.Spawn<Bullet>("BulletPool", bulletResource, pos, angle);
```

### 2. InjectResourceData() - 反射注入

**功能**：自动将 Resource 的所有 public 属性注入到 Entity.Data

**规则**：

- 跳过 Godot 内置属性（ResourcePath 等）
- 跳过只写属性
- 自动触发 Data.OnValueChanged 事件

**扩展**：新增 Entity 类型无需修改代码，反射自动处理

### 3. InjectVisualScene() - 视觉加载

**功能**：从 Resource.VisualScene 加载 AnimatedSprite2D

**流程**：

1. 清理旧的 VisualRoot（对象池复用时）
2. 实例化并挂载为 "VisualRoot"
3. 设置 ZIndex = 10（确保显示层级）

### 4. RegisterComponents() - 自动注册

**识别规则**（按优先级）：

1. 实现 `IComponent` 接口（推荐）
2. 类名以 "Component" 结尾
3. 在 `ECSIndex` 白名单中

**自动操作**：

- 注册 Component 到 EntityManager
- 建立 Entity-Component 关系（EntityRelationshipManager）
- 触发 `IComponent.OnComponentRegistered()` 回调

### 5. UnregisterEntity() - 注销清理

**功能**：Entity 销毁时的完整清理流程

**流程**：

1. 从 `_entities` 和 `_entitiesByType` 移除
2. 注销所有 Component（通过 EntityRelationshipManager 查询）
3. 清理所有关系（作为父或子）

**注意**：必须在 Entity.\_ExitTree() 中调用

---

## 查询接口

### 按类型查询

```csharp
// 查询所有 Enemy
var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");

// 查询所有 Component
var healthComps = EntityManager.GetComponentsByType<HealthComponent>("HealthComponent");

// 通过 Component 查找 Entity
var entity = EntityManager.GetEntityByComponent(component);
```

### 范围查询

```csharp
// AI 寻敌
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
```

---

## 系统协作

### 与 ObjectPool

```csharp
// 初始化对象池
new ObjectPool<Node>(
    () => enemyScene.Instantiate(),
    new ObjectPoolConfig { Name = "EnemyPool", InitialSize = 100 }
);

// EntityManager 自动使用
var enemy = EntityManager.Spawn<Enemy>("EnemyPool", enemyResource, position);
```

### 与 AttributeComponent

```csharp
// EntityManager 注入数据
data.Set("MaxHp", 100);

// AttributeComponent 自动响应
data.On("MaxHp", (oldVal, newVal) => {
    _isDirty = true;  // 标记需要重算
});
```

### 与 SpawnSystem

```csharp
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _enemyResource;

    private void SpawnWave()
    {
        var pos = GetRandomSpawnPosition();

        // 一行代码完成生成
        var enemy = EntityManager.Spawn<Enemy>("EnemyPool", _enemyResource, pos);
    }
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

反射自动注入所有 public 属性，无需添加 case 分支。

---

## 最佳实践

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

## 性能优化

### 热路径优化

```csharp
// ❌ 避免：每帧查询
public override void _Process(double delta)
{
    var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");
}

// ✅ 推荐：缓存查询结果
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
        _cachedEnemies.AddRange(EntityManager.GetEntitiesByType<Enemy>("Enemy"));
    }
}
```

### 内存管理

- **对象池配置**：InitialSize = 平均使用量，MaxSize = 峰值 × 1.5
- **场景切换**：调用 `EntityManager.Clear()` 清理所有实体
- **定期清理**：调用 `ObjectPoolManager.CleanupAll()` 清理闲置对象

---

## 总结

EntityManager 实现了：

1. **统一入口**：Spawn() 自动完成获取、注入、注册
2. **反射注入**：自动处理所有 Resource 属性，无需手动维护
3. **自动识别**：Component 通过 IComponent 接口自动注册
4. **关系管理**：集成 EntityRelationshipManager，支持复杂查询
5. **高性能**：基于索引查询（O(1)）+ 对象池集成

---

**相关文档**：

- 架构理念：`Docs/框架/ECS/Entity/Entity架构设计理念.md`
- API 使用：`Src/ECS/Entity/Core/README.md`

**维护者**：项目团队  
**文档版本**：v2.0  
**创建日期**：2025-01-01
