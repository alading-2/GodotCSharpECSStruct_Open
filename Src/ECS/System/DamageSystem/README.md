# 伤害系统详解 (Damage System Documentation)

## 1. 系统概览 (Overview)

伤害系统采用 **Pipeline (管道)** 模式，将复杂的伤害计算逻辑拆分为多个独立的 **Processor (处理器)**。每个处理器由 `DamageService` 按固定顺序调用，对 `DamageInfo` 上下文进行修改。

- **核心服务**: [DamageService](./DamageService.cs)
- **上下文**: [DamageInfo](./DamageInfo.cs)
- **接口定义**: [IDamageProcessor](./IDamageProcessor.cs)

---

## 2. 伤害处理流程表 (Pipeline Flow)

> **点击类名可直接跳转至代码文件**

| 序 | 处理器 (Processor) | 阶段 | 核心职责 (Core Responsibility) |
|:--:|:---|:---|:---|
| 1 | [BaseDamageProcessor](./Processors/BaseDamageProcessor.cs) | **初始化** | 确定基础面板伤害。<br>`FinalDamage = BaseDamage` |
| 2 | [CritProcessor](./Processors/CritProcessor.cs) | **输出修正** | 判定暴击。若暴击：<br>`FinalDamage *= CritDamageMultiplier` |
| 3 | [DodgeProcessor](./Processors/DodgeProcessor.cs) | **生存判定** | 判定闪避。若闪避成功：<br>`IsDodged = true`, `FinalDamage = 0` (后续流程部分跳过) |
| 4 | [ShieldProcessor](./Processors/ShieldProcessor.cs) | **护盾抵扣** | 优先扣除护盾。<br>**注意**：护盾承受的是**原始伤害**（无视护甲）。 |
| 5 | [DefenseProcessor](./Processors/DefenseProcessor.cs) | **受击减免** | 计算护甲/魔抗带来的减伤。<br>公式：`Reduction = Armor / (Armor + Constant)` |
| 6 | [DamageTakenAmplificationProcessor](./Processors/DamageTakenAmplificationProcessor.cs) | **伤害增幅** | 应用受击者的易伤/减伤修正（如“受到伤害+20%”）。<br>`FinalDamage *= DamageTakenMultiplier` |
| 7 | [FlatReductionProcessor](./Processors/FlatReductionProcessor.cs) | **受击减免** | 应用固定数值减伤（如“格挡 5 点伤害”）。<br>`FinalDamage = Max(0, FinalDamage - FlatReduction)` |
| 8 | [HealthExecutionProcessor](./Processors/HealthExecutionProcessor.cs) | **最终结算** | 实际扣除目标生命值。<br>调用 `HealthComponent.ModifyHealth()` |
| 9 | [LifestealProcessor](./Processors/LifestealProcessor.cs) | **后处理** | 计算吸血逻辑。<br>基于实际造成伤害治疗攻击者。 |
| 10 | [StatisticsProcessor](./Processors/StatisticsProcessor.cs) | **后处理** | 记录伤害统计数据。<br>更新 `DamageDealt` 等统计项。 |

---

## 3. 核心数据结构 (Data Structures)

### 3.1 [DamageInfo](./DamageInfo.cs)
承载单次伤害生命周期的所有信息。
- `Attacker`: 伤害来源实体（可能是子弹、陷阱）。
- `Instigator`: **真正施法者**（IUnit），用于伤害归属统计。
- `Victim`:受击者实体。
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
---

# 详细组件解析 (Detailed Component Analysis)

## 1. 架构详解 (Architecture)
本系统采用 **管道模式 (Pipeline Pattern)** 实现。整个伤害计算过程被视为一条流水线，`DamageInfo` 是流经管道的数据包，而各个 `Processor` 是流水线上的工位。

### 设计优势
- **解耦**: 暴击、防御、护盾等逻辑完全分离，互不依赖。
- **灵活**: 可以轻松插入新的逻辑（如“斩杀”、“元素反应”）而无需修改现有代码，只需注册新的 Processor。
- **可测试**: 每个 Processor 都可以单独进行单元测试。

## 2. 核心类说明 (Core Classes)

### DamageInfo (伤害上下文)
- **生命周期**: 从 `DamageService.Process(info)` 开始，直到管道执行完毕。
- **关键属性**:
  - `Id`: 唯一标识，用于日志追踪。
  - `Attacker` vs `Instigator`: `Attacker` 是直接来源（如子弹节点），`Instigator` 是逻辑来源（如发射子弹的玩家）。统计伤害时应使用 `Instigator`。
  - `Logs`: 记录了伤害计算过程中的关键变化，用于调试。

