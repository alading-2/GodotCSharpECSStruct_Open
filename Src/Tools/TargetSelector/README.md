# TargetSelector 目标选择工具

## 模块定位

`TargetSelector` 负责统一“范围查询”和“随机落点”逻辑，避免业务层反复手写距离遍历、角度判断与随机采样。

当前结构分为三层：

1. `Geometry2D`
   - 通用二维几何算法层
   - 提供形状判定、距离计算、随机采样
2. `GeometryCalculator`
   - 兼容门面
   - 保留旧 API，内部转调 `Geometry2D`
3. `EntityTargetSelector` / `PositionTargetSelector`
   - 目标选择领域层
   - 负责实体过滤、排序、数量裁剪或位置批量生成

## 典型入口

### 查询实体

```csharp
var targets = EntityTargetSelector.Query(new TargetSelectorQuery
{
    Geometry = GeometryType.Circle,
    Origin = caster.GlobalPosition,
    Range = 250f,
    CenterEntity = caster,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    Sorting = AbilityTargetSorting.Nearest,
    MaxTargets = 5,
});
```

### 生成位置

```csharp
var points = PositionTargetSelector.Query(new TargetSelectorQuery
{
    Geometry = GeometryType.Ring,
    Origin = caster.GlobalPosition,
    InnerRange = 120f,
    Range = 280f,
    MaxTargets = 3,
});
```

### 直接使用几何层

```csharp
bool inCone = Geometry2D.IsPointInCone(targetPos, casterPos, facing, 300f, 60f);
Vector2 random = Geometry2D.GetRandomPointInCircle(casterPos, 150f);
```

兼容旧代码时也可以继续使用：

```csharp
bool inCone = GeometryCalculator.IsPointInCone(targetPos, casterPos, facing, 300f, 60f);
```

## 边界说明

- `Geometry2D` 只做纯算法，不依赖 `IEntity`
- `GeometryType`、`TargetSelectorQuery` 仍留在 `TargetSelector` 域
- `GeometryCalculator` 现阶段不删除，避免一次性打断旧调用

## 当前支持的几何语义

- `Circle`
- `Ring`
- `Box`
- `Line`（胶囊）
- `Cone`
- `Global`
- `Single`（通常由外部显式指定目标）

## 约束

1. 不要在业务里直接 `GetTree().GetNodesInGroup(...)` 再手写几何判断。
2. 只需要纯几何能力时，优先调用 `Geometry2D`。
3. 只需要目标选择语义时，继续使用 `EntityTargetSelector` 或 `PositionTargetSelector`。
