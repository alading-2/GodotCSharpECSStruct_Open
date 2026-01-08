# VelocityComponent - 移动组件

## 📋 组件概述

### 职责定义

VelocityComponent 负责管理实体的**物理移动**：

- 从 InputManager 获取输入并转换为移动
- 平滑的加速和减速效果
- 速度限制和碰撞处理
- 与 CharacterBody2D 的物理引擎集成

### 设计理念

```
┌─────────────────────────────────────────────────────┐
│  VelocityComponent 的核心价值                        │
├─────────────────────────────────────────────────────┤
│  1. 平滑移动 - 指数插值实现自然的加速/减速          │
│  2. Data 驱动 - 所有参数可运行时调整（Buff/Debuff）│
│  3. 自动化 - 无需手动调用，自动处理输入和物理      │
│  4. 性能优化 - 缓存引用，避免频繁查询               │
└─────────────────────────────────────────────────────┘
```

---

## 🏗️ 技术实现

### 核心算法：指数插值平滑移动

```csharp
// 计算目标速度
Vector2 targetVelocity = inputDir.Normalized() * Speed;

// 指数插值（比线性插值更自然）
Velocity = Velocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-Acceleration * delta));
```

**为什么使用指数插值？**

| 方法     | 公式                              | 特点               |
| :------- | :-------------------------------- | :----------------- |
| 线性插值 | `Lerp(a, b, t * delta)`           | 速度恒定，不够自然 |
| 指数插值 | `Lerp(a, b, 1 - Exp(-k * delta))` | 快速启动，平滑减速 |

**效果对比**：

```
线性插值：  ————————————————————  (匀速)
指数插值：  ━━━━━━━━━━━━━━━━━━  (快→慢，更自然)
```

---

## 💻 完整实现分析

### 1. 数据驱动设计

```csharp
// 从父节点的 Data 容器读取配置
public float Speed => _data.Get<float>("Speed", 400f);
public float MaxSpeed => _data.Get<float>("MaxSpeed", 1000f);
public float Acceleration => _data.Get<float>("Acceleration", 10.0f);
```

**优势**：

- ✅ 运行时可调整（Buff 系统）
- ✅ 无需重新编译
- ✅ 支持配置热更新

**使用示例**：

```csharp
// 玩家获得加速 Buff
player.GetData().Multiply("Speed", 1.5f);  // 速度提升 50%

// Buff 结束后恢复
player.GetData().Set("Speed", 400f);
```

---

### 2. 自动化输入处理

```csharp
public override void _Process(double delta)
{
    // 自动获取输入
    Vector2 inputDir = InputManager.GetMoveInput();

    // 自动计算和应用移动
    Vector2 targetVelocity = inputDir.Normalized() * Speed;
    Velocity = Velocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-Acceleration * delta));

    // 自动应用到物理引擎
    _parent.Velocity = Velocity;
    _parent.MoveAndSlide();
}
```

**设计考虑**：

- ✅ Entity 无需手动调用移动逻辑
- ✅ 组件自治，降低耦合
- ✅ 符合 ECS 的 System 自动更新理念

---

### 3. 速度限制机制

```csharp
private void ClampVelocity()
{
    if (Velocity.Length() > MaxSpeed)
    {
        Velocity = Velocity.Normalized() * MaxSpeed;
    }
}
```

**应用场景**：

- 正常移动：`Speed = 400`
- 冲刺技能：临时提升到 `800`
- 击退效果：瞬间达到 `1200`
- 最大限制：`MaxSpeed = 1000` 防止速度失控

---

### 4. 物理引擎集成

```csharp
// 应用到 CharacterBody2D
_parent.Velocity = Velocity;
_parent.MoveAndSlide();

// 同步回组件（物理引擎可能修改速度）
Velocity = _parent.Velocity;
```

**为什么需要同步？**

- CharacterBody2D 的 `MoveAndSlide()` 会处理碰撞
- 碰撞后速度可能被修改（如撞墙后速度归零）
- 同步确保组件状态与物理引擎一致

