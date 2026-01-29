# AutoLoad 自动加载系统 (C#)

## 什么应该放在 AutoLoad？

AutoLoad 只管理**真正的全局单例**：

### ✅ 适合放 AutoLoad 的

- **AudioManager**：全局音频管理
- **DataResourceIndex**：全局资源加载
- **GameManager**：游戏流程控制
- **LogWriter**：全局日志文件写入

### ❌ 不适合放 AutoLoad 的

- **Logger**：改为组件，每个节点独立实例
- **ObjectPool**：按需创建，不同系统用不同的池

## 优先级划分 (Priority)

| 阶段            | 数值 | 描述                       | 示例                       |
| :-------------- | :--- | :------------------------- | :------------------------- |
| Priority.Core   | 0    | 核心基础                   | Log, EventBus              |
| Priority.Tool   | 100  | 工具类                     | DebugUtils                 |
| Priority.System | 200  | 系统服务                   | Audio, Save, SpawnSystem   |
| Priority.Game   | 300  | 游戏业务                   | BattleManager, Player      |
| Priority.Debug  | 400  | 调试工具                   | Console                    |

## 如何添加新模块

推荐使用 **去中心化注册** 方式，在模块自身代码中注册，无需修改 `AutoLoad.cs`。

### 1. 纯代码初始化 (逻辑/数据类)

适用于无需 Godot 节点生命周期的类（如 `DataRegister`）。此类无需继承 `Node`。

```csharp
public class MyDataRegister
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(MyDataRegister),
            InitAction = () => Init(), // 指定初始化执行的方法
            Priority = AutoLoad.Priority.Core
        });
    }

    public static void Init() 
    {
        // 注册数据逻辑
    }
}
```

### 2. 节点/场景初始化 (系统类)

适用于需要 `_Ready`, `_Process` 或在场景树中展示的系统。需继承 `Node` 并配套 `.tscn` 场景。

```csharp
public partial class MySystem : Node
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(MySystem),
            // 使用 ResourceManagement 获取自动生成的场景资源
            Scene = ResourceManagement.LoadScene<PackedScene>(nameof(MySystem), ResourceCategory.System),
            Priority = AutoLoad.Priority.System,
            Dependencies = new[] { nameof(TimerManager) } // 使用 nameof() 确保依赖项名称正确
        });
    }
}
```

### 3. 全局访问

```csharp
// 类型安全获取 (仅限继承自 Node 的单例)
var system = AutoLoad.Instance.Get<MySystem>();
```

## 架构原则

1. **去中心化**：各模块自行注册，减少 `AutoLoad` 类的耦合。
2. **有序加载**：严格遵守 Priority 顺序，确保底层服务就绪。
3. **依赖管理**：通过 `Dependencies` 显式声明依赖关系。
4. **层级管理**：通过 `ParentPath` 将单例节点分类挂载，保持场景树整洁。
