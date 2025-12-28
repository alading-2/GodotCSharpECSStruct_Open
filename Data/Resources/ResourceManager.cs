using Godot;
using System.Collections.Generic;

/// <summary>
/// 资源管理器 - 统一管理项目中 Resource 的路径映射。
/// 允许通过简写（如 "Player"）快速获取对应的 Resource 对象。
/// </summary>
public static class ResourceManager
{
    private static readonly Log _log = new("ResourceManager");

    /// <summary>
    /// 资源路径注册表。
    /// Key: 简写名称 (Shorthand)
    /// Value: 资源文件在项目中的完整路径 (res://...)
    /// </summary>
    private static readonly Dictionary<string, string> _pathRegistry = new()
    {
        { "Player", "res://Resources/PLayer/PlayerAttribute.tres" },
        { "WellDone", "res://Resources/PLayer/WellDone.tres" },
        { "Ranger", "res://Resources/PLayer/Ranger.tres" },
        { "Knight", "res://Resources/PLayer/Knight.tres" }
    };

    /// <summary>
    /// 缓存已加载的资源，避免重复加载。
    /// </summary>
    private static readonly Dictionary<string, Resource> _resourceCache = new();

    /// <summary>
    /// 根据简写名称获取 Resource 对象。
    /// </summary>
    /// <param name="name">注册表中的简写名称</param>
    /// <returns>加载后的 Resource 对象，如果未找到或加载失败则返回 null</returns>
    public static Resource? GetResource(string name)
    {
        // 1. 检查缓存
        if (_resourceCache.TryGetValue(name, out var cachedRes))
        {
            return cachedRes;
        }

        // 2. 检查注册表
        if (!_pathRegistry.TryGetValue(name, out var path))
        {
            _log.Error($"未能在 ResourceManager 中找到简写名为 '{name}' 的资源路径。");
            return null;
        }

        // 3. 加载资源
        if (!FileAccess.FileExists(path))
        {
            _log.Error($"资源文件不存在: {path} (简写名: {name})");
            return null;
        }

        var res = GD.Load<Resource>(path);
        if (res != null)
        {
            _resourceCache[name] = res;
            _log.Trace($"成功加载资源: {name} -> {path}");
        }
        else
        {
            _log.Error($"加载资源失败: {path}");
        }

        return res;
    }

    /// <summary>
    /// (可选) 手动注册新路径。
    /// </summary>
    public static void RegisterPath(string name, string path)
    {
        _pathRegistry[name] = path;
        // 如果之前有缓存，清除它以确保重新加载
        _resourceCache.Remove(name);
    }
}