### DamageService (核心服务)
- **单例模式**: `DamageService.Instance` 全局唯一。
- **职责**:
  - 维护处理器列表 `_processors`，并按 `Priority` 排序。
  - 提供 `Process(DamageInfo info)` 入口方法。
  - 自动跳过无效的伤害处理（如 `Victim` 无效）。

### IUnit (战斗单位接口)
- 任何能够造成伤害并需要统计归属的实体（Player, Enemy）都应实现此接口。
- 主要用于区分阵营 (`FactionId`) 和访问数据 (`Data` 属性，通常通过扩展方法或 IEntity 接口访问)。

## 3. 处理器逻辑详解 (Processor Implementations)

### Stage 1: 输出修正 (Outgoing) - 决定理论伤害

#### [BaseDamageProcessor](./Processors/BaseDamageProcessor.cs) (P:0)
*   **逻辑**: 初始化 `FinalDamage`。如果 `BaseDamage` 未设置，尝试从 `Attacker` 读取（当前主要是兜底逻辑）。
*   **目的**: 确保伤害计算有一个非零的起始值。

#### [DamageAmplificationProcessor](./Processors/DamageAmplificationProcessor.cs) (P:10)
*   **逻辑**: 读取 `Instigator` 的 `DataKey.Damage` (作为百分比加成) 修正伤害。
*   **公式**: `FinalDamage *= (1 + Damage% / 100)`。
*   **扩展**: 预留了对近战/远程 (`DamageTags`) 的特定加成接口。

#### [CritProcessor](./Processors/CritProcessor.cs) (P:20)
*   **逻辑**: 读取 `Instigator` 的 `CritChance` 进行概率判定。
*   **效果**: 若暴击，`FinalDamage *= CritDamageMultiplier` (默认读取 `DataKey.CritDamage`，若无则默认 1.5倍)。
*   **标记**: 设置 `IsCritical = true`。

### Stage 2: 生存判定 (Survival) - 决定是否命中

#### [DodgeProcessor](./Processors/DodgeProcessor.cs) (P:100)
*   **逻辑**: 读取 `Victim` 的 `DodgeChance` (上限 60%)。
*   **效果**: 若闪避成功，`Result: FinalDamage = 0`, `IsDodged = true`。
*   **影响**: 后续的 Shield, Defense 等处理器会检测 `IsDodged` 并直接跳过。

### Stage 3: 护盾抵扣 (Shield) - 优先消耗

#### [ShieldProcessor](./Processors/ShieldProcessor.cs) (P:200)
*   **逻辑**: 读取 `Victim` 的 `DataKey.Shield`。
*   **机制**: 护盾承受 **原始伤害** (在护甲减免之前)。这是为了防止高护甲角色配合护盾变得过于坚不可摧。
*   **效果**: 扣除护盾值。若护盾不足，剩余伤害 (`Overflow`) 继续流转；若护盾足够，`FinalDamage = 0`，但可能不标记为 Dodged。

### Stage 4: 受击减免 (Mitigation) - 最终减伤

#### [DefenseProcessor](./Processors/DefenseProcessor.cs) (P:300)
*   **逻辑**: 读取 `Victim` 的 `Armor`。
*   **公式**: `Reduction = Armor / (Armor + 15)` (参考 Brotato 经典公式)。
*   **限制**: 最大减免 90%。
*   **排除**: `DamageType.True` (真实) 伤害不触发此减伤。

#### [DamageTakenAmplificationProcessor](./Processors/DamageTakenAmplificationProcessor.cs) (P:310)
*   **逻辑**: 读取 `Victim` 的 `DataKey.DamageTakenMultiplier`。
*   **效果**: 直接乘算 `FinalDamage` (用于实现易伤 Debuff 或减伤 Buff)。

#### [FlatReductionProcessor](./Processors/FlatReductionProcessor.cs) (P:320)
*   **逻辑**: (预留) 固定数值减伤。
*   **现状**: 目前代码作为扩展点存在，支持实现类似“格挡 5 点伤害”的机制。

### Stage 5: 最终结算 (Execution) - 产生后果

#### [HealthExecutionProcessor](./Processors/HealthExecutionProcessor.cs) (P:500)
*   **逻辑**: 获取 `Victim` 的 `HealthComponent`。
*   **操作**: 调用 `healthComp.ModifyHealth(-FinalDamage)`。
*   **注意**: 即使 `FinalDamage` 为 0，也可能触发 `ModifyHealth(0)`，视具体实现而定是否触发受伤事件。

#### [LifestealProcessor](./Processors/LifestealProcessor.cs) (P:600)
*   **逻辑**: 读取 `Instigator` 的 `LifeSteal` 属性。
*   **机制**: 概率触发 (DataKey.LifeSteal 作为概率值)。
*   **效果**: 触发成功则对 `Instigator` 进行 +1 生命值的治疗 (调用 `ModifyHealth(1)`)。

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
