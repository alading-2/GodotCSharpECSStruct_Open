# SpriteFrames Generator 插件说明

SpriteFrames Generator是扫描指定路径下的图片序列帧批量生成对应的SpriteFrames资源和AnimatedSprite2D场景的Godot 4 编辑器插件，可用于Spine导出动画序列帧到godot，然后一键生成场景。

## 核心特性

- **一键批量生成**：支持预设路径的**递归批量扫描**
- **右键快捷菜单**：支持对任意文件夹进行单独生成
- **智能命名规范**：自动识别 Spine 导出格式和通用命名格式
- **场景智能更新**：保留手动调整的属性（位置、缩放、脚本、子节点等），仅更新动画数据
- **碰撞模板自动注入**：按 `RULES` 规则表将对应的碰撞模板场景实例化到生成的 `AnimatedSprite2D` 中，命名为 `"CollisionShape2D"`

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
| `collision_scene_path` | String | 碰撞模板场景路径；生成时实例化为 `"CollisionShape2D"` 子节点注入到 VisualRoot |
| `default_shape_radius` | float | CollisionShape2D **首次生成**时的胶囊体半径（默认 10.0）；智能更新时忽略，保留手动调整 |
| `default_shape_height` | float | CollisionShape2D **首次生成**时的胶囊体高度（默认 20.0） |
| `default_shape_position` | Vector2 | CollisionShape2D **首次生成**时相对于碰撞模板实例的偏移（默认 Vector2.ZERO） |

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
  └─ CollisionShape2D        ← 碰撞模板实例（CharacterBody2D/Area2D，重命名为此名）
       └─ CollisionShape2D   ← 由 SpriteFramesGenerator 动态添加，默认 CapsuleShape2D
                               首次生成：radius=10, height=20
                               智能更新：自动保留上次手动调整的 shape/transform/disabled
```

- **碰撞模板**（`.tscn`，位于 `Src/ECS/Component/Presets/Collision/`）：仅设置 `collision_layer` / `collision_mask`，不内嵌形状。形状由生成器在每次生成时注入，各角色场景可独立调整。
- **Unit 类型**（EnemyCollision / PlayerCollision）：根节点为 CharacterBody2D，`CollisionComponent` 识别后不绑定信号（类型标记用）。其下的 CollisionShape2D 是该动画视觉体自身的碰撞形状，需按角色体型调整。
- **Effect 类型**（EffectCollision）：根节点为 Area2D，`CollisionComponent` 绑定信号。其下的 CollisionShape2D 定义检测区域，业务代码还需配置 `collision_mask` 方可检测目标。

`SpriteFramesGenerator` 只负责把碰撞模板注入视觉场景；运行时统一桥接碰撞事件的是 `CollisionComponent`。另外 `Tools/ResourceGenerator` 会扫描同一目录下的所有模板场景，生成 `CollisionTypeRegistry.cs` 供运行时按 `(layer, mask)` 反查碰撞语义。

> **智能更新规则**：重新生成时若场景已有 `CollisionShape2D/CollisionShape2D`，插件自动保存其 `shape / transform / disabled` 并在重建后恢复，不会覆盖手动调整结果。

## 为什么不用 ProjectSettings？

Godot 引擎的 `ProjectSettings` 存在以下限制：
1. **Dictionary 无法编辑键值对** — 编辑器中无法动态增删字典的 Key/Value
2. **无 Tooltip 描述** — 动态添加的设置项在编辑器中显示"无可用描述"
3. **复杂结构难以序列化** — 嵌套字典、集合等数据结构无法在 `project.godot` 中友好存储

因此，本插件将所有配置迁移到独立的脚本文件中：
- 直接编辑脚本，注释即文档
- 支持任意复杂数据结构
- 版本控制友好，diff 清晰
