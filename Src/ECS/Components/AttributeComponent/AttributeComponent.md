# AttributeComponent (属性组件)

作为实体的核心数据管理中心，负责管理基础属性值（Base Value）以及来自各种来源（如装备、Buff、技能）的属性修改器（Modifiers）。

## 核心设计

- **脏标记机制**: 使用 `_isDirty` 标记和缓存字典 `_cachedValues`。只有在修改器发生变动或基础值改变时，才会重新触发属性计算，优化性能。
- **公式统一**: 严格遵循公式：`FinalValue = (BaseValue + ΣAdditive) * ΠMultiplicative`。
- **解耦设计**: 基础值存储在实体的 `Data` 容器中，本组件仅负责逻辑运算和缓存管理。

## 属性 (Data 容器)

该组件通常依赖于以下基础属性（键名可自定义）：

| 键名 | 类型 | 说明 |
| :--- | :--- | :--- |
| `BaseDamage` | `float` | 基础攻击力 |
| `BaseSpeed` | `float` | 基础移动速度 |
| `BaseCritRate` | `float` | 基础暴击率 |
| `BaseCritMultiplier` | `float` | 基础暴击倍率 |

## 公开接口

### 属性 (计算后的最终值)

- `Damage`: `float` - 最终攻击力。
- `Speed`: `float` - 最终速度。
- `CritRate`: `float` - 最终暴击率。
- `CritMultiplier`: `float` - 最终暴击倍率。

### 事件 (Action)

- `AttributeChanged`: 当任何属性值发生变动（添加/移除修改器或基础值改变）时触发。

### 方法

- `AddModifier(AttributeModifier modifier)`: 添加一个新的属性修改器。
- `RemoveModifier(string modifierId)`: 根据 ID 移除指定的修改器。
- `HasModifier(string modifierId) -> bool`: 检查是否存在特定修改器。
- `ClearModifiers()`: 清除所有修改器。
- `GetFinalValue(string attrName, string baseAttrKey, float defaultBaseValue) -> float`: 获取指定属性的最终计算值。

## 使用示例

```csharp
// 添加一个增加 20 点基础攻击力的修改器
_attributeComponent.AddModifier(new AttributeModifier {
    Id = "IronSword",
    AttributeName = "Damage",
    Value = 20f,
    Type = ModifierType.Additive
});

// 添加一个增加 10% 速度的修改器
_attributeComponent.AddModifier(new AttributeModifier {
    Id = "SpeedBuff",
    AttributeName = "Speed",
    Value = 1.1f, // 110%
    Type = ModifierType.Multiplicative
});
```
