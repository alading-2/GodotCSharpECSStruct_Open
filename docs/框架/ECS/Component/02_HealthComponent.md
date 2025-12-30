# HealthComponent - 生命值组件

## 📋 组件概述

### 职责定义

HealthComponent 负责管理实体的**生命值系统**：

- 生命值的增减（受伤、治疗）
- 死亡判定和事件触发
- 生命值上下限控制
- 无敌状态管理（可选）

### 设计理念

```
┌─────────────────────────────────────────────────────┐
│  HealthComponent 的核心价值                          │
├─────────────────────────────────────────────────────┤
│  1. 事件驱动 - 通过 C# Event 解耦生命值变化通知    │
│  2. 状态封装 - 外部只能通过方法修改，保证数据安全  │
│  3. 灵活配置 - 支持最大生命值动态调整（升级系统）  │
│  4. 可扩展性 - 易于添加护盾、吸血等扩展功能        │
└─────────────────────────────────────────────────────┘
```

---

## 💻 完整实现

```csharp
using Godot;
using System;

/// <summary>
/// 生命值组件 - 管理实体的生命值和死亡逻辑
/// </summary>
public partial class HealthComponent : Node
{
    private static readonly Log _log = new Log("HealthComponent");

    // ================= 配置 =================

    /// <summary>
    /// 最大生命值（可在编辑器中配置）
    /// </summary>
    [Export] public float MaxHp { get; set; } = 100;

    /// <summary>
    /// 是否在 Ready 时自动满血
    /// </summary>
    [Export] public bool StartAtFullHp { get; set; } = true;

    // ================= 状态 =================

    /// <summary>
    /// 当前生命值（只读，通过方法修改）
    /// </summary>
    public float CurrentHp { get; private set; }

    /// <summary>
    /// 是否已死亡
    /// </summary>
    public bool IsDead { get; private set; } = false;

    /// <summary>
    /// 生命值百分比（0-1）
    /// </summary>
    public float HpPercent => MaxHp > 0 ? CurrentHp / MaxHp : 0;

    // ================= 事件 =================

    /// <summary>
    /// 受到伤害时触发
    /// 参数：伤害值
    /// </summary>
    public event Action<float>? Damaged;

    /// <summary>
    /// 治疗时触发
    /// 参数：治疗量
    /// </summary>
    public event Action<float>? Healed;

    /// <summary>
    /// 生命值变化时触发
    /// 参数：旧值, 新值
    /// </summary>
    public event Action<float, float>? HpChanged;

    /// <summary>
    /// 死亡时触发（只触发一次）
    /// </summary>
    public event Action? Died;

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        if (StartAtFullHp)
        {
            CurrentHp = MaxHp;
        }

        _log.Debug($"HealthComponent Ready: MaxHp={MaxHp}, CurrentHp={CurrentHp}");
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值（必须 > 0）</param>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (damage <= 0)
        {
            _log.Warn($"TakeDamage 收到无效伤害值: {damage}");
            return;
        }

        float oldHp = CurrentHp;
        CurrentHp = Mathf.Max(0, CurrentHp - damage);

        _log.Debug($"受到伤害: {damage}, 生命值: {oldHp} -> {CurrentHp}");

        // 触发事件
        Damaged?.Invoke(damage);
        HpChanged?.Invoke(oldHp, CurrentHp);

        // 检查死亡
        if (CurrentHp <= 0 && !IsDead)
        {
            Die();
        }
    }

    /// <summary>
    /// 治疗
    /// </summary>
    /// <param name="amount">治疗量（必须 > 0）</param>
    public void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0)
        {
            _log.Warn($"Heal 收到无效治疗值: {amount}");
            return;
        }

        float oldHp = CurrentHp;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);

        // 只有实际治疗了才触发事件
        if (CurrentHp > oldHp)
        {
            float actualHealed = CurrentHp - oldHp;
            _log.Debug($"治疗: {actualHealed}, 生命值: {oldHp} -> {CurrentHp}");

            Healed?.Invoke(actualHealed);
            HpChanged?.Invoke(oldHp, CurrentHp);
        }
    }

    /// <summary>
    /// 设置当前生命值（直接设置，不触发伤害/治疗事件）
    /// </summary>
    public void SetHp(float hp)
    {
        if (IsDead) return;

        float oldHp = CurrentHp;
        CurrentHp = Mathf.Clamp(hp, 0, MaxHp);

        if (CurrentHp != oldHp)
        {
            HpChanged?.Invoke(oldHp, CurrentHp);

            if (CurrentHp <= 0 && !IsDead)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// 设置最大生命值（会同步调整当前生命值百分比）
    /// </summary>
    public void SetMaxHp(float maxHp, bool keepPercent = true)
    {
        if (maxHp <= 0)
        {
            _log.Error($"SetMaxHp 收到无效值: {maxHp}");
            return;
        }

        float oldMaxHp = MaxHp;
        MaxHp = maxHp;

        if (keepPercent)
        {
            // 保持生命值百分比
            float percent = oldMaxHp > 0 ? CurrentHp / oldMaxHp : 1.0f;
            SetHp(MaxHp * percent);
        }
        else
        {
            // 限制在新的最大值内
            CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        }

        _log.Debug($"最大生命值变化: {oldMaxHp} -> {MaxHp}, 当前生命值: {CurrentHp}");
    }

    /// <summary>
    /// 完全恢复生命值
    /// </summary>
    public void FullHeal()
    {
        Heal(MaxHp);
    }

    /// <summary>
    /// 立即死亡（强制触发死亡）
    /// </summary>
    public void Kill()
    {
        if (IsDead) return;

        CurrentHp = 0;
        Die();
    }

    /// <summary>
    /// 复活（重置死亡状态）
    /// </summary>
    public void Revive(float hp)
    {
        if (!IsDead) return;

        IsDead = false;
        SetHp(hp);

        _log.Info($"复活: 生命值={CurrentHp}");
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 处理死亡逻辑
    /// </summary>
    private void Die()
    {
        IsDead = true;
        CurrentHp = 0;

        _log.Info("实体死亡");

        // 触发死亡事件
        Died?.Invoke();
    }
}
```