---

## 📖 使用示例

### 示例 1：基础用法（自动移动）

```csharp
// Player.cs
public partial class Player : CharacterBody2D
{
    public override void _Ready()
    {
        // 组件会自动处理移动，无需手动调用
        // 只需确保 VelocityComponent 挂载在场景树中
    }
}
```

**场景树结构**：

```
Player (CharacterBody2D)
├── VelocityComponent
├── HealthComponent
└── Sprite2D
```

---

### 示例 2：配置移动参数

```csharp
// 在 _Ready 中配置
public override void _Ready()
{
    var data = this.GetData();
    data.Set("Speed", 500f);        // 基础速度
    data.Set("MaxSpeed", 1200f);    // 最大速度
    data.Set("Acceleration", 15f);  // 加速度
}
```

---

### 示例 3：冲刺技能

```csharp
public partial class Player : CharacterBody2D
{
    private VelocityComponent _velocity;

    public override void _Ready()
    {
        _velocity = GetNode<VelocityComponent>("VelocityComponent");
    }

    private void OnDashInput()
    {
        // 方式 1：临时提升速度
        this.GetData().Set("Speed", 800f);

        GetTree().CreateTimer(0.3f).Timeout += () =>
        {
            this.GetData().Set("Speed", 400f);  // 恢复
        };

        // 方式 2：直接设置速度向量
        Vector2 dashDir = InputManager.GetMoveInput().Normalized();
        _velocity.SetVelocity(dashDir * 1000f);
    }
}
```

---

### 示例 4：击退效果

```csharp
public partial class Enemy : CharacterBody2D
{
    private VelocityComponent _velocity;

    private void OnHit(Vector2 knockbackDir)
    {
        // 应用击退
        _velocity.SetVelocity(knockbackDir * 500f);

        // 0.2 秒后恢复正常移动
        GetTree().CreateTimer(0.2f).Timeout += () =>
        {
            _velocity.Stop();
        };
    }
}
```

---

### 示例 5：冰冻效果（减速 Buff）

```csharp
public void ApplySlowEffect(Node target, float duration)
{
    var data = target.GetData();

    // 保存原始速度
    float originalSpeed = data.Get<float>("Speed");

    // 减速 50%
    data.Multiply("Speed", 0.5f);

    // 持续时间后恢复
    GetTree().CreateTimer(duration).Timeout += () =>
    {
        data.Set("Speed", originalSpeed);
    };
}
```

---

## 🎯 设计要点

### 1. 为什么不在 \_PhysicsProcess 中处理？

```csharp
// ❌ 不推荐：在 _PhysicsProcess 中
public override void _PhysicsProcess(double delta)
{
    // 物理帧率固定（60 FPS），可能导致输入延迟
}

// ✅ 推荐：在 _Process 中
public override void _Process(double delta)
{
    // 渲染帧率更高，输入响应更快
    // CharacterBody2D.MoveAndSlide() 会自动同步到物理引擎
}
```

**原因**：

- Roguelike 游戏需要快速响应
- `_Process` 帧率更高（通常 > 60 FPS）
- `MoveAndSlide()` 会自动处理物理同步

---

### 2. 加速度参数调优

| Acceleration | 效果               | 适用场景       |
| :----------- | :----------------- | :------------- |
| 5.0          | 缓慢加速，滑冰感   | 冰面、太空     |
| 10.0         | 正常加速（推荐）   | 大多数角色     |
| 20.0         | 快速加速，响应灵敏 | 竞技游戏、Boss |
| 50.0         | 瞬间达到目标速度   | 测试、特殊效果 |

**调试技巧**：

```csharp
// 在编辑器中实时调整
[Export] public float DebugAcceleration = 10.0f;

public override void _Ready()
{
    this.GetData().Set("Acceleration", DebugAcceleration);
}
```

---

### 3. 性能优化

