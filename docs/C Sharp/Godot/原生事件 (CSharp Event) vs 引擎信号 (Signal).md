# Godot C# 核心：原生事件 (C# Event) vs 引擎信号 (Signal)

#CSharp #Godot #Event #Architecture

## 1. 观念纠正：为什么首选 C# Event？

很多 Godot C# 新手会陷入一个误区：_“既然是用 Godot，那我就要把所有事件都写成 `[Signal]`。”_
**这是错的。**

在构建游戏架构（如事件总线、状态机、数据更新）时，**C# 原生 `event`** 才是王道。

### C# Event vs Godot Signal 对比表

| 特性         | C# 原生 Event (`event Action`)      | Godot 信号 (`[Signal]`)                |
| :----------- | :---------------------------------- | :------------------------------------- |
| **性能**     | **极快** (语言底层特性)             | 较慢 (依赖引擎反射)                    |
| **依赖性**   | **无依赖** (纯 C# 类可用)           | 必须继承 `Node` / `GodotObject`        |
| **类型检查** | **编译期强检查** (参数错了直接红线) | 运行时检查 (容易报错)                  |
| **用途**     | **内部架构、逻辑解耦**              | **UI 交互、编辑器连线、GDScript 交互** |

**结论**：除了必须和引擎或 GDScript 交互的情况，**请默认使用 C# Event**。

---

## 2. 核心写法：使用 `Action` 建立事件 (推荐)

现代 C# 开发中，我们不再手动定义 `delegate`，而是直接配合 `System.Action` 使用。

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

## 4. 什么时候还需要用 `[Signal]`？ (特定场景)

虽然 C# Event 很好，但在以下 **3 种情况** 下，你必须用 Godot 的 `[Signal]`：

1. **编辑器连线**：如果你想在 Godot 编辑器的右侧 "Node" 面板里，把信号拖拽连接到另一个节点。
2. **跨语言交互**：如果你的逻辑是 C# 写的，但 UI 是用 **GDScript** 写的，GDScript 只能监听 `[Signal]`，听不懂 C# Event。
3. **引擎自带信号**：按钮的点击 (`Pressed`)、碰撞检测 (`BodyEntered`)，这些是 Godot 提供的，我们只能用 `+=` 去连接它们。

### 示例：监听引擎自带信号

```C#
public override void _Ready()
{
    // Button.Pressed 本质上是 Godot 封装好的 Event
    // 这里的用法和 C# 原生事件完全一致
    GetNode<Button>("Btn").Pressed += () => GD.Print("点击");
}
```

---

## 5. 自定义 `[Signal]` 的写法 (不推荐用于纯逻辑)

如果你确实需要跨语言交互，必须写自定义信号，请注意以下 Godot C# 独有的**命名规则陷阱**。

### 第一步：声明 (陷阱所在)

必须以 `delegate` 声明，且名字必须以 `EventHandler` 结尾。

```C#
[Signal]
// 实际生成的信号名是 "HealthChanged" (去掉了后缀)
public delegate void HealthChangedEventHandler(int newValue);
```

### 第二步：发射

必须使用 `EmitSignal`，且引用 `SignalName`。

```C#
EmitSignal(SignalName.HealthChanged, 100);
```

### 总结

**一句话口诀**：**定义时加 `EventHandler`，使用时去掉它。** (这也是为什么不推荐用的原因之一，太麻烦了)。
