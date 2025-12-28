# System 层架构设计 - 伪 ECS 系统层

## 📋 核心理念

### ECS 中的 System 是什么？

在经典 ECS 架构中：

- **Entity（实体）**：游戏对象的唯一标识符
- **Component（组件）**：纯数据容器，无逻辑
- **System（系统）**：纯逻辑处理器，操作特定组件集合

### 伪 ECS 的 System 设计原则

```
┌─────────────────────────────────────────────────────┐
│  System 的职责边界                                   │
├─────────────────────────────────────────────────────┤
│  ✅ 应该做的：                                       │
│  - 批量处理具有特定组件的实体                        │
│  - 实现跨实体的全局逻辑（碰撞检测、生成管理）       │
│  - 提供无状态的工具方法（伤害计算、效果播放）       │
│  - 管理游戏流程和状态转换                            │
│                                                      │
│  ❌ 不应该做的：                                     │
│  - 存储实体特定的状态（应该在 Component 或 Data）   │
│  - 直接修改 Node 的属性（应该通过 Component）       │
│  - 实现单个实体的行为逻辑（应该在 Entity 类中）     │
└─────────────────────────────────────────────────────┘
```

---

## 🏗️ System 分类体系

### 1. 核心流程系统（Core Systems）

**特征**：管理游戏生命周期和全局状态

- GameFlowSystem - 游戏流程控制
- SceneManagementSystem - 场景管理

### 2. 生成管理系统（Spawn Systems）

**特征**：负责实体的创建、销毁、池化管理

- SpawnSystem - 通用生成系统
- WaveSpawnSystem - 波次生成系统

### 3. 逻辑处理系统（Logic Systems）

**特征**：实现游戏核心玩法逻辑

- MovementSystem - 移动处理（可选，简单游戏可省略）
- CombatSystem - 战斗逻辑协调
- ProgressionSystem - 经验/升级系统

### 4. 工具服务系统（Utility Systems）

**特征**：提供无状态的工具方法

- DamageCalculationService - 伤害计算
- EffectService - 特效播放
- AudioService - 音频管理

### 5. 资源管理系统（Resource Systems）

**特征**：管理资源加载和缓存

- ResourceCacheSystem - 资源缓存

---

## 📐 System 设计模式

### 模式 1：单例 AutoLoad System（全局服务）

**适用场景**：需要全局访问的无状态服务（Service）。

**重要说明**：推荐在 `AutoLoad.cs` 中统一注册，而非手动实现单例模式。

**1. 系统注册 (AutoLoad.cs)**:

```csharp
private void Configure()
{
    // 使用 Priority.System 优先级注册
    Register("DamageCalculationService", "res://Src/ECS/Systems/DamageCalculationService.cs", Priority.System);
}
```

**2. 系统实现 (DamageCalculationService.cs)**:

```csharp
/// <summary>
/// 伤害计算服务 - 无状态工具类
/// </summary>
public partial class DamageCalculationService : Node
{
    private static readonly Log _log = new Log("DamageCalculationService");

    /// <summary>
    /// 计算最终伤害（纯函数，无副作用）
    /// </summary>
    public float CalculateFinalDamage(float baseDamage, Node attacker, Node target)
    {
        float finalDamage = baseDamage;
        // ... 计算逻辑
        return finalDamage;
    }
}
```

**3. 系统访问**:

```csharp
// 在逻辑层中获取并调用
var damageService = AutoLoad.Get<DamageCalculationService>();
float damage = damageService.CalculateFinalDamage(10, attacker, target);
```

**关键特征**：

- ✅ **无状态（Stateless）**: 不存储任何运行时数据。
- ✅ **纯函数设计**: 相同输入产生相同输出。
- ✅ **全局访问**: 通过 `AutoLoad.Get<T>()` 随时获取。
- ✅ **加载控制**: 通过 `Priority.System` 确保在核心工具之后、游戏逻辑之前加载。

