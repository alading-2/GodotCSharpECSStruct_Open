# SpriteFrames Generator 插件说明

这是一个用于 Godot 4 的编辑器插件，可以自动扫描文件夹中的 PNG 序列帧并生成 `SpriteFrames` 资源（.tres），极大简化了 `AnimatedSprite2D` 的动画设置流程。

## 1. 安装与启用

1. 插件文件已自动放置在 `addons/SpriteFramesGenerator` 目录下。
2. 打开 Godot 编辑器。
3. 进入菜单栏 **Project (项目) -> Project Settings (项目设置) -> Plugins (插件)**。
4. 找到 **SpriteFrames Generator** 并勾选 **Enable (启用)**。
   - _注意：如果列表中未出现，请点击编辑器右上角的 "Build" 按钮重新编译 C# 项目。_

## 2. 使用方法

### 方式一：通过 Project 菜单 (推荐)

1. 在 **FileSystem (文件系统)** 面板中，点击选中包含序列帧图片的文件夹（例如 `res://assets/character/guangfa/`）。
2. 点击编辑器顶部菜单栏 **Project (项目) -> Tools (工具) -> Generate SpriteFrames (Auto)**。
3. 插件会自动在当前选中的文件夹下生成一个同名的 `.tres` 文件（例如 `guangfa_SpriteFrames.tres`）。

### 方式二：自动识别规则

插件支持以下两种文件命名格式：

1. **Spine 导出格式 (推荐)**：

   - 格式：`前缀-动画名_序号.png`
   - 示例：`hero_guangfa-Attack1_00.png` -> 识别为动画名 `attack1`
   - 示例：`hero_guangfa-Idle_01.png` -> 识别为动画名 `idle`

2. **简单格式**：
   - 格式：`动画名_序号.png`
   - 示例：`Run_00.png` -> 识别为动画名 `run`

## 3. 生成结果

- **AnimatedSprite2D.tres**：纯 `SpriteFrames` 资源文件，可用于任何 `AnimatedSprite2D` 节点。
- **AnimatedSprite2D.tscn**：完整的动画场景文件。你可以直接将其拖入其他场景中作为子场景使用。它已经自动配置好了 `SpriteFrames` 并默认选中了 `idle` 动画（如果存在）。

## 4. 常见问题

- **Q: 生成后找不到文件？**
  - A: 插件会自动刷新文件系统，如果未出现，请手动右键文件夹 -> `Rescan`。
- **Q: 动画名识别错误？**
  - A: 请确保文件名中包含 `_数字` 结尾，且动画名位于 `-` 之后（如果存在 `-`）或 `_` 之前。
