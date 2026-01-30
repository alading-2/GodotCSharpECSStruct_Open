# Entity 架构设计理念

**文档类型**：架构设计  
**目标受众**：架构师、新成员、AI 助手  
**最后更新**：2026-01-05

---

## 核心理念

本项目的 Entity 架构基于 Godot 哲学：**Scene 即 Entity**。

**核心原则**：
 
 - **Entity = .tscn 文件**：每个游戏对象（Player.tscn, Enemy.tscn）都是独立场景
 - **Component = 子节点**：实现 `IComponent` 接口的功能模块
 - **Entity 是纯容器**：仅仅持有 Data 和 Events，严禁包含业务逻辑
 - **零继承污染**：通过接口而非继承实现能力扩展
 - **统一管理**：EntityManager 作为唯一入口，管理生命周期和数据访问

**管理体系**：

- **EntityManager**：统一管理 Entity 和 Component 的生命周期（生成、注册、查询、销毁）
- **Data 容器**：为 Entity 提供动态数据存储能力
- **EntityRelationshipManager**：管理 Entity-Component、Entity-Entity 之间的关系

---

## 为什么不用 Entity 基类？

### 问题：继承体系冲突

传统做法中，所有 Entity 继承同一个基类：

```csharp
// ❌ 传统做法：强制继承
public abstract class Entity : Node
{
    public Data Data { get; } = new Data();
    public string EntityId { get; set; }
}

// 问题：无法同时继承 Entity 和 CharacterBody2D
public class Player : Entity { }  // ❌ 但 Player 需要 CharacterBody2D 的物理能力
public class Enemy : Entity { }   // ❌ Enemy 也需要 CharacterBody2D
```

**冲突原因**：

- Player 需要 `CharacterBody2D`（物理移动、碰撞检测）
- Bullet 需要 `Area2D`（区域检测）
- Buff 只需要 `Node`（纯逻辑）
- **C# 不支持多继承，无法统一继承同一个基类**

### 解决方案：接口 + 组合

使用 `IEntity` 接口而非基类：

```csharp
// ✅ 接口定义能力
public interface IEntity
{
    Data Data { get; }
    string EntityId { get; }
}

// ✅ 每个 Entity 选择最合适的根节点类型
public partial class Player : CharacterBody2D, IEntity { }  // 物理实体
public partial class Bullet : Area2D, IEntity { }          // 区域检测
public partial class Buff : Node, IEntity { }              // 纯逻辑
```

**优势**：

1. **零继承污染**：符合 Godot "组合优于继承" 哲学
2. **类型自由**：每个 Entity 选择最合适的根节点类型
3. **易扩展**：新增 Entity 无需修改框架代码
4. **符合 Godot 设计**：完全利用 Scene 系统

---

## 架构分层

### 1. 接口层（标记和约定）

#### IEntity 接口

```csharp
public interface IEntity
{
    /// <summary>
    /// 动态数据容器 - 存储运行时数据
    /// </summary>
    Data Data { get; }

    /// <summary>
    /// Entity 唯一标识符（通常是 GetInstanceId().ToString()）
    /// </summary>
    string EntityId { get; }
}
```

**职责**：标记一个 Node 是 Entity，并提供数据容器。

**实现模板**：

```csharp
public partial class Enemy : CharacterBody2D, IEntity
{
    public Data Data { get; private set; } = new Data();
    public string EntityId { get; private set; } = string.Empty;

    public override void _Ready()
    {
        EntityId = GetInstanceId().ToString();
    }
}
```

#### IComponent 接口

```csharp
public interface IComponent
{
    /// <summary>
    /// Component 注册到 Entity 时的回调
    /// 在此方法中缓存 Entity 引用和 Data 容器
    /// </summary>
    void OnComponentRegistered(Node entity);

    /// <summary>
    /// Component 从 Entity 注销时的回调
    /// </summary>
    void OnComponentUnregistered();
}
```

**职责**：标记一个 Node 是 Component，并提供生命周期回调。

**标准实现模板**（2026-01-05 更新）：

