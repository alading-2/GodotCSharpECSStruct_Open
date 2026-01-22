# 伤害系统详解 (Damage System Documentation)

## 1. 系统概览 (Overview)

伤害系统采用 **Pipeline (管道)** 模式，将复杂的伤害计算逻辑拆分为多个独立的 **Processor (处理器)**。每个处理器由 `DamageService` 按固定顺序调用，对 `DamageInfo` 上下文进行修改。

- **核心服务**: [DamageService](./DamageService.cs)
- **上下文**: [DamageInfo](./DamageInfo.cs)
- **接口定义**: [IDamageProcessor](./IDamageProcessor.cs)

---

## 2. 伤害处理流程表 (Pipeline Flow)

> **点击类名可直接跳转至代码文件**

| 序 | 处理器 (Processor) | 优先级 | 阶段 | 核心职责 (Core Responsibility) |
|:--:|:---|:--:|:---|:---|
| 1 | [BaseDamageProcessor](./Processors/BaseDamageProcessor.cs) | 100 | **初始化与前置检查** | 初始化 `FinalDamage = BaseDamage`<br>检查死亡/无敌状态，可提前终止流程 |
| 2 | [DodgeProcessor](./Processors/DodgeProcessor.cs) | 200 | **生存判定** | 判定闪避。若闪避成功：<br>`IsDodged = true`, `IsEnd = true`, `FinalDamage = 0`<br>**注意**：真实伤害不可闪避 |
| 3 | [CritProcessor](./Processors/CritProcessor.cs) | 300 | **输出修正** | 判定暴击。若暴击：<br>`IsCritical = true`, `FinalDamage *= CritDamageMultiplier` |
| 4 | [ShieldProcessor](./Processors/ShieldProcessor.cs) | 400 | **护盾抵扣** | 优先扣除护盾（**TODO**）<br>护盾承受**原始伤害**（护甲减免前） |
| 5 | [DefenseProcessor](./Processors/DefenseProcessor.cs) | 500 | **受击减免** | 计算护甲减伤<br>公式：`multiplier = MyMath.CalculateArmorDamageMultiplier(Armor)` |
| 6 | [DamageTakenAmplificationProcessor](./Processors/DamageTakenAmplificationProcessor.cs) | 600 | **伤害增幅** | 应用受害者的易伤/减伤修正<br>`FinalDamage *= DamageTakenMultiplier` |
| 7 | [FlatReductionProcessor](./Processors/FlatReductionProcessor.cs) | 700 | **受击减免** | 固定数值减伤（**预留**）<br>如"格挡 5 点伤害" |
| 8 | [LifestealProcessor](./Processors/LifestealProcessor.cs) | 800 | **后处理** | 计算吸血逻辑<br>基于 `FinalDamage` 发送治疗请求事件 |
| 9 | [HealthExecutionProcessor](./Processors/HealthExecutionProcessor.cs) | 900 | **最终结算** | 实际扣除目标生命值<br>调用 `HealthComponent.ApplyDamage(info)`<br>**模拟模式**：不实际扣血 |
| 10 | [StatisticsProcessor](./Processors/StatisticsProcessor.cs) | 1000 | **后处理** | 记录伤害统计数据<br>遍历攻击链，累加 `TotalDamageDealt` 等 |

---

## 3. 核心数据结构 (Data Structures)

### 3.1 [DamageInfo](./DamageInfo.cs)
承载单次伤害生命周期的所有信息。
- `Attacker`: 伤害来源实体（可能是子弹、陷阱）。
- `Instigator`: **真正施法者**（IUnit），用于伤害归属统计。
- `Victim`:受害者实体。
- `FinalDamage`: 流转过程中的最终结算伤害值。

### 3.2 [IUnit](./IUnit.cs)
所有具备战斗属性的主体（玩家、敌人）必须实现的接口。
- `FactionId`: 用于区分阵营（友伤判定）。

---

## 4. 设计原则备忘

1.  **护盾优先于护甲**：护盾设计为“额外的血量”，直接承受未减免的伤害，避免高护甲角色配合护盾过于无解。
2.  **HealthComponent 职责单一化**：`HealthComponent` 不再包含 `TakeDamage` 逻辑，仅负责数值存储和变更事件 (`ModifyHealth`)。
3.  **Data 驱动**：所有数值（暴击率、护甲等）均通过 `entity.GetData()` 获取，支持动态 Buff 修改。

---

## 5. 伤害统计系统

### 5.1 统计模块

- **[StatisticsProcessor](./Processors/StatisticsProcessor.cs)** - 管道末端统计处理器，记录攻击方和受击方的伤害数据
- **[DamageStatisticsSystem](./DamageStatisticsSystem.cs)** - 波次统计重置系统，监听波次事件和击杀事件

### 5.2 统计 DataKey

