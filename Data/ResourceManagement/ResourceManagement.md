# ResourceManagement 资源管理系统

**ResourceManagement** 是项目中统一管理 **游戏资产 (Assets)**（如场景预制体）路径的核心系统。

> ⚠️ **注意**：
> 1. 本系统已重构为 **纯 C#** 静态工具类，不再依赖 Godot 引擎。
> 2. 资源路径由 `Tools/ResourceGenerator` 工具扫描生成到 `Data/ResourceManagement/ResourcePaths.cs` 中。
> 3. `ResourceManagement` 仅提供查询服务，**不维护任何运行时缓存**，直接读取预生成的数据。**。

---

## 🚀 快速开始

### 1. 生成资源索引

每次添加或移动 `.tscn` 文件后，需要运行资源生成工具来更新索引：

1. 运行 `Tools/ResourceGenerator` 项目（或者通过 IDE 运行生成工具）。
2. 工具会自动扫描项目目录，生成 `ResourcePaths.cs`。

### 2. 代码调用

#### 获取资源路径（类型安全，推荐）

建议使用与资源同名的类作为泛型参数来获取路径：

```csharp
// 1. 获取路径
string path = ResourceManagement.GetPath<Player>();

// 2. 加载资源 (自行使用 Godot API)
if (path != null)
{
    var playerScene = GD.Load<PackedScene>(path);
    var player = playerScene.Instantiate<Player>();
}
```

#### 获取资源路径（指定名称）

```csharp
string path = ResourceManagement.GetPath("Enemy");
if (path != null)
{
    // Load...
}
```

#### 按分类获取

```csharp
// 获取所有 "Unit" 分类的资源路径
var unitPaths = ResourceManagement.GetPathsByCategory(ResourceCategory.Entity);
```

---

## 📚 API 参考

### `ResourceManagement.GetPath<T>()`
- **描述**: 获取与类型 T 同名的资源路径。
- **返回**: `string?` (res://...)

### `ResourceManagement.GetPath(string name)`
- **描述**: 获取指定名称资源的路径。
- **返回**: `string?`

### `ResourceManagement.GetPathsByCategory(ResourceCategory category)`
- **描述**: 获取指定分类下的所有资源路径。
- **返回**: `List<string>`

---

## 🛠️ 维护指南

### 添加新的资源分类
修改 `Data/ResourceManagement/ResourceCategory.cs` 枚举即可。

### 常见问题

**Q: `GetPath` 返回 null？**
A: 请检查：
1. 是否运行了 `ResourceGenerator` 工具？
2. 资源文件 (`.tscn`) 是否存在于扫描目录下？
3. 名字拼写是否正确？

**Q: 为什么不再直接返回 Loaded Resource？**
A: 解耦。`ResourceManagement` 应该只负责“我在哪里”，而不负责“怎么加载”。这样可以更好地支持异步加载、流式加载或即时加载等不同策略，完全由调用方决定。
