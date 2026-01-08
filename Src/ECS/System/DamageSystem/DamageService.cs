using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
/// <summary>
/// 伤害服务 (Singleton)
/// 负责管理伤害处理管道 (Pipeline) 并对外提供统一的伤害入口，并统一设置优先级。
/// 通过注册不同的 <see cref="IDamageProcessor"/>，实现伤害计算逻辑的解耦（如暴击、闪避、减伤等）。
/// </summary>
public partial class DamageService : Node
{
    // 日志系统实例
    private static readonly Log _log = new("DamageService");

    /// <summary>
    /// 自动注册到引导器 (AutoLoad)
    /// 使用 ModuleInitializer 确保在程序集加载时自动完成注册，无需手动在编辑器中配置。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DamageService",
            Path = "res://Src/ECS/System/DamageSystem/DamageService.cs",
            Priority = AutoLoad.Priority.System
        });
    }

    /// <summary> 获取全局单例实例 </summary>
    public static DamageService Instance;

    // 已注册的伤害处理器列表
    private readonly List<IDamageProcessor> _processors = new();

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            _log.Warn("Duplicate DamageService instance detected. Destroying duplicate.");
            QueueFree();
            return;
        }

        Instance = this;

        // 防止 Reparent 导致处理器重复添加
        _processors.Clear();
        DamageServiceRegister();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 私有构造函数，初始化并注册默认的伤害处理器
    /// </summary>
    private void DamageServiceRegister()
    {
        // 注册默认处理器（按计算逻辑顺序）
        // 1. 基础伤害计算（获取攻击者基础属性）
        RegisterProcessor(new BaseDamageProcessor(), 100);
        // 2. 暴击判定与计算
        RegisterProcessor(new CritProcessor(), 200);
        // 3. 闪避判定（如果闪避成功，后续减伤逻辑通常跳过）
        RegisterProcessor(new DodgeProcessor(), 300);
        // 4. 护盾抵扣
        RegisterProcessor(new ShieldProcessor(), 400);
        // 5. 防御力/护甲减伤
        RegisterProcessor(new DefenseProcessor(), 500);
        // 6. 受伤加成（受击者易伤等效果）
        RegisterProcessor(new DamageTakenAmplificationProcessor(), 600);
        // 7. 固定值减伤
        RegisterProcessor(new FlatReductionProcessor(), 700);
        // 8. 生命百分比斩杀逻辑
        RegisterProcessor(new HealthExecutionProcessor(), 800);
        // 9. 吸血逻辑（基于最终造成的伤害）
        RegisterProcessor(new LifestealProcessor(), 900);
        // 10. 数据统计（记录总伤害、DPS等）
        RegisterProcessor(new StatisticsProcessor(), 1000);

        _log.Debug("DamageServiceRegister 初始化完成");
    }

    /// <summary>
    /// 注册新的伤害处理器，并按优先级自动排序
    /// </summary>
    /// <param name="processor">要注册的处理器实例</param>
    /// <param name="priority">处理器优先级</param>
    public void RegisterProcessor(IDamageProcessor processor, int priority)
    {
        processor.Priority = priority;
        _processors.Add(processor);
        // 按 Priority 从小到大排序，优先级值越小越先执行
        _processors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        _log.Debug($"Registered processor: {processor.GetType().Name} (Priority: {processor.Priority})");
    }

    /// <summary>
    /// 处理伤害请求的主入口
    /// </summary>
    /// <param name="info">包含攻击者、受击者及初始伤害信息的上下文对象</param>
    public void Process(DamageInfo info)
    {
        // 基础合法性检查
        if (info == null || info.Victim == null)
        {
            _log.Warn("伤害系统：Invalid damage info or victim.");
            return;
        }

        // 执行伤害处理管道
        // 遍历所有处理器，每个处理器都会修改 info 对象中的数据
        foreach (var processor in _processors)
        {
            // 注意：部分处理器内部会检查 info.IsDodged 或 info.Amount 是否为 0
            // 以决定是否跳过核心逻辑，但 processor.Process(info) 仍会被调用以维持管道完整性
            processor.Process(info);
            if (info.IsEnd) break;
        }

        // 调试日志输出
        if (info.Logs.Count > 0)
        {
            // 如果需要追踪伤害计算过程，可以取消注释
            _log.Info($"伤害系统日志 {info.Id}: {string.Join(" -> ", info.Logs)}");
        }
    }
}
