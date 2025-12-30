# HitboxComponent - 攻击判定组件

## 📋 组件概述

### 职责定义

HitboxComponent 负责定义**攻击判定区域**：

- 定义伤害值和攻击属性
- 检测与 Hurtbox 的碰撞
- 支持单次/持续伤害模式
- 管理攻击者信息

### 设计理念

```
┌─────────────────────────────────────────────────────┐
│  HitboxComponent 的核心价值                          │
├─────────────────────────────────────────────────────┤
│  1. 职责单一 - 只负责"攻击判定"，不处理伤害计算    │
│  2. 数据传递 - 将攻击信息传递给 Hurtbox            │
│  3. 灵活配置 - 支持多种攻击模式（单次、持续）      │
│  4. 性能优化 - 使用碰撞层过滤，避免无效检测        │
└─────────────────────────────────────────────────────┘
```

---

## 💻 完整实现

```csharp
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 攻击判定组件 - 定义攻击区域和伤害属性
/// 继承 Area2D，通过碰撞检测触发伤害
/// </summary>
public partial class HitboxComponent : Area2D
{
    private static readonly Log _log = new Log("HitboxComponent");

    // ================= 配置 =================

    /// <summary>
    /// 基础伤害值
    /// </summary>
    [Export] public float Damage { get; set; } = 10;

    /// <summary>
    /// 击退力度
    /// </summary>
    [Export] public float Knockback { get; set; } = 0;

    /// <summary>
    /// 伤害类型
    /// </summary>
    [Export] public DamageType DamageType { get; set; } = DamageType.Physical;

    /// <summary>
    /// 是否只造成一次伤害（如子弹）
    /// </summary>
    [Export] public bool OnceOnly { get; set; } = false;

    /// <summary>
    /// 持续伤害间隔（秒）
    /// 0 表示每帧都造成伤害
    /// </summary>
    [Export] public float DamageInterval { get; set; } = 0;

    /// <summary>
    /// 是否在造成伤害后自动禁用
    /// </summary>
    [Export] public bool DisableAfterHit { get; set; } = false;

    // ================= 状态 =================

    /// <summary>
    /// 攻击者节点（通常是武器或实体）
    /// </summary>
    public Node Attacker { get; set; }

    /// <summary>
    /// 已命中的目标列表（用于单次伤害模式）
    /// </summary>
    private HashSet<Node> _hitTargets = new();

    /// <summary>
    /// 持续伤害计时器
    /// </summary>
    private Dictionary<Node, float> _damageTimers = new();

    /// <summary>
    /// 是否已禁用
    /// </summary>
    public bool IsDisabled { get; private set; } = false;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // 自动设置攻击者为父节点
        if (Attacker == null)
        {
            Attacker = GetParent();
        }

        // 监听碰撞事件
        AreaEntered += OnAreaEntered;
        AreaExited += OnAreaExited;

        _log.Debug($"HitboxComponent Ready: Damage={Damage}, OnceOnly={OnceOnly}");
    }

    public override void _Process(double delta)
    {
        if (IsDisabled) return;

        // 更新持续伤害计时器
        if (DamageInterval > 0)
        {
            UpdateDamageTimers((float)delta);
        }
    }

    // ================= 碰撞处理 =================

    /// <summary>
    /// 当进入 Hurtbox 时触发
    /// </summary>
    private void OnAreaEntered(Area2D area)
    {
        if (IsDisabled) return;

        // 检查是否是 Hurtbox
        if (area is not HurtboxComponent hurtbox) return;

        // 检查是否已命中（单次伤害模式）
        if (OnceOnly && _hitTargets.Contains(hurtbox.Owner)) return;

        // 造成伤害
        ApplyDamage(hurtbox);

        // 记录命中目标
        if (OnceOnly)
        {
            _hitTargets.Add(hurtbox.Owner);

            // 如果设置了自动禁用
            if (DisableAfterHit)
            {
                Disable();
            }
        }
        else if (DamageInterval > 0)
        {
            // 初始化持续伤害计时器
            _damageTimers[hurtbox.Owner] = 0;
        }
    }

    /// <summary>
    /// 当离开 Hurtbox 时触发
    /// </summary>
    private void OnAreaExited(Area2D area)
    {
        if (area is not HurtboxComponent hurtbox) return;

        // 移除持续伤害计时器
        _damageTimers.Remove(hurtbox.Owner);
    }

    /// <summary>
    /// 应用伤害到 Hurtbox
    /// </summary>
    private void ApplyDamage(HurtboxComponent hurtbox)
    {
        // 构建伤害数据
        var damageData = new DamageData
        {
            Damage = Damage,
            DamageType = DamageType,
            Knockback = Knockback,
            KnockbackDirection = (hurtbox.GlobalPosition - GlobalPosition).Normalized(),
            Attacker = Attacker
        };

        // 传递给 Hurtbox
        hurtbox.TakeDamage(damageData);

        _log.Trace($"造成伤害: {Damage} -> {hurtbox.Owner.Name}");
    }

    /// <summary>
    /// 更新持续伤害计时器
    /// </summary>
    private void UpdateDamageTimers(float delta)
    {
        var overlappingAreas = GetOverlappingAreas();

        foreach (var area in overlappingAreas)
        {
            if (area is not HurtboxComponent hurtbox) continue;

            if (!_damageTimers.ContainsKey(hurtbox.Owner))
            {
                _damageTimers[hurtbox.Owner] = 0;
            }

            _damageTimers[hurtbox.Owner] += delta;

            // 达到伤害间隔，造成伤害
            if (_damageTimers[hurtbox.Owner] >= DamageInterval)
            {
                ApplyDamage(hurtbox);
                _damageTimers[hurtbox.Owner] = 0;
            }
        }
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 禁用 Hitbox（停止造成伤害）
    /// </summary>
    public void Disable()
    {
        IsDisabled = true;
        Monitoring = false;
        _log.Debug("Hitbox 已禁用");
    }

    /// <summary>
    /// 启用 Hitbox
    /// </summary>
    public void Enable()
    {
        IsDisabled = false;
        Monitoring = true;
        _log.Debug("Hitbox 已启用");
    }

    /// <summary>
    /// 重置命中记录（用于对象池复用）
    /// </summary>
    public void Reset()
    {
        _hitTargets.Clear();
        _damageTimers.Clear();
        IsDisabled = false;
        Monitoring = true;
    }

    /// <summary>
    /// 设置伤害值
    /// </summary>
    public void SetDamage(float damage)
    {
        Damage = damage;
    }

    /// <summary>
    /// 设置攻击者
    /// </summary>
    public void SetAttacker(Node attacker)
    {
        Attacker = attacker;
    }

    public override void _ExitTree()
    {
        AreaEntered -= OnAreaEntered;
        AreaExited -= OnAreaExited;
    }
}

// ================= 数据结构 =================

/// <summary>
/// 伤害数据（传递给 Hurtbox）
/// </summary>
public struct DamageData
{
    public float Damage;
    public DamageType DamageType;
    public float Knockback;
    public Vector2 KnockbackDirection;
    public Node Attacker;
}

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Physical,    // 物理伤害
    Fire,        // 火焰伤害
    Ice,         // 冰霜伤害
    Lightning,   // 雷电伤害
    Poison,      // 毒素伤害
    True         // 真实伤害
}
```

