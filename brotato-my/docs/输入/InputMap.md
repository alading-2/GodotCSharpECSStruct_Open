# Godot 输入映射说明 (Input Map)

> 本文档记录了本项目使用的输入映射配置，兼容 Godot 4.x 版本。
> 按钮命名遵循 Xbox 手柄标准，适配通用 2D 游戏设计规范。

## 移动控制 (Movement)

支持键盘 (WASD / 方向键) 和手柄 (左摇杆 / 十字键)。

| 动作名称       | 键盘按键           | 手柄输入                         | 说明     |
| :------------- | :----------------- | :------------------------------- | :------- |
| **move_up**    | `W`, `Up Arrow`    | 左摇杆上 (Axis 1 -), D-Pad Up    | 向上移动 |
| **move_down**  | `S`, `Down Arrow`  | 左摇杆下 (Axis 1 +), D-Pad Down  | 向下移动 |
| **move_left**  | `A`, `Left Arrow`  | 左摇杆左 (Axis 0 -), D-Pad Left  | 向左移动 |
| **move_right** | `D`, `Right Arrow` | 左摇杆右 (Axis 0 +), D-Pad Right | 向右移动 |

> **说明**：左摇杆提供模拟输入（0.0-1.0），适合平滑移动；D-Pad 提供数字输入，适合精确移动。

## UI 导航 (UI Navigation)

仅手柄输入，用于菜单界面的导航。

| 动作名称     | 手柄输入    | 说明                |
| :----------- | :---------- | :------------------ |
| **ui_up**    | D-Pad Up    | 菜单上移 / 对话选择 |
| **ui_down**  | D-Pad Down  | 菜单下移 / 对话选择 |
| **ui_left**  | D-Pad Left  | 菜单左移 / 标签切换 |
| **ui_right** | D-Pad Right | 菜单右移 / 标签切换 |

## Xbox 按钮映射 (Button Actions)

按照 Xbox 手柄按钮命名，遵循通用 2D 游戏设计规范。

### 面部按钮（ABXY）

| 动作名称  | 键盘按键 | 手柄按钮   | 2D 平台/动作游戏           | 2D RPG/菜单        |
| :-------- | :------- | :--------- | :------------------------- | :----------------- |
| **btn_a** | -        | A (按钮 0) | 确认 / 跳跃 / 主动作       | 确认 / 互动        |
| **btn_b** | `Esc`    | B (按钮 1) | 取消 / 后退 / 次要动作     | 取消 / 后退        |
| **btn_x** | -        | X (按钮 2) | 攻击 / 特殊动作 / 副动作   | 菜单 / 日志 / 地图 |
| **btn_y** | -        | Y (按钮 3) | 强力攻击 / 特殊动作 / 互动 | 物品栏 / 状态      |

### 肩键与扳机

| 动作名称   | 键盘按键 | 手柄按钮              | 2D 平台/动作游戏              | 2D RPG/菜单        |
| :--------- | :------- | :-------------------- | :---------------------------- | :----------------- |
| **btn_lb** | -        | LB (按钮 4)           | 快速切换武器 / 物品 / 技能    | 快速切换菜单标签页 |
| **btn_rb** | -        | RB (按钮 5)           | 快速切换武器 / 物品 / 技能    | 快速切换菜单标签页 |
| **btn_lt** | -        | LT (按钮 9 & Axis 4)  | 瞄准 / 格挡 / 冲刺 / 特殊能力 | 触发特定功能 / 宏  |
| **btn_rt** | -        | RT (按钮 10 & Axis 5) | 瞄准 / 格挡 / 冲刺 / 特殊能力 | 触发特定功能 / 宏  |

### 系统按钮

| 动作名称       | 键盘按键 | 手柄按钮        | 2D 平台/动作游戏     | 2D RPG/菜单   |
| :------------- | :------- | :-------------- | :------------------- | :------------ |
| **btn_start**  | `Esc`    | Start (按钮 6)  | 暂停菜单             | 主菜单 / 设置 |
| **btn_select** | -        | Select (按钮 4) | 地图 / 物品栏 / 日志 | 开启特定菜单  |