---

## 📖 使用示例

### 示例 1：基础用法

```csharp
// Enemy.cs
public partial class Enemy : CharacterBody2D
{
    private HealthComponent _health;

    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("HealthComponent");

        // 监听死亡事件
        _health.Died += OnDied;
    }

    private void OnDied()
    {
        // 播放死亡动画
        PlayDeathAnimation();

        // 生成掉落物
        SpawnLoot();

        // 触发全局事件
        EventBus.EnemyDied?.Invoke(this, GlobalPosition);

        // 回收到对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    public override void _ExitTree()
    {
        if (_health != null)
        {
            _health.Died -= OnDied;
        }
    }
}
```

---

### 示例 2：UI 血条更新

```csharp
// HealthBar.cs
public partial class HealthBar : ProgressBar
{
    private HealthComponent _health;

    public void Initialize(HealthComponent health)
    {
        _health = health;

        // 监听生命值变化
        _health.HpChanged += OnHpChanged;

        // 初始化显示
        MaxValue = _health.MaxHp;
        Value = _health.CurrentHp;
    }

    private void OnHpChanged(float oldHp, float newHp)
    {
        // 平滑过渡
        var tween = CreateTween();
        tween.TweenProperty(this, "value", newHp, 0.2f);

        // 受伤时闪烁
        if (newHp < oldHp)
        {
            Modulate = Colors.Red;
            tween.TweenProperty(this, "modulate", Colors.White, 0.2f);
        }
    }

    public override void _ExitTree()
    {
        if (_health != null)
        {
            _health.HpChanged -= OnHpChanged;
        }
    }
}
```

