# Component 访问 Entity 规范说明

## 核心规范

### Component 访问 Entity

**禁止使用 `GetParent()`** - 所有 Component 必须通过以下方式访问 Entity：

1. **推荐方式**（IComponent 接口回调）：

```csharp
private Data? _data;

public void OnComponentRegistered(Node entity)
{
    if (entity is IEntity iEntity)
    {
        _data = iEntity.Data;  // 缓存 Data 引用
    }
}
```

2. **懒加载方式**（\_Ready 中）：

```csharp
public override void _Ready()
{
    if (_data == null)
    {
        _data = EntityManager.GetEntityData(this);
    }
}
```

3. **需要完整 Entity 引用时**：

```csharp
var entity = EntityManager.GetEntityByComponent(this);
if (entity is CharacterBody2D body)
{
    // 使用 CharacterBody2D 特定功能
    body.MoveAndSlide();
}
```

### Entity 访问 Component

统一使用 EntityManager 方法：

```csharp
// 获取 Component
var health = EntityManager.GetComponent<HealthComponent>(this);

// 动态添加 Component
EntityManager.AddComponent(this, component);

// 移除 Component
EntityManager.RemoveComponent(this, "HealthComponent");
```

### 数据访问

**所有数据统一从 Data 容器访问**：

```csharp
// 读取数据
float hp = _data.Get<float>("CurrentHp", 100f);

// 写入数据
_data.Set("CurrentHp", 80f);

// 增加数据
_data.Add("Score", 10);
```

## 关键变更

### 已修改的 Component

2. **VelocityComponent** - 使用 EntityManager 获取 CharacterBody2D
3. **PickupComponent** - 缓存 Entity 引用（Node2D 类型）
4. **FollowComponent** - 缓存 Entity 引用（Node2D 类型）

### 已更新的文档

1. **Src/ECS/Entity/Core/README.md** - 更新所有示例
2. **Docs/框架/ECS/Entity/Entity 架构设计理念.md** - 更新概念说明
3. **Docs/框架/ECS/Entity/EntityManager 设计说明.md** - 更新设计说明
