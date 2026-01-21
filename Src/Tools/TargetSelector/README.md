# TargetSelector 目标选择工具

## 概述

`TargetSelector` 是一个通用的空间查询和目标筛选工具,用于在游戏中选取符合特定条件的实体。它支持多种几何形状、阵营过滤、类型过滤、排序和数量限制等功能。

**核心特性**:
- 🎯 多种几何形状范围检测(圆形/矩形/线段/扇形/链式/全局)
- 🛡️ 阵营过滤(友军/敌军/中立/自身)
- 👾 类型过滤(英雄/小怪/Boss/建筑/召唤物)
- 📊 多种排序规则(距离/血量/威胁值/随机)
- 🎲 灵活的参数组合,通过 `TargetSelectorQuery` 配置

## 快速开始

```csharp
using Godot;
using System.Collections.Generic;

// 示例1: 圆形范围查询敌人
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Circle,
    Origin = player.GlobalPosition,
    Range = 200f,
    CenterEntity = player,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    MaxTargets = 5
});

// 示例2: 扇形范围查询最近的3个敌人
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Cone,
    Origin = player.GlobalPosition,
    Forward = player.Transform.X, // 前向向量
    Range = 300f,
    Angle = 60f, // 60度扇形
    CenterEntity = player,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    Sorting = AbilityTargetSorting.Nearest,
    MaxTargets = 3
});

// 示例3: 链式传递(如闪电链)，默认不重复目标ChainAllowDuplicate = false
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Chain,
    Origin = player.GlobalPosition,
    ChainCount = 5, // 最多跳5次
    ChainRange = 150f, // 每跳最大距离150
    CenterEntity = player,
    TeamFilter = AbilityTargetTeamFilter.Enemy
});
```

## API 参考

### TargetSelector.Query(query)

主要查询方法,返回符合条件的实体列表。

**参数**: `TargetSelectorQuery` - 查询配置对象
**返回**: `List<IEntity>` - 符合条件的实体列表(已排序、已限制数量)

### TargetSelectorQuery 参数说明

#### 几何参数

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
|:-----------|:-----------|:-----------|:-----------------------------------|
| `Geometry` | `AbilityTargetGeometry` | ✅ | 几何形状类型 |
| `Origin` | `Vector2` | ✅ | 查询原点 |
| `Forward` | `Vector2?` | - | 方向向量 (Line/Cone/Box 需要) |
| `Range` | `float` | - | 半径 (Circle/Cone 使用) |
| `Width` | `float` | - | 宽度 (Box/Line 使用) |
| `Length` | `float` | - | 长度 (Box/Line 使用) |
| `Angle` | `float` | - | 角度 (Cone 使用, 单位: 度) |
| `ChainCount` | `int` | - | 链式跳跃次数 (Chain 使用) |
| `ChainRange` | `float` | - | 链式每跳最大距离 (Chain 使用) |
| `ChainAllowDuplicate` | `bool` | - | 链式是否允许重复目标 (默认 false) |

#### 过滤参数

| 参数 | 类型 | 必填 | 说明 |
|:-------------|:-----------|:-----------|:---------------|
| `CenterEntity` | `IEntity?` | - | 阵营过滤基准实体 |
| `TeamFilter` | `AbilityTargetTeamFilter` | - | 阵营过滤器 |
| `TypeFilter` | `AbilityTargetTypeFilter` | - | 类型过滤器 |

#### 排序与限制参数

| 参数 | 类型 | 必填 | 说明 |
|:-----------|:-----------|:-----------|:-------------------|
| `Sorting` | `AbilityTargetSorting` | - | 排序规则 |
| `MaxTargets` | `int` | - | 最大目标数量 (0 = 不限制) |

## 几何形状详解

### Circle (圆形)

**参数**: `Origin`, `Range`  
**逻辑**: `distance(target, Origin) <= Range`  
**用途**: AOE 技能、光环效果

```csharp
Geometry = AbilityTargetGeometry.Circle,
Origin = caster.GlobalPosition,
Range = 200f
```

### Rectangle (矩形)

**参数**: `Origin`, `Forward`, `Width`, `Length`  
**逻辑**: 旋转矩形包含检测 (支持任意角度)  
**用途**: 扇形剑气、矩形冲击波

```csharp
Geometry = AbilityTargetGeometry.Box,
Origin = caster.GlobalPosition,
Forward = caster.Transform.X,
Width = 100f,
Length = 300f
```

### Line (带宽线段)

**参数**: `Origin`, `Forward`, `Length`, `Width`  
**逻辑**: 点到线段距离 <= Width/2  
**用途**: 激光、贯穿弹道

```csharp
Geometry = AbilityTargetGeometry.Line,
Origin = caster.GlobalPosition,
Forward = caster.Transform.X,
Length = 500f,
Width = 50f
```

### Cone (扇形)

**参数**: `Origin`, `Forward`, `Range`, `Angle`  
**逻辑**: 距离检测 + 角度检测  
**用途**: 喷火、霰弹枪、扇形斩击

```csharp
Geometry = AbilityTargetGeometry.Cone,
Origin = caster.GlobalPosition,
Forward = caster.Transform.X,
Range = 300f,
Angle = 90f // 90度扇形
```

### Chain (链式传递)

**参数**: `Origin`, `ChainCount`, `ChainRange`  
**逻辑**: 贪心算法,每次选最近的未访问目标。  
**关键特性**: **瞬间快照** —— 在调用的一瞬间计算出完整路径。  
**用途**: 瞬发链式杀伤、静电场指示器。