```csharp
public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(HealthComponent));

    // ================= 标准字段 =================
    private Data? _data;
    private IEntity? _entity;

    // ================= IComponent 实现 =================

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
        // 清理引用
        _data = null;
        _entity = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // ✅ 仅做日志或信号连接
        // ❌ 不做验证或懒加载（OnComponentRegistered 已保证初始化）
        _log.Debug("组件就绪");
    }
}
```

**关键规则**：

1. **统一使用 `_entity` 引用**：不再使用 `_owner`、`_body` 等多个引用字段
2. **`OnComponentRegistered` 负责全部初始化**：缓存 `_data` 和 `_entity`
3. **`_Ready` 不做验证**：ObjectPool 和 EntityManager 保证了注册时序
4. **需要特定类型时使用类型判断**：`if (_entity is CharacterBody2D body)`

### 2. 管理层（生命周期控制）

#### EntityManager（统一入口）

**职责**：

1. **生成**：`Spawn<T>(poolName, resource, position)` - 从对象池获取并自动配置
2. **注册**：自动注册 Entity 和 Component，建立关系
3. **查询**：提供类型查询、范围查询、关系查询等接口
4. **销毁**：`Destroy(entity)` - 注销并归还对象池

**设计动机**：

- **统一入口**：所有 Entity/Component 操作都通过 EntityManager
- **自动化**：自动完成数据注入、组件注册、关系建立
- **数据源唯一**：所有查询基于统一的注册表

**生命周期流程**（2026-01-05 更新）：

```
ObjectPool.CreateNew()
   ↓
_createFunc() 实例化
   ↓
检查 is IEntity? → YES
   ↓
EntityManager.Register(Entity)
   ↓
EntityManager.RegisterComponents(Entity)
   ├─ 识别 Component
   ├─ Register(Component)
   ├─ AddRelationship(Entity→Component)
   └─ OnComponentRegistered(Entity) ← 组件缓存 _data 和 _entity
   ↓
AddChild 挂载到场景树
   ↓
Godot._Ready 执行（此时组件已完全初始化）
   ↓
EntityManager.Spawn 注入数据
```

#### EntityRelationshipManager（关系管理）

**职责**：

- 管理 Entity-Component 关系
- 管理 Entity-Entity 关系（如 Player-Item）
- 提供反向查询能力（通过 Component 查找 Entity）

### 3. 数据层（运行时存储）

#### Data 容器

**职责**：为 Entity 提供动态数据存储能力。

**特点**：

- 键值对存储，支持任意类型
- 支持值变更监听
- 支持从 Resource 批量加载数据

**示例**：

```csharp
// 读取数据
float hp = entity.Data.Get<float>(DataKey.CurrentHp);

// 写入数据
entity.Data.Set(DataKey.CurrentHp, 80f);

// 监听变化
entity.Data.On(DataKey.CurrentHp, (oldVal, newVal) => {
    // 更新 UI
});
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
        // EntityManager 自动触发此回调
    }

    public void OnComponentUnregistered()
    {
        // 清理资源
    }
}
```

**优势**：

- 明确的语义标记
- 自动获取 Entity 引用
- 生命周期回调

### 2. 命名约定

类名以 `Component` 结尾自动识别：

```csharp
public partial class VelocityComponent : Node { }  // 自动识别为 Component
```

**优势**：兼容旧代码，无需修改

---

## 数据流转设计

```
1. 静态配置（编辑器，设计时）
   ┌─────────────────────────┐
   │ EnemyResource.tres      │
   │ ├─ MaxHp = 100          │
   │ ├─ Speed = 200          │
   │ └─ VisualScene = ...    │
   └─────────────────────────┘
              ↓

2. 生成时注入（自动，运行时）
   ┌─────────────────────────┐
   │ ObjectPool.CreateNew()  │
   │ ├─ 实例化 Entity        │
   │ ├─ Register(Entity)     │
   │ ├─ RegisterComponents   │
   │ └─ AddChild 挂载        │
   └─────────────────────────┘
              ↓
   ┌─────────────────────────┐
   │ EntityManager.Spawn()   │
   │ ├─ 从对象池获取         │
   │ ├─ 反射注入 Resource    │ → Data.LoadFromResource(resource)
   │ ├─ 加载 VisualScene     │
   │ └─ 返回实例             │
   └─────────────────────────┘
              ↓

3. 运行时数据（动态，游戏逻辑）
   ┌─────────────────────────┐
   │ Entity.Data             │
   │ ├─ Set(DataKey.CurrentHp, 80) │ → 事件触发
   │ ├─ Get<float>(DataKey.Speed)  │
   │ └─ Add(DataKey.Score, 10)     │
   └─────────────────────────┘
              ↓

4. 组件响应（事件驱动）
   ┌─────────────────────────┐
   │ HealthComponent         │
   │ └─ 监听 OnValueChanged  │ → 自动响应逻辑
   │                         │
   │ UI 系统                 │
   │ └─ 监听 OnValueChanged  │ → 更新显示
   └─────────────────────────┘
```

