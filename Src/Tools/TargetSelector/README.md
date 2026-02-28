# TargetSelector 目标选择工具集

## 1. 模块定位

`TargetSelector` 负责统一“范围查询”和“随机落点”逻辑，避免各系统重复手写距离循环、角度判断和随机采样。

分层结构如下：

1. `GeometryCalculator`：纯数学层（不依赖 IEntity）。
2. `EntityTargetSelector`：实体筛选层（几何 + 阵营 + 类型 + 排序 + 数量）。
3. `PositionTargetSelector`：位置生成层（按几何采样随机 `Vector2`）。

---

## 2. 使用入口

### 2.1 查询实体目标（返回 `List<IEntity>`）

```csharp
var targets = EntityTargetSelector.Query(new TargetSelectorQuery
{
    Geometry = GeometryType.Circle,
    Origin = caster.GlobalPosition,
    Range = 250f,
    CenterEntity = caster,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    TypeFilter = EntityType.Unit,
    Sorting = AbilityTargetSorting.Nearest,
    MaxTargets = 5
});
```

### 2.2 查询随机位置（返回 `List<Vector2>`）

```csharp
var points = PositionTargetSelector.Query(new TargetSelectorQuery
{
    Geometry = GeometryType.Ring,
    Origin = caster.GlobalPosition,
    InnerRange = 120f,
    Range = 280f,
    MaxTargets = 3
});
```

### 2.3 直接使用几何工具

```csharp
bool inCone = GeometryCalculator.IsPointInCone(
    point: targetPos,
    origin: casterPos,
    forward: facing,
    range: 300f,
    angle: 60f);

Vector2 random = GeometryCalculator.GetRandomPointInCircle(casterPos, 150f);
```

---

## 3. `TargetSelectorQuery` 参数说明

### 3.1 几何参数

| 字段 | 说明 |
|---|---|
| `Geometry` | 几何类型（必填）：Single / Circle / Ring / Box / Line / Cone / Chain / Global |
| `Origin` | 查询原点（必填） |
| `Forward` | 朝向向量（Box/Line/Cone 常用；不填默认 `Vector2.Right`） |
| `Range` | 半径/最大距离（Circle/Ring/Cone 常用） |
| `InnerRange` | 圆环内半径（Ring） |
| `Width` | 宽度（Box/Line） |
| `Length` | 长度（Box/Line） |
| `Angle` | 扇形角度（Cone，单位度） |
| `ChainCount` | 链式最大跳数（Chain） |
| `ChainRange` | 每跳最大距离（Chain） |
| `ChainAllowDuplicate` | 链式是否允许重复目标（Chain） |

### 3.2 过滤与排序参数（Entity 查询生效）

| 字段 | 说明 |
|---|---|
| `CenterEntity` | 阵营判定参照实体（Self/Friendly/Enemy 依赖该值） |
| `TeamFilter` | 阵营过滤位掩码（Friendly / Enemy / Neutral / Self） |
| `TypeFilter` | 实体类型过滤位掩码（`EntityType`） |
| `Sorting` | 排序规则（Nearest / Farthest / LowestHealth / Random ...） |
| `MaxTargets` | 目标数量上限（0 = 不限） |

> 注意：`PositionTargetSelector` 中 `MaxTargets` 表示“生成点数量”。

---

## 4. 几何语义

- Circle：圆形。
- Ring：圆环（`InnerRange <= dist <= Range`）。
- Box：定向矩形（按 `Forward` 建局部坐标）。
- Line：胶囊线段（线段 + 半宽距离）。
- Cone：扇形（半径 + 夹角）。
- Chain：链式路径预选（仅 `EntityTargetSelector` 生效）。
- Global：全局不过滤几何。
- Single：占位语义，通常由外部直接指定目标。

---

## 5. 链式查询（Chain）说明

`EntityTargetSelector` 的 Chain 采用“贪心最近点”策略：

1. 从 `Origin` 开始找最近目标；
2. 以该目标位置为下一跳起点继续找；
3. 达到 `ChainCount` 或找不到可跳目标即停止。

`ChainAllowDuplicate = false`：同一次链路中目标不会重复。  
`ChainAllowDuplicate = true`：允许回跳，但仍不会“连续命中当前实体本身”。

---

## 6. 执行顺序（Entity 查询）

`EntityTargetSelector.Query` 执行顺序固定：

1. 收集几何候选（或生成 Chain 结果）；
2. 过滤 Team / Type / LifecycleState（Dead、Reviving 会被排除）；
3. 执行排序（Chain 不排序，保留路径顺序）；
4. 按 `MaxTargets` 截断。

---

## 7. 约束与建议

1. 不要在业务中直接 `GetTree().GetNodesInGroup(...)` + 手写距离循环；统一走 TargetSelector。
2. Chain 是“瞬时快照”，带弹道延迟的技能应在每次命中时重新做一次局部查询。
3. `PositionTargetSelector` 只负责几何采样，不保证导航可达与无碰撞，必要时请二次过滤。
