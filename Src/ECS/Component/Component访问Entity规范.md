# Component 访问 Entity 规范说明

## 核心规范

### Component 统一初始化模式（2026-01-05 更新）

**所有 Component 必须遵循以下标准模式**：

```csharp
public partial class MyComponent : Node, IComponent
{
    private static readonly Log _log = new("MyComponent");

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

    public void OnComponentRegistered()
    {
        // 清理引用
        _data = null;
        _entity = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // ✅ 仅做日志或信号连接
        _log.Debug("组件就绪");
    }

    public override void _Process(double delta)
    {
        // ✅ 需要特定类型时使用类型判断
        if (_entity is CharacterBody2D body)
        {
            // 使用 body...
        }
    }
}
```

### 关键规则

1. **仅使用 `_entity` 引用**

   - ❌ 禁止：`_owner`、`_body`、多个引用字段
   - ✅ 正确：统一使用 `_entity`，需要时再转换

2. **禁止使用 `GetParent()`**

   - ❌ 禁止：`GetParent()` 或 `GetParent<T>()`
   - ✅ 正确：使用 `OnComponentRegistered` 缓存 `_entity`

3. **`_Ready` 中禁止验证**
   - ❌ 禁止：检查 `_data == null` 或 `_entity == null`
   - ✅ 正确：`OnComponentRegistered` 保证了初始化，无需验证

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

## 数据访问

**所有数据统一从 Data 容器访问**：

```csharp
// 读取数据
float hp = _data?.Get<float>(DataKey.CurrentHp, 100f) ?? 100f;

// 写入数据
_data?.Set(DataKey.CurrentHp, 80f);

// 增加数据
_data?.Add(DataKey.Score, 10);
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
