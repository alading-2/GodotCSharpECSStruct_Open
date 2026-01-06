# Logger 设计文档 (高级 C# 版)

**核心理念**：专业级游戏开发日志系统。支持实例初始化、分级过滤、按类过滤、格式配置，最大化开发效率。

---

## 1. 为什么选择此方案？

### 1.1 为什么包装 `GD.PrintRich` 而不是使用标准 C# Console？

在 Godot 开发环境中，编辑器的 **Output (输出)** 面板是开发者最常查看的窗口。

- **Godot 的 `GD.PrintRich`**：支持 **BBCode** 颜色标签。这意味着我们可以让 `ERROR` 显示为红色，`SUCCESS` 显示为绿色，`DEBUG` 显示为青色。这种视觉区分能极大地提高阅读日志的效率。
- **标准 C# `Console.WriteLine`**：在 Godot 编辑器中显示为单色纯文本，无法快速区分重要信息。

### 1.2 为什么需要实例和过滤？

在大型项目中，日志噪音是效率杀手。

- **实例初始化**：通过在类中定义 `static Log` 实例，可以自动为该类所有日志标记上下文名称。
- **全局控制**：通过 `Log.GlobalLevel` 一键屏蔽低优先级日志。
- **上下文过滤**：你正在调试 `Player.cs`，不希望被 `EnemyAI.cs` 的海量日志刷屏。通过 `Log.SetLevel("EnemyAI", LogLevel.Warning)` 即可静音敌人模块。

---

## 2. API 与配置

### 2.1 日志等级 (LogLevel)

| 等级        | 值  | 说明                         |
| :---------- | :-- | :--------------------------- |
| **Trace**   | 0   | 最细粒度的运行时流转信息     |
| **Debug**   | 1   | 关键变量或逻辑点调试         |
| **Info**    | 2   | 普通运行信息                 |
| **Success** | 3   | 关键流程成功完成             |
| **Warning** | 4   | 警告 (调用 `GD.PushWarning`) |
| **Error**   | 5   | 错误 (调用 `GD.PushError`)   |
| **None**    | 99  | 关闭日志                     |

### 2.2 全局配置选项

所有配置均为静态属性，可在游戏启动时（如 `Main.cs` 或 `GameManager.cs`）进行设置。

```csharp
// 全局日志等级：只显示 Info 及以上
Log.GlobalLevel = LogLevel.Info;

// 格式控制
Log.ShowTimestamp = true;  // 显示时间戳
Log.ShowContext = true;    // 显示 [类名]

// **核心功能：上下文过滤**
// 屏蔽 Player 类的所有 Trace/Debug 日志，只看 Info 及以上
Log.SetLevel("Player", LogLevel.Info);
// 彻底屏蔽 Enemy 类的所有日志
Log.SetLevel("Enemy", LogLevel.None);
```

---

## 3. 使用示例

### 3.1 基础用法 (推荐)

在每个类中定义一个静态的 `Log` 实例。

```csharp
using BrotatoMy;

public partial class Weapon : Node2D
{
    // 1. 初始化 Log 实例，传入类名
    private static readonly Log Log = new Log("Weapon");

    public override void _Ready()
    {
        // 2. 追踪日志 (仅在 Debug 模式生效，发布版自动剔除)
        Log.Trace("Weapon _Ready called");

        // 3. 调试日志 (仅在 Debug 模式生效，发布版自动剔除)
        Log.Debug($"Loading weapon stats");

        if (LoadStats())
        {
            // 4. 成功日志
            Log.Success("Weapon loaded successfully");
        }
        else
        {
            // 5. 错误日志
            Log.Error("Failed to load weapon stats!");
        }
    }
}
```

### 3.2 进阶用法：特定实例等级

你可以为某个特定类的日志设置独立的等级，而不受全局等级限制。

```csharp
// 该类的日志即使全局是 Info，也会显示 Trace
private static readonly Log Log = new Log("CriticalSystem", LogLevel.Trace);
```

---

## 4. 最佳实践

1.  **开发阶段**：将 `GlobalLevel` 设为 `Debug` 或 `Trace`。
2.  **发布阶段**：无需修改代码，`Trace` 和 `Debug` 调用会被编译器自动移除。建议将 `GlobalLevel` 设为 `Info` 或 `Warning` 以防万一。
3.  **调试特定 Bug**：
    - 在初始化代码中设置 `Log.SetLevel("BuggyScript", LogLevel.Trace);`
    - 设置 `Log.GlobalLevel = LogLevel.Warning;` (屏蔽其他所有脚本的噪音)
    - 这样控制台里就只有那个出 Bug 脚本的详细日志了。

---

## 5. 性能与优化

### 5.1 Release 模式零损耗

利用 C# 的 `[Conditional("DEBUG")]` 特性，所有的 `Log.Trace` 和 `Log.Debug` 调用在发布版本（Release）中会被编译器**彻底移除**。

- **意味着**：即使你在 `_Process` 里每帧写 100 行 `Log.Debug`，发布后的游戏性能损耗也是 **0**。

### 5.2 过滤保护

在进行任何字符串拼接（如时间戳、BBCode）之前，系统会先检查日志等级。如果等级被过滤，方法会立即返回，**不产生任何不必要的性能开销**。

---

## 6. 文件结构

```
scenes/Tools/
└── Logger/
    └── Log.cs             # 核心逻辑
```
