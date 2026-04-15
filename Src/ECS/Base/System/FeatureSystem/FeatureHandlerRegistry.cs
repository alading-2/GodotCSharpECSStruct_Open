using System.Collections.Generic;

/// <summary>
/// Feature 处理器注册表 - 管理代码驱动的生命周期处理器
///
/// 使用方式（在 [ModuleInitializer] 方法中注册）：
/// <code>
/// [ModuleInitializer]
/// public static void Init()
/// {
///     FeatureHandlerRegistry.Register(new MyFeatureHandler());
/// }
/// </code>
///
/// </summary>
public static class FeatureHandlerRegistry
{
    private static readonly Log _log = new(nameof(FeatureHandlerRegistry));
    private static readonly Dictionary<string, IFeatureHandler> _handlers = new();

    // ==================== 注册 ====================

    /// <summary>注册一个 Feature 处理器</summary>
    public static void Register(IFeatureHandler handler)
    {
        if (handler == null || string.IsNullOrEmpty(handler.FeatureId))
        {
            _log.Warn("注册 FeatureHandler 失败：handler 为空或 FeatureId 为空");
            return;
        }

        if (_handlers.ContainsKey(handler.FeatureId))
        {
            _log.Warn($"FeatureHandler 已存在，覆盖注册: {handler.FeatureId}");
        }

        _handlers[handler.FeatureId] = handler;

        _log.Info($"注册 FeatureHandler: {handler.FeatureId}");
    }

    // ==================== 查询 ====================

    /// <summary>根据 FeatureId 获取处理器（未注册返回 null）</summary>
    public static IFeatureHandler? Get(string featureId)
    {
        if (string.IsNullOrEmpty(featureId)) return null;
        return _handlers.TryGetValue(featureId, out var handler) ? handler : null;
    }

    /// <summary>是否已注册指定 FeatureId 的处理器</summary>
    public static bool HasHandler(string featureId)
        => !string.IsNullOrEmpty(featureId) && _handlers.ContainsKey(featureId);
}
