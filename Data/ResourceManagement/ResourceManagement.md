# ResourceManagement 资源管理系统

**ResourceManagement** 是项目中统一管理 **游戏资产 (Assets)** 和 **资源配置 (Config)** 的核心系统。

> ⚠️ **核心概念**：
>
> 1. **ResourcePaths**：由 `Tools/ResourceGenerator` 自动生成的静态类，包含一个按 `ResourceCategory` 分类的嵌套字典 `Resources`。
> 2. **ResourceCategories**：资源被分为 `Entity`, `Component`, `UI`, `Asset`, `EnemyConfig` 等分类。
> 3. **Config vs Asset**：
>    - **Config (.tres)**：数据配置文件（如 EnemyConfig），名称通常为原始名称（如 `豺狼人`），存储在 `Data/Data/Resources` 下。
>    - **Asset (.tscn)**：场景文件（如 视觉表现/动画），名称通常为原始名称（如 `豺狼人`）。

---

## 🚀 快速开始

### 1. 加载资源配置 (Configs)

使用 `Load<T>` 或 `LoadAll<T>` 加载 `.tres` 配置文件。

```csharp
// 1. 加载单个配置
var config = ResourceManagement.Load<EnemyConfig>("豺狼人Config", ResourceCategory.EnemyConfig);

// 2. 加载分类下所有配置
var allEnemies = ResourceManagement.LoadAll<EnemyConfig>(ResourceCategory.EnemyConfig);
foreach (var enemy in allEnemies)
{
    GD.Print($"已加载: {enemy.Name}");
}
```

### 2. 加载场景/预制体 (Assets/Entities)

推荐使用 `Load<T>` 配合 `typeof(T).Name` 进行类型安全的加载：

```csharp
// 1. 直接加载并实例化 (推荐)
// 使用 typeof(T).Name 作为资源名称，确保代码重构与资源同步
var scene = ResourceManagement.Load<PackedScene>(typeof(EnemyEntity).Name, ResourceCategory.Entity);
var enemy = scene.Instantiate<EnemyEntity>();

// 2. 配合 ObjectPool (内部实现)
EntityManager.Spawn<EnemyEntity>(...);
```

### 3. 使用资源生成器

当添加新的 `.tscn` 或 `.tres` 文件后，必须运行生成器更新索引：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

---

## 📚 API 参考

### `Load<T>(string name, ResourceCategory category)`

- **描述**: 从指定分类字典加载资源文件（通常用于 Config）。
- **示例**: `ResourceManagement.Load<EnemyConfig>("豺狼人Config", ResourceCategory.EnemyConfig)`

### `LoadAll<T>(ResourceCategory category)`

- **描述**: 加载指定分类下的所有资源。
- **示例**: `ResourceManagement.LoadAll<AbilityConfig>(ResourceCategory.AbilityConfig)`

### `GetPath(string name, ResourceCategory category)`

- **描述**: 获取资源的路径字符串（不加载）。常用于场景文件或需要延迟加载的资源。
- **示例**: `ResourceManagement.GetPath("HealthBarUI", ResourceCategory.UI)`

### `GetNames(ResourceCategory category)`

- **描述**: 获取该分类下所有注册的资源名称列表。

---

## 🛠️ 最佳实践

### 文件命名规范

为避免 `.tscn` (Asset) 与 `.tres` (Config) 重名冲突，建议：

- **配置文件**：使用 `Config` 后缀，如 `豺狼人Config.tres`
- **资源文件**：直接使用名称，如 `豺狼人.tscn`

### 分类说明

- `Entity`: 游戏实体预制体 (EnemyEntity.tscn)
- `Component`: 组件预制体 (HealthComponent.tscn)
- `UI`: 界面预制体 (HealthBarUI.tscn)
- `Asset`: 美术资源场景 (豺狼人.tscn - 仅包含动画/视觉)
- `EnemyConfig`: 敌人数据配置 (.tres)
- `PlayerConfig`: 玩家数据配置 (.tres)
