using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 目标选择工具 - 通用的空间查询和目标筛选
/// 
/// 设计原则：
/// - 静态工具类，无状态
/// - 支持多种几何形状范围查询
/// - 支持阵营、类型、排序、数量限制等多维度过滤
/// - 不依赖技能系统，可被任意模块调用
/// 
/// 使用示例：
/// var targets = TargetSelector.Query(new TargetSelectorQuery
/// {
///     Geometry = AbilityTargetGeometry.Circle,
///     Origin = caster.GlobalPosition,
///     Range = 200f,
///     CenterEntity = caster,
///     TeamFilter = AbilityTargetTeamFilter.Enemy,
///     MaxTargets = 5
/// });
/// </summary>
public static class TargetSelector
{
    private static readonly Log _log = new("TargetSelector");

    // ==================== 公共 API ====================

    /// <summary>
    /// 通用目标查询入口
    /// 根据配置参数执行几何检测、过滤、排序和数量限制
    /// </summary>
    /// <param name="query">查询配置</param>
    /// <returns>符合条件的实体列表（已排序、已限制数量）</returns>
    public static List<IEntity> Query(TargetSelectorQuery query)
    {
        var candidates = new List<IEntity>();

        // 1. 几何检测：根据形状查询初步候选目标
        candidates = query.Geometry switch
        {
            AbilityTargetGeometry.Circle => QueryCircle(query.Origin, query.Range),
            AbilityTargetGeometry.Box => QueryRectangle(query.Origin, query.Forward ?? Vector2.Right, query.Width, query.Length),
            AbilityTargetGeometry.Line => QueryLineWithWidth(query.Origin, query.Forward ?? Vector2.Right, query.Length, query.Width),
            AbilityTargetGeometry.Cone => QueryCone(query.Origin, query.Forward ?? Vector2.Right, query.Range, query.Angle),
            AbilityTargetGeometry.Chain => QueryChain(query.Origin, query.ChainCount, query.ChainRange, query.CenterEntity, query.TeamFilter, query.TypeFilter, query.ChainAllowDuplicate), // 瞬间快照式链式查询
            AbilityTargetGeometry.Global => QueryGlobal(),
            AbilityTargetGeometry.Single => new List<IEntity>(), // Single 模式通常需要外部预选目标
            _ => new List<IEntity>()
        };

        // 2. 过滤：阵营和类型
        candidates = FilterTargets(candidates, query.CenterEntity, query.TeamFilter, query.TypeFilter);

        // 3. 排序：按优先级
        // 链式查询的结果是有序的（路径顺序），不应被通用排序打乱
        if (candidates.Count > 1 && query.Geometry != AbilityTargetGeometry.Chain)
        {
            SortTargets(candidates, query.Origin, query.Sorting);
        }

        // 4. 限制：最大目标数
        if (query.MaxTargets > 0 && candidates.Count > query.MaxTargets)
        {
            candidates = candidates.GetRange(0, query.MaxTargets);
        }

        return candidates;
    }

    // ==================== 几何检测方法 ====================

