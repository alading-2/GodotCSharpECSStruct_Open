# AI Skill 与规则体系重构（Godot C# ECS）

## 0. 结论先行

**结论**：Skill 不能完全替代你现在的 Rule 文档和项目索引，但非常适合做“第二层上下文系统”，把超长规则拆成可按需加载的能力模块。

推荐目标架构：

1. **Rule（始终生效）**：保留少量不可违反的硬约束。
2. **项目索引（导航层）**：保留文档入口和架构地图。
3. **Skills（按需加载）**：承接场景化流程、模板、示例和检查清单。

也就是：**Rule 管底线，索引管定位，Skill 管执行。**

---

## 1. Skill 是什么（结合公开文档的共性）

从当前公开资料（Anthropic/Claude Agent Skills 体系）看，Skill 的核心是：

- 它是一个**文件系统中的可复用能力包**（不是一次性 prompt）。
- 包里通常有：
  - 元数据（name/description 等）
  - 主说明（SKILL.md）
  - 可选参考资料（reference/examples）
  - 可选脚本（把稳定步骤程序化）
- Agent 会根据请求语义**自动触发**或手动触发（类似 `/skill-name`）。

### 1.2 Agent Skills 官方机制（agentskills.io）

按官方定义，Skill 的运行过程是三段式：

1. **Discovery**：启动时仅加载 `name` 和 `description`，用于判断何时相关。
2. **Activation**：任务命中后，再把完整 `SKILL.md` 加载进上下文。
3. **Execution**：按 `SKILL.md` 执行，并按需读取附带资源/脚本。

这也是 Skill 能显著降低上下文开销的根本原因：**常驻的是简要描述，不是全部细节**。

### 1.3 官方推荐的 Skill 目录结构

```text
my-skill/
├── SKILL.md      # 必需：元数据 + 指令正文
├── scripts/      # 可选：可执行脚本
├── references/   # 可选：参考文档
└── assets/       # 可选：模板/资源
```

对本项目的实践含义：

- `SKILL.md` 负责“任务流程与约束”。
- `references/` 负责“链接到项目索引中的关键文档”。
- `scripts/` 负责“稳定可程序化的检查”（如 `_Process` 中 `new/LINQ` 扫描）。

### 1.4 Skill 的关键价值

1. **渐进式上下文加载**
   - 先只加载轻量描述，命中后再读具体内容。
   - 比“把所有规则塞进 always-on 大文档”更省上下文。

2. **把重复提示词产品化**
   - 例如“做技能开发时必须走 TryTrigger + CastContext + ResponseContext”这类固定流程，可变成 Skill。

3. **可组合**
   - 一个任务可触发多个 Skill（如 Ability + TargetSelector + DamageSystem）。

4. **把不稳定推理变成稳定执行**
   - 复杂且确定的步骤可以放脚本（校验、生成、扫描），减少模型自由发挥带来的漂移。

---

## 2. Skill 能否替代 Rule 和项目索引？

## 2.1 不能完全替代 Rule（原因）

Rule 的定位是“系统级硬约束”，例如你当前这些：

- 必须中文回复
- `_Process` 禁止 new/LINQ
- 禁止直接 GD.Load/ResourceLoader.Load
- 禁止手写技能冷却、范围检测，必须走既有系统

这类约束要**默认始终生效**，不应依赖“是否触发某个 Skill”。

> 所以：Rule 适合保留“短、小、硬、不可协商”的底线规范。

## 2.2 不能完全替代项目索引（原因）

项目索引是“知识地图”，核心价值是：

- 新人/AI快速定位文档与源码入口
- 形成统一术语与边界（Entity/Data/Event/System/UI）
- 修改架构时有统一入口可更新

Skill 是“任务执行单元”，不是“全局信息架构目录”。

> 所以：索引仍然必须保留，并且持续维护。

## 2.3 Skill 最适合替代什么

Skill 最适合替代的是你 Rule 文档里“越来越长的操作说明段落”：

- 详细示例
- 分支流程
- 常见错误与修复
- 场景化 Checklist

这些内容从 always-on 规则中剥离后，能明显降低上下文负担。

---

## 3. 对当前 Godot C# ECS 项目的直接帮助

结合你项目现状（规则很强、体系完整、文档多），Skill 的收益会很高：

1. **降低大规则文档噪音**
   - 当前 rules 已覆盖 Timer/TargetSelector/Data/Event/ObjectPool/Ability/Damage/UI 等大量细节。
   - 将细节迁移到专题 Skill 后，常驻规则可以更短。

2. **提升“任务-规范”命中率**
   - 做技能功能时自动命中 Ability Skill；做对象池相关时命中 ObjectPool Skill。

3. **减少架构违规**
   - 可在 Skill 中写“反例对照 + 修正路径”，比只写一句“禁止”更能防错。

4. **支持渐进演进**
   - 你不需要一次重写全部规则；可先把高频领域（Ability、Damage、UI Bind）做成 Skill。

---

## 4. 建议的三层治理模型（适配你现在的体系）

