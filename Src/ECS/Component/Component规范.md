# Component 规范说明

## 核心原则 (Critical Principles)

> [!IMPORTANT]
> **遵循以下两大原则是写好 Component 的关键**

### 1. 单一职责原则 (Single Responsibility)
*   **只做一件事**：`MoveComponent` 只管移动，`HealthComponent` 只管血量。
*   **低耦合**：Component 不应直接修改其他 Component 的内部状态，而是通过 `Data` 或 `Events` 通信。

### 2. 跨组件通信优先级 (Communication Priority)

> [!IMPORTANT]
> **遵循以下优先级进行跨 Component/System 通信（2026-01-12 更新）**

| 优先级 | 方式 | 适用场景 | 示例 |
|:---:|:---:|:---|:---|
| 1️⃣ | **Event** | 解耦通信,发送者无需知道接收者 | `Entity.Events.Emit(HealRequest)` |
| 2️⃣ | **Data** | 状态读写,无状态查询 | `_data.Get<float>(DataKey.CurrentHp)` |
| 3️⃣ | **GetComponent** | 必须直接调用方法时 | 极少场景,尽量避免 |

**Event 优先示例(治疗)**:
```csharp
// ✅ 正确:发送治疗请求事件,由 HealthComponent 监听处理
entity.Events.Emit(GameEventType.Unit.HealRequest,
    new GameEventType.Unit.HealRequestEventData(amount, HealSource.Regen));

// ❌ 错误:获取组件后直接调用方法(增加耦合)
var health = EntityManager.GetComponent<HealthComponent>(entity);
health?.ApplyHeal(new GameEventType.Unit.HealRequestEventData(amount, HealSource.Regen));
```

> [!WARNING]
> **禁止使用 `GetNodeOrNull<T>()` 获取组件**,必须使用 `EntityManager.GetComponent<T>()`

### 3. 纯数据驱动 (Data-Driven)
*   **状态归 Data**：Component 应尽可能 **无状态 (Stateless)**。所有业务状态（HP, Speed, Level）必须存储在 `Entity.Data` 中。
*   **访问模式推荐**：
    *   直接透传 Data，不建立私有字段缓存。
    *   示例：`public float Speed => _data.Get<float>(DataKey.MoveSpeed);`
*   **禁止私有业务状态**：禁止在 Component 中定义 `private int _hp;` 这样的字段存储业务状态。
*   **无需手动 Reset**：
    *   因为 Entity 在销毁/归还池时会自动调用 `Data.Clear()`。
    *   **结论**：只要状态都在 Data 里，Component 就**不需要**写 `Reset()` 方法来重置属性。

### 4. 数据存储规则

> [!IMPORTANT]
> **区分"需要存 Data"和"不需要存 Data"的数据**

**必须存储到 Data 的数据**：
- ✅ 运行时状态（动态变化）：HP、State、Velocity、计时器等
- ✅ 需要在对象池复用时重置的状态
- ✅ 需要被其他 Component/System 访问的状态
- ✅ 需要序列化或同步的数据

**不需要存储到 Data 的数据**：
- ❌ 组件内部的固定配置：`ReviveDuration`、`Acceleration` 等
- ❌ 临时引用：`Target`、`Collector`、`Source` 等
- ❌ UI 或渲染相关的临时状态（不影响游戏逻辑）

### 5. DataKey 使用规范

> [!IMPORTANT]
> **所有 Data 访问必须使用 `DataKey` 常量，禁止使用字符串字面量**

**所有数据统一从 Data 容器读写**：
```csharp
// ❌ 传统 OOP (状态与逻辑耦合)，新建private字段
private float _hp; // 状态
public void TakeDamage(float amount) { _hp -= amount; } // 逻辑

// ❌ 使用字符串字面量（禁止）
public float CurrentHp => _data.Get<float>("CurrentHp");

// ✅ 数据驱动 (状态归 Data，Component 只管逻辑)
// 统一使用 DataKey 常量访问数据
public float CurrentHp => _data.Get<float>(DataKey.CurrentHp);
float hp = _data.Get<float>(DataKey.CurrentHp);
_data.Set(DataKey.CurrentHp, 80f);
_data.Add(DataKey.Score, 10);
```

**新增 DataKey 的步骤**：
1. 在 `DataKey_Unit.cs` 中定义常量：`public const string MyKey = "MyKey";`
2. 在 `UnitDataRegister.cs` 中注册元数据（重要属性必须注册）
3. 注册格式：单行、加注释分隔、不空行

---

## 核心规范

### Component 统一初始化模式（2026-01-05 更新）

**所有 Component 必须遵循以下标准模式**：

```csharp
public partial class MyComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(MyComponent));

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
            
            // ✅ 在此处注册事件监听
            // 例如: _entity.Events.On<T>(GameEventType.XXX, OnEvent);
        }
    }

    public void OnComponentReset()
    {
        // ✅ 对象池复用前的状态重置（由 EntityManager.UnregisterComponents 自动调用）
        // 例如: 重置计时器、清理临时状态、取消正在进行的操作
    }

    public void OnComponentUnregistered()
    {
        // ✅ 无需在此解绑事件（EntityManager 会自动调用 Events.Clear()）
        
        // 清理引用
        _data = null;
        _entity = null;
    }
}
```

### IComponent 生命周期