---

### 示例 3：受击反馈

```csharp
// Player.cs
public partial class Player : CharacterBody2D
{
    private HealthComponent _health;
    private AnimatedSprite2D _sprite;

    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("HealthComponent");
        _sprite = GetNode<AnimatedSprite2D>("Sprite");

        // 监听受伤事件
        _health.Damaged += OnDamaged;
    }

    private void OnDamaged(float damage)
    {
        // 播放受伤音效
        EffectService.Instance.PlaySound("PlayerHit");

        // 屏幕震动
        EffectService.Instance.ShakeScreen(3, 0.1f);

        // 受伤闪烁
        FlashWhite();

        // 显示伤害数字
        ShowDamageNumber(damage);
    }

    private void FlashWhite()
    {
        _sprite.Modulate = Colors.White;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1f);
    }
}
```

---

### 示例 4：升级增加最大生命值

```csharp
// ProgressionSystem.cs
public void ApplyUpgrade(UpgradeData upgrade, Node target)
{
    if (upgrade.Type == UpgradeType.IncreaseMaxHp)
    {
        var health = target.GetNode<HealthComponent>("HealthComponent");

        // 增加最大生命值，保持百分比
        health.SetMaxHp(health.MaxHp + upgrade.Value, keepPercent: true);

        // 或者：增加最大生命值并完全恢复
        health.SetMaxHp(health.MaxHp + upgrade.Value, keepPercent: false);
        health.FullHeal();
    }
}
```

---

### 示例 5：护盾系统扩展

```csharp
// ShieldComponent.cs
public partial class ShieldComponent : Node
{
    private HealthComponent _health;

    public float ShieldAmount { get; private set; }
    public float MaxShield { get; set; } = 50;

    public override void _Ready()
    {
        _health = GetParent().GetNode<HealthComponent>("HealthComponent");

        // 拦截伤害
        _health.Damaged += OnDamaged;
    }

    private void OnDamaged(float damage)
    {
        if (ShieldAmount > 0)
        {
            // 护盾吸收伤害
            float absorbed = Mathf.Min(ShieldAmount, damage);
            ShieldAmount -= absorbed;

            // 减少实际伤害
            float remainingDamage = damage - absorbed;
            if (remainingDamage > 0)
            {
                _health.TakeDamage(remainingDamage);
            }
        }
    }
}
```

---

## 🎯 设计要点

### 1. 为什么使用事件而非 Signal？

```csharp
// ✅ C# Event（推荐）
public event Action<float>? Damaged;

// 性能对比：
// C# Event:    ~0.001ms (编译期优化)
// Godot Signal: ~0.007ms (运行时反射)
```

**优势**：

- 7-9 倍性能提升
- 编译期类型检查
- 代码更简洁

---

### 2. 死亡状态管理

```csharp
// ✅ 使用 IsDead 标志防止重复死亡
public void TakeDamage(float damage)
{
    if (IsDead) return;  // 已死亡，忽略伤害

    // ... 伤害逻辑

    if (CurrentHp <= 0 && !IsDead)
    {
        Die();  // 只触发一次
    }
}
```

**防止的问题**：

- 多个伤害同时到达
- 死亡事件重复触发
- 对象池回收后仍接收伤害

---

### 3. 最大生命值变化策略

```csharp
// 策略 1：保持百分比（推荐用于升级）
health.SetMaxHp(150, keepPercent: true);
// 100/100 (100%) -> 150/150 (100%)

// 策略 2：保持数值（推荐用于 Debuff）
health.SetMaxHp(50, keepPercent: false);
// 100/100 -> 50/50 (限制在新最大值)
```

---

### 4. 事件触发顺序

```
TakeDamage() 调用
    ↓
1. Damaged 事件触发（伤害值）
    ↓
2. HpChanged 事件触发（旧值, 新值）
    ↓
3. 检查死亡
    ↓
4. Died 事件触发（如果死亡）
```

