# HurtboxComponent (受击判定组件)

检测并响应来自 `HitboxComponent` 的攻击。该组件继承自 `Area2D`，负责维护无敌时间并触发受击事件。

## 核心功能

- **受击检测**: 监听 `Area2D` 的 `area_entered` 信号，识别 `HitboxComponent`。
- **无敌机制**: 提供受击后的无敌时间 (Invincibility Time) 保护，防止在同一帧或短时间内受到多次伤害。
- **事件解耦**: 仅触发 `HitReceived` 事件，不直接修改血量。伤害逻辑由订阅该事件的实体或其他系统决定。
- **动态配置**: 无敌时间从实体的 `Data` 容器中动态读取。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取以下键值：

| 键名                | 类型    | 默认值 | 说明                           |
| :------------------ | :------ | :----- | :----------------------------- |
| `InvincibilityTime` | `float` | `0.0`  | 受到攻击后的无敌持续时间（秒） |

## 公开接口

### 属性

- `InvincibilityTime`: `float` (只读) - 当前配置的无敌时长。
- `IsInvincible`: `bool` (只读) - 当前是否处于无敌状态。

### 事件 (Action)

- `HitReceived(HitboxComponent hitbox)`: 成功接收到一次有效攻击时触发。

## 使用说明

1. 将 `HurtboxComponent` 添加到实体（玩家或敌人）场景中。
2. 配置其 `CollisionLayer` 和 `CollisionMask`（通常 Layer 应匹配攻击方的 Mask）。
3. 添加 `CollisionShape2D` 子节点来定义实体的受击区域。
4. 实体脚本应订阅 `HitReceived` 事件来处理实际的业务逻辑：

```csharp
_hurtbox.HitReceived += (hitbox) => {
    // 1. 调用生命组件扣血
    _healthComponent.TakeDamage(hitbox.Damage);
    // 2. 处理击退效果
    _velocityComponent.ApplyKnockback(hitbox.GlobalPosition, hitbox.Knockback);
};
```
