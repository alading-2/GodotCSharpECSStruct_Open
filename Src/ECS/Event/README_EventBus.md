# EventBus 事件系统使用说明

## 概述

EventBus 是一个高性能、类型安全的事件系统，支持局部事件总线（Entity.Events）和全局事件总线（GlobalEventBus）。

## 核心特性

- ✅ **类型安全**: 使用 `readonly record struct` 定义事件数据，零 GC + 编译期检查
- ✅ **分层架构**: 局部事件（Entity 级别）和全局事件（系统级别）
- ✅ **高性能**: 避免反射调用，优先直接委托调用
- ✅ **优先级支持**: 事件处理器可指定执行优先级
- ✅ **一次性订阅**: 支持 `Once` 订阅，自动解绑
- ✅ **重入保护**: 自动检测并阻止同类型事件递归触发，防止死循环

---

## 快速开始

### 1. 定义事件类型

所有事件必须在 `GameEventType` 中定义为常量 + `readonly record struct`：

```csharp
// Src/ECS/Event/Type/GameEventType_Unit.cs
public static partial class GameEventType
{
    public static class Unit
    {
        /// <summary>单位死亡</summary>
        public const string Dead = "unit:dead";
        public readonly record struct DeadEventData();

        /// <summary>单位受到伤害</summary>
        public const string Damaged = "unit:damaged";
        public readonly record struct DamagedEventData(float Amount);
    }
}
```

**规范**:

- ✅ 使用 `readonly record struct` (零 GC)
- ❌ 不要使用 `record class` (堆分配)
- ✅ 事件名使用小写下划线格式: `"unit:dead"`

---

### 2. 局部事件（Entity.Events）

**用途**: 单个实体的内部通信（如 HealthComponent → Enemy）

#### 如何订阅局部事件

```csharp
public partial class Enemy : CharacterBody2D, IEntity
{
    public EventBus Events { get; } = new EventBus();

    public void OnPoolAcquire()
    {
        // 直接订阅即可（OnPoolRelease 已清空事件）
        Events.On<GameEventType.Unit.DeadEventData>(GameEventType.Unit.Dead, OnDied);
    }

    private void OnDied(GameEventType.Unit.DeadEventData evt)
    {
        _log.Info($"{Name} 死亡");
        ObjectPoolManager.ReturnToPool(this);
    }

    public override void _ExitTree()
    {
        Events.Clear();  // 清理所有订阅
    }

    public void OnPoolRelease()
    {
        Events.Clear();  // 对象池归还时清空所有订阅
    }
}
```

#### 触发事件

```csharp
public partial class HealthComponent : Node
{
    private void OnHpChanged(float oldHp, float newHp)
    {
        if (newHp <= 0 && oldHp > 0)
        {
            // 触发实体的死亡事件
            _entity.Events.Emit(
                GameEventType.Unit.Dead, 
                new GameEventType.Unit.DeadEventData()
            );
        }
    }
}
```

---

### 3. 全局事件（GlobalEventBus）

**用途**: 跨系统通信（如 SpawnSystem → UI）

#### 如何订阅全局事件

```csharp
public partial class SpawnSystem : Node
{
    public override void _Ready()
    {
        // 无参事件
        GlobalEventBus.Global.On(GameEventType.Global.GameStart, OnGameStart);
        
        // 有参事件
        GlobalEventBus.Global.On<GameEventType.Global.GameOverEventData>(
            GameEventType.Global.GameOver, 
            OnGameOver
        );
    }

    private void OnGameStart()
    {
        StartWave(1);
    }

    private void OnGameOver(GameEventType.Global.GameOverEventData evt)
    {
        if (evt.IsVictory)
        {
            GD.Print("游戏胜利!");
        }
    }

    public override void _ExitTree()
    {
        // 必须手动解绑
        GlobalEventBus.Global.Off(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.Off<GameEventType.Global.GameOverEventData>(
            GameEventType.Global.GameOver, 
            OnGameOver
        );
    }
}
```

