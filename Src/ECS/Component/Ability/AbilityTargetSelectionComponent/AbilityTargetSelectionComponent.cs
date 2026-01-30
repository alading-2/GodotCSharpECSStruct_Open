using Godot;
using System.Collections.Generic;

/// <summary>
/// 技能目标选择组件，只处理单位目标
/// <para>职责：作为技能施法流水线的一环，响应 SelectTargets 事件，根据技能配置解析具体目标并填充 CastContext。</para>
/// <para>接收：AbilitySystem 在施法逻辑中发出的 SelectTargets 事件</para>
/// </summary>
public partial class AbilityTargetSelectionComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(AbilityTargetSelectionComponent));
    private IEntity? _entity;

    /// <summary>
    /// 组件注册回调
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            // 订阅目标选择事件
            _entity.Events.On<GameEventType.Ability.SelectTargetsEventData>(
                GameEventType.Ability.SelectTargets,
                OnSelectTargets
            );
            _log.Info($"目标选择组件已注册到: {entity.Name}");
        }
    }

    /// <summary>
    /// 组件注销回调
    /// </summary>
    public void OnComponentUnregistered()
    {
        _entity = null;
    }

    /// <summary>
    /// 组件重置回调
    /// </summary>
    public void OnComponentReset() { }

    /// <summary>
    /// 每帧更新：不再需要，目标选择改为事件驱动即时计算
    /// </summary>
    public override void _Process(double delta)
    {
    }

    private void OnSelectTargets(GameEventType.Ability.SelectTargetsEventData evt)
    {
        var context = evt.Context;
        var ability = context.Ability;

        if (ability == null) return;

        // 1. 如果已有预设目标（来自 Context 初始化），通过
        if (context.HasPreselectedTargets || context.HasPreselectedPosition) return;

        // 2. 执行目标解析 (ResolveTargets)
        ResolveTargets(context);
    }

    /// <summary>
    /// 根据技能配置解析Entity目标
    /// </summary>
    private void ResolveTargets(CastContext context)
    {
        var ability = context.Ability;
        var selection = ability.Data.Get<AbilityTargetSelection>(DataKey.AbilityTargetSelection);

        // 获取施法者位置
        Vector2 origin = Vector2.Zero;
        if (context.Caster is Node2D node) origin = node.GlobalPosition;

        switch (selection)
        {
            case AbilityTargetSelection.Entity:
                {
                    var range = ability.Data.Get<float>(DataKey.AbilityRange);
                    // 搜索范围内威胁值最高的敌人
                    var targets = TargetSelector.Query(new TargetSelectorQuery
                    {
                        Geometry = AbilityTargetGeometry.Circle,
                        Origin = origin,
                        Range = range,
                        CenterEntity = context.Caster,
                        TeamFilter = AbilityTargetTeamFilter.Enemy,
                        Sorting = AbilityTargetSorting.HighestThreat,
                        MaxTargets = 1
                    });

                    if (targets.Count > 0) context.Targets = targets;
                }
                break;

        }
    }
}
