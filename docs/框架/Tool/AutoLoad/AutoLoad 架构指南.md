# AutoLoad 架构指南 (C# AutoLoad 引导器)

#AutoLoad #Godot #CSharp #Singleton

## 1. 核心哲学：为什么不直接用 Godot 的 Autoload？

在 Godot 的原生机制中，每个 Autoload 都是在项目设置里手动添加的独立节点。当项目变大时，会产生以下问题：

- **依赖管理缺失**：无法确保 `ManagerB` 一定在 `ManagerA` 之后完成初始化。
- **配置难以维护**：几十个单例分散在项目设置列表里，缺乏统一的视图。
- **类型安全匮乏**：原生方式通常需要 `GetNode("/root/Name")`，在 C# 中缺乏编译时检查和 IDE 智能提示。
- **生命周期不可控**：所有单例被迫在游戏启动时全量加载，无法按需或按阶段（如 Core -> System -> Game）加载。

**本项目的解决方案：AutoLoad (引导程序模式)**。
项目只注册 **一个** 真正的 Godot Autoload 节点：`AutoLoad.cs`。它作为一个容器，负责根据代码中的配置，按顺序动态加载、初始化并管理其他所有单例。

---

## 2. 系统组成

该系统完全由 C# 实现，核心逻辑集中在 `AutoLoad.cs`：

1. **`AutoLoad.cs` (引导核心)**:
   - **配置中心**：在 `Configure()` 方法中统一注册所有单例。
   - **加载引擎**：负责解析依赖关系、执行资源加载（`.tscn` 或 `.cs`）并实例化。
   - **单例容器**：持有所有已加载单例的引用。
   - **全局访问点**：提供静态 `Instance` 属性和泛型 `Get<T>()` 方法。

---

## 3. 核心功能特性

### 3.1 优先级加载 (Priority)

通过内部类 `Priority` 定义加载顺序，确保底层服务始终先于上层逻辑：

- `Priority.Core (0)`: 核心工具（如日志系统、事件总线）。
- `Priority.System (100)`: 系统服务（如音频管理器、资源管理器）。
- `Priority.Game (200)`: 游戏逻辑（如战斗管理器、玩家数据中心）。
- **规则**：优先级数值越小，越先被加载。

```csharp
private void Configure()
{
    // 注册新单例 (在 AutoLoad.cs 中)
    Register("AudioManager", "res://Src/Managers/AudioManager.tscn", Priority.System);

    // 声明依赖：GameManager 必须在 AudioManager 之后加载
    Register("GameManager", "res://Src/Managers/GameManager.cs", Priority.Game, dependsOn: "AudioManager");
}
```

### 3.2 强类型依赖检查

在注册时使用 `dependsOn` 参数声明依赖：

- AutoLoad 会在加载前检查所有依赖项是否已成功注册并加载。
- 若依赖链缺失，将通过 `Log.Error` 立即报错，防止运行时出现难以追踪的空指针异常。

### 3.3 类型安全访问

彻底告别字符串路径，使用泛型获取实例：

- `AutoLoad.Instance.Get<T>()`：自动通过类名查找并返回强类型引用。

---

## 4. 使用手册

### 4.1 注册新单例 (在 `AutoLoad.cs` 中)

打开 `AutoLoad.cs` 的 `Configure()` 方法：

```csharp
private void Configure()
{
    // 注册场景文件 (.tscn)
    Register("AudioManager", "res://Src/Managers/AudioManager.tscn", Priority.System);

    // 注册纯脚本文件 (.cs)
    Register("GameManager", "res://Src/Managers/GameManager.cs", Priority.Game, dependsOn: "AudioManager");
}
```

### 4.2 C# 代码中访问

```csharp
// 1. 获取强类型引用
var audio = AutoLoad.Instance.Get<AudioManager>();

// 2. 调用方法
audio.PlayMusic("BattleTheme");
```

---

## 5. 最佳实践与注意事项

1. **命名规范**：注册时的 `Name` 必须与类名保持一致，以便 `Get<T>()` 语法糖能正确识别。
2. **避免循环依赖**：设计管理器时应保持单向依赖。如果 A 依赖 B，B 也依赖 A，系统将报错。
3. **初始化时机**：不要在单例的 `_Init()` 中访问其他单例，因为此时目标可能尚未进入场景树。建议在 `_Ready()` 中进行交互。
4. **禁止静态存储 Node**：严禁在 C# 中使用 `static` 变量存储单例引用的 Node 实例。始终通过 `AutoLoad.Instance.Get<T>()` 获取，以防止场景切换或重启导致的引用失效。

---

## 6. 开发者提示词 (AI Prompt Helper)

当你需要 AI 协助创建新的单例管理器时，请使用以下提示：

> "请帮我创建一个名为 `XXXManager` 的全局单例类。要求：
>
> 1. 使用 C# 编写，继承自 `Node`。
> 2. 实现 [具体功能描述]。
> 3. 使用 `private static readonly Log _log = new Log("XXXManager");` 进行日志记录。
> 4. 告诉我如何在 `AutoLoad.cs` 的 `Configure()` 方法中注册它，并设置它依赖于 `AudioManager`。"
