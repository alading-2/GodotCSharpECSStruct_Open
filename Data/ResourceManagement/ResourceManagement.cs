using System;
using System.Collections.Generic;
using System.Linq;
using Brotato.Data.ResourceManagement;

/// <summary>
/// 资源注册表 - 统一管理项目中所有 **资产 (Assets)** 的路径索引
/// 
/// 【定位说明】
/// - 这是一个 **纯C#** 静态工具类，不依赖 Godot 引擎 API。
/// - 仅负责提供资源路径查询，不负责资源加载。
/// <example>
/// ResourceManagement.GetPath<EnemyEntity>()
/// ResourceManagement.GetPath("德鲁伊")
/// </example>
/// -数据源来自自动生成的 <see cref="ResourcePaths"/> 类。
/// </summary>
public static class ResourceManagement
{
    private static readonly Log _log = new("ResourceManagement");

    static ResourceManagement()
    {
    }

    // ========================================
    // 静态快捷 API
    // ========================================

    /// <summary>
    /// 从指定分类加载资源
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
        return category switch
        {
            ResourceCategory.Entity => ResourcePaths.Entities,
            ResourceCategory.Component => ResourcePaths.Components,
            ResourceCategory.UI => ResourcePaths.UI,
            ResourceCategory.Asset => ResourcePaths.Assets,
            ResourceCategory.EnemyConfig => ResourcePaths.EnemyConfigs,
            ResourceCategory.PlayerConfig => ResourcePaths.PlayerConfigs,
            ResourceCategory.AbilityConfig => ResourcePaths.AbilityConfigs,
            ResourceCategory.ItemConfig => ResourcePaths.ItemConfigs,
            _ => ResourcePaths.Other
        };
    }
}
