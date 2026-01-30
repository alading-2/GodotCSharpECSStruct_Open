using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// 资源管理器 - 统一管理项目中所有 **资产 (Assets)** 和 **配置 (Configs)** 的加载
/// 
/// 【说明】
/// - 这是一个静态工具类，封装了 Godot 的加载逻辑。
/// - 强制使用 ResourceCategory 分类管理，禁止硬编码 res:// 路径。
/// - 数据源来自自动生成的 <see cref="ResourcePaths"/> 类。
/// </summary>
public static class ResourceManagement
{
    private static readonly Log _log = new(nameof(ResourceManagement));

    static ResourceManagement()
    {
    }

    // ========================================
    // 静态快捷 API
    // ========================================

    /// <summary>
    /// 从指定分类加载资源，能够获取.tscn,.tres
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="name">资源名称</param>
    /// <param name="category">资源分类</param>
    /// <returns>加载的资源，失败返回 null</returns>
    public static T? Load<T>(string name, ResourceCategory category) where T : class
    {
        var dict = GetDictionaryByCategory(category);
        if (dict.TryGetValue(name, out var data))
        {
            return Godot.GD.Load<T>(data.Path);
        }

        _log.Error($"未找到资源: {category}/{name}");
        return null;
    }



    /// <summary>
    /// 加载指定分类下的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="category">资源分类</param>
    /// <returns>资源列表</returns>
    public static List<T> LoadAll<T>(ResourceCategory category) where T : class
    {
        var dict = GetDictionaryByCategory(category);
        var results = new List<T>();

        foreach (var kvp in dict)
        {
            var resource = Godot.GD.Load<T>(kvp.Value.Path);
            if (resource != null)
                results.Add(resource);
            else
                _log.Warn($"加载失败: {category}/{kvp.Key} ({kvp.Value.Path})");
        }

        return results;
    }

    /// <summary>
    /// 获取分类下所有资源名称
    /// </summary>
    public static List<string> GetNames(ResourceCategory category)
    {
        var dict = GetDictionaryByCategory(category);
        return dict.Keys.ToList();
    }

    /// <summary>
    /// 根据分类获取对应的字典
    /// </summary>
    private static Dictionary<string, ResourceData> GetDictionaryByCategory(ResourceCategory category)
    {
        if (ResourcePaths.Resources.TryGetValue(category, out var dict))
        {
            return dict;
        }
        _log.Error($"未找到分类字典: {category}");
        return new Dictionary<string, ResourceData>();
    }
}
