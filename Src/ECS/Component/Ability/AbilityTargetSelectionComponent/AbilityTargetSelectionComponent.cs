using Godot;
using System.Collections.Generic;

/// <summary>
/// 技能目标选择组件，只处理单位目标
/// <para>职责：作为技能施法流水线（Cast Pipeline）的核心环节，负责响应 SelectTargets 事件。</para>
/// <para>功能：根据技能的配置信息（如范围、几何形状、筛选规则等），利用 TargetSelector 检索有效目标并填充到 CastContext 中。</para>
/// <para>协作：由 AbilitySystem 触发，计算结果供后续的 AbilityEffectComponent 使用。</para>
/// </summary>
public partial class AbilityTargetSelectionComponent : Node, IComponent
{
    /// <summary>
    /// 日志工具，用于调试目标选择过程
    /// </summary>
    private static readonly Log _log = new(nameof(AbilityTargetSelectionComponent));

    /// <summary>
    /// 绑定的实体引用
    /// </summary>
    private IEntity? _entity;

    /// <summary>
    /// 组件注册回调：当组件被添加到实体时调用
    /// </summary>
    /// <param name="entity">所属的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            // 订阅实体层级的目标选择事件
            // 当 AbilitySystem 流程进行到“选择目标”阶段时，会抛出此事件
            _entity.Events.On<GameEventType.Ability.SelectTargetsEventData>(
                GameEventType.Ability.SelectTargets,
                OnSelectTargets
            );
            _log.Info($"目标选择组件已注册到: {entity.Name}");
        }
    }

    /// <summary>
    /// 组件注销回调：清理引用，防止内存泄漏
    /// </summary>
    public void OnComponentUnregistered()
    {
        _entity = null;
    }

    /// <summary>
    /// 处理目标选择事件的核心回调
    /// </summary>
    /// <param name="evt">包含施法上下文（CastContext）的事件数据</param>
    private void OnSelectTargets(GameEventType.Ability.SelectTargetsEventData evt)
    {
        var context = evt.Context;
        var ability = context.Ability;

        // 安全检查：如果技能对象为空，则无法进行目标选择
        if (ability == null) return;

        // 优先级检查：
        // 1. 如果上下文已经有了预选目标（例如：指向性技能在发动时就已经确定了目标）
        // 2. 或者已经有了预选位置（例如：某些范围技能直接施放在鼠标点击处）
        // 则跳过自动解析逻辑，直接沿用已有数据
        if (context.HasPreselectedTargets || context.HasPreselectedPosition) return;

        // 2. 执行自动目标解析 (ResolveTargets)
        ResolveTargets(context);
    }

    /// <summary>
    /// 根据技能的数据配置（Data）解析具体的 Entity 目标
    /// </summary>
    /// <param name="context">施法上下文，解析出的目标将存入 context.Targets</param>
    private void ResolveTargets(CastContext context)
    {
        var ability = context.Ability;
        // 从技能数据容器中获取目标选择模式（例如：实体、位置、自身等）
        var selection = ability.Data.Get<AbilityTargetSelection>(DataKey.AbilityTargetSelection);

        // 获取施法者的全局位置，作为搜索的圆心/起点
        Vector2 origin = Vector2.Zero;
        if (context.Caster is Node2D node) origin = node.GlobalPosition;

        // 根据不同的目标选择模式执行不同的查询逻辑
        switch (selection)
        {
            case AbilityTargetSelection.None:
                // None 类型：无需目标（如自身增益、位移技能）
                // 直接跳过目标选择，技能执行器自行处理
                break;

            case AbilityTargetSelection.Entity:
                {
                    // 读取技能配置的几何形状和参数
                    var geometry = ability.Data.Get<GeometryType>(DataKey.AbilityTargetGeometry);
                    var range = ability.Data.Get<float>(DataKey.AbilityCastRange);
                    var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);
                    var sorting = ability.Data.Get<TargetSorting>(DataKey.TargetSorting);
                    var maxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets);

                    // 构建查询参数
                    var query = new TargetSelectorQuery
                    {
                        Geometry = geometry,
                        Origin = origin,
                        Range = range,
                        CenterEntity = context.Caster,
                        TeamFilter = teamFilter,
                        Sorting = sorting,
                        MaxTargets = maxTargets != 0 ? maxTargets : 1
                    };

                    // 调用目标选择器
                    var targets = EntityTargetSelector.Query(query);

                    // 如果找到了符合条件的目标，填充到上下文中供后续流水线使用
                    if (targets.Count > 0) context.Targets = targets;
                }
                break;

            case AbilityTargetSelection.Point:
                // Point 类型：位置由玩家异步瞄准指定（TargetingManager 填充 context.TargetPosition）
                // 此处不做空间查询，可扩展位置合法性验证（如是否在可行走区域）
                break;

            case AbilityTargetSelection.EntityOrPoint:
                {
                    // EntityOrPoint：先尝试 Entity 自动索敌
                    var geometry = ability.Data.Get<GeometryType>(DataKey.AbilityTargetGeometry);
                    var range = ability.Data.Get<float>(DataKey.AbilityCastRange);
                    var teamFilter = ability.Data.Get<AbilityTargetTeamFilter>(DataKey.AbilityTargetTeamFilter);
                    var sorting = ability.Data.Get<TargetSorting>(DataKey.TargetSorting);
                    var maxTargets = ability.Data.Get<int>(DataKey.AbilityMaxTargets);

                    var targets = EntityTargetSelector.Query(new TargetSelectorQuery
                    {
                        Geometry = geometry,
                        Origin = origin,
                        Range = range,
                        CenterEntity = context.Caster,
                        TeamFilter = teamFilter,
                        Sorting = sorting,
                        MaxTargets = maxTargets != 0 ? maxTargets : 1
                    });

                    // 命中则填充 Entity 目标；未命中则留空，AbilitySystem 会回退到 Point 异步瞄准
                    if (targets.Count > 0) context.Targets = targets;
                }
                break;

                // 后续可在此扩展更多模式，如 Directional(方向), Self(自身) 等
        }
    }
}