---

## 📖 使用示例

### 示例 1：子弹 Hitbox（单次伤害）

```csharp
// Bullet.cs
public partial class Bullet : CharacterBody2D
{
    private HitboxComponent _hitbox;

    public override void _Ready()
    {
        _hitbox = GetNode<HitboxComponent>("Hitbox");

        // 配置为单次伤害
        _hitbox.OnceOnly = true;
        _hitbox.DisableAfterHit = true;
        _hitbox.Damage = 25;
    }
}
```

**场景树结构**：

```
Bullet (CharacterBody2D)
├── Hitbox (HitboxComponent/Area2D)
│   └── CollisionShape2D
└── Sprite2D
```

---

### 示例 2：近战武器 Hitbox（持续伤害）

```csharp
// Sword.cs
public partial class Sword : Node2D
{
    private HitboxComponent _hitbox;

    public override void _Ready()
    {
        _hitbox = GetNode<HitboxComponent>("Hitbox");

        // 配置为持续伤害（每 0.5 秒造成一次）
        _hitbox.OnceOnly = false;
        _hitbox.DamageInterval = 0.5f;
        _hitbox.Damage = 15;

        // 初始禁用
        _hitbox.Disable();
    }

    public void Attack()
    {
        // 攻击时启用 Hitbox
        _hitbox.Enable();

        // 攻击动画结束后禁用
        GetTree().CreateTimer(0.3f).Timeout += () =>
        {
            _hitbox.Disable();
        };
    }
}
```

