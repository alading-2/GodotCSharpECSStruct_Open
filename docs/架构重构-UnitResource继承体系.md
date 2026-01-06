# 架构重构：UnitResource 继承体系

**日期**: 2025-12-31  
**版本**: v1.0

---

## 📋 重构概述

### 重构目标

将 Player 和 Enemy 的共同属性提取到基类 `UnitResource`，建立清晰的继承体系。

### 重构前后对比

#### 重构前

```
Data/Resources/
├── Player/
│   └── PlayerResource.cs    # 独立定义所有属性
└── Enemy/
    └── EnemyResource.cs     # 独立定义所有属性（重复）
```

**问题**：

- MaxHp、Speed、Damage 等属性在两个类中重复定义
- 违反 DRY 原则
- 新增共同属性需要修改两处

#### 重构后

```
Data/Resources/Unit/
├── UnitResource.cs          # 抽象基类（共同属性）
├── PlayerResource.cs        # 玩家特有属性
├── EnemyResource.cs         # 敌人特有属性
├── Player/
│   └── DefaultPlayer.tres
└── Enemy/
    ├── 豺狼人/
    │   └── 豺狼人.tres
    └── 鱼人/
        └── 鱼人.tres
```

**优势**：

- ✅ 共同属性统一管理
- ✅ 符合面向对象设计原则
- ✅ 易于扩展新的 Unit 类型（如 Boss、NPC）
- ✅ 目录结构更清晰

---

## 🏗️ 新的类层次结构

### 1. UnitResource（抽象基类）

```csharp
[GlobalClass]
public abstract partial class UnitResource : Resource
{
    [Export] public string UnitName { get; set; } = "";
    [Export] public float MaxHp { get; set; } = 100;
    [Export] public float Speed { get; set; } = 100;
    [Export] public float Damage { get; set; } = 10;
    [Export] public PackedScene UnitScene { get; set; }
}
```

**职责**：定义所有 Unit 的共同属性。

### 2. PlayerResource（玩家资源）

```csharp
[GlobalClass]
public partial class PlayerResource : UnitResource
{
    [Export] public int InitialExp { get; set; } = 0;
    [Export] public int InitialLevel { get; set; } = 1;
    [Export] public int InitialWeaponSlots { get; set; } = 2;
    [Export] public float PickupRange { get; set; } = 100;
}
```

**职责**：定义玩家特有属性（升级、装备、拾取）。

### 3. EnemyResource（敌人资源）

```csharp
[GlobalClass]
public partial class EnemyResource : UnitResource
{
    [Export] public SpawnPositionStrategy DefaultStrategy { get; set; }
    [Export] public int ExpReward { get; set; } = 5;
    [Export] public float ContactDamage { get; set; } = 10;
}
```

**职责**：定义敌人特有属性（生成策略、经验奖励、碰撞伤害）。

---

## 🔄 数据迁移

### 属性映射表

| 旧属性名 (EnemyResource) | 新属性名 (UnitResource) | 说明       |
| ------------------------ | ----------------------- | ---------- |
| `EnemyName`              | `UnitName`              | 统一命名   |
| `MaxHp`                  | `MaxHp`                 | 保持不变   |
| `Speed`                  | `Speed`                 | 保持不变   |
| `Damage`                 | `Damage`                | 保持不变   |
| `EnemyScene`             | `UnitScene`             | 统一命名   |
| `Damage` (碰撞)          | `ContactDamage`         | 语义更明确 |

### 新增属性

**EnemyResource**：

- `ContactDamage`：碰撞伤害（原 `Damage` 字段的语义）

**PlayerResource**：

- `InitialExp`：初始经验值
- `InitialLevel`：初始等级
- `InitialWeaponSlots`：初始武器槽位数
- `PickupRange`：拾取范围

---

## 📝 迁移清单

### 已完成

- ✅ 创建 `UnitResource.cs` 基类
- ✅ 重构 `PlayerResource.cs` 继承基类
- ✅ 重构 `EnemyResource.cs` 继承基类
- ✅ 迁移敌人配置文件到新路径
  - `豺狼人.tres` → `Data/Resources/Unit/Enemy/豺狼人/豺狼人.tres`
  - `鱼人.tres` → `Data/Resources/Unit/Enemy/鱼人/鱼人.tres`