| DataKey | 类型 | 说明 |
|---------|------|------|
| `TotalDamageTaken` | float | 累计受到的伤害 |
| `WaveDamageTaken` | float | 本波次受到的伤害 |
| `TotalDamageDealt` | float | 累计造成的伤害 |
| `WaveDamageDealt` | float | 本波次造成的伤害 |
| `HighestSingleDamage` | float | 单次最高伤害 |
| `TotalKills` | int | 累计击杀数 |
| `WaveKills` | int | 本波次击杀数 |
| `TotalHits` | int | 总命中次数 |
| `WaveHits` | int | 本波次命中次数 |
| `TotalCriticalHits` | int | 总暴击次数 |
| `WaveCriticalHits` | int | 本波次暴击次数 |

### 5.3 使用示例

```csharp
// 查询玩家统计数据
var player = EntityManager.GetEntitiesByType<Player>("Player").First();
float totalDamage = player.Data.Get<float>(DataKey.TotalDamageDealt);
int kills = player.Data.Get<int>(DataKey.TotalKills);
```

---
---

# 详细组件解析 (Detailed Component Analysis)

## 1. 架构详解 (Architecture)
本系统采用 **管道模式 (Pipeline Pattern)** 实现。整个伤害计算过程被视为一条流水线，`DamageInfo` 是流经管道的数据包，而各个 `Processor` 是流水线上的工位。

### 设计优势
- **解耦**: 暴击、防御、护盾等逻辑完全分离，互不依赖。
- **灵活**: 可以轻松插入新的逻辑（如"斩杀"、"元素反应"）而无需修改现有代码，只需注册新的 Processor。
- **可测试**: 每个 Processor 都可以单独进行单元测试。

### 重要设计约定

#### IsEnd 检查统一由主循环处理
**核心原则**：`DamageService.Process()` 的主循环已经在每个处理器执行后检查 `if (info.IsEnd) break;`，因此**处理器内部不需要检查 `IsEnd`**。

```csharp
// DamageService.cs
foreach (var processor in _processors)
{
    processor.Process(info);
    if (info.IsEnd) break;  // ✅ 统一在此检查
}
```

**各处理器的责任**：
- ✅ **应该检查**: `IsDodged`（某些逻辑需要在闪避后跳过）、`FinalDamage <= 0`（无效伤害）
- ❌ **不应检查**: `IsEnd`（主循环已处理，检查是冗余的）

**示例**：
```csharp
public void Process(DamageInfo info)
{
    // ✅ 正确：只检查本处理器关心的条件
    if (info.IsDodged || info.FinalDamage <= 0) return;
    
    // ❌ 错误：不要检查 IsEnd
    // if (info.IsEnd) return;  // 冗余！主循环已保证后续处理器不会被调用
    
    // 处理器逻辑...
}
```

## 2. 核心类说明 (Core Classes)

### DamageInfo (伤害上下文)
- **生命周期**: 从 `DamageService.Process(info)` 开始，直到管道执行完毕。
- **关键属性**:
  - `Id`: 唯一标识，用于日志追踪。
  - `Attacker`: 伤害来源节点（如子弹、陷阱 Area2D）。
    - 注意：这是**直接来源**，统计归属需通过 `EntityRelationshipManager.FindAncestorOfType<IUnit>(Attacker)` 向上查找。
  - `Victim`: 受害者实体（必须实现 IUnit）。
  - `FinalDamage`: 流转过程中的最终结算伤害值。
  - `IsEnd`: 标记伤害流程是否应提前终止（由主循环检查）。
  - `IsSimulation`: 模拟模式标记。若为 `true`，`HealthExecutionProcessor` 将跳过实际扣血，仅记录日志。
    - **用途**：伤害预测、技能说明面板（显示"预计造成 XXX 伤害"）等
  - `Logs`: 记录伤害计算过程中的关键变化（仅 DEBUG 模式），用于追踪和调试。

### DamageService (核心服务)
- **单例模式**: `DamageService.Instance` 全局唯一。
- **职责**:
  - 维护处理器列表 `_processors`，并按 `Priority` 排序。
  - 提供 `Process(DamageInfo info)` 入口方法。
  - 统一处理 `IsEnd` 检查，确保流程提前终止。
  - 自动跳过无效的伤害处理（如 `Victim` 无效）。

### IUnit (战斗单位接口)
- 任何能够造成伤害并需要统计归属的实体（Player, Enemy）都应实现此接口。
- 主要用于区分阵营 (`FactionId`) 和访问数据 (`Data` 属性，通常通过扩展方法或 IEntity 接口访问)。

## 3. 处理器逻辑详解 (Processor Implementations)

### Stage 1: 输出修正 (Outgoing) - 决定理论伤害

#### [BaseDamageProcessor](./Processors/BaseDamageProcessor.cs) (P:100)
*   **逻辑**: 初始化 `FinalDamage = BaseDamage`。执行前置检查（死亡、无敌、基础伤害 <= 0）。
*   **提前终止**: 若检查失败，设置 `IsEnd = true`, `FinalDamage = 0`。
*   **职责整合**: 合并了原 `PreDamageCheckProcessor` 的功能。

