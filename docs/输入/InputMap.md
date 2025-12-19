# Godot 输入映射与 C# 最佳实践

> 本文档记录了本项目的输入配置，并提供了 Godot C# 下的最佳实践代码示例。
> 本项目已废弃 `InputConstants` 静态类，直接在 `project.godot` 中使用 **PascalCase** 命名的动作，以符合 C# 编码习惯。

## 1. 输入映射表 (Input Map)

按钮命名遵循 Xbox 手柄标准。在 C# 中直接使用字符串常量或 `nameof` (如果适用)。

### 移动与导航

| 动作名称    | 键盘  | 手柄                   | 说明     |
| :---------- | :---- | :--------------------- | :------- |
| `MoveUp`    | W / ↑ | 左摇杆上 / D-Pad Up    | 角色移动 |
| `MoveDown`  | S / ↓ | 左摇杆下 / D-Pad Down  | 角色移动 |
| `MoveLeft`  | A / ← | 左摇杆左 / D-Pad Left  | 角色移动 |
| `MoveRight` | D / → | 左摇杆右 / D-Pad Right | 角色移动 |
| `UiUp`      | ↑     | D-Pad Up               | 菜单导航 |
| `UiDown`    | ↓     | D-Pad Down             | 菜单导航 |

### 动作按钮

| 动作名称    | 键盘  | 手柄 (Xbox) | 功能说明          |
| :---------- | :---- | :---------- | :---------------- |
| `BtnA`      | Space | A           | 确认 / 跳跃       |
| `BtnB`      | Esc   | B           | 取消 / 返回       |
| `BtnX`      | J     | X           | 攻击              |
| `BtnY`      | I     | Y           | 特殊技能          |
| `BtnLB`     | Q     | LB          | 切换武器 (左)     |
| `BtnRB`     | E     | RB          | 切换武器 (右)     |
| `BtnLT`     | -     | LT          | 瞄准 / 模拟量输入 |
| `BtnRT`     | -     | RT          | 射击 / 模拟量输入 |
| `BtnStart`  | Esc   | Start       | 暂停菜单          |
| `BtnSelect` | Tab   | Select      | 地图 / 信息       |

### 摇杆 (模拟量)

| 动作名称          | 摇杆方向 | 说明          |
| :---------------- | :------- | :------------ |
| `StickRightUp`    | 右摇杆上 | 瞄准/视野控制 |
| `StickRightDown`  | 右摇杆下 | 瞄准/视野控制 |
| `StickRightLeft`  | 右摇杆左 | 瞄准/视野控制 |
| `StickRightRight` | 右摇杆右 | 瞄准/视野控制 |

---

## 2. C# 代码实战指南

### 核心原则

1. **PascalCase 命名**：在 `project.godot` 中定义的动作名与 C# 变量/方法命名风格一致。
2. **区分输入模式**：
   - **持续性输入** (移动、瞄准) -> 在 `_Process` 或 `_PhysicsProcess` 中轮询 (`Input.GetVector`)。
   - **一次性输入** (跳跃、交互) -> 在 `_Input` 或 `_UnhandledInput` 事件中处理 (`event.IsActionPressed`)。

### 场景 A: 角色移动 (轮询模式)

```csharp
public override void _PhysicsProcess(double delta)
{
    // 获取归一化的向量 (自动处理死区和斜向速度限制)
    Vector2 direction = Input.GetVector("MoveLeft", "MoveRight", "MoveUp", "MoveDown");

    Velocity = direction * Speed;
    MoveAndSlide();
}
```

### 场景 B: 跳跃与攻击 (事件模式)

```csharp
public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsEcho()) return;

    if (@event.IsActionPressed("BtnA"))
    {
        Jump();
    }
}
```

### 场景 C: 瞄准 (右摇杆)

```csharp
public override void _Process(double delta)
{
    Vector2 aimDir = Input.GetVector("StickRightLeft", "StickRightRight", "StickRightUp", "StickRightDown");
    if (aimDir.LengthSquared() > 0.1f)
    {
        LookAt(GlobalPosition + aimDir);
    }
}
```

### 场景 D: 震动反馈

```csharp
// 设备ID 0, 低频强度, 高频强度, 持续时间
Input.StartJoyVibration(0, 0.5f, 1.0f, 0.2f);
```
