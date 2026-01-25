using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// 资源注册表 - 统一管理项目中所有 **资产 (Assets)** 的索引
/// 
/// 【定位说明】
/// - **资产 (Assets)**：场景 (.tscn)、音效、纹理等，需要通过 ResourceManagement 管理。
/// - **数据 (Data)**：数值属性、生成规则等，**不再**使用 ResourceManagement，而是直接使用纯 C# 类（如 EnemyData）管理。
/// 
/// 【核心优势】
/// - 编辑器友好：通过 [Export] 在 Inspector 中配置资源项
/// - 路径安全：资源引用失效时编辑器立即报错，不会出现硬编码路径失效导致的运行时崩溃
/// - 自动重构：在 Godot 编辑器中移动资源文件时，UID 引用会自动更新
/// 
/// 【支持的资源类型】
/// - 预制体/场景 (.tscn): 映射为 PackedScene，加载后需调用 Instantiate()
/// 
/// 【使用方式】
/// 1. 在 Godot 编辑器中打开 ResourceManagement.tscn
/// 2. 在 Inspector 的 Resources 数组中添加 ResourceEntry
/// 3. 配置资源的简写名称 (Name)、分类 (Category) 并拖入资源文件 (Data)
/// 4. 代码中通过 ResourceManagement.Load<T>("Name") 加载
/// </summary>
[Tool]
[GlobalClass]
public partial class ResourceManagement : Node
{
    private static readonly Log _log = new("ResourceManagement");

    /// <summary>
    /// 模块初始化：在程序集加载时自动向 AutoLoad 注册。
    /// 这确保了 ResourceManagement 节点会随场景树启动。
    /// </summary>
    /// <summary>
    /// 模块初始化：在程序集加载时自动向 AutoLoad 注册。
    /// 这确保了 ResourceManagement 节点会随场景树启动。
    /// </summary>
    // [ModuleInitializer]
    // public static void Initialize()
    // {
    //     AutoLoad.Register(new AutoLoad.AutoLoadConfig
    //     {
    //         Name = "ResourceManagement",
    //         Path = "res://Data/ResourceManagement/ResourceManagement.tscn",
    //         Priority = AutoLoad.Priority.Core  // 核心优先级，确保资源加载器早于其他业务系统加载
    //     });
    // }

    /// <summary>全局单例访问入口</summary>
    public static ResourceManagement? Instance { get; private set; }

    // 内部缓存，用于提高运行时查找效率
    /// <summary>按名称索引的资源缓存</summary>
    private readonly Dictionary<string, Resource> _nameCache = new();
    /// <summary>按分类索引的资源缓存</summary>
    private readonly Dictionary<ResourceCategory, List<Resource>> _categoryCache = new();

    public override void _EnterTree()
    {
        Instance = this;
        BuildCache();
        _log.Success("ResourceManagement 初始化完成并构建缓存");
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null!;
        _nameCache.Clear();
        _categoryCache.Clear();
    }

    /// <summary>
    /// 构建内部加速索引（从 ResourcePaths 构建 Dictionary）。
    /// </summary>
    private void BuildCache()
    {
        _nameCache.Clear();
        _categoryCache.Clear();

        foreach (var kvp in ResourcePaths.All)
        {
            var name = kvp.Key;
            var path = kvp.Value;

            // 延迟加载策略：这里我们不立即 Load，而是等到请求时再 Load，或者为了兼容性，我们先 Load？
            // 原有的 ResourceManagement 是持有 Resource 引用的，意味着启动时全部加载。
            // 为了保持行为一致，这里进行预加载。
            // 如果资源量巨大，建议改为按需加载 (Lazy Load)。但目前为了稳妥，先保持预加载。

            try
            {
                var resource = GD.Load<Resource>(path);
                if (resource == null)
                {
                    _log.Error($"无法加载资源: {name} ({path})");
                    continue;
                }

                _nameCache[name] = resource;

                // 推断分类
                var category = GetCategoryFromPath(path);
                if (!_categoryCache.ContainsKey(category))
                {
                    _categoryCache[category] = new List<Resource>();
                }
                _categoryCache[category].Add(resource);
            }
            catch (System.Exception e)
            {
                _log.Error($"加载资源异常: {name} ({path}). Error: {e.Message}");
            }
        }

        _log.Debug($"缓存构建完成: 共 {_nameCache.Count} 个资源, 分布在 {_categoryCache.Count} 个分类中");
    }

    private ResourceCategory GetCategoryFromPath(string path)
    {
        string p = path.ToLower();

        if (p.Contains("/ui/"))
            return ResourceCategory.UI;

        if (p.Contains("/component/"))
            return ResourceCategory.Component;

        if (p.Contains("/entity/"))
            return ResourceCategory.Entity;

        if (p.Contains("/assets/"))
            return ResourceCategory.Asset;

        return ResourceCategory.Other;
    }