#### 触发事件

```csharp
// 方式1: 直接使用 GlobalEventBus.Global.Emit
GlobalEventBus.Global.Emit(
    GameEventType.Global.WaveStarted, 
    new GameEventType.Global.WaveStartedEventData(waveIndex)
);

// 方式2: 使用便捷方法
GlobalEventBus.TriggerWaveStarted(waveIndex);
```

---

---

## 4. 核心：事件应该在哪个实体上触发？ (Event Ownership)

这是系统设计中最关键的部分。**原则：事件必须在“拥有该状态变化”的实体上触发。**

### 事件归属速查表

| 触发实体 (Trigger) | 负责逻辑 | 典型事件示例 |
| :--- | :--- | :--- |
| **PlayerEntity** | **控制/选中/汇总**：玩家切换了当前技能、玩家升级、获得新技能 | `UI.ActiveSkillSelected`, `Ability.Added`, `Unit.LevelUp` |
| **AbilityEntity** | **内部生命周期/冷却**：技能转好了、能量变了、开始选目标、正式激活 | `Ability.Ready`, `Ability.SelectTargets`, `Ability.Activated` |
| **Item/Buff Entity** | **模块自身逻辑**：被动触发、Buff 叠加、Buff 消失 | `Item.Triggered`, `Buff.Stacked`, `Buff.Expired` |

### 为什么这样划分？ (必读)

1. **拒绝“万能实体”**：不要把所有事件（如 5 个技能的冷却）全发在 Player 上。这会导致 Player 的事件总线变成性能黑洞，且逻辑极难维护。

2. **订阅隔离**：

- **UI 技能条**：绑定 Player，只听 `ActiveSkillSelected`。
- **UI 技能槽**：绑定具体的 AbilityEntity，听它的 `Ready` 或 `Cooldown`。

3. **架构清晰**：看到 `entity.Events.Emit`，你就应该立刻知道这个事件是关于这个实体“本身”的变化，而不是它“手下”的变化。

---

### UI 桥接模式

UI 组件如何处理这种分层关系？

1.  **第一步：听父实体（控制信号）**
    UI 绑定到玩家，监听“我要看哪个技能”。
    ```csharp
    // 监听玩家切换了哪个技能 (在 PlayerEntity 上触发)
    _player.Events.On<ActiveSkillSelectedEventData>(
        GameEventType.UI.ActiveSkillSelected, 
        OnSelectionChanged
    );
    ```

2.  **监听子实体状态事件**
    当 UI 确定了要显示的具体技能后，直接监听该技能实体的内部事件。

    ```csharp
    private void OnSelectionChanged(ActiveSkillSelectedEventData evt) {
        // 获取选中的具体技能实体
        var ability = EntityManager.GetAbilityByName(_player, evt.AbilityName);
        // 监听该技能自己的事件 (在 AbilityEntity 上触发)
        ability.Events.On<ReadyEventData>(GameEventType.Ability.Ready, OnReady);
    }
    ```

---

## 5. EventContext 请求-响应模式

`EventContext` 支持通过事件传递返回值，适用于"请求-响应"场景。

### 使用场景

当事件发送者需要从处理器获取结果时（如技能触发结果、检查结果等）。

### 基本用法

```csharp
// 发送方：将 EventContext 放入业务上下文
var context = new CastContext
{
    Ability = ability,
    Caster = caster,
    ResponseContext = new EventContext()
};
ability.Events.Emit(
    GameEventType.Ability.TryTrigger,
    new GameEventType.Ability.TryTriggerEventData(context)
);

// 读取强类型结果
var result = context.ResponseContext?.HasResult == true
    ? (TriggerResult)context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;

if (result == TriggerResult.Success) {
    _log.Info("技能触发成功");
}
```

