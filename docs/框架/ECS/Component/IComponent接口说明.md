# IComponent 接口说明

## 📋 核心概念

`IComponent` 是 Component 的标记接口，用于 **EntityManager 自动识别和注册 Component**。

### 接口定义

```csharp
public interface IComponent
{
    void OnComponentRegistered(Node entity) { }   // Component 注册时回调
    void OnComponentUnregistered() { }            // Component 注销时回调
}
```

---

## 🎯 三层识别机制

EntityManager 通过以下 3 种方式识别 Component（按优先级）：

| 优先级     | 识别方式            | 示例                                                      | 适用场景             |
| ---------- | ------------------- | --------------------------------------------------------- | -------------------- |
| ⭐⭐⭐⭐⭐ | **IComponent 接口** | `public partial class HealthComponent : Node, IComponent` | 新 Component（推荐） |
| ⭐⭐⭐⭐   | **命名约定**        | `public partial class VelocityComponent : Node`           | 旧 Component（兼容） |

---

## 🔧 使用方法

### 方式 1：IComponent 接口（推荐）

```csharp
using Godot;
using System;
using System.Linq;

public partial class HealthComponent : Node, IComponent
{
    private static readonly Log _log = new("HealthComponent");

    public event Action<float>? Damaged;
    public event Action? Died;

    // ==================== IComponent 接口实现 ====================

    public void OnComponentRegistered(Node entity)
    {
        // 关系已由 EntityManager 自动建立

        // 初始化数据
        var data = entity.GetData();
        data.Set("CurrentHp", data.Get<float>("MaxHp"));

        _log.Debug($"Component 已注册到 Entity: {entity.Name}");
    }

    public void OnComponentUnregistered()
    {
        // 可选：清理资源
    }

    // ==================== 获取 Entity 引用 ====================

    private Node? GetEntity()
    {
        // 通过关系管理器查找所属 Entity
        string componentId = GetInstanceId().ToString();
        var entityId = EntityRelationshipManager
            .GetParentEntitiesByChildAndType(componentId, EntityRelationshipType.ENTITY_TO_COMPONENT)
            .FirstOrDefault();

        return entityId != null ? EntityManager.GetEntityById(entityId) : null;
    }

    // ==================== 业务逻辑 ====================

    public void TakeDamage(float amount)
    {
        var entity = GetEntity();
        if (entity == null) return;

        var data = entity.GetData();
        float currentHp = data.Get<float>("CurrentHp");
        currentHp -= amount;
        data.Set("CurrentHp", currentHp);

        Damaged?.Invoke(amount);

        if (currentHp <= 0)
        {
            Died?.Invoke();
        }
    }
}
```

**优势**：

- ✅ 自动获取 Entity 引用（通过关系管理器）
- ✅ 生命周期回调
- ✅ 编译期类型检查
- ✅ 兼容复杂场景（Component 在容器中）
- ✅ 无需手动存储 entityId

---

### 方式 2：命名约定（兼容旧代码）

```csharp
// ✅ 类名以 "Component" 结尾，自动识别
public partial class VelocityComponent : Node
{
    public override void _Ready()
    {
        // 兼容手动创建：无需存储 entityId
        // 使用时通过关系管理器查找
    }

    private Node? GetEntity()
    {
        // 通过关系管理器查找所属 Entity
        string componentId = GetInstanceId().ToString();
        var entityId = EntityRelationshipManager
            .GetParentEntitiesByChildAndType(componentId, EntityRelationshipType.ENTITY_TO_COMPONENT)
            .FirstOrDefault();

        return entityId != null ? EntityManager.GetEntityById(entityId) : null;
    }
}

// ❌ 不会自动识别
public partial class Velocity : Node { }
```

**劣势**：

- ❌ 需手动实现 GetEntity() 方法
- ❌ 没有生命周期回调

---

**适用场景**：

- Hitbox/Hurtbox（不符合命名约定）
- Godot 内置节点（CollisionShape2D）

---

## ⚠️ 核心注意事项

### 1. 检查空引用

```csharp
// ✅ 正确
public void TakeDamage(float amount)
{
    if (_entity == null) return;
    // ...
}

// ❌ 错误：可能 NullReferenceException
public void TakeDamage(float amount)
{
    var data = _entity.GetData();  // _entity 可能为 null
}
```

### 2. 兼容手动创建

```csharp
// ✅ 正确：兼容手动创建的 Component
public override void _Ready()
{
    if (_entity == null)
    {
        _entity = GetParent();
    }
}
```

---

## 🔄 工作流程

### Entity 生成时

```C#
EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Resource = resource,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = position
})
    ↓
RegisterComponents(entity)  // 自动识别所有 Component
    ├─ 检查 IComponent 接口
    ├─ 检查命名约定
    ├─ Register(component)
    └─ 触发 OnComponentRegistered(entity)
```

### Entity 销毁时

```C#
EntityManager.Destroy(entity)
    ↓
UnregisterComponents(entity)  // 自动注销所有 Component
    ├─ 触发 OnComponentUnregistered()
    ├─ Unregister(component)
    └─ 归还对象池
```

---

## 🎯 快速检查清单

创建新 Component 时，确保：

- [ ] 类名以 "Component" 结尾 **或** 实现 IComponent 接口
- [ ] 实现 `OnComponentRegistered` 和 `OnComponentUnregistered`
- [ ] 在 `_Ready` 中兼容手动创建（检查 `_entity`）

---

## 🔍 常见问题

### Q: Component 没有被自动注册？

**检查**：

1. 类名是否以 "Component" 结尾？
2. 是否实现了 IComponent 接口？
3. 是否通过 `EntityManager.Spawn()` 生成？

**调试**：

```csharp
Log.SetLevel("EntityManager", LogLevel.Debug);
// 查看日志：[EntityManager] 已注册 Component: YourComponent
```

### Q: OnComponentRegistered 没有被调用？

**原因**：没有实现 IComponent 接口，或 Entity 不是通过 EntityManager.Spawn() 生成

### Q: 如何获取其他 Component？

```csharp
public void OnComponentRegistered(Node entity)
{
    _entity = entity;

    // 推荐：通过 Entity 引用
    _healthComp = _entity.GetComponent<HealthComponent>(
        ECSIndex.Component.HealthComponent
    );
}
```

---

## 📚 相关文档

- **EntityManager API 文档**：`Docs/框架/ECS/Entity/EntityManager_API文档.md`
- **Entity 架构设计理念**：`Docs/框架/ECS/Entity/Entity架构设计理念.md`
- **项目规则**：`.trae/rules/project_rules.md`

---

**维护者**：项目团队  
**文档版本**：v2.0  
**创建日期**：2025-01-01  
**最后更新**：2025-01-01
