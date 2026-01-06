# HealthComponent (生命组件)

管理实体的生命值、伤害计算、治疗以及死亡逻辑。支持 C# 原生事件通知和对象池复用。

## 核心功能

- **血量管理**: 实时维护当前生命值，并自动同步到实体的 `Data` 容器。
- **伤害计算**: 提供 `ModifyHealth` 接口修改血量，或直接修改 `Data` 均可生效。
- **治疗逻辑**: 处理治疗效果，确保血量不会超过最大上限。
- **事件驱动**: 监听 `Data` 容器的 `CurrentHp` 变化，自动触发 `Damaged` 和 `Died` 事件。
- **状态重置**: 提供 `Reset()` 方法，适配对象池复用场景。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取/写入以下键值：

| 键名 | 类型 | 读写 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| `MaxHp` | `float` | 读 | `100.0` | 实体的最大生命值 |
| `CurrentHp` | `float` | 读写 | `MaxHp` | 实体的当前生命值 |

## 公开接口

### 属性

- `MaxHp`: `float` (只读) - 当前配置的最大生命值。
- `CurrentHp`: `float` (只读) - 当前的生命值数值。
- `IsDead`: `bool` (只读) - 实体是否已死亡。

### 事件 (Action)

- `Damaged(float actualDamage)`: 受到有效伤害时触发。
- `Healed(float actualHeal)`: 受到有效治疗时触发。
- `Died()`: 实体血量归零时触发（仅触发一次）。

### 方法

- `ModifyHealth(float amount)`: 修改实体血量（正数回血，负数扣血）。
- `Reset()`: 重置血量和死亡标记（用于对象池）。

## 使用示例

```csharp
// 监听死亡事件
healthComponent.Died += () => {
    Log.Info("播放死亡动画并掉落物品");
};

// 造成伤害 (方式 1: 使用组件接口)
healthComponent.ModifyHealth(-25.0f);

// 造成伤害 (方式 2: 直接修改 Data，组件会自动响应)
entity.Data.Add(DataKey.CurrentHp, -25.0f);

```