## 右摇杆控制 (Right Stick)

用于视角控制、瞄准等（2D 游戏中较少使用）。

| 动作名称              | 手柄输入            | 说明              |
| :-------------------- | :------------------ | :---------------- |
| **stick_right_up**    | 右摇杆上 (Axis 3 -) | 视角上移 / 瞄准上 |
| **stick_right_down**  | 右摇杆下 (Axis 3 +) | 视角下移 / 瞄准下 |
| **stick_right_left**  | 右摇杆左 (Axis 2 -) | 视角左移 / 瞄准左 |
| **stick_right_right** | 右摇杆右 (Axis 2 +) | 视角右移 / 瞄准右 |

> **说明**：右摇杆在纯 2D 游戏中不常用，可用于视角调整或瞄准方向。

## 如何在代码中使用

### 移动控制

```gdscript
func _physics_process(delta):
    # 获取移动向量（支持手柄摇杆的模拟量输入）
    var input_dir = Input.get_vector("move_left", "move_right", "move_up", "move_down")
    velocity = input_dir * SPEED
    move_and_slide()
```

### 按钮事件

```gdscript
func _input(event):
    # A 键：确认/跳跃
    if event.is_action_pressed("btn_a"):
        jump()

    # B 键：取消/后退
    if event.is_action_pressed("btn_b"):
        cancel_action()

    # X 键：攻击/菜单
    if event.is_action_pressed("btn_x"):
        attack()

    # Y 键：特殊动作/物品栏
    if event.is_action_pressed("btn_y"):
        open_inventory()

    # Start：暂停菜单
    if event.is_action_pressed("btn_start"):
        get_tree().paused = !get_tree().paused

    # Select：打开地图
    if event.is_action_pressed("btn_select"):
        toggle_map()

    # 肩键：切换武器
    if event.is_action_pressed("btn_lb"):
        switch_weapon_left()

    if event.is_action_pressed("btn_rb"):
        switch_weapon_right()
```

### 扳机键（模拟输入）

```gdscript
func _physics_process(delta):
    # 获取扳机键的力度（0.0 - 1.0）
    var trigger_strength = Input.get_action_strength("btn_rt")

    if trigger_strength > 0.1:
        # 根据力度调整行为（如加速、瞄准等）
        apply_action(trigger_strength)
```

### 右摇杆（瞄准方向）

```gdscript
func _physics_process(delta):
    # 获取右摇杆方向
    var aim_dir = Input.get_vector(
        "stick_right_left",
        "stick_right_right",
        "stick_right_up",
        "stick_right_down"
    )

    if aim_dir.length() > 0.1:
        # 使用右摇杆控制瞄准
        rotation = aim_dir.angle()
```

## 手柄按钮对照表

| Xbox 按钮   | PlayStation  | Nintendo Switch | 按钮索引  |
| :---------- | :----------- | :-------------- | :-------- |
| A           | X (Cross)    | B               | 0         |
| B           | O (Circle)   | A               | 1         |
| X           | □ (Square)   | Y               | 2         |
| Y           | △ (Triangle) | X               | 3         |
| LB          | L1           | L               | 4         |
| RB          | R1           | R               | 5         |
| Start       | Options      | +               | 6         |
| Select/View | Share/Create | -               | 4         |
| LT          | L2           | ZL              | 9/Axis 4  |
| RT          | R2           | ZR              | 10/Axis 5 |

## 命名规范说明

- **`move_*`**: 角色移动相关（通常是左摇杆 + D-Pad）
- **`ui_*`**: UI 界面导航（通常是 D-Pad）
- **`btn_*`**: 手柄按钮动作（遵循 Xbox 命名）
- **`stick_right_*`**: 右摇杆控制（通常用于瞄准/视角）

## 设计原则

本输入映射遵循以下设计原则：

1. **手柄优先**：优先配置手柄按键，键盘按键根据实际需求后续添加
2. **遵循标准**：按照 2D 游戏通用设计规范配置按钮功能
3. **灵活扩展**：保留键盘按键的扩展空间，便于后续自定义
4. **清晰命名**：使用 Xbox 按钮名称，便于理解和维护