```
Spawn 流程:
  ┌─ ObjectPool.Get() ─► OnPoolAcquire (Entity)
  │
  ├─ RegisterComponents() ─► OnComponentRegistered (每个 Component)
  │
  └─ Data.LoadFromResource() ─► 注入数据

Destroy 流程:
  ┌─ UnregisterEntity()
  │   ├─ Events.Clear() + Data.Clear()
  │   │
  │   └─ UnregisterComponents()
  │       ├─ OnComponentReset() ─► 重置组件状态
  │       └─ OnComponentUnregistered() ─► 清理引用
  │
  └─ ObjectPool.Return() ─► OnPoolRelease (Entity)
```

> [!IMPORTANT]
> **关系管理策略**：每次 `Destroy` 会移除所有 Entity-Component 关系，每次 `Spawn` 会重新注册。
> 这是简单直接的做法，避免了复杂的状态管理。代码中的"防止重复注册"检查是一道安全保障，
> 正常流程下不会触发（因为 `Destroy` 已经移除了关系）。



167: ### 关键规则

### 数据访问时序 (Data Access Timing)

> [!IMPORTANT]
> **Component 在 `OnComponentRegistered` 时只能访问配置数据**

| 数据类型 | 来源 | 示例 | 在 OnComponentRegistered 中 | 正确处理方式 |
|:---|:---|:---|:---:|:---|
| **配置数据** | Spawn Config (.tres) | MaxHp, Speed | ✅ **可用** | 直接读取 `_data.Get()` |
| **初始数据** | Spawn 后代码设置 | SkillLevel, Target | ❌ **不可用** | 监听 `PropertyChanged` 事件 |

**错误写法**:
```csharp
public void OnComponentRegistered(Node entity)
{
    // ❌ 错误: 假设 TargetPlayer 在注册时已存在
    // 实际上 TargetPlayer 通常在 Spawn 后才设置
    var target = _data.Get<Node>("TargetPlayer"); 
    if (target != null) StartAttack(target);
}
```

**正确写法**:
```csharp
public void OnComponentRegistered(Node entity)
{
    // ✅ 正确: 监听数据变化
    entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
        GameEventType.Data.PropertyChanged, evt => 
        {
            if (evt.Key == "TargetPlayer") 
                StartAttack(evt.NewValue as Node);
        });
}
```

### 关键规则

1. **仅使用 `_entity` 引用**

   - ❌ 禁止：`_owner`、`_body`、多个引用字段
   - ✅ 正确：统一使用 `_entity`，需要时再转换

2. **禁止使用 `GetParent()`**

   - ❌ 禁止：`GetParent()` 或 `GetParent<T>()`
   - ✅ 正确：使用 `OnComponentRegistered` 缓存 `_entity`

3. **`_Ready` 中禁止验证和事件订阅**
   - ❌ 禁止:检查 `_data == null` 或 `_entity == null`
   - ❌ 禁止:订阅 `Data` 或 `Entity.Events` 事件
   - ✅ 正确:`OnComponentRegistered` 保证了初始化,无需验证
   - ✅ 正确:在 `OnComponentRegistered` 中订阅事件(不是`_Ready`)

> [!IMPORTANT]
> **事件订阅必须在 `OnComponentRegistered` 中进行**,因为此时 Entity 和 Component 的关系已建立

### 正确示例

#### 示例 1：VelocityComponent（需要 CharacterBody2D）

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
        // 使用时再转换
        if (_entity is not CharacterBody2D body) return;

        Vector2 inputDir = InputManager.GetMoveInput();
        Vector2 targetVelocity = inputDir.Normalized() * Speed;

        body.Velocity = targetVelocity;
        body.MoveAndSlide();
    }
}
```

#### 示例 2：PickupComponent（需要 Node2D）

```csharp
public partial class PickupComponent : Area2D, IComponent
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

    private void MoveTowardCollector(float delta)
    {
        if (_entity is not Node2D owner || Collector == null)
            return;

        Vector2 direction = Collector.GlobalPosition - owner.GlobalPosition;
        owner.GlobalPosition += direction.Normalized() * MagnetSpeed * delta;
    }
}
```

### 错误示例（禁止）

```csharp
// ❌ 错误：使用多个引用字段
private Node2D? _owner;
private CharacterBody2D? _body;

// ❌ 错误：在 OnComponentRegistered 中判断多个类型
public void OnComponentRegistered(Node entity)
{
    if (entity is IEntity iEntity) _data = iEntity.Data;
    if (entity is Node2D node2D) _owner = node2D;
    if (entity is CharacterBody2D body) _body = body;
}

// ❌ 错误：在 _Ready 中验证或懒加载
public override void _Ready()
{
    if (_data == null)
    {
        _data = EntityManager.GetEntityData(this);
    }
}
```

## Entity 访问 Component

统一使用 EntityManager 方法：

```csharp
// 获取 Component
var health = EntityManager.GetComponent<HealthComponent>(this);

// 动态添加 Component
EntityManager.AddComponent(this, component);

// 移除 Component
EntityManager.RemoveComponent(this, "HealthComponent");
```



## 更新记录

### 2026-01-05

- ✅ 统一所有组件使用 `_entity` 引用
- ✅ 移除 `_Ready` 中的懒加载和验证逻辑
- ✅ 简化 IComponent 接口为纯接口定义
- ✅ ObjectPool 自动注册 IEntity 类型

### 已更新的 Component

1. **VelocityComponent**
2. **HealthComponent**
3. **HitboxComponent**
4. **HurtboxComponent**
5. **PickupComponent**
6. **FollowComponent**
