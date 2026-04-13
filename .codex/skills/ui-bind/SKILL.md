---
name: ui-bind
description: 开发 UI 组件、将 UI 绑定到 Entity、实现响应式 HUD 时使用。适用于：血条/伤害数字/技能栏等 HUD 组件，UI 监听 Entity 状态变化，UIBase Bind 模式实现。触发关键词：UI组件、UIBase、Bind模式、血条、伤害数字、技能栏、HUD、响应式UI、OnBind。
---

# UI Bind 模式规范

## 核心原则
- **UI 不是 Component**：UI 是数据观察者，不挂载到 Entity 上
- **Bind 模式**：UI 通过 `Bind(entity)` 绑定到特定 Entity，监听其 `Entity.Events`
- **禁止全局事件过滤**：不能监听全局事件再判断"是不是我的 Entity"

## 标准 UIBase 实现

```csharp
public partial class HealthBarUI : UIBase
{
    private Label? _hpLabel;
    private ProgressBar? _hpBar;

    public override void _Ready()
    {
        _hpLabel = GetNode<Label>("HpLabel");
        _hpBar = GetNode<ProgressBar>("HpBar");
    }

    // ✅ Bind 后自动调用，在此订阅 Entity 事件
    protected override void OnBind()
    {
        // 订阅特定 Entity 的事件（不是全局事件！）
        _entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged, OnDataChanged);

        UpdateDisplay();  // 立即刷新初始状态
    }

    // ✅ Unbind 时自动调用（UIManager 管理，通常无需手动调用）
    protected override void OnUnbind()
    {
        // Events 由 UIBase 自动清理，通常留空
    }

    private void OnDataChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        if (evt.Key == DataKey.CurrentHp || evt.Key == DataKey.MaxHp)
            UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_entity == null) return;
        var current = _entity.Data.Get<float>(DataKey.CurrentHp);
        var max = _entity.Data.Get<float>(DataKey.MaxHp);
        if (_hpBar != null) _hpBar.Value = max > 0 ? current / max : 0;
        if (_hpLabel != null) _hpLabel.Text = $"{current:F0}/{max:F0}";
    }
}
```

## 使用 UIManager 管理 UI

```csharp
// UIManager 自动处理血条/伤害数字的对象池和绑定
// 无需手动 Spawn，Entity 注册时 UIManager 自动创建并绑定

// 手动绑定（特殊情况）
var healthBar = healthBarPool.Spawn();
healthBar.Bind(enemyEntity);   // 绑定到特定 Entity

// 解绑
healthBar.Unbind();
```

## 伤害数字 UI（对象池模式）

```csharp
public partial class DamageNumberUI : UIBase
{
    protected override void OnBind()
    {
        // 订阅受伤事件
        _entity.Events.On<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);
    }

    private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
    {
        ShowNumber(evt.Amount, evt.IsCrit);
        // 显示后自动归还对象池（通过 TimerManager 延迟）
        TimerManager.Instance.Delay(1.5f).OnComplete(() => Unbind());
    }
}
```

## 技能栏 UI（监听技能增删）

```csharp
protected override void OnBind()
{
    // 监听技能增删事件
    _entity.Events.On<GameEventType.Ability.AddedEventData>(
        GameEventType.Ability.Added, OnAbilityAdded);
    _entity.Events.On<GameEventType.Ability.RemovedEventData>(
        GameEventType.Ability.Removed, OnAbilityRemoved);

    // 初始化已有技能
    var abilities = EntityManager.GetAbilities(_entity as Node);
    foreach (var ability in abilities) AddSlot(ability);
}
```

## 禁止事项
- ❌ 将 UI 做成 Component 挂载到 Entity
- ❌ 监听 `GlobalEventBus` 全局事件然后判断"是不是我的 Entity"
- ❌ 在 UI 中直接读写 Entity.Data（应通过事件驱动更新）
- ❌ 在 `_Process` 中每帧轮询 Data（应事件驱动）

## 关键文件路径
- **核心基类** → `Src/ECS/UI/Core/UIBase.cs`
- **管理器** → `Src/ECS/UI/Core/UIManager.cs`
- **开发指南** → `Src/ECS/UI/README.md`
- **架构设计** → `Docs/框架/UI/UI架构设计理念.md`
- **血条示例** → `Src/ECS/UI/UI/HealthBarUI/HealthBarUI.cs`
- **伤害数字示例** → `Src/ECS/UI/UI/DamageNumberUI/DamageNumberUI.cs`
- **技能栏示例** → `Src/ECS/UI/UI/SkillUI/ActiveSkillBarUI.cs`
- **技能槽示例** → `Src/ECS/UI/UI/SkillUI/ActiveSkillSlotUI.cs`
