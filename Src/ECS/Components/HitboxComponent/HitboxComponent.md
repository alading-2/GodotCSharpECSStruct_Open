# HitboxComponent (攻击判定组件)

定义攻击范围、伤害数值及击退属性。该组件继承自 `Area2D`，利用 Godot 的碰撞系统来检测与 `HurtboxComponent` 的重叠。

## 核心功能

- **碰撞检测**: 作为攻击发起方，检测与其重叠的受击区域。
- **数据负载**: 携带伤害 (Damage) 和击退 (Knockback) 信息，传递给受击方。
- **来源追踪**: 记录攻击发起者 (`Source`)，用于处理经验归属或避免自伤。
- **动态配置**: 伤害和击退参数从实体的 `Data` 容器中读取。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取以下键值：

| 键名 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `Damage` | `float` | `10.0` | 该判定区造成的伤害基础值 |
| `Knockback` | `float` | `100.0` | 该判定区造成的击退力度 |

## 公开接口

### 属性

- `Damage`: `float` (只读) - 当前配置的伤害值。
- `Knockback`: `float` (只读) - 当前配置的击退力。
- `Source`: `Node?` - 攻击的发起者节点引用。

## 使用说明

1. 将 `HitboxComponent` 添加到武器或子弹场景中。
2. 配置其 `CollisionLayer` 和 `CollisionMask`（通常 Mask 应包含受击方所在的层）。
3. 添加 `CollisionShape2D` 子节点来定义实际的攻击形状。
4. 当 `Area2D` 检测到重叠时，受击方的 `HurtboxComponent` 会读取此组件的数据。