```csharp
// ✅ 缓存父节点引用
private CharacterBody2D _parent;

public override void _Ready()
{
    _parent = GetParent<CharacterBody2D>();  // 只查找一次
}

// ✅ 缓存 Data 引用
private Data _data;

public override void _Ready()
{
    _data = GetParent().GetData();  // 只查找一次
}

// ✅ 使用属性访问 Data（避免每帧查询）
public float Speed => _data.Get<float>("Speed", 400f);
```

---

## 🔧 扩展建议

### 1. 添加移动状态

```csharp
public enum MoveState
{
    Normal,   // 正常移动
    Dashing,  // 冲刺中
    Stunned,  // 眩晕（无法移动）
    Frozen    // 冰冻（减速）
}

private MoveState _state = MoveState.Normal;

public override void _Process(double delta)
{
    if (_state == MoveState.Stunned)
    {
        Velocity = Vector2.Zero;
        return;
    }

    // ... 正常移动逻辑
}
```

---

### 2. 添加移动事件

```csharp
public event Action? StartedMoving;
public event Action? StoppedMoving;

private bool _wasMoving = false;

public override void _Process(double delta)
{
    // ... 移动逻辑

    bool isMoving = Velocity.Length() > 1.0f;

    if (isMoving && !_wasMoving)
    {
        StartedMoving?.Invoke();
    }
    else if (!isMoving && _wasMoving)
    {
        StoppedMoving?.Invoke();
    }

    _wasMoving = isMoving;
}
```

---

### 3. 添加移动方向锁定

```csharp
public bool IsDirectionLocked { get; set; } = false;
private Vector2 _lockedDirection;

public void LockDirection(Vector2 direction)
{
    IsDirectionLocked = true;
    _lockedDirection = direction.Normalized();
}

public override void _Process(double delta)
{
    Vector2 inputDir = IsDirectionLocked
        ? _lockedDirection
        : InputManager.GetMoveInput();

    // ... 移动逻辑
}
```

**应用场景**：

- 冲刺技能（锁定冲刺方向）
- 击退效果（强制移动方向）
- 过场动画（自动移动）

---

## 📊 性能指标

### 目标性能

- 单个组件 CPU 占用 < 0.01ms
- 支持 500+ 实体同时移动
- 无 GC 分配（零垃圾回收）

### 性能测试

```csharp
[Test]
public void PerformanceTest_500Entities()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    for (int i = 0; i < 500; i++)
    {
        var entity = CreateEntityWithVelocity();
        entity._Process(0.016);  // 模拟一帧
    }

    stopwatch.Stop();
    Assert.Less(stopwatch.ElapsedMilliseconds, 5);  // 应在 5ms 内完成
}
```

---

## 🐛 常见问题

### Q1: 为什么移动感觉"滑"？

**A**: 加速度太低，增加 `Acceleration` 值：

```csharp
data.Set("Acceleration", 20.0f);  // 从 10 增加到 20
```

---

### Q2: 为什么冲刺后速度不恢复？

**A**: 确保在冲刺结束后重置速度：

```csharp
// ❌ 错误：只设置了 Velocity，没有重置 Speed
_velocity.SetVelocity(dashDir * 1000f);

// ✅ 正确：冲刺结束后恢复 Speed
GetTree().CreateTimer(0.3f).Timeout += () =>
{
    this.GetData().Set("Speed", 400f);
};
```

---

### Q3: 为什么撞墙后会"粘"在墙上？

**A**: 这是 `MoveAndSlide()` 的正常行为，如果需要"滑墙"效果：

```csharp
// 在 CharacterBody2D 中设置
MotionMode = MotionModeEnum.Floating;  // 或 Grounded
```

---

## 📝 相关文档

- [Component 系统设计](./00_Component系统设计.md)
- [HealthComponent - 生命值组件](./02_HealthComponent.md)
- [FollowComponent - 跟随组件](./05_FollowComponent.md)

---

**文档版本**: v1.0  
**最后更新**: 2025-12-25  
**作者**: 架构设计团队