## 4.1 第 1 层：Always-on Rule（只留 10~20 条硬约束）

只保留如下类型：

- 语言/交互约束（中文回复）
- 性能红线（`_Process` 禁止 new/LINQ）
- 架构红线（EntityManager 生命周期、Data 单一真相、EventBus 通信）
- 资源红线（禁止硬编码路径加载）
- 维护纪律（新增功能必须更新项目索引）

## 4.2 第 2 层：项目索引（稳定导航）

继续维护 `Docs/框架/项目索引.md`，并新增“AI Skill 体系”入口。

## 4.3 第 3 层：Skills（任务化能力包）

建议首批拆分：

1. `skill-ability-feature`
   - 覆盖 TryTrigger 入口、CastContext、TargetSelection、Cooldown/Charge、返回值读取。

2. `skill-damage-pipeline`
   - 覆盖 DamageInfo 构建、DamageService 调用、禁止直接改 HP、调试点。

3. `skill-ui-bind-entity`
   - 覆盖 UIBase Bind 模式、Entity.Events 监听、禁止全局事件过滤。

4. `skill-resource-loading`
   - 覆盖 ResourceManagement 分类加载、禁止 GD.Load 字符串路径。

5. `skill-ecs-component-pattern`
   - 覆盖“组件无业务状态字段”、DataKey 使用、事件通信优先级。

---

## 5. 迁移路线（低风险、可回滚）

## 阶段 A：规则瘦身（1~2 天）

- 从现有 rules 中抽离“详细示例和长流程段落”。
- 仅保留硬约束 + 指向技能/文档的链接。

## 阶段 B：高频 Skill 落地（3~5 天）

优先做 3 个高频：Ability、Damage、UI。

每个 Skill 统一结构：

- 何时触发（适用场景）
- 必做步骤（最短闭环）
- 禁止事项（反例）
- 验收清单（自检）
- 参考链接（项目索引中的关键文档）

## 阶段 C：脚本化校验（可选）

将确定性检查脚本化（例如扫描 `_Process` 中 `new` / LINQ，扫描 `GD.Load("res://`)）。

## 阶段 D：评估指标

每两周看 4 个指标：

- 架构违规次数
- 单任务平均交互轮次
- AI 首次产出可用率
- 文档维护耗时

---

## 6. 你这个项目的“替代判断”

如果你的问题是：

> “Skill 能否替代现在的规则和项目索引？”

我的建议是：

1. **不做完全替代**。
2. **做分层重构**：Rule 精简 + 索引保留 + Skill 承接细节。
3. **先迁移高频模块**（Ability/Damage/UI），用结果验证，再全量推广。

---

## 7. 最小可执行模板（可直接套用）

下面是一个可落地的 Skill 说明模板（概念模板，按你所用工具链语法微调）：

```md
---
name: ability-feature-implementation
description: 实现或修改技能功能时使用。适用于冷却、充能、目标选择、触发链路相关需求。
---

# 目标
在不破坏现有 ECS 架构的前提下完成技能功能。

# 必做步骤
1. 先检查是否已有 Ability 组件可复用（Cooldown/Charge/Trigger）。
2. 统一走 TryTrigger 事件入口，不直接调用执行逻辑。
3. 目标选择统一走 TargetSelector（禁止手写范围查询）。
4. 结果通过 CastContext.ResponseContext 返回。
5. 新增/修改后更新 Docs/框架/项目索引.md。

# 禁止事项
- 禁止手写冷却计时逻辑。
- 禁止绕过 EventBus 直接跨组件调用。
- 禁止在 Component 私有字段存业务状态。

# 自检清单
- [ ] 能通过 TryTrigger 路径完整触发
- [ ] DataKey 使用正确
- [ ] 无新增架构红线违规
```

---

## 8. 最后的建议

你现在的方向是对的：**规则文档写得越长，边际收益会下降**。Skill 的真正价值不是“替代全部文档”，而是把高频操作知识从“常驻大脑负担”变成“按需调用能力”。

对你这个 Godot C# ECS 项目，最优策略是：

- **保留短规则（底线）**
- **保留索引（地图）**
- **引入 Skills（执行）**

这样既能守住架构一致性，又能让 AI 在复杂任务上更稳定、更省上下文。

---

## 9. 当前项目的落地策略（更新）

结合 agentskills.io 的规范，你的项目建议继续沿用并强化以下策略：

1. **保留项目索引作为真地图**
   - `Docs/框架/项目索引.md` 负责人类可读的完整导航。
2. **新增/使用 `project-index` Skill 作为 AI 地图入口**
   - AI 先命中 `@project-index`，再跳转到具体模块 Skill。
3. **所有模块 Skill 必须带“关键文件路径”段落**
   - 避免 Skill 变成抽象口号，确保能直接定位代码。
4. **框架改动必须同步改 Skill**
   - 规则层已加入硬约束，防止 Skill 与代码脱节。

简化理解：

- **项目索引** = 总地图（稳定）
- **project-index Skill** = 导航助手（按需加载）
- **模块 Skill** = 施工 SOP（可执行）
