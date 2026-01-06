using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 伤害服务 (Singleton)
/// <para>负责管理伤害处理管道 (Pipeline) 并对外提供统一的伤害入口。</para>
/// </summary>
public partial class DamageService
{
    private static readonly Log _log = new("DamageService");
    private static DamageService _instance;
    public static DamageService Instance => _instance ??= new DamageService();

    private readonly List<IDamageProcessor> _processors = new();

    private DamageService()
    {
        // 注册默认处理器
        // TODO: 后续通过反射或依赖注入自动注册
        RegisterProcessor(new BaseDamageProcessor());
        RegisterProcessor(new DamageAmplificationProcessor());
        RegisterProcessor(new CritProcessor());
        RegisterProcessor(new DodgeProcessor());
        RegisterProcessor(new ShieldProcessor());
        RegisterProcessor(new DefenseProcessor());
        RegisterProcessor(new DamageTakenAmplificationProcessor());
        RegisterProcessor(new FlatReductionProcessor());
        RegisterProcessor(new HealthExecutionProcessor());
        RegisterProcessor(new LifestealProcessor());
        RegisterProcessor(new StatisticsProcessor());
    }

    /// <summary>
    /// 注册新的伤害处理器
    /// </summary>
    public void RegisterProcessor(IDamageProcessor processor)
    {
        _processors.Add(processor);
        _processors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        _log.Debug($"Registered processor: {processor.GetType().Name} (Priority: {processor.Priority})");
    }

    /// <summary>
    /// 处理伤害请求
    /// </summary>
    public void Process(DamageInfo info)
    {
        if (info == null || !Godot.GodotObject.IsInstanceValid(info.Victim))
        {
            _log.Warn("Invalid damage info or victim.");
            return;
        }

        // 执行管道
        foreach (var processor in _processors)
        {
            // 如果伤害已经归零且非特殊情况，是否还要继续？
            // 部分逻辑可能仍需执行（如统计），具体由 processor 内部判断。
            // 闪避会标记 IsDodged，此时部分 processor 应该跳过。

            processor.Process(info);
        }

        // 输出日志
        if (info.Logs.Count > 0)
        {
            // _log.Trace($"Damage Log for {info.Id}: {string.Join(" -> ", info.Logs)}");
        }
    }
}