---

### 模式 2：场景挂载 System（有状态管理器）

**适用场景**：需要管理实体集合或游戏状态

```csharp
/// <summary>
/// 波次生成系统 - 管理敌人生成逻辑
/// </summary>
public partial class WaveSpawnSystem : Node
{
    private static readonly Log _log = new Log("WaveSpawnSystem");

    // === 配置数据 ===
    [Export] public Godot.Collections.Array<WaveData> WaveConfigs { get; set; }
    [Export] public Node2D SpawnContainer { get; set; }  // 敌人父节点

    // === 运行时状态 ===
    private int _currentWaveIndex = 0;
    private float _waveTimer = 0;
    private List<Node> _activeEnemies = new();  // 追踪活跃敌人

    // === 对象池 ===
    private Dictionary<string, ObjectPool<Node>> _enemyPools = new();

    public override void _Ready()
    {
        // 初始化对象池
        InitializeEnemyPools();

        // 监听敌人死亡事件
        EventBus.EnemyDied += OnEnemyDied;
    }

    public override void _Process(double delta)
    {
        if (_currentWaveIndex >= WaveConfigs.Count) return;

        _waveTimer += (float)delta;

        // 定时生成逻辑
        ProcessWaveSpawning((float)delta);
    }

    /// <summary>
    /// 生成单个敌人
    /// </summary>
    public Node SpawnEnemy(EnemyData enemyData, Vector2 position)
    {
        // 从对象池获取
        var enemy = _enemyPools[enemyData.EnemyType].Spawn(SpawnContainer);

        if (enemy is Node2D enemy2D)
        {
            enemy2D.GlobalPosition = position;
        }

        // 初始化敌人数据
        enemy.GetData().Set("EnemyData", enemyData);

        _activeEnemies.Add(enemy);
        _log.Debug($"生成敌人: {enemyData.EnemyName} at {position}");

        return enemy;
    }

    private void OnEnemyDied(Node enemy, Vector2 position)
    {
        _activeEnemies.Remove(enemy);

        // 检查波次完成条件
        if (_activeEnemies.Count == 0 && IsWaveSpawningComplete())
        {
            CompleteCurrentWave();
        }
    }

    public override void _ExitTree()
    {
        EventBus.EnemyDied -= OnEnemyDied;
    }
}
```

**关键特征**：

- ✅ 管理实体集合
- ✅ 持有运行时状态
- ✅ 监听全局事件
- ✅ 负责对象池管理

---

### 模式 3：静态工具类（纯函数集合）

**适用场景**：不需要 Godot 生命周期的纯逻辑

```csharp
/// <summary>
/// 数学工具类 - 纯静态方法
/// </summary>
public static class MathUtils
{
    /// <summary>
    /// 计算圆形范围内的随机位置
    /// </summary>
    public static Vector2 RandomPositionInCircle(Vector2 center, float radius)
    {
        float angle = GD.Randf() * Mathf.Tau;
        float distance = GD.Randf() * radius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    /// <summary>
    /// 计算屏幕外的生成位置
    /// </summary>
    public static Vector2 GetOffscreenSpawnPosition(Vector2 targetPosition, float distance)
    {
        var viewport = Engine.GetMainLoop() as SceneTree;
        var viewportRect = viewport.Root.GetViewport().GetVisibleRect();

        // 在屏幕边缘外生成
        // ... 实现逻辑

        return Vector2.Zero;
    }
}
```

**关键特征**：

- ✅ 完全无状态
- ✅ 不依赖 Godot 生命周期
- ✅ 可单独单元测试
- ✅ 性能最优

---

## 🎯 System 设计检查清单

在实现每个 System 前，问自己：

### 1. 职责检查

- [ ] 这个 System 是否只负责一个明确的功能领域？
- [ ] 是否可以用一句话描述它的职责？
- [ ] 是否避免了"上帝类"（God Class）的设计？

