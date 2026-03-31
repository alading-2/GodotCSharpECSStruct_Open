# SpriteFrames Generator 插件说明

SpriteFrames Generator是扫描指定路径下的图片序列帧批量生成对应的SpriteFrames资源和AnimatedSprite2D场景的Godot 4 编辑器插件，可用于Spine导出动画序列帧到godot，然后一键生成场景。

## 核心特性

- **一键批量生成**：支持预设路径的**递归批量扫描**
- **右键快捷菜单**：支持对任意文件夹进行单独生成
- **智能命名规范**：自动识别 Spine 导出格式和通用命名格式
- **场景智能更新**：保留手动调整的属性（位置、缩放、脚本、子节点等），仅更新动画数据
- **碰撞 Profile 自动注入**：按 `RULES` 规则表将对应的 Collision Profile 场景实例化到生成的 `AnimatedSprite2D` 中，命名为 `"CollisionShape2D"`

## 0.下载
[插件下载连接](https://downgit.github.io/#/home?url=https://github.com/alading-2/GodotCSharpECSStruct_Open/tree/main/addons/SpriteFramesGenerator)

## 1. 安装与启用

1. 插件文件应位于 `addons/SpriteFramesGenerator` 目录下。
2. 打开 Godot 编辑器。
3. 进入菜单栏 **Project (项目) -> Project Settings (项目设置) -> Plugins (插件)**。
4. 找到 **SpriteFrames Generator** 并勾选 **Enable (启用)**。

## 2. 配置设置

所有配置集中在 `sprite_frames_config.gd` 文件中，直接编辑即可：

| 配置项 | 类型 | 说明 |
| :--- | :--- | :--- |
| `BATCH_PATHS` | PackedStringArray | 批量生成时递归扫描的根目录列表 |
| `DEFAULT_FPS` | float | 默认播放帧率（如 `10.0`） |
| `DEFAULT_LOOP` | bool | 是否默认开启循环播放 |
| `NAME_MAP` | Dictionary | 动画名称映射表（如 `"movement" -> "run"`） |
| `LOOP_ANIMATIONS` | PackedStringArray | 强制循环播放的动画名称白名单 |
| `RULES` | Array | 规则表（详见下方）：匹配指定路径下的所有文件夹，支持统一命名和碰撞场景注入 |

### RULES 详解

`RULES` 中每个元素支持以下字段：

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `key` | String | 规则标识（仅注释用） |
| `paths` | Array[String] | 该规则适用的资源路径列表 |
| `unified_animation_name` | String | 统一动画名称（非空则按字母序重命名为 Key, Key1, Key2…） |
| `collision_scene_path` | String | Collision Profile 场景路径；生成时实例化为 `"CollisionShape2D"` 子节点注入到 VisualRoot |

## 3. 使用方法

### 方式一：批量生成 (推荐)

1. 点击编辑器顶部菜单栏 **Project (项目) -> Tools (工具) -> Generate All SpriteFrames (Batch)**。
2. 插件会自动**递归扫描**配置文件中定义的路径。

### 方式二：手动选中生成

1. 在 **FileSystem (文件系统)** 面板中，**右键点击** 包含序列帧图片的文件夹。
2. 在弹出的菜单中选择 **Generate SpriteFrames (Single/Selection)**。

## 4. 命名规则

文件名末尾必须有 `_数字`，插件以此识别序列帧顺序：

- `动画名_序号.png` — 如 `idle_00.png`、`attack_01.png`
- `角色名-动画名_序号.png` — Spine 导出格式，如 `hero-idle_00.png`

同一文件夹内不同动画名（`idle`、`attack` 等）会生成为同一角色的多个动画。

## 5. 生成结果

对于每个处理的文件夹（如 `Unit/Enemy/豺狼人`），会在其中创建 `AnimatedSprite2D` 子文件夹：

1. **`豺狼人.tres`** — SpriteFrames 资源（每次覆盖更新）
2. **`豺狼人.tscn`** — AnimatedSprite2D 场景（智能更新，保留手动属性）

若匹配到 `RULES` 中的 `collision_scene_path`，标准结构如下：

```text
AnimatedSprite2D "豺狼人"
  └─ [CollisionShape2D]  ← Collision Profile 实例（CharacterBody2D/Area2D）
                         预配置 collision_layer/mask，无 shape
                         连接的实体内嵌： Src/ECS/Component/Presets/Collision/Unit/EnemyCollision.tscn
```

> Profile 场景无形状（Godot 提示 ! 警告，属正常），运行时由业务代码按需赋値。

## 为什么不用 ProjectSettings？

Godot 引擎的 `ProjectSettings` 存在以下限制：
1. **Dictionary 无法编辑键值对** — 编辑器中无法动态增删字典的 Key/Value
2. **无 Tooltip 描述** — 动态添加的设置项在编辑器中显示"无可用描述"
3. **复杂结构难以序列化** — 嵌套字典、集合等数据结构无法在 `project.godot` 中友好存储

因此，本插件将所有配置迁移到独立的脚本文件中：
- 直接编辑脚本，注释即文档
- 支持任意复杂数据结构
- 版本控制友好，diff 清晰
