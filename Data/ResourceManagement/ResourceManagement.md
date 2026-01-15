# ResourceRegistry 资源管理系统

**ResourceRegistry** 是项目中统一管理 **游戏资产 (Assets)**（如场景预制体）的核心系统。
> ⚠️ **注意**：游戏数值数据（如敌人属性、生成规则）和配置已移至纯 C# 类（`EnemyData`, `PlayerData`）管理，不再通过此系统加载。

相比硬编码路径，本系统采用 **Godot 原生 Export 模式**，具有以下优势：

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
  - **Name**: 资源的简写名称（代码加载时使用，如 "EnemyEntity", "DamageComponent"）。
  - **Category**: 选择资源分类（目前主要用于 Entity 和 Component）。
  - **Data**: 拖入对应的 `.tscn` 文件。
    - *提示*：主要支持 `PackedScene` (预制体)。
6. 保存场景 (`Ctrl + S`)。

### 2. 代码调用

使用 `ResourceRegistry` 类的静态 API 进行加载。

#### 加载场景预制体

```csharp
// 加载预制体 (PackedScene)
var playerScene = ResourceRegistry.Load<PackedScene>("PlayerEntity");
var playerNode = playerScene.Instantiate();
```

#### 检查资源是否存在

```csharp
if (ResourceRegistry.Has("PlayerEntity"))
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