---

### 示例 3：AOE 技能 Hitbox

```csharp
// Explosion.cs
public partial class Explosion : Node2D
{
    [Export] public float Damage { get; set; } = 50;
    [Export] public float Radius { get; set; } = 200;

    private HitboxComponent _hitbox;

    public override void _Ready()
    {
        _hitbox = GetNode<HitboxComponent>("Hitbox");

        // 配置 Hitbox
        _hitbox.Damage = Damage;
        _hitbox.OnceOnly = true;  // 每个敌人只受伤一次

        // 设置碰撞形状大小
        var shape = _hitbox.GetNode<CollisionShape2D>("CollisionShape2D");
        if (shape.Shape is CircleShape2D circle)
        {
            circle.Radius = Radius;
        }

        // 0.1 秒后自动销毁
        GetTree().CreateTimer(0.1f).Timeout += () =>
        {
            QueueFree();
        };
    }
}
```

---

### 示例 4：动态调整伤害

```csharp
// WeaponComponent.cs
public partial class WeaponComponent : Node2D
{
    private HitboxComponent _hitbox;

    public void ApplyDamageBonus(float multiplier)
    {
        // 从 Data 容器读取基础伤害
        float baseDamage = GetParent().GetData().Get<float>("Damage", 10f);

        // 应用加成
        _hitbox.SetDamage(baseDamage * multiplier);
    }
}
```

---

## 🎯 设计要点

### 1. Hitbox vs Hurtbox 职责划分

```
┌─────────────────────────────────────────────────────┐
│  Hitbox（攻击判定）                                  │
│  - 定义伤害值和攻击属性                              │
│  - 检测碰撞                                          │
│  - 传递伤害数据                                      │
└─────────────────────────────────────────────────────┘
                        ↓ DamageData
┌─────────────────────────────────────────────────────┐
│  Hurtbox（受击判定）                                 │
│  - 接收伤害数据                                      │
│  - 调用 DamageCalculationService 计算最终伤害       │
│  - 应用到 HealthComponent                           │
└─────────────────────────────────────────────────────┘
```

**为什么这样设计？**

- ✅ 职责清晰：Hitbox 不关心伤害计算
- ✅ 易于扩展：可以在 Hurtbox 中添加护甲、抗性等
- ✅ 性能优化：伤害计算只在 Hurtbox 中进行一次

---

### 2. 单次 vs 持续伤害

| 模式     | OnceOnly | DamageInterval | 适用场景           |
| :------- | :------- | :------------- | :----------------- |
| 单次伤害 | `true`   | 任意           | 子弹、投射物       |
| 持续伤害 | `false`  | `> 0`          | 近战武器、毒云     |
| 每帧伤害 | `false`  | `0`            | 激光、火焰（慎用） |

**性能考虑**：

- 每帧伤害会频繁触发事件，影响性能
- 推荐使用 `DamageInterval >= 0.1` 秒

---

### 3. 碰撞层配置

```gdscript
# project.godot
[layer_names]

2d_physics/layer_1="Player"
2d_physics/layer_2="Enemy"
2d_physics/layer_3="PlayerWeapon"
2d_physics/layer_4="EnemyWeapon"
```

**配置示例**：

```csharp
// 玩家武器的 Hitbox
CollisionLayer = 0;  // 不属于任何层
CollisionMask = 1 << 1;  // 只检测 Enemy 层（Layer 2）

// 敌人武器的 Hitbox
CollisionLayer = 0;
CollisionMask = 1 << 0;  // 只检测 Player 层（Layer 1）
```

---

### 4. 对象池复用

