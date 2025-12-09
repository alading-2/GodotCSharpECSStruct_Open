# Bootstrap 自动加载系统

## 什么应该放在 Bootstrap？

Bootstrap 只管理**真正的全局单例**：

### ✅ 适合放 Bootstrap 的

- **AudioManager**：全局音频管理
- **ResourceManager**：全局资源加载
- **GameManager**：游戏流程控制
- **LogWriter**：全局日志文件写入

### ❌ 不适合放 Bootstrap 的

- **Logger**：改为组件，每个节点独立实例
- **ObjectPool**：按需创建，不同系统用不同的池

## 配置方法

在 `autoload_config.gd` 中添加：

```gdscript
singletons.append(SingletonConfig.new(
    "AudioManager",
    "res://scenes/managers/audio_manager.gd",
    LoadOrder.MANAGER
))
```

## 架构原则

**全局单例 vs 组件化**：

| 类型     | 使用方式   | 示例                          |
| -------- | ---------- | ----------------------------- |
| 全局单例 | Bootstrap  | AudioManager, ResourceManager |
| 组件     | 附加到节点 | Logger, HealthBar             |
| 工具类   | 按需创建   | ObjectPool, Tween             |

## 当前已加载的单例

- **LogWriter**：全局日志文件写入器

## 添加新单例

1. 创建脚本（如 `audio_manager.gd`）
2. 在 `autoload_config.gd` 中添加配置
3. 通过 `Bootstrap.audio_manager` 访问
