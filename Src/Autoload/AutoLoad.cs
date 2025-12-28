using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// AutoLoad - 游戏启动引导器 (去中心化版)
/// <para>核心职责：</para>
/// <para>1. 统一管理全局单例：项目在 Godot 设置中只需注册这一个 Autoload。</para>
/// <para>2. 自动注册机制：各系统通过 [ModuleInitializer] 自行向引导器注册，实现解耦。</para>
/// <para>3. 加载顺序控制：通过 Priority (数值越小越先加载) 确保底层服务先于业务逻辑启动。</para>
/// <para>4. 类型安全访问：提供泛型接口 AutoLoad.Instance.Get<T>()，消除字符串硬编码。</para>
/// </summary>
/// <example>
/// 推荐的系统注册方式（在各自系统中实现）：
/// <code>
/// [ModuleInitializer]
/// public static void Initialize()
/// {
///     AutoLoad.Register("AudioManager", "res://Src/Managers/AudioManager.tscn", AutoLoad.Priority.System);
/// }
/// </code>
/// </example>
public partial class AutoLoad : Node
{
    // 使用项目定义的 Log 工具记录启动日志
    private static readonly Log _log = new Log("AutoLoad");

    /// <summary>
    /// 全局静态访问点。
    /// 在 C# 脚本中通过 AutoLoad.Instance 即可获取此引导器实例。
    /// </summary>
    public static AutoLoad Instance { get; private set; } = default!;

    /// <summary>
    /// 内部配置结构，存储单例的元数据。
    /// </summary>
    internal record ManagerConfig(string Name, string Path, int Priority, string[] Dependencies);

    /// <summary>
    /// 存储通过静态方式预注册的配置。
    /// </summary>
    private static readonly List<ManagerConfig> _staticConfigs = new();

    /// <summary>
    /// 运行时容器，存储已实例化的单例节点引用。
    /// </summary>
    private readonly Dictionary<string, Node> _singletons = new();

    /// <summary>
    /// 当前待加载的配置队列。
    /// </summary>
    private readonly List<ManagerConfig> _configs = new();

    /// <summary>
    /// 优先级常量定义。数值越小，加载越早。
    /// </summary>
    public static class Priority
    {
        public const int Core = 0;      // 核心基础（日志、事件总线）
        public const int System = 100;  // 系统服务（音频、资源加载、生成系统）
        public const int Game = 200;    // 游戏业务（战斗逻辑、关卡管理）
        public const int Debug = 900;   // 调试工具
    }

    /// <summary>
    /// Godot 生命周期回调：当此节点进入场景树时触发启动流程。
    /// </summary>
    public override void _Ready()
    {
        // 初始化单例静态引用
        Instance = this;
        _log.Info("🚀 游戏启动序列开始...");

        // 1. 合并所有预注册的静态配置
        _configs.AddRange(_staticConfigs);

        // 2. 执行遗留的显式配置（不推荐）
        Configure();

        // 3. 执行加载流程
        LoadAll();
    }

    /// <summary>
    /// [不推荐使用] 显式配置中心。
    /// 建议使用各系统的 [ModuleInitializer] 进行自动注册。
    /// 此处仅保留用于处理无法修改源码的第三方模块或特殊快速测试。
    /// </summary>
    private void Configure()
    {
        // 仅在特殊情况下在此处 Register
    }

    /// <summary>
    /// 静态注册接口：允许任何类在任何地方向 AutoLoad 注册。
    /// 推荐在各自类的 [ModuleInitializer] 中调用。
    /// </summary>
    /// <param name="name">全局唯一标识名（建议与类名一致）</param>
    /// <param name="path">资源路径 (.tscn 或 .cs)</param>
    /// <param name="priority">加载优先级 (Priority 常量)</param>
    /// <param name="dependsOn">依赖项的名称列表</param>
    public static void Register(string name, string path, int priority, params string[] dependsOn)
    {
        var config = new ManagerConfig(name, path, priority, dependsOn);

        if (Instance != null)
        {
            // 如果引导器已在运行，则动态注入并立即加载
            Instance._configs.Add(config);
            Instance.LoadOne(config);
        }
        else
        {
            // 否则加入待处理队列，等待 _Ready 统一处理
            _staticConfigs.Add(config);
        }
    }

    /// <summary>
    /// 按照优先级和依赖顺序加载所有已注册模块。
    /// </summary>
    private void LoadAll()
    {
        _configs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var config in _configs)
        {
            LoadOne(config);
        }

        _log.Success($"✅ 初始化序列完成! 共激活 {_singletons.Count} 个全局模块。");
    }

    /// <summary>
    /// 实例化并挂载单个模块。
    /// </summary>
    private void LoadOne(ManagerConfig config)
    {
        if (_singletons.ContainsKey(config.Name)) return;

        // 依赖检查
        foreach (var dep in config.Dependencies)
        {
            if (!_singletons.ContainsKey(dep))
            {
                _log.Error($"💥 [{config.Name}] 加载失败: 依赖项 [{dep}] 未就绪。");
                return;
            }
        }

        if (!ResourceLoader.Exists(config.Path))
        {
            _log.Error($"❌ 路径不存在: {config.Path}");
            return;
        }

        try
        {
            var res = GD.Load(config.Path);
            Node? instance = res switch
            {
                PackedScene scene => scene.Instantiate(),
                CSharpScript script => script.New().As<Node>(),
                _ => null
            };

            if (instance == null) throw new Exception("无法创建节点实例。");

            instance.Name = config.Name;
            AddChild(instance);
            _singletons[config.Name] = instance;

            _log.Info($"📦 [Loaded] {config.Name}");
        }
        catch (Exception e)
        {
            _log.Error($"❌ 模块 [{config.Name}] 实例化异常: {e.Message}");
        }
    }

    /// <summary>
    /// 获取指定的单例实例。
    /// </summary>
    public T? Get<T>() where T : class
    {
        var name = typeof(T).Name;
        if (_singletons.TryGetValue(name, out var node))
            return node as T;
        return null;
    }
}
