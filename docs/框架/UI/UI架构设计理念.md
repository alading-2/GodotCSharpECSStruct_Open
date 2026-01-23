# UI架构设计理念：响应式绑定 (Reactive Binding)

> [!NOTE]
> 本文档旨在解决ECS架构下的UI实现难题：如何让独立的UI系统（View）与数据驱动的实体（Model）高效解耦与同步。

## 1. 核心设计哲学

在我们的ECS框架中，UI **不应该** 是Entity的一个Component。

- **Entity (Model)**: 负责逻辑和数据（如 HP, 技能CD, 物品数据）。它是"无形"的逻辑载体。
- **UI (View)**: 负责呈现。它是Godot原生的 `Control` 节点树。

### ❌ 错误的思路
将血条做成 `HealthBarComponent` 挂载在 EnemyEntity 上。
- **问题**: 破坏了 Godot Scene 的层级结构（UI通常需要在一个 CanvasLayer 下统一管理，而不是跟着 物理层的 Enemy 跑）。
- **性能**: UI 实例化开销大，且通常不需要每帧 ECS 处理。

### ✅ 正确的思路：绑定 (Binding)
UI 是一个独立的系统（或对象池中的对象），它**持有** Entity 的引用，并**监听** Entity 的事件。

```mermaid
graph LR
    E[Entity] -- "1.修改数据" --> D[Data]
    D -- "2.触发局部事件" --> EV[Entity.Events]
    UI[UI Node] -- "3.收到通知" --> UI
    UI -.->| "4.读取数据" | D
    UI -.->| "0.Bind(Entity)" | E
```

---

## 2. 核心机制：UIEventBridge (事件桥接)

针对你的疑问：**"EnemyEntity生命值变化事件，怎么知道哪个UI是我的？"**

答案是：**不要监听全局事件，监听 Entity 的局部事件。**

### 2.1 绑定模式 (The Binding Pattern)

当一个 Enemy 生成时，UI Manager 分配一个血条，并执行 **Bind** 操作。

```csharp
// 血条 UI 类
public partial class HealthBarUI : Control, IPoolable
{
    private IEntity _targetEntity;

    // 🎯 核心方法：绑定实体
    public void Bind(IEntity entity)
    {
        _targetEntity = entity;

        // 关键点：订阅的是这个特定 Entity 的事件，而不是全局事件
        // 这样就不需要判断 "这是不是我的 Entity" —— 肯定是你绑定的那个！
        _targetEntity.Events.On<float>(GameEventType.Data.HealthChanged, OnHealthChanged);
        
        // 初始刷新
        UpdateDisplay();
    }

    private void OnHealthChanged(float newHp)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var maxHp = _targetEntity.Data.Get<float>(DataKey.MaxHp);
        var currentHp = _targetEntity.Data.Get<float>(DataKey.CurrentHp);
        _progressBar.Value = currentHp / maxHp * 100;
    }

    // 回收时解绑
    public void Reset()
    {
        _targetEntity = null;
        // 注意：实际项目中需要处理事件解绑，或者在 Entity 销毁时 EventBus 自动清理
    }
}
```

### 2.2 生命周期管理

UI 的生命周期通常比 Entity 稍微滞后一点（生成）和同步（销毁）。

1.  **Spawn**:
    *   `EntityManager` 生成 Enemy。
    *   `GlobalEventBus` 发送 `UnitSpawned` 事件。
    *   `UIManager` 收到 `UnitSpawned`，从对象池取出一个 `HealthBarUI`。
    *   调用 `HealthBarUI.Bind(enemy)`。

2.  **Update**:
    *   Enemy 受伤 -> `Data` 变动 -> `Entity.Events` 触发 `HealthChanged`。
    *   `HealthBarUI` 的回调被执行 -> 更新进度条。

3.  **Despawn**:
    *   Enemy 死亡 -> `GlobalEventBus` 发送 `UnitDespawned`。
    *   `UIManager` 收到事件，找到对应的 `HealthBarUI`。
    *   调用 `HealthBarUI.Unbind()` 并归还对象池。

---

## 3. 为什么这样设计？

| 特性 | 组件化 UI (不推荐) | 绑定式 UI (推荐) |
| :--- | :--- | :--- |
| **层级管理** | 困难 (UI跟着物理节点跑，会被遮挡) | 简单 (UI统一在 CanvasLayer/HUD 层) |
| **性能** | 差 (ECS Iterate 包含 UI) | 优 (UI 只有在事件触发时才通过回调更新) |
| **解耦** | 差 (逻辑与表现耦合) | 优 (Data只管存数据，UI只管显示) |
| **多视图** | 困难 | 简单 (一个 Entity 可以同时绑定 头顶血条 + 左上角头像血条 + 队友列表血条) |


## 4. UI 框架分层建议

建议在 `Src/UI` 下建立以下结构：

- **Core/**
    - `UIBase.cs`: 封装 `Bind`, `Unbind` 等通用逻辑。
    - `ViewModel`: 如果逻辑极复杂，可以用中间层，但目前直接绑定 Entity 足够。
- **HUD/**
    - `HealthBarUI.cs`: 头顶血条（需要每帧更新 Position 跟随 Entity）。
    - `DamageNumberUI.cs`: 伤害数字（纯特效，一次性）。
- **Systems/**
    - `UIManager.cs`: 监听全局生成的单位，管理血条的分配和回收。

## 5. 常见问题解答

**Q: 伤害数字 (Damage Number) 怎么做？**
A: 伤害数字是一次性的，不需要 Bind。
1. `DamageSystem` 计算完伤害，发送全局事件 `GameEventType.Damage.Applied`。
2. `DamageNumberUIManager` 监听这个全局事件。
3. 从对象池 spawn 一个数字 UI，设置位置和数值，播放动画，然后回收。

**Q: 背包界面 (Inventory) 怎么做？**
A: 背包通常是单例或绑定给 Player 的。
1. 打开背包 UI 时，调用 `InventoryUI.Bind(PlayerEntity)`。
2. 监听 `InventoryChanged` 事件刷新格子。

**Q: 技能冷却 UI 怎么做？**
A: 绑定 AbilityEntity。
1. 技能栏的每个 Icon 绑定一个具体的 `AbilityEntity`。
2. 监听 `AbilityCooldownStarted` 事件，开启 UI 上的 Timer/Tween 动画。
