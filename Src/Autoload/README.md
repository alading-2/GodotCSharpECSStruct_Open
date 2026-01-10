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

## 如何添加新单例

推荐使用 **去中心化注册** 方式，在模块自身代码中注册，无需修改 `AutoLoad.cs`。

1. **创建单例**：创建对应的 `.cs` 脚本（需继承 `Node`）或 `.tscn` 场景。
2. **注册配置**：在类中使用 `[ModuleInitializer]` 特性进行注册。

   ```csharp
   using System.Runtime.CompilerServices;

   public partial class MySystem : Node
   {
       [ModuleInitializer]
       public static void Initialize()
       {
           AutoLoad.Register(new AutoLoad.AutoLoadConfig
           {
               Name = nameof(MySystem),
               Path = "res://Src/Systems/MySystem.cs", // 或 .tscn 路径
               Priority = AutoLoad.Priority.System,
               // 可选：指定挂载父节点，默认为 "AutoLoad"
               ParentPath = "AutoLoad/MySystems",
               // 可选：指定依赖项，确保依赖模块先加载
               Dependencies = new[] { "OtherSystem" }
           });
       }
   }
   ```
3. **全局访问**：
   ```csharp
   // 类型安全获取
   var system = AutoLoad.Instance.Get<MySystem>();
   ```

## 架构原则

1. **去中心化**：各模块自行注册，减少 `AutoLoad` 类的耦合。
2. **有序加载**：严格遵守 Priority 顺序，确保底层服务就绪。
3. **依赖管理**：通过 `Dependencies` 显式声明依赖关系。
4. **层级管理**：通过 `ParentPath` 将单例节点分类挂载，保持场景树整洁。
