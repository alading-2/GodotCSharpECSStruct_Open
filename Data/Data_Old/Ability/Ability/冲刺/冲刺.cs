
using System.Runtime.CompilerServices;
/// <summary>
/// 冲刺技能执行器 - 示例实现
/// 
/// 功能：向前冲刺指定距离
/// 配置：通过 AbilityData["Dash"] 获取参数
/// 注册：使用 [ModuleInitializer] 自动注册到 AbilityExecutorRegistry
/// </summary>
public class DashExecutor : IAbilityExecutor
{
    private static readonly Log _log = new("DashExecutor");

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("Dash", new DashExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        if (caster == null || ability == null)
        {
            _log.Error("冲刺失败：施法者或技能为空");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 获取冲刺距离
        var range = ability.Data.Get<float>(DataKey.AbilityRange);
        if (range <= 0) range = 300f; // 默认值

        // TODO: 实现实际冲刺逻辑
        // 1. 获取施法者朝向
        // 2. 计算目标位置
        // 3. 执行位移（可能需要 Physics 检测）
        // 4. 播放特效/动画

        _log.Info($"执行冲刺: 距离 {range}");

        return new AbilityExecutedResult
        {
            TargetsHit = 1  // 冲刺只影响自己
        };
    }
}
