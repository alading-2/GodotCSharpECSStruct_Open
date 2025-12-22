using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AutoLoad - 游戏启动引导器 (All-in-One 版)
/// <para>核心职责：</para>
/// <para>1. 统一管理全局单例：取代 Godot 节点树中杂乱的多个 Autoload，项目只需注册这一个真正的 Autoload。</para>
/// <para>2. 加载顺序控制：通过优先级（Priority）确保底层服务先于业务逻辑启动。</para>
/// <para>3. 依赖关系检查：在实例化前验证依赖项是否已就绪，避免空指针崩溃。</para>
/// <para>4. 类型安全访问：提供泛型接口，消除 GetNode("/root/...") 带来的字符串硬编码。</para>
/// </summary>
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
    /// <param name="Name">单例在场景树中的节点名称，也用于注册索引</param>
    /// <param name="Path">资源路径（支持 .tscn 场景或 .cs/.gd 脚本）</param>
    /// <param name="Priority">加载优先级，数值越小越先加载</param>
    /// <param name="Dependencies">依赖的其他单例名称列表</param>
    private record ManagerConfig(string Name, string Path, int Priority, string[] Dependencies);

    /// <summary>
    /// 存储待加载单例的配置清单。
    /// </summary>
    private readonly List<ManagerConfig> _configs = new();

    /// <summary>
    /// 运行时容器，存储已实例化的单例节点引用。
    /// </summary>
    private readonly Dictionary<string, Node> _singletons = new();

    /// <summary>
    /// 优先级常量定义，用于规范化加载顺序。
    /// </summary>
    public static class Priority
    {
        public const int Core = 0;      // 核心工具（如日志写入器、事件总线）
        public const int System = 100;  // 系统服务（如音频管理器、资源加载器）
        public const int Game = 200;    // 游戏业务（如战斗管理器、玩家数据中心）
        public const int Debug = 900;   // 调试专用工具（仅在开发环境使用）
    }

    /// <summary>
    /// Godot 生命周期回调：当此节点进入场景树时触发启动流程。
    /// </summary>
    public override void _Ready()
    {
        // 初始化单例静态引用
        Instance = this;
        _log.Info("🚀 游戏启动序列开始...");

        // 1. [配置阶段] 收集所有需要加载的模块信息
        Configure();

        // 2. [执行阶段] 按照优先级和依赖关系加载模块
        LoadAll();
    }

    /// <summary>
    /// 配置中心：在这里显式注册项目中所有的全局管理器。
    /// 所有的 Register 调用都应在此方法内完成。
    /// </summary>
    private void Configure()
    {
        // === 核心层 (Priority.Core) ===
        // 示例: Register("LogWriter", "res://Src/Tools/Logger/LogWriter.cs", Priority.Core);

        // === 系统层 (Priority.System) ===
        // 示例: Register("AudioManager", "res://Src/Managers/AudioManager.tscn", Priority.System);

        // === 业务层 (Priority.Game) ===
        // 示例: Register("GameManager", "res://Src/Managers/GameManager.cs", Priority.Game, dependsOn: "AudioManager");
    }

    /// <summary>
    /// 注册一个管理器到待加载清单。
    /// </summary>
    /// <param name="name">全局唯一的名称（建议与类名一致）</param>
    /// <param name="path">资源文件路径</param>
    /// <param name="priority">加载阶段/优先级</param>
    /// <param name="dependsOn">该管理器启动前必须就绪的依赖项名称</param>
    private void Register(string name, string path, int priority, params string[] dependsOn)
    {
        _configs.Add(new ManagerConfig(name, path, priority, dependsOn));
    }

    /// <summary>
    /// 对清单中的所有配置进行排序并依次加载。
    /// </summary>
    private void LoadAll()
    {
        // 基于优先级数值升序排列，确保底层模块先处理
        _configs.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var config in _configs)
        {
            LoadOne(config);
        }

        _log.Success($"✅ 启动完成! 共成功加载 {_singletons.Count} 个全局模块。");
    }

    /// <summary>
    /// 处理单个模块的加载、实例化与注册逻辑。
    /// </summary>
    private void LoadOne(ManagerConfig config)
    {
        // 1. 依赖性预检：如果声明了依赖但依赖项未加载，则报错跳过
        foreach (var dep in config.Dependencies)
        {
            if (!_singletons.ContainsKey(dep))
            {
                _log.Error($"💥 加载失败: [{config.Name}] 依赖于 [{dep}]，但后者尚未就绪！请检查加载优先级。");
                return;
            }
        }

        // 2. 路径有效性检查
        if (!ResourceLoader.Exists(config.Path))
        {
            _log.Error($"❌ 找不到资源文件: {config.Path}");
            return;
        }

        Node? instance = null;
        try
        {
            // 加载 Godot 资源
            var res = GD.Load(config.Path);

            if (res is PackedScene scene)
            {
                // 如果是场景文件 (.tscn)，通过实例化生成节点
                instance = scene.Instantiate();
            }
            else if (res is Script script)
            {
                // 在 Godot 4 C# 中，Script 类本身不包含 New() 方法 (这是 GDScript 的语法糖)
                // 我们通过 Call("new") 来调用构造函数，它会返回一个包含新对象的 Variant
                instance = script.Call("new").As<Node>();
            }

            if (instance == null) throw new Exception("资源实例化后的对象为空，或未继承自 Node 类型。");
        }
        catch (Exception e)
        {
            _log.Error($"❌ 实例化模块 [{config.Name}] 时发生错误: {e.Message}");
            return;
        }

        // 3. 节点挂载与单例注册
        // 设置节点在场景树中的名称，方便调试观察
        instance.Name = config.Name;
        // 将单例作为引导器的子节点，其生命周期将随引导器直到游戏结束
        AddChild(instance);
        // 存入运行时容器，供后续通过 Get<T>() 查找
        _singletons[config.Name] = instance;

        _log.Info($"📦 [Loaded] {config.Name} (Priority: {config.Priority})");
    }

    // ==========================================
    // 公共 API：供其他业务逻辑调用
    // ==========================================

    /// <summary>
    /// 根据注册名称获取单例实例。
    /// </summary>
    /// <typeparam name="T">期望的类型</typeparam>
    /// <param name="name">注册时使用的名称</param>
    /// <returns>实例引用，若不存在则返回 null</returns>
    public T? Get<T>(string name) where T : class
    {
        if (_singletons.TryGetValue(name, out var node))
            return node as T;
        return null;
    }

    /// <summary>
    /// [推荐用法] 根据类型名称自动查找单例。
    /// 要求注册名（Name）必须与类名一致。
    /// 示例：AutoLoad.Instance.Get&lt;AudioManager&gt;();
    /// </summary>
    /// <typeparam name="T">管理器类型</typeparam>
    /// <returns>强类型实例引用</returns>
    public T? Get<T>() where T : class
    {
        // 约定优于配置：尝试通过类型名称查找同名注册项
        var name = typeof(T).Name;
        return Get<T>(name);
    }
}
