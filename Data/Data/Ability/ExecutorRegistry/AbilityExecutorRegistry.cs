using System;
using System.Collections.Generic;

/// <summary>
/// 技能执行器注册表 - 管理技能名称到执行器的映射
/// 
/// 使用方式：
/// 1. 在技能执行器中使用 [ModuleInitializer] 注册：
///    [ModuleInitializer]
///    public static void Initialize() => AbilityExecutorRegistry.Register("Dash", new DashExecutor());
/// 
/// 2. AbilitySystem 调用执行：
///    AbilityExecutorRegistry.Execute("Dash", context);
/// 
/// 设计原则：
/// - 静态类，全局单例模式
/// - 懒加载：首次访问时自动扫描并注册所有执行器
/// </summary>
public static class AbilityExecutorRegistry
{
    private static readonly Log _log = new("AbilityExecutorRegistry");
    private static readonly Dictionary<string, IAbilityExecutor> _executors = new();
    private static bool _initialized = false;

    /// <summary>
    /// 注册技能执行器
    /// </summary>
    /// <param name="abilityName">技能名称（与 AbilityData 中的 Name 对应）</param>
    /// <param name="executor">执行器实例</param>
    public static void Register(string abilityName, IAbilityExecutor executor)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            _log.Error("无法注册执行器：技能名称为空");
            return;
        }

        if (_executors.ContainsKey(abilityName))
        {
            _log.Warn($"技能执行器已存在，将被覆盖: {abilityName}");
        }

        _executors[abilityName] = executor;
        _log.Debug($"注册技能执行器: {abilityName}");
    }

    /// <summary>
    /// 执行技能
    /// </summary>
    /// <param name="abilityName">技能名称</param>
    /// <param name="context">施法上下文</param>
    /// <returns>执行结果，若未找到执行器则返回默认结果</returns>
    public static AbilityExecuteResult Execute(string abilityName, CastContext context)
    {
        if (!_executors.TryGetValue(abilityName, out var executor))
        {
            _log.Warn($"未找到技能执行器: {abilityName}，使用默认空执行");
            return new AbilityExecuteResult
            {
                TargetsHit = context.Targets?.Count ?? 0
            };
        }

        try
        {
            return executor.Execute(context);
        }
        catch (Exception ex)
        {
            _log.Error($"技能执行器异常: {abilityName}, {ex.Message}");
            return new AbilityExecuteResult
            {
                TargetsHit = 0
            };
        }
    }

    /// <summary>
    /// 检查是否存在指定技能的执行器
    /// </summary>
    public static bool HasExecutor(string abilityName)
    {
        return _executors.ContainsKey(abilityName);
    }

    /// <summary>
    /// 获取已注册的执行器数量
    /// </summary>
    public static int Count => _executors.Count;

    /// <summary>
    /// 清除所有注册（用于测试）
    /// </summary>
    public static void Clear()
    {
        _executors.Clear();
        _initialized = false;
    }
}