> [!CAUTION]
> **关于“时空错位”的警告**  
> `QueryChain` 返回的是 T=0 时刻的计算快照。如果你的技能有明显的“飞行延迟”或“每跳等待时间”，请**不要**使用此方法。  
> **原因**：在延迟期间，预选的目标可能已经搬走或死亡。  
> **方案**：在 `AbilityExecutor` 中使用协程，每命中一次后，以当前命中者为圆心调用 `Circle` 查询下一个目标。

```csharp
Geometry = AbilityTargetGeometry.Chain,
Origin = caster.GlobalPosition,
ChainCount = 5,
ChainRange = 150f
```

### Global (全局)

**参数**: 无  
**逻辑**: 返回所有实体(配合过滤器使用)  
**用途**: 全屏技能、全局BUFF

```csharp
Geometry = AbilityTargetGeometry.Global,
TeamFilter = AbilityTargetTeamFilter.Enemy // 所有敌人
```

## 阵营过滤

使用 `TeamFilter` 参数筛选阵营,支持位运算组合:

```csharp
// 只选敌人
TeamFilter = AbilityTargetTeamFilter.Enemy

// 选友军和自己
TeamFilter = AbilityTargetTeamFilter.FriendlyAndSelf

// 选所有单位
TeamFilter = AbilityTargetTeamFilter.All

// 自定义组合(友军+中立)
TeamFilter = AbilityTargetTeamFilter.Friendly | AbilityTargetTeamFilter.Neutral
```

**前提**: 目标实体必须设置 `DataKey.Team` 数据,否则会被过滤掉。

## 类型过滤

使用 `TypeFilter` 参数筛选单位类型:

```csharp
// 只选单位 (生物)
TypeFilter = AbilityTargetTypeFilter.Unit

// 选所有可攻击单位 (单位 + 建筑)
TypeFilter = AbilityTargetTypeFilter.AllAttackable

// 自定义组合 (单位 + 掉落物)
TypeFilter = AbilityTargetTypeFilter.Unit | AbilityTargetTypeFilter.Item
```

**前提**: 目标实体必须设置 `DataKey.EntityType` 数据,且值与 `AbilityTargetTypeFilter` 对应。

## 排序规则

| 规则 | 说明 | 依赖数据 |
|------|------|----------|
| `Nearest` | 距离最近 | - |
| `Farthest` | 距离最远 | - |
| `LowestHealth` | 血量最低 | `CurrentHp` |
| `HighestHealth` | 血量最高 | `CurrentHp` |
| `LowestHealthPercent` | 血量百分比最低 | `HpPercent` |
| `HighestHealthPercent` | 血量百分比最高 | `HpPercent` |
| `Random` | 随机排序 | - |
| `HighestThreat` | 威胁值最高 | `Threat` |

## 高级用法

### 组合过滤

```csharp
// 查询200范围内,血量百分比最低的3个敌方英雄
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Circle,
    Origin = healer.GlobalPosition,
    Range = 200f,
    CenterEntity = healer,
    TeamFilter = AbilityTargetTeamFilter.Friendly,
    TypeFilter = AbilityTargetTypeFilter.Unit,
    Sorting = AbilityTargetSorting.LowestHealthPercent,
    MaxTargets = 3
});
```

### 无阵营过滤

```csharp
// 查询所有单位(不区分敌友)
TeamFilter = AbilityTargetTeamFilter.All

// 或者不设置CenterEntity
CenterEntity = null
```

### 动态计算方向

```csharp
// 扇形朝向鼠标
Vector2 mousePos = GetGlobalMousePosition();
Vector2 forward = (mousePos - caster.GlobalPosition).Normalized();

var query = new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Cone,
    Origin = caster.GlobalPosition,
    Forward = forward,
    Range = 300f,
    Angle = 45f
};
```

## 性能优化提示

1. **使用MaxTargets**: 如果只需要前N个目标,务必设置 `MaxTargets`,避免不必要的处理
2. **先几何后过滤**: 工具内部已优化为先几何检测再过滤,无需担心顺序
3. **避免频繁查询**: 对于AI寻敌等高频操作,建议加入冷却时间或间隔调用
4. **空间哈希优化(TODO)**: 当前实现是全表遍历,未来可升级为空间哈希或Quadtree

## 集成示例(技能系统)

在 `AbilitySystem` 中的使用方式:

```csharp
// AbilitySystem 自动从技能数据构造 TargetSelectorQuery
private static List<IEntity> SelectTargetsUsingSelector(AbilityEntity ability, IEntity owner)
{
    var query = new TargetSelectorQuery
    {
        Geometry = (AbilityTargetGeometry)ability.Data.Get<int>(DataKey.AbilityTargetGeometry),
        Origin = (owner as Node2D)?.GlobalPosition ?? Vector2.Zero,
        Range = ability.Data.Get<float>(DataKey.AbilityRange),
        // ... 其他参数从技能配置读取
    };
    
    return TargetSelector.Query(query);
}
```

## 已知限制

- **无物理障碍检测**: 当前实现基于数学计算,不检测墙体遮挡,如需此功能可后续升级为混合方式
- **实体类型硬编码**: `GetAllNode2DEntities` 中需要手动列举实体类型,未来可改为反射或中心注册表
- **性能瓶颈**: 全表遍历,当实体数量>1000时可能卡顿,需空间分区优化

## 相关文档

- [AbilityEnums.cs](../../../Data/DataKeyRegister/Ability/AbilityEnums.cs) - 枚举定义
- [DataKey_Ability.cs](../../../Data/DataKeyRegister/Ability/DataKey_Ability.cs) - 技能相关DataKey
- [AbilitySystem.cs](../../ECS/System/AbilitySystem/AbilitySystem.cs) - 技能系统集成实例