**关键点**：

1. **Resource 是静态配置**：在编辑器中设计，不可运行时修改
2. **Data 是运行时数据**：存储游戏过程中的动态变化
3. **自动注入**：EntityManager 自动将 Resource 数据复制到 Data
4. **事件驱动**：Data 变更时自动触发监听器
5. **注册前置**：ObjectPool 在创建时立即注册，确保 `_Ready` 时组件已初始化

---

## Component 访问 Entity 的模式

### 标准模式：通过 IComponent 回调缓存（推荐）

```csharp
public partial class HealthComponent : Node, IComponent
{
    private Data? _data;
    private IEntity? _entity;

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    public void TakeDamage(float amount)
    {
        if (_data == null) return;

        float hp = _data.Get<float>(DataKey.CurrentHp);
        _data.Set(DataKey.CurrentHp, hp - amount);
    }
}
```

**优势**：

- 性能最高（预先缓存）
- 明确的生命周期管理
- 符合接口语义
- 统一命名规范

### 需要特定类型时使用类型判断

```csharp
public partial class VelocityComponent : Node, IComponent
{
    private Data? _data;
    private IEntity? _entity;

    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;
        }
    }

    public override void _Process(double delta)
    {
        // 需要 CharacterBody2D 时再转换
        if (_entity is not CharacterBody2D body) return;

        Vector2 inputDir = InputManager.GetMoveInput();
        body.Velocity = inputDir.Normalized() * Speed;
        body.MoveAndSlide();
    }
}
```

**优势**：

- 统一使用 `_entity` 引用
- 需要时再转换具体类型
- 代码清晰，易于维护

**推荐策略**：

- **优先使用标准模式**（IComponent 回调缓存），性能最佳
- **统一使用 `_entity` 引用**，不使用 `_owner`、`_body` 等多个字段
- **需要特定类型时使用类型判断**（`if (_entity is CharacterBody2D body)`）
- **`_Ready` 不做验证**，ObjectPool 保证了初始化时序

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
| 需要 Data 容器       | ✅     | ❌        |

**判断流程**：

```
1. 是否可以独立存在？
   ├─ 是 → Entity（如 Enemy、Item、Bullet）
   └─ 否 → 继续判断

2. 是否依附于其他对象？
   ├─ 是 → Component（如 HealthComponent、VelocityComponent）
   └─ 否 → 可能是普通 Node

3. 是否需要 Resource 配置？
   ├─ 是 → Entity
   └─ 否 → Component
```

**实例**：

- **Entity**：Player、Enemy、Weapon、Item、Bullet、Buff、Skill
- **Component**：HealthComponent、VelocityComponent、HitboxComponent、PickupComponent

**边界案例**：

- **Weapon**：即使装备在 Player 上，仍是 Entity（有独立配置、可拾取/丢弃）
- **Buff**：虽然依附于 Unit，但有独立配置和生命周期，是 Entity
- **Hitbox**：纯功能模块，无独立配置，是 Component

---

## 设计优势

### 1. 符合 Godot 哲学

- **Scene 系统原生支持**：完全利用 .tscn 文件的可视化编辑
- **零学习成本**：开发者无需学习新的框架语法
- **编辑器友好**：所有配置都在编辑器中可见

### 2. 零继承污染

```csharp
// ✅ 自由选择根节点类型
Player : CharacterBody2D, IEntity      // 物理实体
Bullet : Area2D, IEntity               // 区域检测
Buff   : Node, IEntity                 // 纯逻辑
UI     : Control, IEntity              // UI 实体（如果需要）
```

