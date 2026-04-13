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
public partial class TestSystem : CanvasLayer, ITestModuleHost
{
    /// <summary>测试系统日志器，用于记录初始化与关键调试操作。</summary>
    private static readonly Log _log = new(nameof(TestSystem));

    /// <summary>测试系统单例，方便其它调试工具直接访问当前测试面板。</summary>
    public static TestSystem? Instance { get; private set; }

    /// <summary>当前被测试面板选中的实体；属性面板、技能面板等都会围绕它刷新。</summary>
    public IEntity? SelectedEntity { get; private set; }

    /// <summary>当前已注册的测试模块列表，顺序就是下拉菜单显示顺序。</summary>
    private readonly List<TestModuleBase> _modules = new();

    /// <summary>当前处于前台的测试模块。</summary>
    private TestModuleBase? _currentModule;

    /// <summary>实体拾取器，负责把鼠标点击解析成 IEntity。</summary>
    private readonly TestEntityPicker _entityPicker = new();

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
        CacheUiNodes();
        BindUiEvents();
        RegisterSceneModules();
        if (_modules.Count > 0)
        {
            SwitchModule(0);
        }
        UpdateSelectedEntityDisplay();
        _log.Info("TestSystem 初始化完成");
    }

    /// <summary>
    /// 离开场景树时清理单例引用，避免调试系统指向失效对象。
    /// </summary>
    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 统一处理场景中的鼠标点击。
    /// <para>
    /// 只有在测试面板可见且“选择实体”开关打开时，才会尝试把鼠标位置下的实体设为当前目标。
    /// </para>
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_panelVisible || !_selectionToggle.ButtonPressed)
        {
            return;
        }

        if (@event is not InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        var entity = _entityPicker.FindEntityAtScreenPosition(this, mouseEvent.Position);
        if (entity != null)
        {
            SetSelectedEntity(entity);
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
        if (SelectedEntity == entity)
        {
            RefreshCurrentModule();
            return;
        }

        SelectedEntity = entity;
        UpdateSelectedEntityDisplay();

        foreach (var module in _modules)
        {
            module.OnSelectedEntityChanged(entity);
        }

        RefreshCurrentModule();
    }

    /// <summary>
    /// 刷新当前显示的测试模块。
    /// <para>
    /// 通过模块下拉框选中的索引找到对应模块，再触发其 Refresh。
    /// </para>
    /// </summary>
    public void RefreshCurrentModule()
    {
        _currentModule?.Refresh();
    }

    private void CacheUiNodes()
    {
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

    private void BindUiEvents()
    {
        _toggleButton.Pressed += OnTogglePressed;
        _moduleSelector.ItemSelected += OnModuleSelected;
        _refreshButton.Pressed += RefreshCurrentModule;
        _clearSelectionButton.Pressed += () => SetSelectedEntity(null);
    }

    /// <summary>
    /// 扫描模块宿主中的模块实例，并按场景子节点顺序完成注册。
    /// </summary>
    private void RegisterSceneModules()
    {
        _modules.Clear();
        _moduleSelector.Clear();

        foreach (var child in _moduleHost.GetChildren())
        {
            if (child is not TestModuleBase module)
            {
                continue;
            }

            RegisterModule(module);
        }

        if (_modules.Count == 0)
        {
            throw new InvalidOperationException("TestSystem 未在 ModuleHost 下找到任何 TestModuleBase 子模块");
        }
    }

    /// <summary>
    /// 注册一个测试模块。
    /// </summary>
    private void RegisterModule(TestModuleBase module)
    {
        module.Initialize(this);
        _modules.Add(module);
        _moduleSelector.AddItem(module.DisplayName);
        module.OnSelectedEntityChanged(SelectedEntity);
    }

    /// <summary>
    /// 切换测试面板显示与隐藏。
    /// </summary>
    private void OnTogglePressed()
    {
        _panelVisible = !_panelVisible;
        _panel.Visible = _panelVisible;
        _toggleButton.Text = _panelVisible ? "测试" : "测试(隐藏)";
    }

    /// <summary>
    /// 模块下拉框回调，按索引切换当前测试模块。
    /// </summary>
    private void OnModuleSelected(long index)
    {
        SwitchModule((int)index);
    }

    /// <summary>
    /// 激活指定模块，并让其它模块进入失活状态。
    /// <para>
    /// 切换时会先通知旧模块失活，再显示新模块并刷新一次界面，避免模块残留旧数据。
    /// </para>
    /// </summary>
    private void SwitchModule(int index)
    {
        if (index < 0 || index >= _modules.Count)
        {
            return;
        }

        if (_currentModule != null)
        {
            _currentModule.OnDeactivated();
            _currentModule.Visible = false;
        }

        _currentModule = _modules[index];
        _currentModule.Visible = true;
        _currentModule.OnActivated();
        _currentModule.Refresh();
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
