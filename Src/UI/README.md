# UI 系统开发指南

本文档介绍如何在项目中开发和使用 UI 系统。我们的 UI 系统采用 **"Binding Pattern" (绑定模式)**，UI 并非 Entity 的 Component，而是作为独立的 View 层，通过监听 Entity 的事件来驱动显示。

## 1. 核心架构

- **Entity (Model)**: 纯数据和逻辑，不包含任何 UI 节点。
- **UI (View)**: 继承自 `UIBase` 的 Godot Control 节点，**"绑定"** 到 Entity 上。
- **Event (Bridge)**: UI 通过订阅 `Entity.Events` 来感知数据变化。

## 2. 快速开始：创建新的 UI 组件

### 第一步：创建 Scene 和 Script

1. 创建一个继承自 `Control` (或其子类) 的场景，例如 `MyCustomUI.tscn`。
2. 创建对应的 C# 脚本 `MyCustomUI.cs`，**必须继承自 `UIBase`**。

### 第二步：实现脚本逻辑

```csharp
using Godot;

public partial class MyCustomUI : UIBase
{
    // 1. 获取 UI 节点引用
    private Label _label;
    
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }

    // 2. 重写 OnBind - 绑定时调用
    protected override void OnBind()
    {
        // 订阅 Entity 的局部事件 (推荐)
        // 注意：这是 entity.Events，不是 GlobalEventBus
        _entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
            GameEventType.Data.PropertyChanged,
            OnDataChanged
        );

        // 初始刷新
        UpdateDisplay();
    }

    // 3. 重写 OnUnbind - 解绑时调用 (可选)
    // EventBus 会自动清理订阅，通常只需处理一些特有的清理逻辑
    protected override void OnUnbind()
    {
        // 停止动画等...
    }

    // 4. 事件回调
    private void OnDataChanged(GameEventType.Data.PropertyChangedEventData evt)
    {
        // 过滤我们关心的 Key
        if (evt.Key == DataKey.CurrentHp)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        var hp = _entity.Data.Get<float>(DataKey.CurrentHp);
        _label.Text = $"HP: {hp}";
    }
}
```

### 第三步：使用对象池 (推荐)

如果你的 UI 需要频繁创建（如头顶血条、飘字），请使用对象池。

1. **注册池名称**：在 `Src/Tools/ObjectPool/ObjectPoolInit.cs` 的 `ObjectPoolNames` 中添加常量。
2. **初始化池**：在 `ObjectPoolInit.InitPools()` 中添加初始化代码。

```csharp
// ObjectPoolInit.cs
new ObjectPool<MyCustomUI>(
    () => (MyCustomUI)ResourceRegistry.LoadScene<MyCustomUI>().Instantiate(),
    new ObjectPoolConfig { 
        Name = ObjectPoolNames.MyCustomUIPool, 
        ParentPath = "UI/MyContainer" // UI 挂载的父节点路径
    }
);
```

### 第四步：手动绑定或自动管理

**方式 A：手动绑定** (适用于一次性 UI，如详情面板)

```csharp
var ui = objectPool.Spawn();
ui.Bind(targetEntity);
```

**方式 B：自动管理** (适用于 HUD，如 `UIManager`)

参考 `Src/UI/Core/UIManager.cs`，监听全局 `Unit.Created` 事件，自动为新单位分配 UI。

## 3. 核心类说明

### `UIBase`
- `Bind(IEntity entity)`: 绑定实体，触发 `OnBind()`。
- `Unbind()`: 解绑实体，触发 `OnUnbind()`，自动清理该 Entity 的事件订阅。
- `GetBoundEntity()`: 获取当前绑定的 Entity。
- 实现了 `IPoolable`，支持对象池自动回收生命周期。

### `UIManager`
- 位于 `Src/UI/Core/UIManager.cs`。
- 一个 AutoLoad 单例。
- 负责监听单位生成/销毁，自动管理头顶血条 (`HealthBarUI`)。

## 4. 常见问题

- **Q: 为什么 UI 不做成 Component Component?**
  - A: UI 是显示层(View)，Component 是逻辑层(Logic)。由各种 Layout Container 管理 UI 的层级比挂在 Entity 下更合理（避免随 Entity 旋转、缩放导致的显示问题）。

- **Q: 怎么获取 Entity 的数据？**
  - A: 在 `OnBind` 后，使用 `_entity.Data.Get<T>(Key)`。

- **Q: 为什么 `DamageNumberUI` 不继承 `UIBase`?**
  - A: `UIBase` 是为 **"持久化绑定"** 设计的（需要由 `Bind` 建立长连接并监听 Entity 变化）。
  - 而 `DamageNumberUI` 是 **"瞬态"** 的特效（Fire-and-Forget），它只需要显示那一刻的快照数据（数值），不需要持续监听 Entity。为了避免不必要的 `Bind/Unbind` 开销，它直接继承 `Control` 并实现 `IPoolable`。

- **Q: 怎么响应数据变化？**
  - A: 监听 `_entity.Events`。不要在 `_Process` 每一帧去 Get 数据，性能太差。