### 3. 高扩展性

新增 Entity 类型只需：

1. 创建 `.tscn` 场景文件
2. 创建 C# 脚本实现 `IEntity`
3. 注册对象池（如果需要）

**无需修改框架代码！**

### 4. 高性能

- **索引查询**：O(1) 查询速度（基于 Dictionary）
- **对象池集成**：避免频繁的 new/GC
- **事件驱动**：按需响应，减少轮询

### 5. 易维护

- **职责清晰**：EntityManager 管理生命周期，Data 管理数据
- **模块解耦**：Entity、Component、Manager 职责分离
- **统一入口**：所有操作通过 EntityManager

---

## 架构对比

| 特性         | 传统 ECS       | 本项目 (Scene 即 Entity)  |
| ------------ | -------------- | ------------------------- |
| Entity 定义  | C# 类 + ID     | .tscn 文件 + IEntity      |
| 组件管理     | 组件数组       | EntityManager + 关系管理  |
| 继承污染     | 有（需要基类） | 无（接口实现）            |
| 编辑器支持   | 弱             | 强（可视化配置）          |
| Godot 兼容性 | 需要大量适配   | 完美契合                  |
| 性能         | 高（纯数据）   | 高（索引 + 对象池）       |
| 维护成本     | 高（框架复杂） | 低（利用原生系统）        |
| 学习成本     | 高             | 低（Godot 开发者 0 成本） |

---

## 常见误区

### 误区 1：Component 需要 Data 容器

❌ **错误认知**：Component 也应该有 Data 容器。

✅ **正确理解**：只有 Entity 拥有 Data，Component 通过 EntityManager 访问 Entity 的 Data。

**原因**：

- Component 是功能模块，无状态（或状态很少）
- 所有运行时数据应存储在 Entity.Data 中
- 避免数据分散，保持单一数据源

### 误区 2：直接使用 GetParent() 查找 Entity

❌ **不推荐**：

```csharp
var entity = GetParent() as IEntity;  // 假设父节点就是 Entity
```

✅ **推荐**：

```csharp
// 通过 EntityManager 查询（支持任意层级）
var entity = EntityManager.GetEntityByComponent(this);
```

**原因**：

- EntityManager 支持 Component 在容器节点下（如 `Entity/Component/HealthComponent`）
- 更灵活，支持动态添加的 Component
- 数据源唯一（EntityRelationshipManager）

### 误区 3：手动注册 Component

❌ **错误做法**：

```csharp
public override void _Ready()
{
    EntityManager.Register(this, "HealthComponent");  // 不需要！
}
```

✅ **正确理解**：

- EntityManager.Spawn() 会自动注册所有 Component
- 只有特殊情况（如单例 Player）才需要手动注册 Entity

---

## 设计哲学

### 1. 组合优于继承

```csharp
// ❌ 继承：固定结构
class Enemy : Entity { }

// ✅ 组合：灵活结构
Enemy.tscn
├── HealthComponent
├── VelocityComponent
└── HitboxComponent
```

### 2. 接口定义能力

- `IEntity`：标记为实体，提供 Data 容器
- `IComponent`：标记为组件，提供生命周期回调
- `IPoolable`：标记为可池化对象，提供池化回调

### 3. 统一入口管理

所有操作通过 EntityManager：

- 生成：`EntityManager.Spawn()`
- 查询：`EntityManager.GetEntitiesByType()`
- 销毁：`EntityManager.Destroy()`

### 4. 数据驱动设计

- 静态配置：Resource（编辑器设计）
- 运行时数据：Data（游戏逻辑，支持修改器和计算属性）

---

## 相关文档

- **API 使用指南**：[`Src/ECS/Entity/Core/README.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Src/ECS/Entity/Core/README.md)
- **EntityManager 详细设计**：[`Docs/框架/ECS/Entity/EntityManager设计说明.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/ECS/Entity/EntityManager设计说明.md)
- **项目规则**：[`.agent/rules/projectrules.md`](file:///e:/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/.agent/rules/projectrules.md)

---

**维护者**：项目团队  
**文档版本**：v3.0  
**创建日期**：2026-01-04
