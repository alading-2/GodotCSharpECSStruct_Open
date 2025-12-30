# AutoLoad 自动加载系统 (C#)

## 什应该放在 AutoLoad？

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

| 阶段            | 数值 | 示例                       |
| :-------------- | :--- | :------------------------- |
| Priority.Core   | 0    | 核心工具 (Log, EventBus)   |
| Priority.System | 100  | 系统服务 (Audio, Save, UI) |
| Priority.Game   | 200  | 游戏逻辑 (Battle, Player)  |
| Priority.Debug  | 900  | 仅开发使用的工具           |

## 如何添加新单例

1. **创建单例**：创建对应的 `.cs` 脚本或 `.tscn` 场景。
2. **注册配置**：在 `AutoLoad.cs` 的 `Configure()` 方法中调用 `Register()`。
   ```csharp
   Register("AudioManager", "res://Src/Managers/AudioManager.tscn", Priority.System);
   ```
3. **全局访问**：
   ```csharp
   var audio = AutoLoad.Instance.Get<AudioManager>();
   ```

## 架构原则
