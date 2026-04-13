using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 实体目标选择器。
/// 负责从已注册实体中筛出满足几何范围、阵营/类型过滤、排序与数量限制的目标集合。
///
/// 执行顺序：
/// 1) 几何候选收集（或 Chain 路径生成）；
/// 2) 阵营与类型过滤；
/// 3) 排序（Chain 保留路径顺序不排序）；
/// 4) MaxTargets 截断。
/// </summary>
public static class EntityTargetSelector
{
    private static readonly Log _log = new(nameof(EntityTargetSelector));

    /// <summary>
    /// 查询并返回符合条件的实体列表。
    /// 支持常规几何范围扫描（Circle/Ring/Box/Line/Cone/Global）。
    /// </summary>
    /// <param name="query">查询配置参数</param>
    /// <returns>符合条件的 List&lt;IEntity&gt;</returns>
    public static List<IEntity> Query(TargetSelectorQuery query)
    {
        var candidates = new List<IEntity>();

        if (query.Geometry == GeometryType.Single)
        {
            // Single 模式通常需要外部预选目标
            candidates = new List<IEntity>();
        }
        else
        {
            // 遍历全量 Node2D 实体并执行几何命中判定。
            foreach (var entity in GetAllNode2DEntities())
            {
                if (entity is Node2D node2D)
                {
                    if (GeometryCalculator.IsPointInGeometry(node2D.GlobalPosition, query))
                    {
                        candidates.Add(entity);
                    }
                }
            }
            candidates = FilterTargets(candidates, query.CenterEntity, query.TeamFilter, query.TypeFilter);
        }

        if (candidates.Count > 1)
        {
            SortTargets(candidates, query.Origin, query.Sorting);
        }

        if (query.MaxTargets > 0 && candidates.Count > query.MaxTargets)
        {
            candidates = candidates.GetRange(0, query.MaxTargets);
        }

        return candidates;
    }

    /// <summary>
    /// 对候选目标执行通用过滤：阵营、类型、生命周期状态。
    /// 会过滤 Dead / Reviving 实体，避免选中无效目标。
    /// </summary>
    private static List<IEntity> FilterTargets(List<IEntity> targets, IEntity? centerEntity, AbilityTargetTeamFilter teamFilter, EntityType typeFilter)
    {
        var filtered = new List<IEntity>();
        foreach (var target in targets)
        {
            if (!PassTeamFilter(target, centerEntity, teamFilter)) continue;
            if (!PassTypeFilter(target, typeFilter)) continue;
            if (target.Data.Has(DataKey.LifecycleState))
            {
                var state = target.Data.Get<LifecycleState>(DataKey.LifecycleState);
                if (state == LifecycleState.Dead || state == LifecycleState.Reviving) continue;
            }
            filtered.Add(target);
        }
        return filtered;
    }

    /// <summary>
    /// 阵营过滤判定。
    /// 若 filter 为 None 或 All 则直接放行；
    /// 若目标为自身则仅由 Self 标志决定；
    /// 其余按 center 与 target 阵营关系判定 Friendly / Enemy / Neutral。
    /// </summary>
    private static bool PassTeamFilter(IEntity target, IEntity? center, AbilityTargetTeamFilter filter)
    {
        if (filter == AbilityTargetTeamFilter.None || filter == AbilityTargetTeamFilter.All) return true;
        bool isSelf = IsSameEntity(target, center);
        if (isSelf) return filter.HasFlag(AbilityTargetTeamFilter.Self);
        Team targetTeam = target.Data.Get<Team>(DataKey.Team);
        if (targetTeam == Team.Neutral) return filter.HasFlag(AbilityTargetTeamFilter.Neutral);
        if (center == null) return false;
        Team centerTeam = center.Data.Get<Team>(DataKey.Team);
        bool isSameTeam = centerTeam == targetTeam;
        if (isSameTeam) return filter.HasFlag(AbilityTargetTeamFilter.Friendly);
        return filter.HasFlag(AbilityTargetTeamFilter.Enemy);
    }

