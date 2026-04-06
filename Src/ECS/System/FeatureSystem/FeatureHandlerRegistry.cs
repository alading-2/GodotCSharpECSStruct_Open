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
/// 分组查询：
/// <code>
/// // 获取所有 Ability 处理器
/// var all = FeatureHandlerRegistry.GetByGroup(FeatureId.Ability.Groups.Root);
/// // 获取所有主动技能处理器
/// var actives = FeatureHandlerRegistry.GetByGroup(FeatureId.Ability.Groups.Active);
/// </code>
/// </summary>
public static class FeatureHandlerRegistry
{
    private static readonly Log _log = new(nameof(FeatureHandlerRegistry));
    private static readonly Dictionary<string, IFeatureHandler> _handlers = new();
    private static readonly Dictionary<string, List<IFeatureHandler>> _groupIndex = new();

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

        // 按分组逐级索引："Ability.Movement" 同时注册到 "Ability" 和 "Ability.Movement"
        IndexGroup(handler);

        _log.Info($"注册 FeatureHandler: {handler.FeatureId} (group: {handler.FeatureGroup})");
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

    /// <summary>
    /// 获取指定分组及其所有子分组下的全部处理器。
    /// group 使用 FeatureId.Ability.Groups.* 常量，如 "Ability.Active"。
    /// </summary>
    public static IReadOnlyList<IFeatureHandler> GetByGroup(string group)
    {
        if (string.IsNullOrEmpty(group)) return System.Array.Empty<IFeatureHandler>();
        return _groupIndex.TryGetValue(group, out var list)
            ? list
            : System.Array.Empty<IFeatureHandler>();
    }

    /// <summary>获取指定分组下所有处理器的 FeatureId 列表。</summary>
    public static IReadOnlyList<string> GetIdsByGroup(string group)
    {
        if (string.IsNullOrEmpty(group)) return System.Array.Empty<string>();
        if (!_groupIndex.TryGetValue(group, out var list)) return System.Array.Empty<string>();
        var ids = new string[list.Count];
        for (int i = 0; i < list.Count; i++) ids[i] = list[i].FeatureId;
        return ids;
    }

    // ==================== 内部工具 ====================

    private static void IndexGroup(IFeatureHandler handler)
    {
        var group = handler.FeatureGroup;
        if (string.IsNullOrEmpty(group)) return;

        // 拆 "Ability.Movement" → 注册到 "Ability" 和 "Ability.Movement"
        int start = 0;
        while (true)
        {
            int dot = group.IndexOf('.', start);
            var key = dot < 0 ? group : group.Substring(0, dot);
            if (!_groupIndex.TryGetValue(key, out var list))
                _groupIndex[key] = list = new List<IFeatureHandler>();
            if (!list.Contains(handler)) list.Add(handler);
            if (dot < 0) break;
            start = dot + 1;
        }
    }
}
