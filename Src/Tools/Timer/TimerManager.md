# TimerManager - 高性能定时器系统

## 概述

`TimerManager` 是轻量级、高性能的定时器管理系统，专为 Godot 4.x + C# 设计。

## 核心优势

- **对象池集成**: 完全集成项目的 `ObjectPool<T>` 系统，零 GC 压力
- **链式 API**: 流畅的 `.OnComplete()`, `.OnLoop()`, `.WithTag()` 调用
- **批量管理**: Tag 系统支持批量操作
- **TimeScale 支持**: 游戏时间 / 真实时间双模式

## 快速开始

### 延迟执行（单次）

```csharp
TimerManager.Instance.Delay(2.0f)
    .OnComplete(() => GD.Print("2秒后执行"));

// 带进度追踪
TimerManager.Instance.Delay(10.0f)
    .OnUpdate(p => progressBar.Value = p)
    .OnComplete(() => GD.Print("完成"));
```

### 无限循环

```csharp
var timer = TimerManager.Instance.Loop(1.0f)
    .WithTag("Buff")
    .OnLoop(() => GD.Print("每秒执行"));
```

### 重复 N 次

```csharp
TimerManager.Instance.Repeat(0.5f, 5)
    .OnRepeat(n => GD.Print($"剩余 {n} 次"))
    .OnComplete(() => GD.Print("全部完成"));

// 立即执行模式：创建后立刻触发第一次回调，两种写法：
TimerManager.Instance.Repeat(0.5f, 5, true)
    .OnRepeat(n => GD.Print($"剩余 {n} 次"));
TimerManager.Instance.Repeat(0.5f, 5)
    .Immediate()
    .OnRepeat(n => GD.Print($"剩余 {n} 次"));
```

### 倒计时

```csharp
TimerManager.Instance.Countdown(10.0f, 0.5f)
    .OnCountdown((elapsed, progress) => {
        label.Text = $"剩余 {10 - elapsed:F0}s";
        progressBar.Value = progress;
    })
    .OnComplete(() => GD.Print("时间到"));
```

### 真实时间（不受暂停影响）

```csharp
// UI 动画使用真实时间
TimerManager.Instance.Delay(0.5f, useUnscaledTime: true)
    .OnComplete(() => panel.Hide());
```

## 标签管理

```csharp
// 创建时设置标签
var timer = TimerManager.Instance.Loop(1.0f)
    .WithTag("Buff");

// 批量操作
TimerManager.Instance.CancelByTag("Buff");
TimerManager.Instance.SetAllTimerPausedByTag("Buff", true);
```

## 生命周期管理（重要）

定时器是池化对象，必须在 `_ExitTree` 中主动取消：

```csharp
public partial class Enemy : CharacterBody2D
{
    private GameTimer _regenTimer;

    public override void _Ready()
    {
        _regenTimer = TimerManager.Instance.Loop(1.0f)
            .WithTag("Buff")
            .OnLoop(OnRegen);
    }

    public override void _ExitTree()
    {
        _regenTimer?.Cancel();
        _regenTimer = null;
    }
}
```

## API 参考

### 工厂方法

| 方法 | 说明 |
|------|------|
| `Delay(float duration)` | 延迟执行（单次） |
| `Loop(float interval)` | 无限循环 |
| `Repeat(float interval, int count)` | 重复 N 次 |
| `Countdown(float duration, float interval)` | 倒计时 |

### 链式配置

| 方法 | 说明 |
|------|------|
| `.OnComplete(Action)` | 完成回调 |
| `.OnLoop(Action)` | 循环回调 |
| `.OnRepeat(Action<int>)` | 重复回调（参数：次数） |
| `.OnTick(Action<float,float>)` | 倒计时回调（参数：elapsed, progress），也可使用 `.Countdown()` |
| `.OnCountdown(Action<float,float>)` | 同上（链式调用更自然） |
| `.OnUpdate(Action<float>)` | 进度更新（参数：0-1） |
| `.WithTag(string)` | 设置标签 |

### 管理方法

| 方法 | 说明 |
|------|------|
| `Cancel(string id)` | 按 ID 取消 |
| `CancelByTag(string tag)` | 按标签取消 |
| `SetAllTimerPaused(bool)` | 暂停/恢复全部 |
| `SetAllTimerPausedByTag(string, bool)` | 按标签暂停 |
