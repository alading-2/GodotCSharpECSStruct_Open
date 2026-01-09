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
/// using System.Runtime.CompilerServices;
/// public partial class MySystem : Node
/// {
///     [ModuleInitializer]
///     public static void Initialize()
///     {
///         AutoLoad.Register(new AutoLoad.AutoLoadConfig
///         {
///             Name = nameof(MySystem),
///             Path = "res://Src/Systems/MySystem.cs", // 或 .tscn 路径
///             Priority = AutoLoad.Priority.System,
///             // 可选：指定挂载父节点，默认为 "AutoLoad"
///             ParentPath = "AutoLoad/MySystems",
///             // 可选：指定依赖项，确保依赖模块先加载
///             Dependencies = new[] { "OtherSystem" }
//         });
//     }
// }
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
    /// AutoLoad 配置对象
    /// </summary>
    public record AutoLoadConfig
    {
        /// <summary> 全局唯一标识名（建议与类名一致） </summary>
        public required string Name { get; init; }
        /// <summary> 资源路径 (.tscn 或 .cs) </summary>
        public required string Path { get; init; }
        /// <summary> 加载优先级 (Priority 常量) </summary>
        public required int Priority { get; init; }
        /// <summary> 父节点路径 (例如 "AutoLoad/DataRegistry")，默认为 "AutoLoad" </summary>
        public string ParentPath { get; init; } = "AutoLoad";
        /// <summary> 依赖项的名称列表 </summary>
        public string[] Dependencies { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// 存储通过静态方式预注册的配置。
    /// </summary>
    private static readonly List<AutoLoadConfig> _staticConfigs = new();

    /// <summary>
    /// 运行时容器，存储已实例化的单例节点引用。
    /// </summary>
    private readonly Dictionary<string, Node> _singletons = new();



    /// <summary>
    /// 优先级常量定义。数值越小，加载越早。
    /// </summary>
    public static class Priority
    {
        public const int Core = 0;      // 核心基础（日志、事件总线）
        public const int Tool = 100;    // 工具类（如调试工具）
        public const int System = 200;  // 系统服务（音频、资源加载、生成系统）
        public const int Game = 300;    // 游戏业务（战斗逻辑、关卡管理）
        public const int Debug = 400;   // 调试工具
    }

    /// <summary>
    /// Godot 生命周期回调：当此节点进入场景树时触发启动流程。
    /// </summary>
    public override void _Ready()
    {
        // 初始化单例静态引用
        Instance = this;

        // 初始化 ParentManager (注入 Root)
        ParentManager.Init(GetTree().Root);

        _log.Info("🚀 游戏启动序列开始...");


        // 1. 执行遗留的显式配置（不推荐）
        Configure();

        // 2. 执行加载流程
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
    /// <param name="config">注册配置对象</param>
    public static void Register(AutoLoadConfig config)
    {
        if (Instance != null)
        {
            _log.Error($"[AutoLoad] 错过初始化阶段! 无法注册模块 '{config.Name}'。\n" +
                       $"AutoLoad 仅用于游戏启动前的系统引导。如需运行时动态创建系统，请直接实例化 Node 并挂载到 ParentManager。");
            return;
        }

        // [ModuleInitializer]（模块初始化器）：是在 程序集（DLL）被加载时 立即执行的。这是一个非常早期的阶段，发生在 Godot 引擎完全初始化场景树之前。
        // _Ready：是在节点（Node）进入场景树后 才执行的。
        // 正常注册流程（_Ready 执行前）
        _staticConfigs.Add(config);
    }

    /// <summary>
    /// 按照优先级和依赖顺序加载所有已注册模块。
    /// </summary>
    private void LoadAll()
    {
        _staticConfigs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var config in _staticConfigs)
        {
            LoadOne(config);
        }

        // 加载完成后清空静态配置，释放引用
        _staticConfigs.Clear();

        _log.Success($"✅ 初始化序列完成! 共激活 {_singletons.Count} 个全局模块。");
    }

    /// <summary>
    /// 实例化并挂载单个模块。
    /// </summary>
    private void LoadOne(AutoLoadConfig config)
    {
        if (_singletons.ContainsKey(config.Name)) return;

        // 依赖检查
        if (config.Dependencies != null)
        {
            foreach (var dep in config.Dependencies)
            {
                if (!_singletons.ContainsKey(dep))
                {
                    _log.Error($"💥 [{config.Name}] 加载失败: 依赖项 [{dep}] 未就绪。");
                    return;
                }
            }
        }

        if (!ResourceLoader.Exists(config.Path))
        {
            _log.Error($"❌ 路径不存在: {config.Path}");
            return;
        }

        try
        {
            // 1. 加载资源 (支持 .tscn 场景或 .cs 脚本)
            var res = GD.Load(config.Path);

            // 2. 根据资源类型进行实例化
            Node? instance = res switch
            {
                // 如果是场景文件，直接实例化节点树
                PackedScene scene => scene.Instantiate(),
                // 如果是纯 C# 脚本，创建脚本对象并转换为 Node（要求该类必须继承自 Node）
                CSharpScript script => script.New().As<Node>(),
                _ => null
            };

            if (instance == null) throw new Exception("无法创建节点实例。类型不符合要求或实例化失败。");

            // 3. 设置节点名称（在场景树中显示的唯一标识）
            instance.Name = config.Name;

            // 处理挂载点
            Node parent = this; // 默认挂载到 AutoLoad

            parent = ParentManager.EnsurePath(this, config.ParentPath ?? "AutoLoad");

            parent.AddChild(instance);
            _singletons[config.Name] = instance;

            _log.Info($"📦 [Loaded] {config.Name} 注册成功，Priority：{config.Priority}");
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
