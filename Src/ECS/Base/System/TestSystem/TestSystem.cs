using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 运行时测试系统入口。
/// <para>
/// 负责在 Debug 环境下挂载一个可交互的测试面板，统一承接“选中实体”“切换测试模块”“刷新测试数据”等操作。
/// </para>
/// <para>
/// 这个系统本身不承载具体测试逻辑，具体行为由各个 <see cref="TestModuleBase"/> 子模块实现。
/// </para>
/// </summary>
public partial class TestSystem : CanvasLayer
{
    /// <summary>测试系统日志器，用于记录初始化与关键调试操作。</summary>
    private static readonly Log _log = new(nameof(TestSystem));

    /// <summary>测试系统单例，方便其它调试工具直接访问当前测试面板。</summary>
    public static TestSystem? Instance { get; private set; }

    /// <summary>当前被测试面板选中的实体；属性面板、技能面板等都会围绕它刷新。</summary>
    public IEntity? SelectedEntity => _selectionContext?.SelectedEntity;

    /// <summary>当前已注册的测试模块列表，顺序就是下拉菜单显示顺序。</summary>
    private readonly List<ITestModule> _modules = new();

    /// <summary>模块 Id 到模块实例的快速映射。</summary>
    private readonly Dictionary<string, ITestModule> _modulesById = new(StringComparer.Ordinal);

    /// <summary>所有测试模块 Id 的列表，用于下拉选择器的索引映射。</summary>
    private readonly List<string> _moduleIdsByIndex = new();

    /// <summary>当前真正处于前台的测试模块。</summary>
    private ITestModule? _currentModule;

    /// <summary>模块待刷新缓冲区，供统一刷新调度器在帧末冲刷。</summary>
    private readonly List<TestModuleBase> _pendingRefreshModules = new();

    /// <summary>测试面板根节点，作为所有 UI 的父容器。</summary>
    private Control _root = null!;

    /// <summary>总开关按钮，用来显示或隐藏测试面板。</summary>
    private Button _toggleButton = null!;

    /// <summary>模块下拉选择器，用来切换属性测试、技能测试等子模块。</summary>
    private OptionButton _moduleSelector = null!;

    /// <summary>承载整个调试界面的面板容器。</summary>
    private PanelContainer _panel = null!;

    /// <summary>提示用户如何进入选实体模式的说明文本。</summary>
    private Label _selectionHintLabel = null!;

    /// <summary>是否允许在场景中通过鼠标点击选择实体。</summary>
    private CheckButton _selectionToggle = null!;

    /// <summary>手动清空当前选中实体的按钮。</summary>
    private Button _clearSelectionButton = null!;

    /// <summary>强制刷新当前模块内容的按钮。</summary>
    private Button _refreshButton = null!;

    /// <summary>显示当前选中实体名称、类型和 ID 的标签。</summary>
    private Label _selectedEntityLabel = null!;

    /// <summary>真正放置各个测试模块实例的宿主容器。</summary>
    private VBoxContainer _moduleHost = null!;

    /// <summary>面板当前可见性缓存，用于避免重复处理隐藏/显示逻辑。</summary>
    private bool _panelVisible = true;

    /// <summary>统一选中实体上下文。</summary>
    private TestSelectionContext _selectionContext = null!;

    /// <summary>统一刷新调度器。</summary>
    private TestRefreshScheduler _refreshScheduler = null!;

    /// <summary>模块共享上下文。</summary>
    private TestModuleContext _moduleContext = null!;

    /// <summary>当前帧是否已经安排过一次模块刷新冲刷。</summary>
    private bool _refreshFlushQueued;

