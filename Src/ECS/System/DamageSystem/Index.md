# 伤害系统设计与流程 (Damage System & Pipeline)

## 1. 系统概览 (Overview)

伤害系统采用 **Pipeline (管道)** 模式，将复杂的伤害计算逻辑拆分为多个独立的 **Processor (处理器)**。每个处理器由 `DamageService` 按固定顺序调用，对 `DamageInfo` 上下文进行修改。

- **核心服务**: [DamageService](../../../../Src/ECS/System/DamageSystem/DamageService.cs)
- **上下文**: [DamageInfo](../../../../Src/ECS/System/DamageSystem/DamageInfo.cs)
- **接口定义**: [IDamageProcessor](../../../../Src/ECS/System/DamageSystem/IDamageProcessor.cs)

---

## 2. 伤害处理流程表 (Pipeline Flow)

> **点击类名可直接跳转至代码文件**

| 序 | 处理器 (Processor) | 阶段 | 核心职责 (Core Responsibility) |
|:--:|:---|:---|:---|
| 1 | [BaseDamageProcessor](../../../../Src/ECS/System/DamageSystem/Processors/BaseDamageProcessor.cs) | **初始化** | 确定基础面板伤害。<br>`FinalDamage = BaseDamage` |
| 2 | [DamageAmplificationProcessor](../../../../Src/ECS/System/DamageSystem/Processors/DamageAmplificationProcessor.cs) | **输出修正** | 应用攻击者的百分比增伤（如“伤害+10%”）。<br>`FinalDamage *= (1 + DamageMultiplier)` |
| 3 | [CritProcessor](../../../../Src/ECS/System/DamageSystem/Processors/CritProcessor.cs) | **输出修正** | 判定暴击。若暴击：<br>`FinalDamage *= CritDamageMultiplier` |
| 4 | [DodgeProcessor](../../../../Src/ECS/System/DamageSystem/Processors/DodgeProcessor.cs) | **生存判定** | 判定闪避。若闪避成功：<br>`IsDodged = true`, `FinalDamage = 0` (后续流程部分跳过) |
| 5 | [ShieldProcessor](../../../../Src/ECS/System/DamageSystem/Processors/ShieldProcessor.cs) | **护盾抵扣** | 优先扣除护盾。<br>**注意**：护盾承受的是**原始伤害**（无视护甲）。 |
| 6 | [DefenseProcessor](../../../../Src/ECS/System/DamageSystem/Processors/DefenseProcessor.cs) | **受击减免** | 计算护甲/魔抗带来的减伤。<br>公式：`Reduction = Armor / (Armor + Constant)` |
| 7 | [DamageTakenAmplificationProcessor](../../../../Src/ECS/System/DamageSystem/Processors/DamageTakenAmplificationProcessor.cs) | **受击减免** | 应用受击者的易伤/减伤修正（如“受到伤害+20%”）。<br>`FinalDamage *= DamageTakenMultiplier` |
| 8 | [FlatReductionProcessor](../../../../Src/ECS/System/DamageSystem/Processors/FlatReductionProcessor.cs) | **受击减免** | 应用固定数值减伤（如“格挡 5 点伤害”）。<br>`FinalDamage = Max(0, FinalDamage - FlatReduction)` |
| 9 | [HealthExecutionProcessor](../../../../Src/ECS/System/DamageSystem/Processors/HealthExecutionProcessor.cs) | **最终结算** | 实际扣除目标生命值。<br>调用 `HealthComponent.ModifyHealth()` |
| 10 | [LifestealProcessor](../../../../Src/ECS/System/DamageSystem/Processors/LifestealProcessor.cs) | **后处理** | 计算吸血逻辑。<br>基于实际造成伤害治疗攻击者。 |
| 11 | [StatisticsProcessor](../../../../Src/ECS/System/DamageSystem/Processors/StatisticsProcessor.cs) | **后处理** | 记录伤害统计数据。<br>更新 `DamageDealt` 等统计项。 |

---

## 3. 核心数据结构 (Data Structures)

### 3.1 [DamageInfo](../../../../Src/ECS/System/DamageSystem/DamageInfo.cs)
承载单次伤害生命周期的所有信息。
- `Attacker`: 伤害来源实体（可能是子弹、陷阱）。
- `Instigator`: **真正施法者**（IUnit），用于伤害归属统计。
- `Victim`:受击者实体。
- `FinalDamage`: 流转过程中的最终结算伤害值。

### 3.2 [IUnit](../../../../Src/ECS/System/DamageSystem/IUnit.cs)
所有具备战斗属性的主体（玩家、敌人）必须实现的接口。
- `FactionId`: 用于区分阵营（友伤判定）。

---

## 4. 设计原则备忘

1.  **护盾优先于护甲**：护盾设计为“额外的血量”，直接承受未减免的伤害，避免高护甲角色配合护盾过于无解。
2.  **HealthComponent 职责单一化**：`HealthComponent` 不再包含 `TakeDamage` 逻辑，仅负责数值存储和变更事件 (`ModifyHealth`)。
3.  **Data 驱动**：所有数值（暴击率、护甲等）均通过 `entity.GetData()` 获取，支持动态 Buff 修改。
