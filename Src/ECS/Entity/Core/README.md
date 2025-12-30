# Entity 系统说明文档

## 📋 概述

Entity 系统是 Brotato 复刻项目的核心模块，基于 **Scene 即 Entity** 的伪 ECS 设计理念。

## 🎯 核心概念

- **Entity = Scene (.tscn)**：每个游戏对象都是独立的场景文件
- **EntityManager**：统一的生命周期管理入口（生成、注册、查询、销毁）
- **EntityRelationshipManager**：管理 Entity 间的关系（父子、拥有、装备等）
- **Component = 子节点**：通过 NodeExtensions 动态挂载功能模块

## 📁 文件结构

```
Src/ECS/Entity/Core/
├── EntityManager.cs                    # ✅ 已完成 - 实体管理器
├── EntityRelationshipManager.cs       # ✅ 已完成 - 关系管理器
└── EntityRelationshipType.cs          # ✅ 已完成 - 关系类型常量

Docs/框架/ECS/Entity/
├── README.md                           # 本文档
├── Entity架构深度解析.md              # 架构设计详解
└── EntityManager设计说明.md           # EntityManager 详细说明
```

## 🚀 快速开始

### 1. 生成 Entity

```csharp
// 生成敌人（带位置）
var enemy = EntityManager.Spawn<Enemy>(enemyResource, spawnPosition);

// 生成 Buff（无位置，挂载到玩家）
var buff = EntityManager.Spawn<Buff>(buffResource, player);

// 发射子弹（带位置和方向）
var bullet = EntityManager.Spawn<Bullet>(bulletResource, position, angle);
```

### 2. 建立关系

```csharp
// 玩家拾取物品
EntityRelationshipManager.AddRelationship(
    playerId,
    itemId,
    EntityRelationshipType.UNIT_TO_ITEM
);

// 查询玩家的所有物品
var itemIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
    playerId,
    EntityRelationshipType.UNIT_TO_ITEM
);
```

### 3. Entity 脚本模板

```csharp
public partial class Enemy : CharacterBody2D
{
    private static readonly Log _log = new("Enemy");

    public override void _Ready()
    {
        // EntityManager.Spawn 已自动注册，无需手动调用
    }

    public override void _ExitTree()
    {
        // 必须：注销自己
        EntityManager.Unregister(this);
    }

    private void Die()
    {
        // 回收到对象池（而不是 QueueFree）
        EntityManager.Recycle(this);
    }
}
```

## 📚 详细文档

### [Entity 架构深度解析.md](./Entity架构深度解析.md)

**内容**：

- Scene 即 Entity 的设计理念
- 为什么不使用 Entity 基类
- 三层架构（Scene + Manager + Extension）
- EntityManager 完整实现
- EntityRelationshipManager 三索引结构
- 数据驱动：Data 与 AttributeComponent
- 架构总结与优势

**适用人群**：所有开发者（必读）

### [EntityManager 设计说明.md](./EntityManager设计说明.md)

**内容**：

- 设计动机与解决方案
- 核心方法详解（Spawn、InjectResourceData、Register、Recycle）
- 与其他系统的协作（ObjectPool、AttributeComponent、SpawnSystem）
- 扩展性设计
- 性能考量
- 最佳实践

**适用人群**：深入了解 EntityManager 实现细节的开发者

## ⚡ 核心特性

### 1. 三个 Spawn 方法重载

| 方法签名                                    | 适用场景      | 示例            |
| ------------------------------------------- | ------------- | --------------- |
| `Spawn<T>(Resource, Node?)`                 | 无位置 Entity | Buff、背包物品  |
| `Spawn<T>(Resource, Vector2, Node?)`        | 带位置 Entity | Enemy、掉落物品 |
| `Spawn<T>(Resource, Vector2, float, Node?)` | 带方向 Entity | Bullet、投射物  |

### 2. 自动化流程

```
EntityManager.Spawn()
    ↓
ObjectPool.Get()        # 从对象池获取实例
    ↓
InjectResourceData()    # 注入 Resource 配置到 Data
    ↓
Register()              # 自动注册到 EntityManager
    ↓
返回已配置好的实例
```

### 3. 三索引关系管理

EntityRelationshipManager 采用高效的三索引结构：

- **主存储**：`relationshipId -> RelationshipRecord`
- **父索引**：`parentEntityId -> Set<relationshipId>`
- **子索引**：`childEntityId -> Set<relationshipId>`
- **类型索引**：`relationType -> Set<relationshipId>`

支持多维度查询：

- 查询父 Entity 的所有子 Entity（如玩家的所有物品）
- 查询子 Entity 的所有父 Entity（如物品的拥有者）
- 按关系类型查询所有关系

## 🎨 使用场景

### SpawnSystem 中生成敌人

```csharp
public partial class SpawnSystem : Node
{
    [Export] private EnemyResource _enemyResource;

    private void SpawnEnemy(Vector2 position)
    {
        // 一行代码完成：获取实例 + 数据注入 + 注册
        var enemy = EntityManager.Spawn<Enemy>(_enemyResource, position);

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

        return itemIds.Select(id => EntityManager.GetEntityById(id));
    }
}
```

## 🔧 扩展指南

### 新增 Entity 类型

只需三步：

1. **创建 Resource**

```csharp
[GlobalClass]
public partial class ItemResource : Resource
{
    [Export] public string ItemName { get; set; }
    [Export] public int Value { get; set; }
}
```

2. **注册对象池**（在 `ObjectPoolInit.cs`）

```csharp
new ObjectPool<Node>(
    () => itemScene.Instantiate(),
    new ObjectPoolConfig { Name = PoolNames.ItemPool }
);
```

3. **添加注入逻辑**（在 `EntityManager.InjectResourceData`）

```csharp
case ItemResource itemRes:
    data.Set("ItemName", itemRes.ItemName);
    data.Set("Value", itemRes.Value);
    break;
```

## ⚠️ 注意事项

1. **必须注销**：Entity 在 `_ExitTree()` 中必须调用 `EntityManager.Unregister(this)`
2. **使用 Recycle**：Entity 销毁时使用 `EntityManager.Recycle()` 而不是 `QueueFree()`
3. **关系清理**：`Recycle()` 会自动清理所有关系，无需手动调用
4. **性能优化**：查询方法返回 `IEnumerable`，使用 LINQ 延迟执行

## 📊 性能特点

- **零 GC 优化**：使用 HashSet、Dictionary 等高效数据结构
- **索引查询**：O(1) 时间复杂度的类型查询
- **对象池集成**：自动复用实例，避免频繁 GC
- **反射缓存**：可缓存 MethodInfo 优化反射调用

## 🔗 相关模块

- **ObjectPool**：`Src/Tools/ObjectPool/ObjectPool.cs`
- **NodeExtensions**：`Src/Tools/Extensions/NodeExtension/NodeExtensions.cs`
- **Data**：`Src/Tools/Data/Data.cs`
- **SpawnSystem**：`Src/ECS/System/Spawn/SpawnSystem.cs`

---

**维护者**：项目团队  
**最后更新**：2024-12-30  
**版本**：v1.0