```csharp
// 接收方：写入结果到 EventContext
public static void HandleTryTrigger(GameEventType.Ability.TryTriggerEventData eventData)
{
    var context = eventData.Context;
    var resultContext = context.ResponseContext;
    
    var result = TryTriggerAbilityWithContext(context);
    resultContext?.SetResult(result);  // 写入强类型结果
}
```

### EventContext API

| 方法 | 说明 |
|:---|:---|
| `SetResult<T>(T result)` | 设置强类型结果 |
| `GetResult<T>()` | 获取强类型结果（返回 `T?`） |
| `HasResult` | 检查是否有结果 |
| `Success` | 操作是否成功（bool） |
| `SetFailed(string reason)` | 标记失败并记录原因 |

### 为什么不创建专用 Context 类？

`EventContext` 已经足够通用：
- ✅ 通过泛型支持任意返回类型
- ✅ 内置 `Success`/`FailReason` 字段
- ✅ 避免为每个事件创建新的 Context 类

**推荐做法**：优先使用 `EventContext`，只在需要额外字段时才继承。

---

## 高级特性

### 优先级订阅

```csharp
// 高优先级处理器先执行
bus.On("CombatEvent", HandleShield, priority: 10);  // 先执行
bus.On("CombatEvent", HandleArmor, priority: 5);    // 后执行
bus.On("CombatEvent", HandleDamage, priority: 0);   // 最后执行
```

**应用场景**:

- Buff 系统优先级叠加
- 伤害计算管道（护盾 → 护甲 → 血量）

---

### 一次性订阅（Once）

```csharp
// 只触发一次，自动解绑
GlobalEventBus.Global.Once(
    GameEventType.Global.GameStart,
    () => GD.Print("游戏首次启动")
);

GlobalEventBus.Global.Emit(GameEventType.Global.GameStart, default);  // 打印
GlobalEventBus.Global.Emit(GameEventType.Global.GameStart, default);  // 不打印
```

**应用场景**:

- 新手引导
- 一次性成就解锁

---

### 重入保护(自动防死循环)

`EventBus` 自动检测并阻止同类型事件的递归触发,防止事件死循环。

```csharp
// ❌ 这种情况会被自动阻止
public void OnHealRequest(GameEventType.Unit.HealRequestEventData evt)
{
    // 处理治疗逻辑...
    
    // 假设这里又触发了 HealRequest 事件(错误设计)
    _entity.Events.Emit(GameEventType.Unit.HealRequest, evt);  
    // ⚠️ 输出警告: "检测到事件重入,已阻止: [unit:heal_request]"
}
```

**重入保护机制**:

- 当事件正在执行时,同类型的事件不会再次触发
- 自动输出警告日志,帮助开发者发现设计问题
- **不同类型**的事件可以正常嵌套触发

**应用场景**:

- 防止 `HealRequest` → `HealApplied` → `HealRequest` 循环
- 防止 `Damaged` 事件触发器中意外再次触发 `Damaged`
- 保护系统稳定性,避免栈溢出

> [!WARNING]
> 如果看到重入警告,说明事件设计可能存在问题,应检查事件触发逻辑

---

## 最佳实践

### ✅ 推荐做法

```csharp
// 1. 事件名使用常量，不要硬编码字符串
✅ Events.Emit(GameEventType.Unit.Dead, new GameEventType.Unit.DeadEventData());
❌ Events.Emit("unit:dead", new GameEventType.Unit.DeadEventData());

// 2. 使用 readonly record struct 定义事件数据
✅ public readonly record struct DamagedEventData(float Amount);
❌ public record DamagedEventData(float Amount);  // class 会堆分配

// 3. _ExitTree 时必须清理事件
public override void _ExitTree()
{
    Events.Clear();  // 局部事件
    GlobalEventBus.Global.Off(...);  // 全局事件
}

// 4. 对象池复用时统一订阅位置
public void OnPoolAcquire()
{
    // 直接订阅即可（OnPoolRelease 已调用 Events.Clear()）
    Events.On<DeadEventData>(GameEventType.Unit.Dead, OnDied);
}

public void OnPoolRelease()
{
    Events.Clear();  // 归还对象池时清空所有事件订阅
}
```