    // ========================================
    // 静态快捷 API
    // ========================================

    /// <summary>
    /// 类型安全的资源加载。通常用`LoadScene`而不是`Load`
    /// </summary>
    /// <param name="name">资源名称</param>
    /// <returns>找到并匹配类型的资源对象，失败返回 null</returns>
    private static T? Load<T>(string name) where T : Resource
    {
        if (Instance == null)
        {
            _log.Error($"尝试加载资源 '{name}' 失败: ResourceManagement 单例未就绪");
            return null;
        }

        if (!Instance._nameCache.TryGetValue(name, out var resource))
        {
            _log.Error($"未能在注册表中找到名为 '{name}' 的资源。请检查 ResourceManagement.tscn 配置");
            return null;
        }

        if (resource is T typed)
        {
            return typed;
        }

        _log.Error($"资源 '{name}' 类型不匹配。配置类型: {resource.GetType().Name}, 期望类型: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 类型安全的场景加载 - 自动使用类型名作为资源名称。
    /// 要求: ResourceManagement.tscn 中的 Name 必须与类型名完全一致。
    /// 
    /// 示例:
    /// <code>
    /// // 加载 Player 场景 (自动查找名为 "Player" 的资源)
    /// var scene = ResourceManagement.LoadScene<Player>();
    /// var player = scene.Instantiate<Player>();
    /// 
    /// // 等价于:
    /// var scene = ResourceManagement.Load<PackedScene>(nameof(Player));
    /// </code>
    /// </summary>
    /// <typeparam name="TEntity">Entity 类型 (如 Player, Enemy)</typeparam>
    /// <returns>找到的 PackedScene，失败返回 null</returns>
    public static PackedScene? LoadScene<TEntity>() where TEntity : Node
    {
        return Load<PackedScene>(typeof(TEntity).Name);
    }

    /// <summary>
    /// 通用资源加载。不进行类型强制转换。
    /// 适用于需要动态识别类型的底层系统。
    /// </summary>
    /// <param name="name">资源简写名称</param>
    /// <returns>资源基类对象，失败返回 null</returns>
    public static Resource? GetResource(string name)
    {
        if (Instance == null)
        {
            _log.Error($"尝试加载资源 '{name}' 失败: ResourceManagement 单例未就绪");
            return null;
        }

        if (Instance._nameCache.TryGetValue(name, out var resource))
        {
            return resource;
        }

        _log.Error($"未能在注册表中找到名为 '{name}' 的资源。请检查 ResourceManagement.tscn 配置");
        return null;
    }

    /// <summary>
    /// 批量加载指定分类下的所有资源，并转换为指定类型。
    /// 特别适用于波次生成系统加载所有符合条件的生成规则。
    /// </summary>
    /// <typeparam name="T">资源子类型</typeparam>
    /// <param name="category">目标分类</param>
    /// <returns>匹配类型的资源列表，若分类下无资源则返回空列表</returns>
    public static List<T> LoadAllInCategory<T>(ResourceCategory category) where T : Resource
    {
        if (Instance == null)
        {
            _log.Error("尝试按分类加载资源失败: ResourceManagement 单例未就绪");
            return new List<T>();
        }

        if (!Instance._categoryCache.TryGetValue(category, out var resources))
        {
            _log.Debug($"分类 {category} 下目前没有注册任何资源");
            return new List<T>();
        }

        var result = resources.OfType<T>().ToList();
        _log.Debug($"分类 {category} 加载完成: 匹配类型 {typeof(T).Name} 的资源共 {result.Count} 个");
        return result;
    }

    /// <summary>
    /// 获取指定分类下的所有注册名称。
    /// 常用于 UI 列表填充或调试列表。
    /// </summary>
    /// <param name="category">目标分类</param>
    /// <returns>名称字符串列表</returns>
    public static List<string> GetNamesInCategory(ResourceCategory category)
    {
        if (Instance == null)
        {
            return new List<string>();
        }

        var result = new List<string>();
        foreach (var kvp in ResourcePaths.All)
        {
            if (Instance.GetCategoryFromPath(kvp.Value) == category)
            {
                result.Add(kvp.Key);
            }
        }
        return result;
    }

    /// <summary>
    /// 快速检查是否存在指定名称的资源。
    /// </summary>
    /// <param name="name">资源简写名称</param>
    /// <returns>存在则返回 true</returns>
    public static bool Has(string name)
    {
        return Instance?._nameCache.ContainsKey(name) == true;
    }
}