### 2. 状态检查

- [ ] 是否真的需要持有状态？（优先考虑无状态设计）
- [ ] 状态是否可以通过参数传递？
- [ ] 是否可以改为静态工具类？

### 3. 依赖检查

- [ ] 是否避免了循环依赖？
- [ ] 是否通过事件而非直接引用通信？
- [ ] 是否可以通过依赖注入解耦？

### 4. 性能检查

- [ ] 是否避免了在 `_Process` 中频繁分配内存？
- [ ] 是否使用了对象池？
- [ ] 是否缓存了频繁访问的引用？

---

## 📂 目录结构

```
Src/ECS/Systems/
├── Core/                       # 核心流程系统
│   ├── GameFlowSystem.cs
│   └── SceneManagementSystem.cs
│
├── Spawn/                      # 生成管理系统
│   ├── SpawnSystem.cs
│   └── WaveSpawnSystem.cs
│
├── Logic/                      # 逻辑处理系统
│   ├── MovementSystem.cs       # 可选
│   ├── CombatSystem.cs
│   └── ProgressionSystem.cs
│
├── Service/                    # 工具服务系统
│   ├── DamageCalculationService.cs
│   ├── EffectService.cs
│   └── AudioService.cs
│
├── Resource/                   # 资源管理系统
│   └── ResourceCacheSystem.cs
│
└── Utils/                      # 静态工具类
    ├── MathUtils.cs
    └── SpawnUtils.cs
```

---

## 🔄 System 间通信规则

### 规则 1：优先使用事件（Event）

```csharp
// ❌ 不好：直接调用
public class WaveSystem
{
    private ProgressionSystem _progression;

    private void OnWaveComplete()
    {
        _progression.AddExperience(100);  // 紧耦合
    }
}

// ✅ 好：通过事件
public class WaveSystem
{
    private void OnWaveComplete()
    {
        EventBus.WaveCompleted?.Invoke(_currentWaveIndex);  // 解耦
    }
}

public class ProgressionSystem
{
    public override void _Ready()
    {
        EventBus.WaveCompleted += OnWaveCompleted;
    }

    private void OnWaveCompleted(int waveIndex)
    {
        AddExperience(100);
    }
}
```

### 规则 2：服务类可以直接调用

```csharp
// ✅ 允许：调用无状态服务（通过 AutoLoad）
public class HurtboxComponent : Area2D
{
    private void OnHitboxEntered(HitboxComponent hitbox)
    {
        // 通过 AutoLoad 获取服务
        var damageService = AutoLoad.Get<DamageCalculationService>("DamageCalculationService");

        // 直接调用服务方法
        float damage = damageService.CalculateFinalDamage(
            hitbox.Damage,
            hitbox.Owner,
            this.Owner
        );

        _health.TakeDamage(damage);
    }
}
```

### 规则 3：避免 System 间直接引用

```csharp
// ❌ 禁止：System 间直接依赖
public class SystemA : Node
{
    private SystemB _systemB;  // 不要这样做
}

// ✅ 推荐：通过事件或服务
public class SystemA : Node
{
    private void DoSomething()
    {
        EventBus.SomethingHappened?.Invoke();  // 发布事件
        // 或
        SomeService.Instance.DoWork();  // 调用服务
    }
}
```

---

## 📊 System 性能优化指南

### 1. 批量处理优化

```csharp
// ❌ 低效：逐个处理
public void DamageAllEnemiesInRange(Vector2 center, float radius, float damage)
{
    foreach (var enemy in _activeEnemies)
    {
        if (enemy.GlobalPosition.DistanceTo(center) <= radius)
        {
            enemy.GetNode<HealthComponent>("Health").TakeDamage(damage);
        }
    }
}

// ✅ 高效：使用物理查询
public void DamageAllEnemiesInRange(Vector2 center, float radius, float damage)
{
    var spaceState = GetWorld2D().DirectSpaceState;
    var query = PhysicsShapeQueryParameters2D.Create();
    query.Shape = new CircleShape2D { Radius = radius };
    query.Transform = new Transform2D(0, center);
    query.CollisionMask = LayerMask.Enemy;

    var results = spaceState.IntersectShape(query);
    foreach (var result in results)
    {
        if (result["collider"].AsGodotObject() is Node enemy)
        {
            enemy.GetNode<HealthComponent>("Health").TakeDamage(damage);
        }
    }
}
```

