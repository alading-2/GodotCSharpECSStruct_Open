- [C# 信号 — Godot Engine (4.5) 简体中文文档](https://docs.godotengine.org/zh-cn/4.5/tutorials/scripting/c_sharp/c_sharp_signals.html#doc-c-sharp-signals)


# Godot C# 开发速查手册 (Indie Dev Standard)

#CSharp #Godot #BestPractices #Cheatsheet

> **目标**：为独立游戏开发者提供一套**高性能**、**可维护**、**强类型**的 C# 脚本标准。拒绝“玩具代码”，拥抱工程化。

---

## 1. 终极脚本模板 (The Ultimate Template)

不要再写裸奔的类了。一个合格的 Godot C# 脚本应该包含 **命名空间**、**类特性** 和 **代码分区**。

```csharp
using Godot;
using System;
using System.Collections.Generic;

// 1. 命名空间：防止类名冲突，建议格式：项目名.模块名
namespace MyGame.Characters;

// 2. [GlobalClass]：让这个类出现在 Godot 的 "Add Node" 搜索栏里，且能作为资源类型导出
[GlobalClass]
public partial class PlayerController : CharacterBody2D
{
    // --- 区域划分 (#region) 让代码可读性提升 10 倍 ---

    #region Exports (编辑器配置)
    [ExportGroup("Movement Stats")] // 分组
    [Export] public float MoveSpeed { get; set; } = 300.0f;
    [Export] public float Friction { get; set; } = 20.0f;

    [ExportGroup("Combat")]
    [Export] public PackedScene? BulletPrefab { get; set; } // 使用可空类型 ? 防止未赋值警告
    #endregion

    #region Internal Variables (内部变量)
    private Sprite2D _sprite = null!; // null! 告诉编译器"我会在 _Ready 里赋值，别报错"
    private bool _isDead = false;
    #endregion

    #region C# Events (逻辑事件)
    // 纯逻辑事件，用于解耦 UI 和 音效
    public event Action<int>? OnHealthChanged;
    #endregion

    #region Lifecycle (生命周期)
    public override void _Ready()
    {
        // 推荐使用 %UniqueName 获取节点 (Godot 场景里右键节点 -> Access as Unique Name)
        // 性能比路径查找快，且不怕节点移动位置
        _sprite = GetNode<Sprite2D>("%SpriteVisual");

        // 初始检查
        if (BulletPrefab == null) GD.PushWarning("Player: 子弹预制体未绑定！");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead) return;
        HandleMovement((float)delta);
    }
    #endregion

    #region Logic Methods (逻辑方法)
    private void HandleMovement(float delta)
    {
        // ... 移动逻辑 ...
    }
    #endregion
}
````

---

## 2. 核心特性：C# 里的 Godot 黑魔法

### A. `[Export]` 进阶用法

不要只 Export `int` 和 `float`，Godot C# 的导出非常强大。

```C#
// 1. 导出枚举 (下拉菜单)
public enum WeaponType { Sword, Gun, Magic }
[Export] public WeaponType CurrentWeapon { get; set; }

// 2. 导出文件路径 (自动过滤后缀)
[Export(PropertyHint.File, "*.png,*.jpg")]
public string IconPath { get; set; } = "";

// 3. 导出资源数组
[Export] public Godot.Collections.Array<AudioStream> FootstepSounds { get; set; } = new();

// 4. 导出节点引用 (比 GetNode 更安全，允许在编辑器里拖拽节点)
[Export] public Node2D? TargetNode { get; set; }
```

### B. 节点获取的“最佳实践”

| **写法**                    | **安全性**            | **性能**            | **推荐场景**          |
| --------------------------- | --------------------- | ------------------- | --------------------- |
| `GetNode<T>("Path/To")`     | 低 (路径改了就崩)     | 中                  | 极少使用              |
| `GetNode<T>("%UniqueName")` | **高** (移动节点不怕) | **高** (有缓存)     | **首选** (场景内引用) |
| `[Export] public T Node`    | **极高** (所见即所得) | **极高** (无需查找) | **首选** (跨层级引用) |
| `GetNodeOrNull<T>(...)`     | 中                    | 中                  | 可能不存在的组件      |

### C. 信号的“陷阱与解法”

虽然我们推荐用 C# Event，但如果必须用 `[Signal]` (比如为了编辑器连线)，请记住口诀：**定义带后缀，使用去后缀**。

```C#
// 1. 定义
[Signal] public delegate void AmmoChangedEventHandler(int newValue);

// 2. 发射
EmitSignal(SignalName.AmmoChanged, 10);

// 3. 异步等待信号 (例如：等待动画播放完毕)
await ToSignal(GetNode<AnimationPlayer>("Anim"), AnimationPlayer.SignalName.AnimationFinished);
```

---

## 3. 性能红线 (Roguelite 必读)

在《Brotato》这种同屏几百个怪物的游戏中，以下代码是**绝对禁止**出现在 `_Process` 或 `_PhysicsProcess` 中的。

### 🚫 禁止列表

1. **禁止每帧 `new` 引用类型**：

   - ❌ `var list = new List<int>();`
   - ❌ `var timer = new Timer();`
   - ✅ 使用成员变量 `_list.Clear()` 或对象池。

2. **禁止每帧字符串拼接**：

   - ❌ `Label.Text = "HP: " + hp;` (产生大量垃圾 string)
   - ✅ 只有数值变化时才更新 UI (使用 Signal 或 Setter)。

3. **禁止每帧 `GetNode`**：

   - ❌ `GetNode<Player>("Player").Position`
   - ✅ 在 `_Ready` 里缓存引用 `_player = GetNode...`。

4. **禁止滥用 LINQ**：

   - ❌ `enemies.Where(e => ...).ToList()` (每帧调用会卡顿)
   - ✅ 使用 `for` 循环或维护特定列表。

---

## 4. 输入处理的“工业标准”

对于动作游戏，不要用 `if` 检测按键，要使用 **向量输入** 和 **事件缓冲**。

```C#
public override void _PhysicsProcess(double delta)
{
    // 1. 获取全向移动向量 (自动处理摇杆死区、归一化)
    // 建议在项目设置 -> 输入映射 中配置好 "move_left" 等
    Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");

    // 2. 移动逻辑
    if (inputDir != Vector2.Zero)
    {
        Velocity = inputDir * Speed;
    }
    else
    {
        // 3. 实现平滑刹车 (摩擦力)
        Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
    }

    MoveAndSlide(); // Godot 4 不需要参数，自动使用 Velocity
}

// 4. 处理单次点击 (跳跃/攻击)
public override void _UnhandledInput(InputEvent @event)
{
    if (@event.IsActionPressed("attack"))
    {
        // 使用输入缓冲防止吃键
        _inputBufferTimer = 0.1f;
    }
}
```

---

## 5. 调试与日志 (Debugging)

不要只用 `GD.Print`，要学会分类分级。

```C#
// 1. 普通日志 (开发时看)
GD.Print("Player Spawned");

// 2. 警告 (黄色) - 逻辑可能有问题但没崩
if (Speed > 1000) GD.PushWarning("速度异常过快！");

// 3. 错误 (红色) - 必须修
if (Target == null) GD.PushError("目标丢失！");

// 4. 仅在 Debug 模式下运行的代码
if (OS.IsDebugBuild())
{
    // 绘制调试线、显示 FPS、开启作弊码
    DrawDebugLine();
}
```

---

## 6. 常用代码片段 (Snippets)

### 延迟调用 (CallDeferred)

**场景**：在物理回调中修改物理状态，或在后台线程更新 UI。

```C#
// 现代 Lambda 写法
Callable.From(() => {
    AddChild(newMonster);
    newMonster.GlobalPosition = Vector2.Zero;
}).CallDeferred();
```

### 类型检测与强转

```C#
// 模式匹配 (推荐)
if (body is Enemy enemy)
{
    enemy.TakeDamage(10);
}

// 安全强转
var player = body as Player; // 失败返回 null，不报错
if (player != null) { ... }
```

### 资源加载 (ResourceLoader)

```C#
// 动态加载资源
var scene = GD.Load<PackedScene>("res://Enemies/Boss.tscn");
var boss = scene.Instantiate<Node2D>();
AddChild(boss);
```

---

## 7. 架构思维导图

做独立游戏，记住三个“不要”：

1. **不要在 Player 里写 UI 更新代码** -> 用 **C# Event** 通知 UI。
2. **不要用继承做技能系统** -> 用 **组合 (Component)** 挂节点。
3. **不要在 Update 里遍历所有怪物** -> 用 **Area2D** 碰撞检测或 **静态列表**。
