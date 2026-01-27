
# DataForge Studio 独立版 - 架构分析与重构计划

> **核心思想**: 摆脱 Godot 编辑器的 API 束缚，利用 .NET 生态 (WPF/Avalonia) 的成熟 UI 能力，打造 Excel 级别的专业配置工具。

## 1. 为什么要“叛逃” Godot？

正如您所说，Godot 的插件系统在做复杂 UI 工具时存在天然劣势：
1. **API 简陋**: Godot 的 UI 控件主要为游戏设计，缺乏 DataGrid、Excel筛选、列重排等高级功能。
2. **生态缺失**: .NET 拥有无数成熟的 MVVM 框架和控件库，Godot 中即使是简单的拖拽列宽都需要手写逻辑。
3. **开发效率**: 插件每次修改都需要 Godot 重新加载脚本，甚至重启编辑器。独立工具拥有热重载和完整的调试能力。
4. **性能瓶颈**: 复杂数据量下 Godot 的 Tree 控件性能远不如 WPF 的虚拟化 DataGrid。
5. **解耦**: 独立工具不依赖 Godot 运行，即使项目代码编译报错，工具依然能打开修改配置。

---

## 2. 技术栈选择

**推荐**: **WPF (.NET 8)**
*   **理由**: 
    *   您在 Windows 环境开发。
    *   WPF 极其成熟，拥有不仅也是目前Windows下桌面开发的首选。
    *   **DataGrid**: WPF 原生自带强大的 DataGrid，支持排序、虚拟化、列重排、自定义模板。
    *   **MVVM**: CommunityToolkit.Mvvm 极其高效。
    *   **样式**: 通过 MaterialDesignInXamlToolkit 可以轻松获得现代、好看的 UI。

**(备选)**: **Avalonia** (若需跨平台支持 Linux/macOS)

---

## 3. 架构设计

### 3.1 核心流程变化

| 功能 | Godot 插件方案 | **DataForge Studio (独立版)** |
| :--- | :--- | :--- |
| **启动方式** | Godot 菜单内启动 | 独立 .exe 运行 |
| **项目定位** | 自动获取 `res://` | **向上查找 `project.godot` 文件** |
| **Schema 获取** | `Type.GetType("DataKey")` (反射) | **Roslyn 解析 `DataKey.cs` 源码** |
| **数据读取** | `FileAccess.Open` | `System.IO.File` |
| **UI 渲染** | `Tree` / `TreeItem` | **DataGrid** (WPF) |
| **代码生成** | `StringBuilder` | `StringBuilder` (逻辑完全复用) |

### 3.2 关键技术点

#### A. 源码解析代替反射 (Roslyn)
不再依赖 DLL 加载（避免文件占用和版本冲突），直接解析 C# 源码。
```csharp
// 伪代码：解析 DataKey
var tree = CSharpSyntaxTree.ParseText(File.ReadAllText("DataKey.cs"));
var fields = tree.GetRoot().DescendantNodes()
    .OfType<FieldDeclarationSyntax>()
    .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
    .Select(f => f.Declaration.Variables.First().Identifier.Text);
```
**优势**: 即使项目编译失败，依然能读取 Schema 并修改数据！

#### B. 数据绑定 (MVVM)
WPF 的 DataGrid 可以直接绑定到 `ObservableCollection<ExpandoObject>` 或 `DataTable`。
*   **列动态生成**: 根据 Schema 动态创建 DataGridColumn。
*   **双向绑定**: 也就是改了 UI，数据源自动变。

#### C. 资源路径验证
虽然脱离了 Godot，但我们依然可以验证资源。
*   解析 `project.godot` 获取资源根目录。
*   验证 `res://icon.png` -> `E:/Project/icon.png` 是否存在。

---

## 4. 重构路线图

我们将创建一个新的解决方案文件夹 `Tools/DataForgeStudio`。

### Phase 1: 基础设施 (1天)
1.  创建 WPF .NET 8 项目。
2.  引入 `CommunityToolkit.Mvvm`, `Newtonsoft.Json` (或 System.Text.Json)。
3.  实现 `ProjectLocator`: 自动向上查找 Godot 项目根目录。

### Phase 2: Schema 解析 (1天)
1.  实现 `SchemaParser`: 读取 `Data/DataKey.cs`。
2.  解析 `public const string` 字段作为列定义。
3.  解析枚举类型文件（如 `Team.cs`）。

### Phase 3: 核心编辑器 (2-3天)
1.  实现主界面 `MainWindow.xaml`。
2.  使用 `DataGrid` 动态绑定数据。
3.  实现 Excel 风格功能：
    *   [x] 复制/粘贴 (WPF 自带)
    *   [x] 排序 (WPF 自带)
    *   [x] 列宽调整 (WPF 自带)
    *   [x] 多行删除 (简单的 List 操作)
    *   [x] 撤销/重做 (Command 模式)

### Phase 4: 代码生成移植 (0.5天)
1.  将 `DataForgeWindow.Code.cs` 的逻辑移植到 `CodeGenerator` 类。
2.  适配文件路径 API。

---

## 5. 预期成果

最终我们将得到一个 **Standalone Tool**：
1.  **秒开**: 双击即用，无需打开 Godot。
2.  **强壮**: 项目崩了也能改配置。
3.  **丝滑**: 真正的 Excel 体验（WPF DataGrid 性能吊打 Godot Tree）。
4.  **美观**: 现代化的 Windows 应用界面。

这正是“磨刀不误砍柴工”，用最合适的工具（WPF）做最合适的事（数据编辑）。
