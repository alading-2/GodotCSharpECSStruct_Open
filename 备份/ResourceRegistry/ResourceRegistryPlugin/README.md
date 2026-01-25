# ResourceManagementPlugin - 资源注册表自动化工具

这是一个 Godot 4.5 C# 插件，旨在自动化管理项目中 `ResourceManagement.tscn` 的资源索引，告别手动拖拽。

## 核心功能

*   **一键更新**：通过 `项目 -> 工具 -> Update Resource Registry` 自动扫描项目中的 `.tscn` 文件。
*   **高度可配置**：支持自定义扫描白名单和排除黑名单，避免扫描无关目录（如 `addons`）。
*   **智能分类**：根据文件路径自动识别资源分类（`UI`、`Component`、`Entity`）。
*   **命名唯一化**：配合 `SpriteFramesGenerator` 插件，确保自动生成的资源具有唯一的 Key。

## 配置说明

插件启用后，可在 `项目设置 -> 一般 -> Resource Registry` 中找到以下配置：

| 配置项 | 类型 | 说明 | 默认值 |
| :--- | :--- | :--- | :--- |
| `scan_paths` | `PackedStringArray` | 需要扫描的根目录列表 | `["res://assets", "res://Src/UI", ...]` |
| `exclude_paths` | `PackedStringArray` | 需要跳过的目录列表 | `["res://addons", "res://.godot", ...]` |
| `registry_file_path` | `String` | `ResourceManagement.tscn` 的存放路径 | `res://Data/ResourceManagement/ResourceManagement.tscn` |
| `auto_update_on_startup` | `Bool` | 是否在 Godot 启动时自动运行扫描 | `True` |

## 💡 自动化说明

本插件默认开启了 **启动时自动扫描**。
这意味着每当你打开 Godot 编辑器时，它都会在后台自动刷新 `ResourceManagement.tscn`，确保你的资源引用始终与文件系统同步。你无需手动点击任何按钮。

## 使用步骤

1.  **启用插件**：在 `项目 -> 项目设置 -> 插件` 中勾选 `ResourceManagementPlugin`。
2.  **（可选）调整路径**：如果你的资源存放在其他目录，请前往项目设置修改 `scan_paths`。
3.  **运行更新**：点击编辑器顶部菜单 `项目 -> 工具 -> Update Resource Registry`。
4.  **验证结果**：控制台将输出扫描结果，此时打开 `ResourceManagement.tscn` 即可看到自动填充的资源数组。

## 注意事项

*   插件运行会完全重刷 `ResourceManagement.tscn` 中的 `Resources` 数组。
*   如果路径匹配多个分类逻辑，将应用默认排序中的第一个匹配项。
