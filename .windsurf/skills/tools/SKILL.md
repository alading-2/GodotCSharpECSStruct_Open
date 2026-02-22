---
name: tools
description: 使用 TimerManager 计时器、ObjectPool 对象池、TargetSelector 目标查询、ResourceManagement 资源加载时使用。适用于：需要延迟/循环定时器，高频生成销毁对象，范围内查找敌人，加载场景或配置资源。触发关键词：TimerManager、定时器、延迟执行、对象池、ObjectPool、TargetSelector、范围查找、ResourceManagement、加载资源。
---

# 工具类使用规范

## TimerManager - 计时器

禁止 `new Timer()` 和 `GetTree().CreateTimer()`，统一用 `TimerManager`。

```csharp
// 延迟执行
TimerManager.Instance.Delay(2.0f).OnComplete(() => DoSomething());

// 循环执行
var timer = TimerManager.Instance.Loop(1.0f).OnLoop(() => DamageOverTime());

// 带参数
TimerManager.Instance.Delay(0.5f).OnComplete(() => Explode(damage));

// 清理（重要！在 _ExitTree 或 OnPoolRelease 中取消）
public override void _ExitTree() { timer?.Cancel(); }
public void OnPoolRelease() { timer?.Cancel(); }
```

API 文档：`Src/Tools/Timer/TimerManager.md`

---

## ObjectPool - 对象池

禁止手动 `new` + `QueueFree()`，高频对象必须走对象池。

强制场景：子弹、伤害数字、特效、敌人。

```csharp
// 通过 EntityManager 使用对象池生成
var bullet = EntityManager.Spawn<BulletEntity>(new EntitySpawnConfig
{
    Config = bulletData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.BulletPool,
    Position = spawnPosition,
    Rotation = angle
});

// 销毁（自动归还对象池）
EntityManager.Destroy(bullet);

// 对象池初始化：ObjectPoolInit.cs（已配置，无需修改）
```

实现 `IPoolable` 接口：

```csharp
public void OnPoolAcquire() { /* 取出时初始化 */ }
public void OnPoolRelease() { /* 归还时清理 */ }
public void OnPoolReset()   { /* 数据重置，通常留空 */ }
```

API 文档：`Src/Tools/ObjectPool/ObjectPool.md`

---

## TargetSelector - 目标选择

禁止 `GetTree().GetNodesInGroup()` 和手写距离计算。

```csharp
// 查找范围内最近的 5 个敌人
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Circle,
    Origin = caster.GlobalPosition,
    Range = 200f,
    CenterEntity = caster,
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    Sorting = AbilityTargetSorting.Nearest,
    MaxTargets = 5
});

// 扇形范围
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Cone,
    Origin = caster.GlobalPosition,
    Range = 150f,
    Angle = 60f,           // 扇形角度（度）
    Direction = facing,    // 朝向向量
    TeamFilter = AbilityTargetTeamFilter.Enemy,
    MaxTargets = 10
});

// 矩形范围
var targets = TargetSelector.Query(new TargetSelectorQuery
{
    Geometry = AbilityTargetGeometry.Rectangle,
    Origin = caster.GlobalPosition,
    Range = 200f,          // 长度
    Width = 80f,           // 宽度
    Direction = facing,
    TeamFilter = AbilityTargetTeamFilter.Enemy
});
```

TeamFilter 选项：`Enemy` / `Ally` / `All` / `Self`
Sorting 选项：`Nearest` / `Farthest` / `LowestHp` / `HighestHp` / `Random`

API 文档：`Src/Tools/TargetSelector/README.md`

---

## ResourceManagement - 资源加载

禁止 `GD.Load<T>("res://...")` 和硬编码字符串路径。

```csharp
// 加载场景（Entity/Component）
var scene = ResourceManagement.Load<PackedScene>("EnemyEntity", ResourceCategory.Entity);
var compScene = ResourceManagement.Load<PackedScene>("HealthComponent", ResourceCategory.Component);

// 加载配置
var config = ResourceManagement.Load<Resource>("德鲁伊", ResourceCategory.PlayerConfig);
var enemyConfig = ResourceManagement.Load<Resource>("史莱姆", ResourceCategory.EnemyConfig);

// 加载系统场景
var sysScene = ResourceManagement.Load<PackedScene>("DamageService", ResourceCategory.System);
```

ResourceCategory 分类：`Entity` / `Component` / `PlayerConfig` / `EnemyConfig` / `System` / ...

例外（允许直接路径）：
- `.tscn` 内部资源引用（Godot 自动管理）
- `[Export]` 导出属性指向的资源
- `ResourceGenerator` 等底层工具

新增资源后运行 ResourceGenerator 自动更新 `ResourcePaths.cs`。

API 文档：`Data/ResourceManagement/ResourceManagement.md`

---

## 关键文件路径
- **TimerManager** → `Src/Tools/Timer/TimerManager.cs` | 文档 → `Src/Tools/Timer/TimerManager.md`
- **GameTimer** → `Src/Tools/Timer/GameTimer.cs`
- **ObjectPool** → `Src/Tools/ObjectPool/ObjectPool.cs` | 文档 → `Src/Tools/ObjectPool/ObjectPool.md`
- **IPoolable 接口** → `Src/Tools/ObjectPool/IPoolable.cs`
- **ObjectPoolManager** → `Src/Tools/ObjectPool/ObjectPoolManager.cs`
- **TargetSelector** → `Src/Tools/TargetSelector/TargetSelector.cs` | 文档 → `Src/Tools/TargetSelector/README.md`
- **TargetSelectorQuery** → `Src/Tools/TargetSelector/TargetSelectorQuery.cs`
- **ResourceManagement** → `Data/ResourceManagement/ResourceManagement.cs` | 文档 → `Data/ResourceManagement/ResourceManagement.md`
- **ResourcePaths（自动生成）** → `Data/ResourceManagement/ResourcePaths.cs`
