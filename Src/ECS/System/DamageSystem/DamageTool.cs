using System.Collections.Generic;
using Godot;

/// <summary>
/// 伤害应用参数。
/// 描述"怎么打"：伤害量、类型、标签、来源，以及可选的 DoT 节奏和重复命中规则。
/// </summary>
internal sealed record DamageApplyOptions
{
    public float Damage { get; init; }                                  // 每次命中伤害量
    public DamageType Type { get; init; }                               // 伤害类型
    public DamageTags Tags { get; init; } = DamageTags.None;            // 伤害标签（位掩码）
    public Node? Attacker { get; init; }                                // 伤害来源节点（null 时由上层决定）
    public float TickInterval { get; init; }                            // DoT 间隔（秒），<=0 表示单次
    public float TotalDuration { get; init; }                           // DoT 总时长（秒），<=0 表示单次
    public bool AllowRepeatHitSameTarget { get; init; } = true;         // 是否允许重复命中同一目标
}

/// <summary>
/// 伤害应用结果。
/// </summary>
internal readonly record struct DamageApplyResult(int HitCount, GameTimer? Timer);

/// <summary>
/// 伤害应用工具（归属 DamageSystem）。
///
/// 职责：对目标列表执行伤害结算，含单次、持续（DoT）和重复命中控制。
/// 不负责目标查询和特效生成——只关心"怎么打、打多久、能不能重复打同一目标"。
/// </summary>
internal static class DamageTool
{
    private static readonly Log _log = new(nameof(DamageTool));

    /// <summary>
    /// 对目标列表执行一次伤害结算（不含 DoT 调度）。
    /// </summary>
    /// <param name="targets">目标列表</param>
    /// <param name="options">伤害参数</param>
    /// <param name="hitRegistry">命中注册表；非 null 时启用"每目标只命中一次"拦截</param>
    /// <returns>本次实际命中数量</returns>
    public static int ApplyToList(
        IReadOnlyList<IEntity> targets,
        DamageApplyOptions options,
        HashSet<ulong>? hitRegistry = null)
    {
        if (DamageService.Instance == null)
        {
            _log.Warn("DamageService 不存在，跳过伤害结算");
            return 0;
        }

        // 持续伤害自动附加 Persistent 标签，便于处理器识别 DoT 来源
        var tags = options.Tags;
        if (options.TickInterval > 0f && options.TotalDuration > 0f)
        {
            tags |= DamageTags.Persistent;
        }

        int count = 0;
        foreach (var target in targets)
        {
            if (target is not IUnit victim) continue;       // 非战斗单位跳过
            if (!CanHit(target, hitRegistry)) continue;     // 重复命中检查

            DamageService.Instance.Process(new DamageInfo
            {
                Attacker = options.Attacker,
                Victim = victim,
                Damage = options.Damage,
                Type = options.Type,
                Tags = tags
            });
            count++;
        }

        return count;
    }

    /// <summary>
    /// 调度持续伤害（DoT）。
    ///
    /// 每隔 TickInterval 秒通过 targetsProvider 重新获取目标并结算一轮伤害，
    /// 持续 TotalDuration 秒后自动停止。guardian 节点失效时也会提前终止。
    /// </summary>
    /// <param name="targetsProvider">每次 tick 调用以获取最新目标列表（可返回 null 表示本轮跳过）</param>
    /// <param name="options">伤害参数（TickInterval 与 TotalDuration 必须 > 0）</param>
    /// <param name="guardian">守护节点；失效时自动取消计时器，防止僵尸 tick</param>
    /// <param name="hitRegistry">命中注册表；非 null 时整个 DoT 生命周期内每目标只命中一次</param>
    /// <returns>GameTimer（可手动取消）；参数无效或依赖缺失时返回 null</returns>
    public static GameTimer? ScheduleDoT(
        System.Func<IReadOnlyList<IEntity>?> targetsProvider,
        DamageApplyOptions options,
        Node? guardian = null,
        HashSet<ulong>? hitRegistry = null)
    {
        if (options.TickInterval <= 0f || options.TotalDuration <= 0f)
        {
            _log.Warn("ScheduleDoT: TickInterval 或 TotalDuration 无效，不创建 DoT 计时器");
            return null;
        }

        if (TimerManager.Instance == null)
        {
            _log.Warn("TimerManager 不存在，持续伤害降级为单次");
            return null;
        }

        var interval = Mathf.Max(options.TickInterval, 0.01f); // 间隔下限，防止 0 或负数导致异常
        var duration = Mathf.Max(options.TotalDuration, interval);

        GameTimer? timer = null;
        timer = TimerManager.Instance.Countdown(duration, interval)
            .OnLoop(() =>
            {
                // 守护节点失效时终止 DoT，避免引用悬空和无效结算
                if (guardian != null && !GodotObject.IsInstanceValid(guardian))
                {
                    timer?.Cancel();
                    return;
                }

                var targets = targetsProvider();
                if (targets == null || targets.Count == 0) return;

                ApplyToList(targets, options, hitRegistry);
            });

        return timer;
    }

    /// <summary>
    /// 判断目标是否可以被命中（用于重复命中控制）。
    /// </summary>
    /// <param name="target">目标实体</param>
    /// <param name="hitRegistry">命中注册表；null 表示不限制，允许无限次命中</param>
    public static bool CanHit(IEntity target, HashSet<ulong>? hitRegistry)
    {
        if (hitRegistry == null) return true;                                               // 不限制重复命中
        if (target is not Node node || !GodotObject.IsInstanceValid(node)) return false;    // 无效节点不可命中
        return hitRegistry.Add(node.GetInstanceId());                                       // Add 返回 false = 已命中过
    }

    /// <summary>
    /// 创建命中注册表（用于"整个施法周期内每目标只命中一次"的场景）。
    /// </summary>
    public static HashSet<ulong> CreateHitRegistry() => new HashSet<ulong>();
}
