/// <summary>
/// 技能执行器接口 - 定义技能效果执行的标准入口
/// 
/// 架构说明：
/// - 每个技能可以有独立的执行器实现
/// - 执行器只负责"怎么放"，不处理触发/冷却/消耗
/// - AbilitySystem 通过 AbilityExecutorRegistry 调用执行器
/// 
/// 存放位置：Data/Data/Ability/Ability/
/// 原因：执行器是技能数据的逻辑补充，与 AbilityData 配置对应
/// </summary>
public interface IAbilityExecutor
{
    /// <summary>
    /// 执行技能效果
    /// </summary>
    /// <param name="context">施法上下文，包含施法者、技能、目标等信息</param>
    /// <returns>执行结果</returns>
    AbilityExecuteResult Execute(CastContext context);
}
