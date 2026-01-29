# ResourceManagement - 资源管理系统

## 概述
`ResourceManagement` 是项目的统一资源加载入口。它通过 `ResourcePaths.cs`（由 `ResourceGenerator` 自动生成）获取资源路径，并提供类型安全的加载方法。

## 核心原则
1. **禁止硬编码**: 严禁在业务代码中使用 `res://` 开头的字符串路径。统一管理有利于路径变更时的重构。
2. **统一分类**: 资源必须属于 `ResourceCategory` 中的某一分类。
3. **类型安全**: 优先使用 `ResourceManagement.Load<T>` 进行加载。

## 使用方法

### 1. 加载场景 (PackedScene)
推荐使用 `typeof(T).Name` 进行类型安全的加载，确保代码重构（如类更名）时能自动保持同步。
```csharp
// 自动匹配类型名作为资源名
var scene = ResourceManagement.Load<PackedScene>(typeof(PlayerEntity).Name, ResourceCategory.Entity);
```

### 2. 加载配置 (Resource/tres)
```csharp
// 加载单个配置
var config = ResourceManagement.Load<Resource>("德鲁伊", ResourceCategory.PlayerConfig);

// 加载分类下所有配置
var allEnemies = ResourceManagement.LoadAll<Resource>(ResourceCategory.EnemyConfig);
```

### 3. 加载 UI
```csharp
var uiScene = ResourceManagement.Load<PackedScene>("HealthBarUI", ResourceCategory.UI);
```

## 🛠️ 最佳实践

### 文件命名与分类规范
为避免 `.tscn` (Asset) 与 `.tres` (Config) 冲突，项目采用以下约定：

- **Entity**: 游戏实体预制体 (如 `PlayerEntity.tscn`)
- **Component**: 组件预制体 (如 `HealthComponent.tscn`)
- **UI**: 界面预制体 (如 `HealthBarUI.tscn`)
- **Asset**: 纯视觉资源场景 (如 `豺狼人.tscn` - 仅包含动画/解构视图)
- **Config**: 数据配置文件 (如 `豺狼人.tres`)，通常存储在 `Data/Data/Resources` 下。

## 资源生成
本项目使用 `Tools/ResourceGenerator` 自动扫描目录并生成 `ResourcePaths.cs`。

> [!IMPORTANT]
> **什么时候需要运行生成器？**
> 当你 **添加、重命名、移动或删除** 了任何 `.tscn` 或 `.tres` 文件时，必须运行生成器更新索引，否则 `ResourceManagement.Load` 将无法找到资源。
> 
> ```bash
> dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
> ```