---

### ❌ 常见误区

```csharp
// 1. 忘记解绑导致内存泄漏
❌ public override void _ExitTree()
{
    // 忘记清理事件
}

// 2. 重复订阅导致多次触发
❌ public override void _Ready()
{
    Events.On<DeadEventData>(GameEventType.Unit.Dead, OnDied);
    Events.On<DeadEventData>(GameEventType.Unit.Dead, OnDied);  // 重复!
}

// 3. 类型不匹配（会触发警告日志）
❌ bus.On<int>("HP", (int x) => {});
   bus.Emit<string>("HP", "bad");  // 警告: 类型不匹配

// 4. 在 _Process 中订阅/触发事件（GC 压力）
❌ public override void _Process(float delta)
{
    Events.On<T>(...);  // 每帧订阅，性能极差
}
```

---

## 性能优化

### 零 GC 事件数据

```csharp
// ✅ 栈分配 (零 GC)
public readonly record struct DamagedEventData(float Amount);

// 对比
// ❌ 堆分配 (产生 GC)
public record DamagedEventData(float Amount);  // class
```

### 避免热路径中的事件

```csharp
// ❌ 每帧触发事件 (性能差)
public override void _Process(float delta)
{
    Events.Emit("Tick", delta);  // 每秒 60 次
}

// ✅ 只在状态变化时触发
private float _lastHp = 100;
public void ModifyHealth(float amount)
{
    float newHp = _lastHp + amount;
    if (!Mathf.IsEqualApprox(newHp, _lastHp))
    {
        Events.Emit(...);  // 只在变化时触发
        _lastHp = newHp;
    }
}
```

---

## API 参考

### EventBus 核心方法

| 方法 | 说明 | 示例 |
| :--- | :--- | :--- |
| `On<T>(string, Action<T>, int)` | 订阅有参事件 | `bus.On<int>("HP", OnHpChanged)` |
| `On(string, Action, int)` | 订阅无参事件 | `bus.On("Start", OnStart)` |
| `Once<T>(string, Action<T>, int)` | 一次性订阅（有参） | `bus.Once<int>("Load", OnLoad)` |
| `Once(string, Action, int)` | 一次性订阅（无参） | `bus.Once("Init", OnInit)` |
| `Emit<T>(string, T)` | 触发有参事件 | `bus.Emit("HP", 100)` |
| `Emit(string)` | 触发无参事件 | `bus.Emit("Start")` |
| `Off<T>(string, Action<T>)` | 取消订阅（有参） | `bus.Off<int>("HP", OnHpChanged)` |
| `Off(string, Action)` | 取消订阅（无参） | `bus.Off("Start", OnStart)` |
| `Clear()` | 清空所有订阅 | `bus.Clear()` |
| `ClearEvent(string)` | 清空指定事件 | `bus.ClearEvent("HP")` |

---

## 调试技巧

### 启用日志

```csharp
// 在 EventBus 中调整日志级别
private static readonly Log _log = new Log("EventBus", LogLevel.Debug);
```

### 类型不匹配警告

当订阅类型与触发类型不匹配时，会输出警告日志：

```text
[WARN] EventBus: 事件 [HP] 类型不匹配: 订阅者需要 Action<Int32>, 但事件数据是 Action<String>
```

**解决方法**: 确保 `On<T>` 和 `Emit<T>` 的类型参数一致。

---

## 架构决策

详见 [EventBus架构设计.md](../../../Docs/框架/ECS/Event/EventBus架构设计.md)

**核心理念**:
- 为什么不使用 C# 原生 `event`
- 为什么移除 DynamicInvoke
- 三层事件总线架构
- 与现代游戏框架的对比
