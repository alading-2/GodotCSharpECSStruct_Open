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
    /// 获取资源的加载路径
    /// </summary>
    /// <param name="name">资源名称 (类名或文件名)</param>
    /// <returns>资源路径 (res://...)，未找到返回 null</returns>
    /// <summary>
    /// 获取资源的加载路径 (泛型版本)
    /// </summary>
    /// <typeparam name="T">资源对应的类型 (使用类名作为 Key)</typeparam>
    /// <returns>资源路径</returns>
    public static string? GetPath<T>()
    {
        return GetPath(typeof(T).Name);
    }

    public static string? GetPath(string name)
    {
        if (ResourcePaths.All.TryGetValue(name, out var data))
        {
            return data.Path;
        }

        _log.Error($"未能在 ResourcePaths 中找到名为 '{name}' 的资源路径。请检查 ResourceGenerator 是否运行。");
        return null;
    }

    /// <summary>
    /// 获取指定分类下的所有资源路径
    /// </summary>
    /// <param name="category">资源分类</param>
    /// <returns>资源路径列表</returns>
    public static List<string> GetPathsByCategory(ResourceCategory category)
    {
        return ResourcePaths.All.Values
            .Where(data => data.Category == category)
            .Select(data => data.Path)
            .ToList();
    }

    public static List<string> GetNamesByCategory(ResourceCategory category)
    {
        return ResourcePaths.All
            .Where(kvp => kvp.Value.Category == category)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// 快速检查是否存在指定名称的资源
    /// </summary>
    public static bool Has(string name)
    {
        return ResourcePaths.All.ContainsKey(name);
    }
}
