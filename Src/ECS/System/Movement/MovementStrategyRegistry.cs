using System.Collections.Generic;

/// <summary>
/// 运动策略注册表 - MoveMode 到 IMovementStrategy 的映射
/// <para>
/// 各策略在静态构造器或 ModuleInitializer 中自注册。
/// EntityMovementComponent 通过 <c>Get(MoveMode)</c> 获取对应策略实例。
/// </para>
/// </summary>
public static class MovementStrategyRegistry
{
    private static readonly Dictionary<MoveMode, IMovementStrategy> _strategies = new();

    /// <summary>注册一种运动策略</summary>
    public static void Register(MoveMode mode, IMovementStrategy strategy)
    {
        _strategies[mode] = strategy;
    }

    /// <summary>获取指定模式的运动策略，未注册则返回 null</summary>
    public static IMovementStrategy? Get(MoveMode mode)
    {
        return _strategies.TryGetValue(mode, out var strategy) ? strategy : null;
    }
}