```csharp
// Bullet.cs (实现 IPoolable)
public partial class Bullet : CharacterBody2D, IPoolable
{
    private HitboxComponent _hitbox;

    public void OnPoolAcquire()
    {
        // 重置 Hitbox 状态
        _hitbox.Reset();
        _hitbox.Enable();
    }

    public void OnPoolRelease()
    {
        _hitbox.Disable();
    }
}
```

---

## 🔧 扩展建议

### 1. 添加穿透效果

```csharp
[Export] public int MaxPenetration { get; set; } = 0;  // 0 = 不穿透
private int _penetrationCount = 0;

private void OnAreaEntered(Area2D area)
{
    if (area is not HurtboxComponent hurtbox) return;

    ApplyDamage(hurtbox);
    _penetrationCount++;

    // 达到穿透上限，禁用
    if (_penetrationCount >= MaxPenetration)
    {
        Disable();
    }
}
```

---

### 2. 添加暴击支持

```csharp
public bool CanCrit { get; set; } = true;

private void ApplyDamage(HurtboxComponent hurtbox)
{
    var damageData = new DamageData
    {
        Damage = Damage,
        CanCrit = CanCrit,  // 传递暴击标志
        Attacker = Attacker
    };

    hurtbox.TakeDamage(damageData);
}
```

---

### 3. 添加命中特效

```csharp
[Export] public string HitEffectName { get; set; } = "Hit";

private void ApplyDamage(HurtboxComponent hurtbox)
{
    // ... 伤害逻辑

    // 播放命中特效
    EffectService.Instance.PlayEffect(HitEffectName, hurtbox.GlobalPosition);
}
```

---

### 4. 添加伤害衰减

```csharp
[Export] public float DamageFalloff { get; set; } = 0;  // 每次命中减少的伤害百分比

private void ApplyDamage(HurtboxComponent hurtbox)
{
    var damageData = new DamageData
    {
        Damage = Damage,
        // ...
    };

    hurtbox.TakeDamage(damageData);

    // 衰减伤害
    if (DamageFalloff > 0)
    {
        Damage *= (1 - DamageFalloff);
    }
}
```

---

## 📊 性能优化

### 1. 使用碰撞层过滤

```csharp
// ✅ 只检测敌人层
CollisionMask = 1 << 1;  // Layer 2

// ❌ 检测所有层（性能差）
CollisionMask = 0xFFFFFFFF;
```

---

### 2. 禁用不必要的 Hitbox

```csharp
// 武器未攻击时禁用
public void OnAttackEnd()
{
    _hitbox.Disable();
}

// 攻击时启用
public void OnAttackStart()
{
    _hitbox.Enable();
}
```

---

### 3. 使用对象池

```csharp
// 子弹回收时自动禁用 Hitbox
public void OnPoolRelease()
{
    _hitbox.Disable();
}
```

---

## 🐛 常见问题

### Q1: 为什么子弹穿透了敌人？

**A**: 检查 `OnceOnly` 和碰撞层配置：

```csharp
_hitbox.OnceOnly = true;  // 确保单次伤害
_hitbox.CollisionMask = 1 << 1;  // 确保检测敌人层
```

---

### Q2: 为什么持续伤害触发太频繁？

**A**: 设置合理的 `DamageInterval`：

```csharp
_hitbox.DamageInterval = 0.5f;  // 每 0.5 秒一次
```

---

### Q3: 为什么 Hitbox 无法造成伤害？

**A**: 检查以下几点：

1. Hitbox 是否启用：`_hitbox.Monitoring == true`
2. 碰撞层是否正确：`CollisionMask` 包含目标层
3. Hurtbox 是否存在：目标有 `HurtboxComponent`

---

## 📝 相关文档

- [Component 系统设计](./00_Component系统设计.md)
- [HurtboxComponent - 受击判定组件](./04_HurtboxComponent.md)
- [HealthComponent - 生命值组件](./02_HealthComponent.md)
- [DamageCalculationService - 伤害计算服务](../System/04_DamageCalculationService.md)

---

**文档版本**: v1.0  
**最后更新**: 2025-12-25  
**作者**: 架构设计团队