    /// <summary>
    /// 圆形范围查询
    /// </summary>
    /// <param name="origin">圆心位置</param>
    /// <param name="range">半径</param>
    private static List<IEntity> QueryCircle(Vector2 origin, float range)
    {
        var results = new List<IEntity>();

        // 遍历所有已注册的 Entity（通过 EntityManager 的类型索引优化）
        // 这里简化处理：遍历所有 Node2D 类型的实体
        foreach (var entity in GetAllNode2DEntities())
        {
            if (entity is Node2D node2D)
            {
                float distance = node2D.GlobalPosition.DistanceTo(origin);
                if (distance <= range)
                {
                    results.Add(entity);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 矩形范围查询（支持旋转）
    /// </summary>
    /// <param name="origin">矩形中心点</param>
    /// <param name="forward">前向方向（归一化）</param>
    /// <param name="width">宽度</param>
    /// <param name="length">长度</param>
    private static List<IEntity> QueryRectangle(Vector2 origin, Vector2 forward, float width, float length)
    {
        var results = new List<IEntity>();
        forward = forward.Normalized();

        // 计算矩形的局部坐标系
        Vector2 right = new Vector2(-forward.Y, forward.X); // 垂直于前向的右向量

        foreach (var entity in GetAllNode2DEntities())
        {
            if (entity is Node2D node2D)
            {
                Vector2 localPos = node2D.GlobalPosition - origin;

                // 投影到局部坐标系
                float forwardDist = localPos.Dot(forward);
                float rightDist = localPos.Dot(right);

                // 检查是否在矩形内（中心对齐）
                if (Mathf.Abs(forwardDist) <= length / 2f && Mathf.Abs(rightDist) <= width / 2f)
                {
                    results.Add(entity);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 带宽度的线段范围查询（实际形状为“胶囊体 Capsule”）
    /// <para>特点：以 [origin] 为起点，向 [forward] 方向延伸。形状两端为半圆，判定比矩形更宽容，手感更好。</para>
    /// </summary>
    /// <param name="origin">射线起点（如枪口、玩家位置）</param>
    /// <param name="forward">射线延伸方向</param>
    /// <param name="length">射线长度</param>
    /// <param name="width">射线的直径/宽度</param>
    private static List<IEntity> QueryLineWithWidth(Vector2 origin, Vector2 forward, float length, float width)
    {
        var results = new List<IEntity>();
        forward = forward.Normalized();
        Vector2 endPoint = origin + forward * length;

        foreach (var entity in GetAllNode2DEntities())
        {
            if (entity is Node2D node2D)
            {
                // 计算点到线段的距离
                float distToLine = PointToSegmentDistance(node2D.GlobalPosition, origin, endPoint);

                if (distToLine <= width / 2f)
                {
                    results.Add(entity);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 扇形范围查询
    /// </summary>
    /// <param name="origin">扇形顶点</param>
    /// <param name="forward">中心方向（归一化）</param>
    /// <param name="range">半径</param>
    /// <param name="angle">张角（度数）</param>
    private static List<IEntity> QueryCone(Vector2 origin, Vector2 forward, float range, float angle)
    {
        var results = new List<IEntity>();
        forward = forward.Normalized();
        float halfAngleRad = Mathf.DegToRad(angle / 2f);

        foreach (var entity in GetAllNode2DEntities())
        {
            if (entity is Node2D node2D)
            {
                Vector2 toTarget = node2D.GlobalPosition - origin;
                float distance = toTarget.Length();

                // 距离检测
                if (distance > range) continue;

                // 角度检测
                float angleToTarget = forward.AngleTo(toTarget);
                if (Mathf.Abs(angleToTarget) <= halfAngleRad)
                {
                    results.Add(entity);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 链式传递查询（如闪电链）
    /// <para>
    /// ⚠️ **设计说明 (必读)**: 
    /// 本方法采用“瞬间快照”模式。在调用的一瞬间，它会根据当前场景所有实体的坐标，
    /// 预先计算出一条完整的弹射路径。
    /// </para>
    /// <para>
    /// <b>适用场景：</b>
    /// 1. 瞬发结算技能（如瞬间杀伤 10 个目标的激光、无飞行过程的电弧）。
    /// 2. 性能敏感场景（高频触发且无需考虑飞行时间）。
    /// 3. UI 辅助线/范围指示器的预览绘制。
    /// </para>
    /// <para>
    /// <b>局限性（时空错位问题）：</b>
    /// 本方法不适用于带有“缓慢飞行过程”或“跳跃间隔”的技能（如巫妖的大招、缓慢飞行的火球）。
    /// 因为当技能在 0.5 秒后跳跃到第 3 个目标时，该目标可能已经移动或死亡。
    /// 对于此类逻辑，建议在 AbilityExecutor 中通过循环+单次查询（如 QueryCircle）动态实现。
    /// </para>
    /// </summary>
    /// <param name="origin">起点位置</param>
    /// <param name="chainCount">最大跳跃次数</param>
    /// <param name="chainRange">每跳跨度的最大距离</param>
    /// <param name="centerEntity">基准实体（用于阵营/类型过滤）</param>
    /// <param name="teamFilter">阵营过滤规则</param>
    /// <param name="typeFilter">类型过滤规则</param>
    /// <param name="allowDuplicate">是否允许在路径中重复出现同一个目标</param>
    private static List<IEntity> QueryChain(Vector2 origin, int chainCount, float chainRange, IEntity? centerEntity, AbilityTargetTeamFilter teamFilter, AbilityTargetTypeFilter typeFilter, bool allowDuplicate)
    {
        // 最终查询结果
        var results = new List<IEntity>();
        // 闪电链访问过的实体，避免重复访问（仅在不允许重复时使用）
        var visited = new HashSet<IEntity>();

        // 当前位置
        Vector2 currentPos = origin;
        // 当前实体
        IEntity? currentEntity = null;

        // 获取所有候选目标
        var allCandidates = GetAllNode2DEntities().ToList();

        for (int i = 0; i < chainCount; i++)
        {
            IEntity? nextTarget = null;
            float minDistance = float.MaxValue;

            // 贪心算法：找最近的未访问目标
            foreach (var candidate in allCandidates)
            {
                // 如果不允许重复，且已访问过，则跳过
                if (!allowDuplicate && visited.Contains(candidate)) continue;

                // 不能跳回当前所在的实体（防止 A->A 立即原地弹跳）
                if (IsSameEntity(candidate, currentEntity)) continue;


                // 阵营和类型过滤
                if (!PassTeamFilter(candidate, centerEntity, teamFilter)) continue;
                if (!PassTypeFilter(candidate, typeFilter)) continue;

                if (candidate is Node2D node2D)
                {
                    float distance = node2D.GlobalPosition.DistanceTo(currentPos);
                    // _log.Debug($"Candidate {node2D.Name} dist: {distance}");

                    if (distance <= chainRange && distance < minDistance)
                    {
                        minDistance = distance;
                        nextTarget = candidate;
                    }
                }
            }

            // 如果找到下一个目标
            if (nextTarget != null)
            {
                results.Add(nextTarget);
                // 仅在不允许重复时记录已访问
                if (!allowDuplicate)
                {
                    visited.Add(nextTarget);
                }

                // 更新当前位置和实体
                if (nextTarget is Node2D nextNode2D)
                {
                    currentPos = nextNode2D.GlobalPosition;
                }
                currentEntity = nextTarget;
            }
            else
            {
                // 无法继续跳跃，提前结束
                break;
            }
        }

        return results;
    }
    /// <summary>
    /// 全局查询（返回所有实体）
    /// </summary>
    private static List<IEntity> QueryGlobal()
    {
        return GetAllNode2DEntities().ToList();
    }

    // ==================== 过滤方法 ====================

    /// <summary>
    /// 统一过滤接口（阵营 + 类型）
    /// </summary>
    private static List<IEntity> FilterTargets(List<IEntity> targets, IEntity? centerEntity, AbilityTargetTeamFilter teamFilter, AbilityTargetTypeFilter typeFilter)
    {
        var filtered = new List<IEntity>();

        foreach (var target in targets)
        {
            // 阵营过滤
            if (!PassTeamFilter(target, centerEntity, teamFilter)) continue;

            // 类型过滤
            if (!PassTypeFilter(target, typeFilter)) continue;

            filtered.Add(target);
        }

        return filtered;
    }

    /// <summary>
    /// 阵营过滤检测
    /// </summary>
    /// <param name="target">待检测目标</param>
    /// <param name="center">基准实体（用于判断友军/敌军）</param>
    /// <param name="filter">过滤规则</param>
    /// <returns>是否通过</returns>
    private static bool PassTeamFilter(IEntity target, IEntity? center, AbilityTargetTeamFilter filter)
    {
        // 快速路径：无过滤或全选
        if (filter == AbilityTargetTeamFilter.None || filter == AbilityTargetTeamFilter.All)
            return true;

        // Self 检测 (优先处理，避免后续逻辑误判)
        bool isSelf = IsSameEntity(target, center);
        if (isSelf)
            return filter.HasFlag(AbilityTargetTeamFilter.Self);

        // 绝对阵营：Neutral (不依赖 center)
        Team targetTeam = target.Data.Get<Team>(DataKey.Team);
        if (targetTeam == Team.Neutral)
            return filter.HasFlag(AbilityTargetTeamFilter.Neutral);

        // 相对阵营：Friendly/Enemy (需要 center)
        if (center == null) return false;

        Team centerTeam = center.Data.Get<Team>(DataKey.Team);
        bool isSameTeam = centerTeam == targetTeam;

        if (isSameTeam) return filter.HasFlag(AbilityTargetTeamFilter.Friendly);
        return filter.HasFlag(AbilityTargetTeamFilter.Enemy);
    }

    /// <summary>
    /// 类型过滤检测
    /// </summary>
    /// <param name="target">待检测目标</param>
    /// <param name="filter">过滤规则</param>
    private static bool PassTypeFilter(IEntity target, AbilityTargetTypeFilter filter)
    {
        // 如果没有设置过滤规则，跳过类型过滤
        if (filter == AbilityTargetTypeFilter.None)
            return true;

        // 获取目标类型
        if (!target.Data.Has(DataKey.EntityType))
            return false;

        EntityType entityType = target.Data.Get<EntityType>(DataKey.EntityType);

        // 将 EntityType 转换为对应的 Filter Flag
        // 修正: EntityType 值 (1,2,4...) 本身已对齐 Filter Flag，直接强转即可
        // 之前逻辑 (1 << entityType) 是错误的，会造成位移过量
        AbilityTargetTypeFilter typeFlag = (AbilityTargetTypeFilter)entityType;

        return filter.HasFlag(typeFlag);
    }

    // ==================== 排序方法 ====================

    /// <summary>
    /// 目标排序
    /// 内部逻辑已优化：血量百分比排序现在直接使用实体的计算属性 DataKey.HpPercent (0-100)
    /// </summary>
    /// <param name="targets">目标列表（原地修改）</param>
    /// <param name="origin">参考位置</param>
    /// <param name="sorting">排序规则</param>
    private static void SortTargets(List<IEntity> targets, Vector2 origin, AbilityTargetSorting sorting)
    {
        switch (sorting)
        {
            case AbilityTargetSorting.None:
                break;

            case AbilityTargetSorting.Nearest:
                targets.Sort((a, b) =>
                {
                    float distA = GetEntityPosition(a).DistanceTo(origin);
                    float distB = GetEntityPosition(b).DistanceTo(origin);
                    return distA.CompareTo(distB);
                });
                break;

            case AbilityTargetSorting.Farthest:
                targets.Sort((a, b) =>
                {
                    float distA = GetEntityPosition(a).DistanceTo(origin);
                    float distB = GetEntityPosition(b).DistanceTo(origin);
                    return distB.CompareTo(distA); // 反向排序
                });
                break;

            case AbilityTargetSorting.LowestHealth:
                targets.Sort((a, b) =>
                {
                    float hpA = a.Data.Get<float>(DataKey.CurrentHp);
                    float hpB = b.Data.Get<float>(DataKey.CurrentHp);
                    return hpA.CompareTo(hpB);
                });
                break;

            case AbilityTargetSorting.HighestHealth:
                targets.Sort((a, b) =>
                {
                    float hpA = a.Data.Get<float>(DataKey.CurrentHp);
                    float hpB = b.Data.Get<float>(DataKey.CurrentHp);
                    return hpB.CompareTo(hpA);
                });
                break;
            case AbilityTargetSorting.HighestHealthPercent:
                targets.Sort((a, b) =>
                {
                    float percentA = a.Data.Get<float>(DataKey.HpPercent);
                    float percentB = b.Data.Get<float>(DataKey.HpPercent);
                    return percentB.CompareTo(percentA);
                });
                break;
            case AbilityTargetSorting.LowestHealthPercent:
                targets.Sort((a, b) =>
                {
                    float percentA = a.Data.Get<float>(DataKey.HpPercent);
                    float percentB = b.Data.Get<float>(DataKey.HpPercent);
                    return percentA.CompareTo(percentB);
                });
                break;

            case AbilityTargetSorting.Random:
                // Fisher-Yates 洗牌
                Random rng = new Random();
                for (int i = targets.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (targets[i], targets[j]) = (targets[j], targets[i]);
                }
                break;

            case AbilityTargetSorting.HighestThreat:
                targets.Sort((a, b) =>
                {
                    float threatA = a.Data.Has(DataKey.Threat) ? a.Data.Get<float>(DataKey.Threat) : 0;
                    float threatB = b.Data.Has(DataKey.Threat) ? b.Data.Get<float>(DataKey.Threat) : 0;
                    return threatB.CompareTo(threatA);
                });
                break;
        }
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 健壮的实体相等性判断（处理 Godot 对象包装器引用不一致）
    /// </summary>
    private static bool IsSameEntity(IEntity? a, IEntity? b)
    {
        if (a == b) return true;
        if (a is Node n1 && b is Node n2) return n1 == n2;
        return false;
    }

    /// <summary>
    /// 获取所有 Node2D 类型的 Entity
    /// 使用 EntityManager 的全局查询接口，无需硬编码类型列表
    /// </summary>
    private static IEnumerable<IEntity> GetAllNode2DEntities()
    {
        // 使用新的 EntityManager API，获取所有实现 Node2D 的 Entity
        return EntityManager.GetEntitiesByInterface<Node2D>()
            .Cast<IEntity>();
    }

    /// <summary>
    /// 获取实体的全局位置
    /// </summary>
    private static Vector2 GetEntityPosition(IEntity entity)
    {
        if (entity is Node2D node2D)
        {
            return node2D.GlobalPosition;
        }
        return Vector2.Zero;
    }

    /// <summary>
    /// 计算点到线段的最短距离
    /// </summary>
    private static float PointToSegmentDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 line = lineEnd - lineStart;
        float lineLength = line.Length();

        if (lineLength < 0.001f)
        {
            // 线段退化为点
            return point.DistanceTo(lineStart);
        }

        // 计算点在线段上的投影比例 t
        float t = Mathf.Clamp((point - lineStart).Dot(line) / (lineLength * lineLength), 0f, 1f);

        // 投影点
        Vector2 projection = lineStart + line * t;

        return point.DistanceTo(projection);
    }
}