### 2. 缓存优化

```csharp
public partial class WaveSpawnSystem : Node
{
    // ✅ 缓存频繁访问的引用
    private Node2D _player;
    private Viewport _viewport;
    private Rect2 _viewportRect;

    public override void _Ready()
    {
        _player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
        _viewport = GetViewport();
        UpdateViewportRect();
    }

    private void UpdateViewportRect()
    {
        _viewportRect = _viewport.GetVisibleRect();
    }
}
```

### 3. 对象池集成

```csharp
public partial class SpawnSystem : Node
{
    private Dictionary<string, ObjectPool<Node>> _pools = new();

    /// <summary>
    /// 注册实体类型到对象池
    /// </summary>
    public void RegisterEntityType(string typeName, PackedScene scene, int initialSize = 20)
    {
        _pools[typeName] = new ObjectPool<Node>(
            () => scene.Instantiate<Node>(),
            new ObjectPoolConfig
            {
                Name = $"{typeName}Pool",
                InitialSize = initialSize,
                MaxSize = initialSize * 5
            }
        );
    }

    /// <summary>
    /// 生成实体（自动使用对象池）
    /// </summary>
    public Node SpawnEntity(string typeName, Node parent, Vector2 position)
    {
        var entity = _pools[typeName].Spawn(parent);

        if (entity is Node2D entity2D)
        {
            entity2D.GlobalPosition = position;
        }

        return entity;
    }
}
```

---

## 🧪 System 测试策略

### 1. 无状态服务测试

```csharp
[TestFixture]
public class DamageCalculationServiceTests
{
    [Test]
    public void CalculateFinalDamage_WithCrit_ReturnsDoubledDamage()
    {
        // Arrange
        var attacker = new Node();
        attacker.GetData().Set("CritChance", 1.0f);  // 100% 暴击
        attacker.GetData().Set("CritMultiplier", 2.0f);

        var target = new Node();

        // Act
        float damage = DamageCalculationService.Instance.CalculateFinalDamage(
            100, attacker, target
        );

        // Assert
        Assert.AreEqual(200, damage);
    }
}
```

### 2. 有状态系统测试

```csharp
[TestFixture]
public class WaveSpawnSystemTests
{
    private WaveSpawnSystem _system;

    [SetUp]
    public void Setup()
    {
        _system = new WaveSpawnSystem();
        // 初始化测试数据
    }

    [Test]
    public void SpawnEnemy_AddsToActiveEnemies()
    {
        // Arrange
        var enemyData = new EnemyData { EnemyName = "Slime" };

        // Act
        _system.SpawnEnemy(enemyData, Vector2.Zero);

        // Assert
        Assert.AreEqual(1, _system.ActiveEnemyCount);
    }
}
```

---

## 📝 下一步

阅读各个 System 的详细设计文档：

1. [SpawnSystem - 通用生成系统](./01_SpawnSystem.md)
2. [WaveSpawnSystem - 波次生成系统](./02_WaveSpawnSystem.md)
3. [ProgressionSystem - 进度系统](./03_ProgressionSystem.md)
4. [DamageCalculationService - 伤害计算服务](./04_DamageCalculationService.md)
5. [EffectService - 特效服务](./05_EffectService.md)
6. [GameFlowSystem - 游戏流程系统](./06_GameFlowSystem.md)

---

**文档版本**: v1.0
**最后更新**: 2025-12-25
**作者**: 架构设计团队