    /// <summary>
    /// 模块初始化入口。
    /// <para>
    /// 把 TestSystem 注册到 AutoLoad，确保仅在调试优先级链路中自动挂载。
    /// </para>
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(TestSystem),
            Scene = ResourceManagement.Load<PackedScene>(nameof(TestSystem), ResourceCategory.System),
            Priority = AutoLoad.Priority.Debug,
            ParentPath = "Debug"
        });
    }

    /// <summary>
    /// 进入场景树时做单例校验，并把测试系统设置为始终处理输入。
    /// </summary>
    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;
        Layer = 100;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// 场景准备完成后构建 UI、注册子模块，并设置默认模块。
    /// </summary>
    public override void _Ready()
    {
        // 缓存 UI 节点
        CacheUiNodes();
        // 绑定 UI 事件
        BindUiEvents();
        // 初始化上下文
        InitializeContexts();
        // 从场景中注册模块
        RegisterModulesFromScene();
        // 设置默认模块
        if (_modules.Count > 0)
        {
            TrySwitchModule(_moduleIdsByIndex[0]);
        }
        BindMouseSelectionEvents();
        SyncMouseSelectionRequest(); // 根据当前开关状态同步一次鼠标选择请求
        // 更新选中实体显示
        UpdateSelectedEntityDisplay();
        _log.Info("TestSystem 初始化完成");
    }

    /// <summary>
    /// 离开场景树时清理单例引用，避免调试系统指向失效对象。
    /// </summary>
    public override void _ExitTree()
    {
        _currentModule?.DeactivateModule();
        if (_selectionContext != null)
        {
            _selectionContext.SelectionChanged -= OnSelectionChanged;
        }
        UnbindMouseSelectionEvents();
        CancelMouseSelectionRequest();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 设置当前测试实体。
    /// <para>
    /// 实体切换后会先通知所有模块更新选中目标，再刷新当前激活模块的界面。
    /// </para>
    /// </summary>
    public void SetSelectedEntity(IEntity? entity)
    {
        if (_selectionContext == null)
        {
            return;
        }

        // 设置选中的实体
        if (!_selectionContext.SetSelectedEntity(entity))
        {
            RefreshCurrentModule();
        }
    }

    /// <summary>
    /// 刷新当前显示的测试模块。
    /// <para>
    /// 通过模块下拉框选中的索引找到对应模块，再触发其 Refresh。
    /// </para>
    /// </summary>
    public void RefreshCurrentModule()
    {
        if (!_panelVisible)
        {
            return;
        }

        _currentModule?.Refresh();
    }

    /// <summary>当前激活模块 Id。</summary>
    public string CurrentModuleId => _currentModule?.Definition.Id ?? string.Empty;

    /// <summary>
    /// 按稳定模块 Id 切换当前模块。
    /// </summary>
    public bool TrySwitchModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || !_modulesById.ContainsKey(moduleId))
        {
            return false;
        }

        SwitchModule(moduleId);
        var index = _moduleIdsByIndex.IndexOf(moduleId);
        if (index >= 0)
        {
            _moduleSelector.Select(index); // 同步下拉框选中态
        }

        return true;
    }

    private void InitializeContexts()
    {
        _selectionContext = new TestSelectionContext();
        _selectionContext.SelectionChanged += OnSelectionChanged;
        _refreshScheduler = new TestRefreshScheduler(QueueScheduledModuleFlush);
        _moduleContext = new TestModuleContext(this, _selectionContext, _refreshScheduler);
    }

    /// <summary>
    /// 缓存所有 UI 节点引用。
    /// </summary>
    private void CacheUiNodes()
    {
        // 缓存 UI 节点
        _root = GetNode<Control>("Root");
        _toggleButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/ToggleButton");
        _moduleSelector = GetNode<OptionButton>("Root/TopLeft/Layout/Toolbar/ModuleSelector");
        _panel = GetNode<PanelContainer>("Root/TopLeft/Layout/Panel");
        _selectionHintLabel = GetNode<Label>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/SelectionHintLabel");
        _selectionToggle = GetNode<CheckButton>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/SelectionToggle");
        _refreshButton = GetNode<Button>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/RefreshButton");
        _clearSelectionButton = GetNode<Button>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/ClearSelectionButton");
        _selectedEntityLabel = GetNode<Label>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/SelectedEntityLabel");
        _moduleHost = GetNode<VBoxContainer>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/ModuleHost");

        // 设置鼠标过滤
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout/Toolbar").MouseFilter = Control.MouseFilterEnum.Ignore;
        _panel.MouseFilter = Control.MouseFilterEnum.Stop;
        GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout").MouseFilter = Control.MouseFilterEnum.Ignore;
        _selectionHintLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow").MouseFilter = Control.MouseFilterEnum.Ignore;
        _moduleHost.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// 绑定所有 UI 事件。
    /// </summary>
    private void BindUiEvents()
    {
        _toggleButton.Pressed += OnTogglePressed;
        _moduleSelector.ItemSelected += OnModuleSelected;
        _refreshButton.Pressed += RefreshCurrentModule;
        _clearSelectionButton.Pressed += () => SetSelectedEntity(null);
        _selectionToggle.Toggled += OnSelectionToggleToggled;
    }

    /// <summary>
    /// 注册一个测试模块，并把它挂到统一的 UI 宿主中。
    /// </summary>
    private void RegisterModule(ITestModule module)
    {
        // 1. 初始化模块
        module.Initialize(_moduleContext);
        // 2. 添加到模块列表
        _modules.Add(module);
        // 3. 添加到模块字典
        _modulesById[module.Definition.Id] = module;
        // 4. 添加到模块索引列表
        _moduleIdsByIndex.Add(module.Definition.Id);
        // 5. 添加到模块选择器
        _moduleSelector.AddItem(module.Definition.DisplayName);
        // 6. 通知模块当前选中实体（如果有的话）
        module.OnSelectedEntityChanged(SelectedEntity);
    }

    /// <summary>
    /// 从模块宿主节点中扫描并注册所有测试模块。
    /// <para>
    /// 宿主层不再直接依赖具体模块类型；后续新增模块只需要把模块场景挂到 ModuleHost 下即可接入。
    /// </para>
    /// </summary>
    private void RegisterModulesFromScene()
    {
        _modules.Clear();
        _modulesById.Clear();
        _moduleIdsByIndex.Clear();
        _moduleSelector.Clear();

        var sceneModules = new List<TestModuleBase>();
        foreach (Node child in _moduleHost.GetChildren())
        {
            if (child is not TestModuleBase module)
            {
                continue;
            }

            sceneModules.Add(module);
        }

        sceneModules.Sort(static (left, right) =>
        {
            var orderCompare = left.Definition.SortOrder.CompareTo(right.Definition.SortOrder);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            return string.Compare(left.Definition.DisplayName, right.Definition.DisplayName, StringComparison.Ordinal);
        });

        foreach (var module in sceneModules)
        {
            if (string.IsNullOrWhiteSpace(module.Definition.Id))
            {
                _log.Error($"TestSystem 模块缺少稳定 Id: module={module.Name}");
                continue;
            }

            if (_modulesById.ContainsKey(module.Definition.Id))
            {
                _log.Error($"TestSystem 模块 Id 重复: id={module.Definition.Id} module={module.Name}");
                continue;
            }

            RegisterModule(module);
        }

        if (_modules.Count == 0)
        {
            _log.Warn("TestSystem 未在 ModuleHost 下找到任何测试模块，请检查 TestSystem.tscn 挂载配置");
        }
    }

    /// <summary>
    /// 切换测试面板显示与隐藏。
    /// </summary>
    private void OnTogglePressed()
    {
        _panelVisible = !_panelVisible;
        _panel.Visible = _panelVisible;
        _toggleButton.Text = _panelVisible ? "测试" : "测试(隐藏)";
        SyncMouseSelectionRequest();

        if (_currentModule == null)
        {
            return;
        }

        if (_panelVisible)
        {
            _currentModule.ActivateModule();
            return;
        }

        _currentModule.SuspendModule();
    }

    /// <summary>
    /// 模块下拉框回调，按索引切换当前测试模块。
    /// </summary>
    private void OnModuleSelected(long index)
    {
        if (index < 0 || index >= _moduleIdsByIndex.Count)
        {
            return;
        }

        TrySwitchModule(_moduleIdsByIndex[(int)index]);
    }

    /// <summary>
    /// 选择实体开关变化时，同步通用鼠标选择请求。
    /// </summary>
    private void OnSelectionToggleToggled(bool _)
    {
        SyncMouseSelectionRequest();
    }

    /// <summary>
    /// 激活指定模块，并让其它模块进入失活状态。
    /// <para>
    /// 切换时会先通知旧模块失活，再显示新模块并刷新一次界面，避免模块残留旧数据。
    /// </para>
    /// </summary>
    private void SwitchModule(string moduleId)
    {
        if (_currentModule != null)
        {
            _currentModule.DeactivateModule();
            _currentModule.ModuleRoot.Visible = false;
        }

        _currentModule = null;
        if (_modulesById.TryGetValue(moduleId, out var currentModule))
        {
            _currentModule = currentModule;
            currentModule.ModuleRoot.Visible = true;
            if (_panelVisible)
            {
                currentModule.ActivateModule();
            }
        }
    }

    /// <summary>
    /// 选中实体变化后的统一广播入口。
    /// </summary>
    private void OnSelectionChanged(IEntity? entity)
    {
        UpdateSelectedEntityDisplay();
        foreach (var module in _modules)
        {
            module.OnSelectedEntityChanged(entity);
        }
    }

    /// <summary>
    /// 请求宿主在帧末统一冲刷模块刷新。
    /// </summary>
    private void QueueScheduledModuleFlush()
    {
        if (_refreshFlushQueued)
        {
            return;
        }

        _refreshFlushQueued = true;
        CallDeferred(nameof(FlushScheduledModuleRefreshes));
    }

    /// <summary>
    /// 执行当前帧累计的模块刷新请求。
    /// </summary>
    private void FlushScheduledModuleRefreshes()
    {
        _refreshFlushQueued = false;
        if (!IsInsideTree())
        {
            return;
        }

        _refreshScheduler.DrainPending(_pendingRefreshModules);
        foreach (var module in _pendingRefreshModules)
        {
            module.FlushScheduledRefreshInternal();
        }

        _pendingRefreshModules.Clear();
    }

    /// <summary>
    /// 将当前选中实体显示到顶部信息栏。
    /// <para>
    /// 若实体没有名称数据，则回退到节点名，确保调试 UI 始终有可读信息。
    /// </para>
    /// </summary>
    private void UpdateSelectedEntityDisplay()
    {
        if (SelectedEntity is not Node node)
        {
            _selectedEntityLabel.Text = "未选择";
            return;
        }

        var name = SelectedEntity.Data.Get<string>(DataKey.Name.Key);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = node.Name.ToString();
        }

        var id = SelectedEntity.Data.Get<string>(DataKey.Id.Key);
        _selectedEntityLabel.Text = $"{name} | {node.GetType().Name} | {id}";
    }

}
