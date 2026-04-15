# FeatureSystem 三段生命周期保留与模板化 Handler 改造计划

  ## 摘要

  保留 Activated -> Execute -> Ended，但重新定义语义：Activated 表示一次运行开始，Execute 表示真正执行效果并返回结果，Ended 表示一次运行结束与清理。同步技能可同帧走完三段；异步/引导/持续
  能力未来可在 Execute 后延迟 Ended。同时将 AbilityConfig 改为显式区分“技能信息”和“执行器选择”，让多个技能模板复用同一个 FeatureHandlerId。

  ## 核心语义

  - OnActivated(FeatureContext)：只做本次运行开始逻辑，例如激活态、锁重入、前摇表现、临时状态、运行上下文初始化；不在这里造成核心效果。
  - OnExecute(FeatureContext)：唯一效果入口，例如伤害、生成投射物、应用 Buff、位移、物品效果；返回值写入 FeatureContext.ExecuteResult。
  - OnEnded(FeatureContext, FeatureEndReason)：本次运行结束清理，例如解除状态、停止特效/计时器、释放订阅、记录完成/取消/中断。
  - Granted/Removed 仍表示 Feature 挂载与移除；Enabled/Disabled 仍表示启停，不与一次运行混用。

  - 调整 FeatureSystem 执行 API：保留 OnFeatureActivated(ctx)、OnFeatureEnded(ctx, reason)，并明确 OnFeatureActivated 内顺序为 FeatureIsActive=true -> OnActivated -> Feature.Activated事
    件 -> OnExecute -> ExecuteResult -> Feature.Executed事件 -> ActivationCount++。
  - AbilitySystem 对同步技能仍在执行后立即调用 OnFeatureEnded(ctx, Completed)；后续异步能力可由返回的执行句柄或专门 API 延迟结束。
  - AbilityConfig 新增导出字段 FeatureHandlerId，映射 DataKey.FeatureHandlerId；Name 是技能名，FeatureGroupId 暂时保留为技能展示分组，不再作为主要执行器索引。
  - 移除 AbilityConfig 中的执行器 ID 推导入口；AbilityConfig.FeatureHandlerId 必须显式填写，不再由技能分组和技能名称组合出执行器 ID。
  - 移除或弱化 IFeatureHandler.FeatureGroup 与 FeatureHandlerRegistry 分组索引；运行时查找只根据 FeatureHandlerId。如果测试面板需要分组，使用 AbilityConfig.FeatureGroupId。
  - Ability 子域不再保留专属 FeatureHandler 基类；通用 Handler 直接实现 IFeatureHandler，并从 FeatureContext.ActivationData 读取上下文。
  - 更新 CircleDamage 等示例：技能 Handler 直接实现 IFeatureHandler.OnExecute；新模板型 AOE Handler 通过读取 ctx.Feature.Data 支持多个技能复用。
  - 更新 addons/resources_spreadsheet_view/column_labels_zh.gd，至少补充 FeatureHandlerId、FeatureGroupId、后续如新增则补充 FeatureEndReason / FeatureExecutionMode 中文列名。

  ## 数据与配置目标

  - 技能资源目标结构：

  Name = "圆环伤害"
  FeatureGroupId = "技能.被动"
  FeatureHandlerId = "Handler.Ability.AreaDamage"
  AbilityDamage = 10
  AbilityEffectRadius = 500
  EffectScene = ...

  - Name 和 FeatureGroupId 只描述技能本身与展示分类。
  - FeatureHandlerId 决定执行哪个代码处理器。
  - 多个技能可共享同一 FeatureHandlerId，通过各自 DataKey 参数产生不同效果。

  ## 文档与 Skill 同步

  - 更新 Docs/框架/项目索引.md：修正 AbilityConfig、FeatureSystem、IFeatureHandler、FeatureHandlerRegistry 描述。
  - 更新 Src/ECS/Base/System/FeatureSystem/README.md 和 Src/ECS/Base/System/AbilitySystem/README.md：明确三段生命周期用途、同步/异步能力差异、模板 Handler 写法。
  - 更新 .codex/skills/feature-system/SKILL.md：禁止把核心效果写在 OnActivated，推荐写在 OnExecute。
  - 更新 .codex/skills/ability-system/SKILL.md：要求显式填写 FeatureHandlerId，缺失或未注册对应处理器时添加技能失败。

  ## 测试计划

  - dotnet build 编译通过。
  - CircleDamageConfig.tres 显式配置 FeatureHandlerId 后，周期触发正常造成伤害并发出 Ability.Executed。
  - 新建或调整一个测试技能，与 CircleDamage 使用同一 FeatureHandlerId，但不同 Name、半径、伤害、特效，确认模板复用成功。
  - 验证 FeatureIsActive 在同步技能执行期间置 true，结束后恢复 false。
  - 验证禁用技能无法执行，移除技能仍正确触发 OnRemoved 并回滚 Modifier。
  - 验证测试面板/数据表格中 FeatureHandlerId 和 FeatureGroupId 中文列头显示正确。
