# Entity 架构设计理念

**文档类型**：架构设计  
**目标受众**：架构师、新成员、AI 助手  
**最后更新**：2025-01-01

---

## 核心理念

本项目的 Entity 架构基于 Godot 哲学：**Scene 即 Entity**。

- **Entity = .tscn 文件**：每个游戏对象（Player.tscn, Enemy.tscn）都是独立场景
- **Component = 子节点**：实现 `IComponent` 接口的功能模块
- **EntityManager**：统一管理生命周期（生成、注册、查询、销毁）
- **EntityRelationshipManager**：管理所有关系（Entity-Component、Entity-Entity）

---

## 为什么不用 Entity 基类？

### 问题

```csharp
// ❌ 传统做法：强制继承
public abstract class Entity : Node { }
public class Player : Entity { }  // 但 Player 需要 CharacterBody2D
public class Enemy : Entity { }   // Enemy 也需要 CharacterBody2D
```

**冲突**：

- Player 需要 `CharacterBody2D`（物理移动）
- Bullet 需要 `Area2D`（碰撞检测）
- Buff 只需要 `Node`（纯逻辑）
- **无法统一继承同一个基类**

### 解决方案

```
Player.tscn  → CharacterBody2D（根节点类型自由选择）
Enemy.tscn   → CharacterBody2D
Bullet.tscn  → Area2D
Buff.tscn    → Node
```

**优势**：

- 零继承污染，符合 Godot 组合优于继承哲学
- 每个 Entity 选择最合适的根节点类型
- 新增 Entity 无需修改框架代码

---

## 三层架构

### 1. Scene 层（Entity 定义）

Entity 以 .tscn 文件存在，在编辑器中可视化配置：

```
Enemy.tscn (CharacterBody2D)
├── HealthComponent
├── VelocityComponent
├── AttributeComponent
└── CollisionShape2D
```

### 2. Manager 层（生命周期管理）

**EntityManager**：

- `Spawn<T>(poolName, resource, position)`：生成 Entity
- `Register(entity, type)`：注册到全局索引
- `GetEntitiesByType<T>(type)`：类型查询
- `Destroy(entity)`：回收到对象池

**EntityRelationshipManager**：

- `AddRelationship(parentId, childId, type)`：建立关系
- `GetChildEntitiesByParentAndType()`：查询子实体
- `GetParentEntitiesByChildAndType()`：反向查询

### 3. Extension 层（能力扩展）

通过扩展方法为所有 Node 提供能力：

```csharp
// NodeExtensions
var data = entity.GetData();  // 动态数据容器
var health = entity.Component().HealthComponent;  // 组件访问

// EntityManager
var entity = EntityManager.GetEntityByComponent(component);  // Component 查找 Entity
```

---

## Component 识别机制

EntityManager 自动识别 Component 的三种方式（按优先级）：

### 1. IComponent 接口（推荐）

```csharp
public partial class HealthComponent : Node, IComponent
{
    public void OnComponentRegistered(Node entity)
    {
        // Entity-Component 关系已由 EntityManager 自动建立
    }

    public void OnComponentUnregistered()
    {
        // 清理资源
    }
}
```

### 2. 命名约定

类名以 `Component` 结尾自动识别：

```csharp
public partial class VelocityComponent : Node { }  // 自动识别
```

### 3. 白名单（特殊情况）

在 `ECSIndex` 中注册特殊类型：

```csharp
ECSIndex.RegisterComponentType("Hitbox");
```

---

## 数据流转

```
1. 静态配置（编辑器）
   EnemyResource.tres
   ├── MaxHp = 100
   ├── Speed = 200
   └── Damage = 10

2. 生成时注入（自动）
   EntityManager.Spawn()
   └── InjectResourceData()  // 反射注入所有属性到 Data

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

## Component 查找 Entity

### 推荐方式：EntityManager

```csharp
public partial class HealthComponent : Node, IComponent
{
    public void TakeDamage(float damage)
    {
        // 通过 EntityManager 查找所属 Entity
        var entity = EntityManager.GetEntityByComponent(this);
        if (entity == null) return;

        var data = entity.GetData();
        float currentHp = data.Get<float>("CurrentHp");
        data.Set("CurrentHp", currentHp - damage);
    }
}
```

**优势**：

- 兼容任意层级（Component 可以在容器 Node 下）
- 支持反向查询（通过 Component 类型查找所有 Entity）
- 数据源唯一（EntityRelationshipManager）

### 备用方式：GetParent()

```csharp
// 仅限简单场景（Component 是直接子节点）
private Node? _entity;

public void OnComponentRegistered(Node entity)
{
    _entity = entity;
}
```

---

## Entity vs Component 判断标准

| 特征                 | Entity | Component |
| -------------------- | ------ | --------- |
| 有独立 Resource 配置 | ✅     | ❌        |
| 可独立存在           | ✅     | ❌        |
| 有自己的属性         | ✅     | ❌        |
| 可被装备/拾取        | ✅     | ❌        |
| 提供功能模块         | ❌     | ✅        |
| 依附于其他对象       | ❌     | ✅        |

**实例**：

- **Entity**：Player、Enemy、Weapon、Item、Bullet、Buff
- **Component**：HealthComponent、VelocityComponent、AttributeComponent

---

## 设计优势

1. **符合 Godot 哲学**：完全利用 Scene 系统，零学习成本
2. **零继承污染**：每个 Entity 自由选择根节点类型
3. **高扩展性**：新增 Entity 只需创建 .tscn 和 .cs 文件
4. **高性能**：基于索引查询（O(1)）+ 对象池集成
5. **易维护**：职责清晰，模块解耦

---

## 架构对比

| 特性         | 传统 ECS       | 本项目 (Scene 即 Entity)  |
| ------------ | -------------- | ------------------------- |
| Entity 定义  | C# 类          | .tscn 文件                |
| 组件管理     | 组件数组       | EntityRelationshipManager |
| 继承污染     | 有             | 无                        |
| 编辑器支持   | 弱             | 强                        |
| Godot 兼容性 | 需要大量适配   | 完美契合                  |
| 性能         | 高（纯数据）   | 高（索引 + 对象池）       |
| 维护成本     | 高（框架复杂） | 低（利用原生系统）        |

---

## 相关文档

- **API 使用指南**：`Src/ECS/Entity/Core/README.md`
- **EntityManager 详细设计**：`Docs/框架/ECS/Entity/EntityManager设计说明.md`
- **项目规则**：`.trae/rules/project_rules.md`

---

**维护者**：项目团队  
**文档版本**：v2.0  
**创建日期**：2025-01-01
