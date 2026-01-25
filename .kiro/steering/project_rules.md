# 项目规则 - Godot 4.5 C# (.NET 8.0)

> [!IMPORTANT]
> **本文档仅列出核心规范摘要，详细文档请查阅 [项目索引](Docs/框架/项目索引.md)**
>
> 遇到不确定的架构问题，请优先阅读：
> 1. [Docs/框架/项目索引.md](Docs/框架/项目索引.md) - 唯一的架构入口
> 2. [Docs/框架/ECS/Entity/Entity架构设计理念.md](Docs/框架/ECS/Entity/Entity架构设计理念.md) - 核心设计哲学

## 1. 核心工具类

### 1.1 日志系统 (Log)

详见：[Src/Tools/Logger/README.md](Src/Tools/Logger/README.md)

```csharp
private static readonly Log _log = new Log("ClassName");
// 使用 _log.Debug(), _log.Info(), _log.Error() 等
```

### 1.2 动态数据容器 (Data)

详见：[Src/ECS/Data/README.md](Src/ECS/Data/README.md)

- **核心原则**：Data 是唯一数据源，Component 应无状态。
- **事件监听**：**严禁使用 Data.On**，统一使用 `Entity.Events` 监听数据变更。

```csharp
// 读写数据
var hp = data.Get<int>(DataKey.HP);
data.Set(DataKey.HP, 100);

// ✅ 正确：通过 EventBus 监听数据变化
entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
    GameEventType.Data.PropertyChanged, 
    evt => {
        if (evt.Key == DataKey.HP) { /* ... */ }
    }
);
```

### 1.3 对象池 (ObjectPool)

详见：[Src/Tools/ObjectPool/ObjectPool.md](Src/Tools/ObjectPool/ObjectPool.md)

- **强制场景**：子弹、伤害数字、特效、敌人。
- **销毁**：统一使用 `EntityManager.Destroy(entity)`，它会自动处理归还逻辑。

## 2. 架构模式 (Pseudo-ECS)

### 2.1 Entity (实体)

详见：[Src/ECS/Entity/Entity规范.md](Src/ECS/Entity/Entity规范.md)

- **定义**：Scene 即 Entity，实现 `IEntity` 接口。
- **管理**：必须通过 `EntityManager` 进行 Spawn/Register/Destroy。

### 2.2 Component (组件)

详见：[Src/ECS/Component/Component规范.md](Src/ECS/Component/Component规范.md)

- **原则**：单一职责，无状态（数据存 Entity.Data）。
- **通信**：
  1. **Event** (优先)：`entity.Events.Emit()`
  2. **Data**：`data.Get()`
  3. **GetComponent** (最后手段)：`EntityManager.GetComponent<T>()`

### 2.3 ResourceManagement (资源管理)

详见：[Data/ResourceManagement/ResourceManagement.md](Data/ResourceManagement/ResourceManagement.md)

- **废弃**：`ECSIndex` 已移除。
- **新机制**：使用 `ResourceManagement` 加载场景预制体。
- **配置**：在 `Data/ResourceManagement.tscn` 中配置资源映射。

```csharp
// 加载资源
var scene = ResourceManagement.Load<PackedScene>("EnemyBasic");
```

## 3. C# 脚本规范

- **类定义**：`public partial class`，类名 = 文件名。
- **静态变量**：**严禁**存储 Node/Resource 引用（防止内存泄漏）。
- **GC 优化**：`_Process` 中禁止 `new` 对象或 LINQ。

## 4. 文档维护原则

1. **索引为王**：所有新文档必须链接到 [项目索引.md](Docs/框架/项目索引.md)。
2. **分层明确**：
   - `Docs/`：讲设计（Why），给 AI 和架构师看。
   - `Src/`：讲用法（How），给开发者看。
3. **规则精简**：本规则文档保持精简，避免与具体文档重复，防止维护困难。
