# SpriteFrames Generator 插件说明

这是一个用于 Godot 4 的编辑器插件，旨在极大简化从序列帧图片到 `AnimatedSprite2D` 的工作流。它可以自动扫描文件夹中的 PNG 序列帧，智能分组，生成 `SpriteFrames` 资源（.tres），并创建或更新 `AnimatedSprite2D` 场景（.tscn）。

## 核心特性

- **一键批量生成**：支持预设路径的**递归批量扫描**。只需配置父级目录（如 `res://assets/Unit`），插件会自动查找所有包含序列帧的子文件夹。
- **右键快捷菜单**：支持对任意文件夹进行单独生成。
- **智能命名规范**：
  - 自动识别 Spine 导出格式和通用命名格式。
  - **自动重命名**：生成的场景和资源将直接使用**文件夹名称**（如 `豺狼人.tscn`），方便在资源注册表中作为唯一 Key 使用。
- **场景智能更新（Smart Update）**：
  - **初次生成**：创建标准的 `AnimatedSprite2D` 场景。
  - **后续更新**：如果场景已存在，插件会自动**保留**你手动调整的所有属性（位置、缩放、脚本、子节点等），仅更新动画数据引用。这意味着你可以放心地调整场景，而不必担心重新生成序列帧时前功尽弃。

## 1. 安装与启用

1. 插件文件应位于 `addons/SpriteFramesGenerator` 目录下。
2. 打开 Godot 编辑器。
3. 进入菜单栏 **Project (项目) -> Project Settings (项目设置) -> Plugins (插件)**。
4. 找到 **SpriteFrames Generator** 并勾选 **Enable (启用)**。
   - _注意：如果列表中未出现，请点击编辑器右上角的 "Build" 按钮重新编译 C# 项目。_

## 2. 配置设置

所有配置集中在 **`SpriteFramesConfig.cs`** 文件中，直接编辑源码即可，保存后重新编译生效。

> **为什么不用 ProjectSettings？** Godot 引擎限制：Dictionary 类型无法在编辑器中动态增删键值对，动态设置项无法显示 Tooltip 描述。使用独立配置文件可获得 IDE 自动补全、编译期检查、注释即文档等优势。

**配置项说明**：

| 配置项 | 类型 | 说明 |
| :--- | :--- | :--- |
| `BatchPaths` | string[] | 批量生成时递归扫描的根目录列表（如 `res://assets`） |
| `DefaultFps` | float | 默认播放帧率（如 `10.0`） |
| `DefaultLoop` | bool | 是否默认开启循环播放 |
| `NameMap` | Dictionary | 动画名称映射表（如 `"movement" -> "run"`） |
| `LoopAnimations` | HashSet | 强制循环播放的动画名称白名单 |
| `UnifiedEffectPaths` | string[] | 统一 Effect 命名的路径列表 |

> **提示**：插件默认采用"智能更新"策略。当重新生成时，会自动保留场景中你手动修改过的属性（位置、脚本、子节点等），只更新动画数据。
> **GDScript 版本**：如果你不想依赖 C# 编译，可以使用 `addons/SpriteFramesGenerator_GDS` 中的纯 GDScript 版本（功能完全相同）。

## 3. 使用方法

### 方式一：批量生成 (推荐)

1. 点击编辑器顶部菜单栏 **Project (项目) -> Tools (工具) -> Generate All SpriteFrames (Batch)**。
2. 插件会自动**递归扫描**项目设置中定义的路径。
3. 只要任一子文件夹内包含 PNG 序列，且未包含生成的 `AnimatedSprite2D` 目录，就会自动在其中创建子文件夹并生成/更新场景。

### 方式二：手动选中生成 (Single)

1. 在 **FileSystem (文件系统)** 面板中，**右键点击** 包含序列帧图片的文件夹。
2. 在弹出的菜单中选择 **Generate SpriteFrames (Single/Selection)**。

## 4. 命名规则与识别

插件支持以下两种文件命名格式（不区分大小写）：

### A. Spine 导出格式 (推荐)
- **格式**：`前缀-动画名_序号.png`
- **示例**：
  - `hero_guangfa-Attack1_00.png` -> 识别为动画名为 `attack1`，第 0 帧
  - `hero_guangfa-Idle_01.png` -> 识别为动画名为 `idle`，第 1 帧

### B. 简单格式
- **格式**：`动画名_序号.png`
- **示例**：
  - `Run_00.png` -> 识别为动画名为 `run`，第 0 帧

### 自动名称映射
插件内置了映射表，会将常见的异名动作统一化：
- `movement` -> `run`
- `deaded`, `death`, `die` -> `dead`

## 5. 生成结果详解

对于每个处理的文件夹（例如 `Unit/Enemy/豺狼人`），插件会在其中创建一个 `AnimatedSprite2D` 子文件夹，包含以下文件：

1.  **`豺狼人.tres` (SpriteFrames 资源)**
    *   文件名与父文件夹同名。
    *   包含所有识别到的动作、帧序列、帧率（默认 10fps）和循环设置（默认开启）。
    *   **更新机制**：每次生成都会直接覆盖此文件，确保动画数据是最新的。

2.  **`豺狼人.tscn` (场景文件)**
    *   文件名与父文件夹同名，且场景根节点也会被重命名为 `豺狼人`。
    *   **如果是新文件**：创建一个居中、重置变换的 `AnimatedSprite2D` 节点。
    *   **如果文件已存在（智能更新）**：
        *   插件会加载现有场景。
        *   **保留**：位置、旋转、缩放、偏移量、挂载的脚本、添加的子节点等。
        *   **更新**：仅将 `SpriteFrames` 属性指向最新的 `.tres` 资源，并校验当前动画名有效性。
