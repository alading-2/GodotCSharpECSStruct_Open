# ResourceRegistry 资源管理系统

**ResourceRegistry** 是项目中统一管理 Resource（如单位配置、生成规则、物品数据）的核心系统。

相比旧版的 `DataResourceIndex`（硬编码路径），新系统采用 **Godot 原生 Export 模式**，具有以下优势：

- 🎨 **可视化配置**：直接在 Godot 编辑器中拖拽资源，无需编写代码。
- 🛡️ **路径安全**：文件移动或重命名时，Godot 会自动更新引用（UID 机制），路径错误会立即报红。
- 📦 **自动单例**：作为 AutoLoad 自动加载，全局可用。

---

## 🚀 快速开始

### 1. 配置资源

所有的资源注册都在 Godot 编辑器中完成，无需修改 C# 代码。

1. 在 Godot 编辑器中，打开场景 **`Data/ResourceRegistry.tscn`**。
2. 选中根节点 **ResourceRegistry**。
3. 在右侧 **Inspector (检查器)** 面板中，找到 **Resources** 数组。
4. 点击 **Add Element**（添加元素）。
5. 在新增的 **ResourceEntry** 中填写：
  - **Name**: 资源的简写名称（代码加载时使用，如 "豺狼人"）。
  - **Category**: 选择资源分类（如 `Unit`, `SpawnRule`）。
  - **Data**: 拖入对应的 `.tres` 或 `.tscn` 文件。
    - *提示*：`PackedScene` (预制体) 也是一种 Resource，完全兼容。
6. 保存场景 (`Ctrl + S`)。

### 2. 代码调用

使用 `ResourceRegistry` 类的静态 API 进行加载。

#### 单个资源加载

```csharp
// 推荐：使用泛型 API，类型安全
var enemyData = ResourceRegistry.Load<EnemyResource>("豺狼人");

// 加载预制体 (PackedScene)
var playerScene = ResourceRegistry.Load<PackedScene>("Player");
var playerNode = playerScene.Instantiate();

if (enemyData != null)
{
    // 使用资源...
}
```

#### 按分类批量加载

常用于系统初始化，例如加载所有敌人生成规则。

```csharp
// 获取所有 "SpawnRule" 分类的资源
var rules = ResourceRegistry.LoadAllInCategory<EnemySpawnConfig>(ResourceCategory.SpawnRule);

foreach (var rule in rules)
{
    // 激活规则...
}
```

#### 检查资源是否存在

```csharp
if (ResourceRegistry.Has("倚天剑"))
{
    // ...
}
```

---

## 📚 API 参考

### `ResourceRegistry.Load<T>(string name)`

- **描述**: 根据名称加载指定类型的资源。
- **参数**:
  - `name`: 资源的简写名称（在编辑器中配置的 Name）。
- **返回**: 成功返回资源实例，失败（未找到或类型不匹配）返回 `null`。

### `ResourceRegistry.LoadAllInCategory<T>(ResourceCategory category)`

- **描述**: 加载指定分类下的所有资源。
- **参数**:
  - `category`: 资源分类枚举（`Unit`, `SpawnRule`, `Item`, `Weapon`）。
- **返回**: 资源列表 `List<T>`。如果分类为空，返回空列表。

### `ResourceRegistry.GetNamesInCategory(ResourceCategory category)`

- **描述**: 获取指定分类下的所有资源名称列表。
- **返回**: `List<string>`。

---

## 🛠️ 维护指南

### 添加新的资源分类

如果需要新的分类（例如 "Skill"），请修改 `Data/ResourceEntry.cs` 中的 `ResourceCategory` 枚举：

```csharp
public enum ResourceCategory
{
    Unit,
    SpawnRule,
    Item,
    Weapon,
    Skill, // 新增分类
}
```

修改后，重新编译项目，Godot 编辑器的下拉菜单会自动更新。

### 常见问题

**Q: 代码中加载返回 null？**
A: 请检查：
1. 是否忘记在 `ResourceRegistry.tscn` 中保存？
2. `Name` 字符串是否拼写正确（区分大小写）？
3. 泛型类型 `<T>` 是否与实际资源类型匹配？

**Q: 移动了 .tres 文件怎么办？**
A: 无需做任何事！Godot 会自动更新 `ResourceRegistry.tscn` 中的引用路径。这是本系统的最大优势。