    /// <summary>
    /// 类型过滤判定。
    /// 当 filter 为 EntityType.None 时视为不过滤；否则需要目标存在 EntityType 且命中位掩码。
    /// </summary>
    private static bool PassTypeFilter(IEntity target, EntityType filter)
    {
        if (filter == EntityType.None) return true;
        if (!target.Data.Has(DataKey.EntityType)) return false;
        EntityType entityType = target.Data.Get<EntityType>(DataKey.EntityType);
        return filter.HasFlag(entityType);
    }

    /// <summary>
    /// 对目标集合执行排序。
    /// 排序仅改变顺序，不改变元素集合；随机排序使用 Fisher-Yates 洗牌。
    /// </summary>
    private static void SortTargets(List<IEntity> targets, Vector2 origin, TargetSorting sorting)
    {
        switch (sorting)
        {
            case TargetSorting.None: break;
            case TargetSorting.Nearest:
                targets.Sort((a, b) => GetEntityPosition(a).DistanceTo(origin).CompareTo(GetEntityPosition(b).DistanceTo(origin)));
                break;
            case TargetSorting.Farthest:
                targets.Sort((a, b) => GetEntityPosition(b).DistanceTo(origin).CompareTo(GetEntityPosition(a).DistanceTo(origin)));
                break;
            case TargetSorting.LowestHealth:
                targets.Sort((a, b) => a.Data.Get<float>(DataKey.CurrentHp).CompareTo(b.Data.Get<float>(DataKey.CurrentHp)));
                break;
            case TargetSorting.HighestHealth:
                targets.Sort((a, b) => b.Data.Get<float>(DataKey.CurrentHp).CompareTo(a.Data.Get<float>(DataKey.CurrentHp)));
                break;
            case TargetSorting.HighestHealthPercent:
                targets.Sort((a, b) => b.Data.Get<float>(DataKey.HpPercent).CompareTo(a.Data.Get<float>(DataKey.HpPercent)));
                break;
            case TargetSorting.LowestHealthPercent:
                targets.Sort((a, b) => a.Data.Get<float>(DataKey.HpPercent).CompareTo(b.Data.Get<float>(DataKey.HpPercent)));
                break;
            case TargetSorting.Random:
                Random rng = new Random();
                for (int i = targets.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (targets[i], targets[j]) = (targets[j], targets[i]);
                }
                break;
            case TargetSorting.HighestThreat:
                targets.Sort((a, b) =>
                    (b.Data.Has(DataKey.Threat) ? b.Data.Get<float>(DataKey.Threat) : 0).CompareTo(a.Data.Has(DataKey.Threat) ? a.Data.Get<float>(DataKey.Threat) : 0));
                break;
        }
    }

    /// <summary>
    /// 判断两个 IEntity 是否引用同一实体。
    /// 优先使用引用比较；若均为 Node，则进一步按 Godot 节点实例比较。
    /// </summary>
    private static bool IsSameEntity(IEntity? a, IEntity? b)
    {
        if (a == b) return true;
        if (a is Node n1 && b is Node n2) return n1 == n2;
        return false;
    }

    /// <summary>
    /// 获取当前生命周期管理器中的全部 Node2D 且实现 IEntity 的节点。
    /// </summary>
    private static IEnumerable<IEntity> GetAllNode2DEntities()
    {
        return NodeLifecycleManager.GetNodesByInterface<Node2D>().OfType<IEntity>();
    }

    /// <summary>
    /// 读取实体世界坐标。
    /// 非 Node2D 实体返回 Vector2.Zero（理论上不应在 TargetSelector 中出现）。
    /// </summary>
    private static Vector2 GetEntityPosition(IEntity entity)
    {
        if (entity is Node2D node2D) return node2D.GlobalPosition;
        return Vector2.Zero;
    }
}
