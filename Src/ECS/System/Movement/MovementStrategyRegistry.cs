using System;
using System.Collections.Generic;

/// <summary>
/// 运动策略注册表，维护 <c>MoveMode</c> 到策略工厂函数的映射关系。
/// <para>
/// 每个策略类在自己的静态初始化阶段注册工厂函数，
/// <c>EntityMovementComponent</c> 在切换运动模式时调用 <c>Create()</c> 生成独立实例，
/// 从而允许策略持有私有运行时状态（如 _currentAngle、_startPoint），无单例污染问题。
/// </para>
/// </summary>
public static class MovementStrategyRegistry
{
    private static readonly Dictionary<MoveMode, Func<IMovementStrategy>> _factories = new();

    /// <summary>
    /// 注册一种运动策略的工厂函数。
    /// <para>
    /// 如果同一个 <c>MoveMode</c> 被重复注册，后注册的工厂会覆盖前面的实现。
    /// </para>
    /// </summary>
    public static void Register(MoveMode mode, Func<IMovementStrategy> factory)
    {
        _factories[mode] = factory;
    }

    /// <summary>
    /// 创建指定模式对应的运动策略新实例。
    /// <para>
    /// 未注册时返回 <c>null</c>，调度器会记录告警并保持当前状态不执行该模式逻辑。
    /// 每次切换策略都会创建新实例，保证策略私有状态干净。
    /// </para>
    /// </summary>
    public static IMovementStrategy? Create(MoveMode mode)
    {
        return _factories.TryGetValue(mode, out var factory) ? factory() : null;
    }
}