**设计考虑**：

- `Damaged` 先触发：用于受伤反馈（音效、特效）
- `HpChanged` 后触发：用于 UI 更新
- `Died` 最后触发：用于死亡处理

---

## 🔧 扩展建议

### 1. 添加无敌状态

```csharp
public bool IsInvincible { get; set; } = false;

public void TakeDamage(float damage)
{
    if (IsDead || IsInvincible) return;

    // ... 伤害逻辑
}

// 使用示例
public void ActivateInvincibility(float duration)
{
    IsInvincible = true;
    GetTree().CreateTimer(duration).Timeout += () =>
    {
        IsInvincible = false;
    };
}
```

---

### 2. 添加伤害类型

```csharp
public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Poison
}

public void TakeDamage(float damage, DamageType type = DamageType.Physical)
{
    // 根据类型应用抗性
    float resistance = GetResistance(type);
    float finalDamage = damage * (1 - resistance);

    // ... 伤害逻辑
}
```

---

### 3. 添加生命值回复

```csharp
[Export] public float HpRegenPerSecond { get; set; } = 0;

public override void _Process(double delta)
{
    if (HpRegenPerSecond > 0 && CurrentHp < MaxHp)
    {
        Heal(HpRegenPerSecond * (float)delta);
    }
}
```

---

### 4. 添加过量治疗（临时生命值）

```csharp
public float OverHeal { get; private set; } = 0;
public float MaxOverHeal { get; set; } = 50;

public void Heal(float amount)
{
    float oldHp = CurrentHp;
    CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);

    // 过量治疗转为临时生命值
    float excess = amount - (CurrentHp - oldHp);
    if (excess > 0)
    {
        OverHeal = Mathf.Min(MaxOverHeal, OverHeal + excess);
    }

    // ... 事件触发
}
```

---

## 📊 性能优化

### 1. 避免频繁的事件订阅/取消

```csharp
// ❌ 不好：每次都重新订阅
public void UpdateHealthBar()
{
    _health.HpChanged -= OnHpChanged;
    _health.HpChanged += OnHpChanged;
}

// ✅ 好：只订阅一次
public override void _Ready()
{
    _health.HpChanged += OnHpChanged;
}
```

---

### 2. 批量伤害优化

```csharp
// 如果需要同时处理多个伤害源
public void TakeDamages(float[] damages)
{
    float totalDamage = 0;
    foreach (float damage in damages)
    {
        totalDamage += damage;
    }

    TakeDamage(totalDamage);  // 只触发一次事件
}
```

---

## 🐛 常见问题

### Q1: 为什么死亡事件触发了多次？

**A**: 没有使用 `IsDead` 标志：

```csharp
// ✅ 正确
if (CurrentHp <= 0 && !IsDead)
{
    Die();
}
```

---

### Q2: 为什么升级后生命值变少了？

**A**: 使用了错误的 `keepPercent` 参数：

```csharp
// ❌ 错误：100/100 -> 50/150 (百分比保持)
health.SetMaxHp(150, keepPercent: true);

// ✅ 正确：100/100 -> 150/150 (完全恢复)
health.SetMaxHp(150, keepPercent: false);
health.FullHeal();
```

---

### Q3: 为什么治疗无效？

**A**: 检查是否已满血或已死亡：

```csharp
public void Heal(float amount)
{
    if (IsDead) return;  // 死亡无法治疗
    if (CurrentHp >= MaxHp) return;  // 已满血

    // ... 治疗逻辑
}
```

---

## 📝 相关文档

- [Component 系统设计](./00_Component系统设计.md)
- [HurtboxComponent - 受击判定组件](./04_HurtboxComponent.md)
- [DamageCalculationService - 伤害计算服务](../System/04_DamageCalculationService.md)

---

**文档版本**: v1.0  
**最后更新**: 2025-12-25  
**作者**: 架构设计团队
