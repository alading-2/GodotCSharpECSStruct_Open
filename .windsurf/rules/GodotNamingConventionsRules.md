---
trigger: always_on
---

# 项目规则

## 1. 项目基本信息

- **项目名称**: 复刻土豆兄弟 (Brotato-like)
- **项目类型**: 2D Rogue-like 独立游戏
- **游戏引擎**: Godot Engine 4.5
- **开发语言**: GDScript

## 2. 命名规范 (Naming Conventions)

严格遵循 [Godot 官方 GDScript 风格指南](https://docs.godotengine.org/zh-cn/4.5/tutorials/scripting/gdscript/gdscript_styleguide.html)。

### 2.1 文件命名

**必须使用 snake_case (蛇形命名法)**

- 正确: `health_component.gd`, `player.tscn`, `level_01.tscn`
- 错误: `HealthComponent.gd`, `Player.tscn`, `Level01.tscn`

### 2.2 类与节点命名

**使用 PascalCase (大驼峰命名法)**

- 类名 (class_name): `class_name HealthComponent`
- 节点名 (Node Name): `HealthComponent`, `Player`, `Sprite2D`
- 类型转换: `extends CharacterBody2D`

### 2.3 变量与函数命名

**使用 snake_case (蛇形命名法)**

- 变量: `var current_health`, `var move_speed`
- 函数: `func take_damage()`, `func _on_timer_timeout()`
- 私有成员 (建议): 以 `_` 开头，如 `var _is_dead`

### 2.4 常量与信号

- **常量**: ALL_CAPS (全大写下划线), 如 `const MAX_Health = 100` -> `const MAX_HEALTH = 100`
- **信号**: snake_case + 过去时态, 如 `signal died`, `signal health_changed`

## 3. 项目结构规范

### 3.1 目录结构

```
brotato-my/
├── assets/              # 资源 (snake_case)
├── scenes/              # 场景与代码
│   ├── autoload/        # 全局单例
│   ├── components/      # 通用组件 (health_component.tscn)
│   ├── entities/        # 游戏实体 (player.tscn, enemy.tscn)
│   ├── managers/        # 逻辑管理器
│   └── ui/              # 界面
├── resources/           # 数据资源 (.tres)
└── docs/                # 文档
```

### 3.2 脚本挂载规则

- **脚本必须挂载到 Node 上**: 尽量不要有游离的 `.gd` 文件。
- **组件化**: 优先创建 `component_name.tscn` 包含脚本和基本节点结构，方便复用。

## 4. 资源管理

- 所有资源文件名使用 snake_case。
- 优先使用 Godot 的 `.tres` (Text Resource) 格式以便于版本控制。

---

**注意**: 请时刻保持代码整洁，遵循上述命名规范。