#### [CritProcessor](./Processors/CritProcessor.cs) (P:300)
*   **逻辑**: 查找攻击者 IUnit（通过 `FindAncestorOfType`），读取 `CritRate` 进行概率判定。
*   **效果**: 若暴击，`IsCritical = true`, `FinalDamage *= (CritDamage / 100)`。
*   **数据来源**: `DataKey.CritRate`（暴击率）, `DataKey.CritDamage`（暴击倍率，如 150）。

### Stage 2: 生存判定 (Survival) - 决定是否命中

#### [DodgeProcessor](./Processors/DodgeProcessor.cs) (P:200)
*   **逻辑**: 读取 `Victim` 的 `DodgeChance`。
*   **效果**: 若闪避成功，`FinalDamage = 0`, `IsDodged = true`, `IsEnd = true`，流程提前终止。
*   **特殊**: `DamageType.True` (真实伤害) **不可闪避**，直接跳过此判定。
*   **执行时机**: 在暴击判定之前，可提前终止后续无用计算，提升性能。

### Stage 3: 护盾抵扣 (Shield) - 优先消耗

#### [ShieldProcessor](./Processors/ShieldProcessor.cs) (P:200)
*   **逻辑**: 读取 `Victim` 的 `DataKey.Shield`。
*   **机制**: 护盾承受 **原始伤害** (在护甲减免之前)。这是为了防止高护甲角色配合护盾变得过于坚不可摧。
*   **效果**: 扣除护盾值。若护盾不足，剩余伤害 (`Overflow`) 继续流转；若护盾足够，`FinalDamage = 0`，但可能不标记为 Dodged。

### Stage 4: 受击减免 (Mitigation) - 最终减伤

#### [DefenseProcessor](./Processors/DefenseProcessor.cs) (P:500)
*   **逻辑**: 读取 `Victim` 的 `Armor`，调用 `MyMath.CalculateArmorDamageMultiplier(armor)`。
*   **效果**: 正护甲减伤，负护甲增伤。
*   **公式**: 参考 Brotato 经典公式（具体实现见 `MyMath` 工具类）。
*   **注意**: 当前代码未检查 `DamageType.True`（已注释），真实伤害仍受护甲影响。

#### [DamageTakenAmplificationProcessor](./Processors/DamageTakenAmplificationProcessor.cs) (P:310)
*   **逻辑**: 读取 `Victim` 的 `DataKey.DamageTakenMultiplier`。
*   **效果**: 直接乘算 `FinalDamage` (用于实现易伤 Debuff 或减伤 Buff)。

#### [FlatReductionProcessor](./Processors/FlatReductionProcessor.cs) (P:320)
*   **逻辑**: (预留) 固定数值减伤。
*   **现状**: 目前代码作为扩展点存在，支持实现类似“格挡 5 点伤害”的机制。

### Stage 5: 最终结算 (Execution) - 产生后果

#### [HealthExecutionProcessor](./Processors/HealthExecutionProcessor.cs) (P:500)
*   **逻辑**: 获取 `Victim` 的 `HealthComponent`。
*   **操作**: 分为两种模式：
    *   **正常执行**: 调用 `healthComp.ApplyDamage(info)` 实际扣血。
    *   **模拟模式** (`info.IsSimulation = true`): 仅记录日志，**不实际扣血**。用于伤害预测功能。
*   **注意**: 即使 `FinalDamage` 为 0，也根据具体实现决定是否触发受击事件。

#### [LifestealProcessor](./Processors/LifestealProcessor.cs) (P:800)
*   **逻辑**: 查找攻击者 IUnit，读取 `LifeSteal` 作为触发概率。
*   **机制**: 概率判定成功后，基于 `FinalDamage` 计算回血量（当前公式：`FinalDamage * (lifestealChance / 100)`）。
*   **效果**: 发送 `HealRequest` 事件到 IUnit（由 `HealthComponent` 处理）。
*   **执行时机**: 在 `HealthExecutionProcessor` 之前，确保获取到最终伤害值。

#### [StatisticsProcessor](./Processors/StatisticsProcessor.cs) (P:700)
*   **逻辑**: 记录伤害统计。
*   **用途**: 用于结算面板显示“造成总伤害”。

## 4. 扩展指南 (Extension Guide)

若需实现新的伤害逻辑 (例如：背刺伤害加成)：
1.  新建类实现 `IDamageProcessor`。
2.  设定合适的 `Priority` (例如希望在暴击前计算，可设为 15)。
3.  实现 `Process` 方法编写逻辑。
4.  在 `DamageService` 构造函数中注册 (或使用依赖注入/反射机制)。

```csharp
public class BackstabProcessor : IDamageProcessor
{
    public int Priority => 15;
    public void Process(DamageInfo info) { /* ... */ }
}
```
