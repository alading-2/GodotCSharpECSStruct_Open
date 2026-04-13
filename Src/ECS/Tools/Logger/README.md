# Log 日志系统

## 概述

**路径**: `Src/Tools/Logger/Log.cs`

**核心价值**:
- 统一的调试输出接口
- 分级过滤（Trace/Debug/Info/Success/Warning/Error）
- 条件编译优化（Release 版本零性能损耗）

## 快速开始

```csharp
// 推荐用法：每个类声明一个静态实例
private static readonly Log _log = new Log("ClassName");

public void MyMethod()
{
    _log.Trace("细粒度追踪");  // [Conditional("DEBUG")]
    _log.Debug("调试信息");    // [Conditional("DEBUG")]
    _log.Info("普通信息");
    _log.Success("成功提示");
    _log.Warn("警告");         // 自动推送到 Debugger 面板
    _log.Error("错误");        // 自动推送到 Debugger 面板
}
```

## 全局配置

```csharp
// 发布版本建议 Info 或更高
Log.GlobalLevel = LogLevel.Info;

// 针对特定类调试
Log.SetLevel("ClassName", LogLevel.Debug);
```

## 设计亮点

```csharp
// 实例化设计，每个类独立上下文
private static readonly Log Log = new Log("Player");

// 条件编译，DEBUG 模式外完全移除
[Conditional("DEBUG")]
public void Debug(object message) { }
```

## 使用场景

- **开发阶段**：追踪游戏逻辑流程
- **性能分析**：记录关键操作耗时
- **错误排查**：定位 Bug 来源