- ✅ 创建默认玩家配置 `DefaultPlayer.tres`
- ✅ 更新 `EntityManager.InjectResourceData()` 支持新类型
- ✅ 更新测试场景引用路径
- ✅ 删除旧的资源文件

### 待手动处理

- ⚠️ **Godot 编辑器中的引用**：

  - 打开 Godot 编辑器
  - 检查所有场景中的 `EnemyResource` 引用
  - 如果出现 "Missing Resource" 警告，重新指定路径

- ⚠️ **旧目录清理**：
  - 删除 `Data/Resources/Enemy/` 目录（如果为空）
  - 删除 `Data/Resources/PLayer/` 目录（如果为空）

---

## 🎯 使用示例

### 1. 在编辑器中创建新的敌人配置

1. 右键 `Data/Resources/Unit/Enemy/` → 新建资源
2. 选择 `EnemyResource`
3. 配置属性：
   ```
   UnitName: "新敌人"
   MaxHp: 50
   Speed: 120
   Damage: 15
   ContactDamage: 12
   ExpReward: 10
   DefaultStrategy: Rectangle
   UnitScene: [选择敌人场景]
   ```

### 2. 在代码中加载资源

```csharp
// 加载敌人资源
var enemy = GD.Load<EnemyResource>("res://Data/Resources/Unit/Enemy/豺狼人/豺狼人.tres");

// 加载玩家资源
var player = GD.Load<PlayerResource>("res://Data/Resources/Unit/Player/DefaultPlayer.tres");

// 访问共同属性（来自 UnitResource）
GD.Print($"单位名称: {enemy.UnitName}");
GD.Print($"最大生命: {enemy.MaxHp}");

// 访问特有属性
GD.Print($"经验奖励: {enemy.ExpReward}");
GD.Print($"拾取范围: {player.PickupRange}");
```

### 3. EntityManager 自动注入

```csharp
// EntityManager 会自动识别类型并注入所有属性
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Resource = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = pos
});

// Data 容器中会包含所有属性（基类 + 子类）
var data = enemy.GetData();
GD.Print(data.Get<string>("UnitName"));      // 来自 UnitResource
GD.Print(data.Get<float>("MaxHp"));          // 来自 UnitResource
GD.Print(data.Get<int>("ExpReward"));        // 来自 EnemyResource
```

---

## 🔮 未来扩展

### 新增 Unit 类型示例

#### Boss 资源

```csharp
[GlobalClass]
public partial class BossResource : UnitResource
{
    [Export] public int PhaseCount { get; set; } = 3;
    [Export] public float[] PhaseHpThresholds { get; set; }
    [Export] public PackedScene[] PhaseSkills { get; set; }
}
```

#### NPC 资源

```csharp
[GlobalClass]
public partial class NpcResource : UnitResource
{
    [Export] public string DialogueKey { get; set; }
    [Export] public bool IsQuestGiver { get; set; }
    [Export] public string[] ShopItems { get; set; }
}
```

---

## ⚠️ 注意事项

### 1. 脚本层仍然分离

**重要**：虽然 Resource 使用继承，但 **Player.cs 和 Enemy.cs 脚本仍然保持分离**。

```
Src/ECS/Entity/Unit/
├── Player.tscn
├── Player.cs        # 玩家逻辑（输入、升级）
├── Enemy.tscn
└── Enemy.cs         # 敌人逻辑（AI、对象池）
```

**原因**：

- Player 和 Enemy 的行为本质不同（参考 `Entity架构最终方案.md`）
- 共享逻辑通过 Component 实现，而非继承

### 2. 属性命名约定

- **基类属性**：通用命名（`UnitName`、`MaxHp`）
- **子类属性**：语义明确（`ContactDamage`、`PickupRange`）
- **避免**：`EnemyXxx`、`PlayerXxx` 前缀（已在类型中体现）

### 3. EntityManager 兼容性

`EntityManager.InjectResourceData()` 使用反射自动注入，**无需修改**即可支持新的 Resource 类型。

---

## 📚 相关文档

- `Entity架构最终方案.md`：Entity 脚本分离的理念
- `EntityManager设计说明.md`：数据注入机制
- `项目规则.md`：命名规范和架构原则

---

**重构完成！** 🎉

现在你可以：

1. 在 Godot 编辑器中打开项目
2. 检查资源引用是否正常
3. 运行测试场景验证功能
