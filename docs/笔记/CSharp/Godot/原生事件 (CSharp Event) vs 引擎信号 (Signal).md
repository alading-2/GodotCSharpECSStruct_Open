# Godot C# 核心：原生事件 (C# Event) vs 引擎信号 (Signal)

#CSharp #Godot #Event #Architecture

## 1. 观念纠正：为什么首选 C# Event？

很多 Godot C# 新手会陷入一个误区：_“既然是用 Godot，那我就要把所有事件都写成 `[Signal]`。”_
**这是错的。**

在构建游戏架构（如事件总线、状态机、数据更新）时，**C# 原生 `event`** 才是王道。

### C# Event vs Godot Signal 对比表

| 特性               | C# 原生 Event (`event Action`)          | Godot 信号 (`[Signal]`)                    |
| :----------------- | :---------------------------------------- | :------------------------------------------- |
| **性能**     | **极快** (语言底层特性)             | 较慢 (依赖引擎反射)                          |
| **依赖性**   | **无依赖** (纯 C# 类可用)           | 必须继承 `Node` / `GodotObject`          |
| **类型检查** | **编译期强检查** (参数错了直接红线) | 运行时检查 (容易报错)                        |
| **用途**     | **内部架构、逻辑解耦**              | **UI 交互、编辑器连线、GDScript 交互** |

**结论**：除了必须和引擎或 GDScript 交互的情况，**请默认使用 C# Event**。

---

## 2. 核心写法：使用 `Action` 建立事件 (推荐)

现代 C# 开发中，我们不再手动定义 `delegate`，而是直接配合 `System.Action` 使用。

### 快速掌握

```C#
// 定义
public event Action<int> OnHealthChanged;

// 触发
OnHealthChanged?.Invoke(currentHp);

// 订阅
player.OnHealthChanged += UpdateUI;

// 取消
player.OnHealthChanged -= UpdateUI;
```

### A. 定义事件 (发布者)

不需要 `[Signal]`，不需要 `EventHandler` 后缀，就是一个标准的 C# 变量。

```csharp
using System; // 必须引用 Action

public class PlayerHealth // 注意：这甚至不需要是 Node，可以是纯 C# 类
{
    // 1. 定义事件：参数是 int (当前血量)
    public event Action<int> OnHealthChanged;

    private int _hp;

    public void TakeDamage(int dmg)
    {
        _hp -= dmg;

        // 2. 触发事件 (?.Invoke 是防爆写法，没人监听就不执行)
        OnHealthChanged?.Invoke(_hp);
    }
}
```

### B. 监听事件 (订阅者)

使用 `+=` 订阅，使用 `-=` 取消。

```C#
public partial class GameUI : Control
{
    private PlayerHealth _playerHealth;

    public void Init(PlayerHealth health)
    {
        _playerHealth = health;

        // 3. 订阅
        _playerHealth.OnHealthChanged += UpdateHpBar;
    }

    private void UpdateHpBar(int currentHp)
    {
        GD.Print($"UI更新血量: {currentHp}");
    }

    // 4. 重要：销毁时必须解绑，否则内存泄漏！
    // C# 事件是强引用，如果不解绑，发布者会一直拽着订阅者不松手。
    public override void _ExitTree()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHpBar;
        }
    }
}
```

---

## 3. 架构神器：静态事件总线 (Event Bus)

这是 `C# Event` 最强大的应用场景。你不需要在场景里放一个 "EventBus" 节点，直接写一个静态类即可。

```C#
// EventBus.cs
public static class EventBus
{
    // 定义一个全局静态事件
    // 比如：游戏结束事件
    public static event Action OnGameOver;

    // 提供一个触发方法
    public static void TriggerGameOver()
    {
        OnGameOver?.Invoke();
    }
}
```

**使用：**

- **任何地方触发**：`EventBus.TriggerGameOver();`
- **任何地方监听**：`EventBus.OnGameOver += ShowLoseScreen;`

---

## 4. 重点：监听引擎原生信号 (C#使用Godot原生信号)

很多初学者还在找 `Connect("area_entered", ...)`，其实 Godot 已经把所有原生信号都包装成了 **C# 事件**。

### A. 核心规则：命名转换

- **GDScript**: `area_entered` (蛇形命名)
- **C# Event**: `AreaEntered` (大驼峰命名)

### B. 实战：碰撞检测与模式匹配

这是处理游戏逻辑最优雅的方式。不需要通过字符串判断，直接利用 C# 的**类型系统**。

```C#
public partial class Player : CharacterBody2D
{
    private static readonly Log _log = new Log("Player");

    public override void _Ready()
    {
        // 1. 获取节点并订阅事件
        var hurtBox = GetNode<Area2D>("HurtBox");
        hurtBox.AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {      
        // 2. 利用模式匹配 (Pattern Matching) 识别对象
        switch (area)
        {
            case Bullet b when b.Damage > 0:
                _log.Info($"受到子弹伤害: {b.Damage}");
                TakeDamage(b.Damage);
                b.QueueFree();
                break;

            case Coin c:
                _log.Success("捡到金币！");
                AddMoney(c.Value);
                c.QueueFree();
                break;
        }
    }

    public override void _ExitTree()
    {
        // 3. 别忘了在退出树时取消订阅
        GetNode<Area2D>("HurtBox").AreaEntered -= OnAreaEntered;
    }
}
```

### C. 如何查找参数？

不需要死记硬背。在 IDE (VS Code) 中输入 `+=` 后，把鼠标悬停在事件名（如 `BodyEntered`）上，IDE 会告诉你回调函数的签名。

- `Pressed` -> `() => ...` (无参数)
- `AreaEntered` -> `(Area2D area) => ...`
- `TextChanged` -> `(string newText) => ...`

---

## 5. 什么时候还需要用 `[Signal]`？

虽然 C# Event 覆盖了 90% 的场景，但在以下情况你必须使用 Godot 的 `[Signal]`：

1. **编辑器连线**：需要在 Godot 编辑器的 "Node" 面板手动连接信号。
2. **跨语言交互**：你的逻辑是 C#，但 UI 或其他脚本是 **GDScript** 写的。
3. **动画轨道调用**：在 AnimationPlayer 的轨道中插入信号调用。

---

## 6. 自定义 `[Signal]` 的写法

如果确实需要定义自定义信号，请遵循 Godot C# 的特定语法。

### 第一步：声明

必须以 `delegate` 声明，且名字必须以 `EventHandler` 结尾。

```C#
[Signal]
// 实际生成的信号名是 "HealthChanged" (自动去掉后缀)
public delegate void HealthChangedEventHandler(int newValue);
```

### 第二步：发射

必须使用 `EmitSignal`，且建议引用 `SignalName` 以获得类型安全。

```C#
EmitSignal(SignalName.HealthChanged, currentHp);
```

### 总结

- **逻辑内部**：用 `event Action` (C# 原生)。
- **监听引擎**：直接 `+=` (官方包装好的 Event)。
- **跨语言/编辑器**：用 `[Signal]`。