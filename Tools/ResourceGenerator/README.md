# ResourceGenerator 资源路径生成器

**ResourceGenerator** 是一个 C# 控制台工具，用于自动扫描项目中的资源文件（`.tscn` 和 `.tres`），并生成类型安全的路径索引类 `ResourcePaths`。

## 📖 核心功能

- **自动化**: 遍历指定目录，自动提取资源名称与路径。
- **分类管理**: 根据文件路径和扩展名自动对资源进行分类（Entity, Component, UI, Config 等）。
- **去重与优先级**:

  - 自动处理同名资源冲突。
  - **优先级策略**: 遇到同名资源时，优先保留 `.tscn` (场景/表现)，忽略 `.tres` (数据/配置)。
- **类型安全**: 生成一个包含嵌套字典的 C# 类，提供 IDE 补全支持。

## 🛠️ 使用方法

### 运行生成器

在添加、删除或重命名资源文件后，运行以下命令：

```bash
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
```

### 查看生成文件

生成的文件位于 `Data/ResourceManagement/ResourcePaths.cs`。

## ⚙️ 配置说明

在 `ResourceGenerator.cs` 中可以配置以下参数：

### 扫描路径 (`ScanPaths`)

定义生成器会扫描的根目录：

- `assets`: 基础美术资源
- `Src/UI`: UI 界面
- `Src/ECS/Entity`: 游戏实体
- `Src/ECS/Component`: 基础组件
- `Data/Data/Resources`: 游戏数据配置

### 排除路径 (`ExcludePaths`)

定义生成器会跳过目录：

- `addons`, `.godot`, `Src/Test`, `Src/Tools` 等。

## 📂 自动分类逻辑

1. **配置优先**: 位于 `Data/Data/Resources` 下的 `.tres` 文件。
2. **路径匹配**: 
   - `res://Src/UI/` -> `UI`
   - `res://Src/ECS/Entity/` -> `Entity`
   - `res://Src/ECS/Component/` -> `Component`
   - `res://assets/` -> `Asset`
3. **兜底**: 其他 unrecognized 路径归类为 `Other`。

## ⚠️ 开发规范

- **命名规范**: 请保持资源文件名称唯一。如果 `Asset` 和 `Config` 需要同名，生成器会根据路径逻辑尝试区分，但建议在 Config 文件名后加 `Config` 后缀以示区别（例如 `豺狼人.tscn` 和 `豺狼人Config.tres`）。
- **手动更改**: **不要手动修改 `ResourcePaths.cs`**，因为每次运行生成器都会覆盖该文件。
