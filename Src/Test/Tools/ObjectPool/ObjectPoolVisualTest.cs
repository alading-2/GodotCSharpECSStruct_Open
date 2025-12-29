using Godot;
using System;
using System.Collections.Generic;

namespace BrotatoMy.Test;

/// <summary>
/// C# 版对象池可视化测试场景
/// 提供直观的 UI 面板、实时图表和交互控制
/// </summary>
public partial class ObjectPoolVisualTest : Control
{
    private ObjectPool<VisualTestBullet> _pool;
    private Rect2 _spawnBounds;

    // UI References
    private Label _statsLabel;
    private CheckButton _autoSpawnToggle;
    private HSlider _spawnRateSlider;
    private ProgressBar _poolLoadBar;
    private Control _poolVisualizerContainer; // 展示池中闲置对象的容器

    // State
    private bool _isAutoSpawn = false;
    private float _spawnTimer = 0;
    private float _spawnInterval = 0.1f;
    private int _maxPoolSize = 100;

    public override void _Ready()
    {
        // 0. 初始化游戏容器 (ZIndex=1 确保可见)
        var gameContainer = new Node2D { Name = "GameContainer", ZIndex = 1 };
        AddChild(gameContainer);

        // 1. 构建 UI
        BuildUI();

        // 2. 初始化对象池
        InitializePool();

        // 3. 设置生成边界 (留出 UI 空间)
        // 顶部留出 100px 给统计信息，底部留出 150px 给控制面板
        _spawnBounds = new Rect2(20, 100, GetViewportRect().Size.X - 40, GetViewportRect().Size.Y - 250);

        // 4. 绘制边界框 (Debug)
        var border = new ReferenceRect
        {
            Position = _spawnBounds.Position,
            Size = _spawnBounds.Size,
            BorderColor = Colors.Yellow,
            EditorOnly = false,
            BorderWidth = 2.0f
        };
        AddChild(border);
    }

    private void InitializePool()
    {
        _pool = new ObjectPool<VisualTestBullet>(
            createFunc: () =>
            {
                var b = new VisualTestBullet();
                b.Name = "VisualBullet"; // 方便调试
                return b;
            },
            config: new ObjectPoolConfig
            {
                Name = "VisualTestPool",
                InitialSize = 10,
                MaxSize = _maxPoolSize,
                EnableStats = true,
                ParentPath = $"{Name}/GameContainer" // 明确指定父节点路径
            }
        );

        // 绑定池事件以更新 UI
        _pool.OnInstanceAcquire += _ => UpdatePoolVisualizer();
        _pool.OnInstanceRelease += _ => UpdatePoolVisualizer();
    }

    public override void _Process(double delta)
    {
        // 自动生成逻辑
        if (_isAutoSpawn)
        {
            _spawnTimer += (float)delta;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0;
                SpawnRandom();
            }
        }

        // 更新统计 UI
        UpdateStatsUI();
    }

    public override void _Input(InputEvent @event)
    {
        // 点击屏幕生成
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            // 排除点击 UI 区域
            if (_spawnBounds.HasPoint(mb.Position))
            {
                SpawnAt(mb.Position);
            }
        }
    }

    private void SpawnRandom()
    {
        var x = (float)GD.RandRange(_spawnBounds.Position.X, _spawnBounds.End.X);
        var y = (float)GD.RandRange(_spawnBounds.Position.Y, _spawnBounds.End.Y);
        SpawnAt(new Vector2(x, y));
    }

    private void SpawnAt(Vector2 pos)
    {
        var bullet = _pool.Spawn(); // 使用新的 Spawn 语义

        // 随机速度方向
        var angle = (float)GD.RandRange(0, Math.PI * 2);
        var speed = (float)GD.RandRange(100, 300);
        var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

        bullet.Init(pos, velocity, _spawnBounds);
    }

    // --- UI 构建与更新 ---

    private void BuildUI()
    {
        // 全屏容器
        var mainContainer = new VBoxContainer
        {
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetLeft = 20,
            OffsetTop = 20,
            OffsetRight = -20,
            OffsetBottom = -20,
            MouseFilter = MouseFilterEnum.Ignore // 让点击穿透到 _Input
        };
        AddChild(mainContainer);

        // 1. 顶部标题和统计
        var header = new HBoxContainer();
        var title = new Label
        {
            Text = "对象池可视化测试 (C#)",
            LabelSettings = new LabelSettings { FontSize = 24, FontColor = Colors.Cyan }
        };
        header.AddChild(title);

        _statsLabel = new Label { Text = "状态: 正在初始化..." };
        header.AddChild(new Control { CustomMinimumSize = new Vector2(50, 0) }); // Spacer
        header.AddChild(_statsLabel);

        mainContainer.AddChild(header);

        // 2. 进度条 (池负载)
        var barContainer = new HBoxContainer();
        barContainer.AddChild(new Label { Text = "池负载 (活跃/最大): " });
        _poolLoadBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(400, 20),
            MaxValue = _maxPoolSize,
            ShowPercentage = true
        };
        barContainer.AddChild(_poolLoadBar);
        mainContainer.AddChild(barContainer);

        // Spacer for Play Area
        mainContainer.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

        // 3. 底部控制面板
        var controls = new PanelContainer();
        var controlsHBox = new HBoxContainer();
        controls.AddChild(controlsHBox);

        // 按钮：生成 1 个
        var btnSpawn1 = new Button { Text = "生成 1 个" };
        btnSpawn1.Pressed += () => SpawnRandom();
        controlsHBox.AddChild(btnSpawn1);

        // 按钮：生成 10 个
        var btnSpawn10 = new Button { Text = "生成 10 个" };
        btnSpawn10.Pressed += () => { for (int i = 0; i < 10; i++) SpawnRandom(); };
        controlsHBox.AddChild(btnSpawn10);

        // 开关：自动生成
        _autoSpawnToggle = new CheckButton { Text = "自动生成" };
        _autoSpawnToggle.Toggled += (on) => _isAutoSpawn = on;
        controlsHBox.AddChild(_autoSpawnToggle);

        // 滑动条：生成频率
        controlsHBox.AddChild(new Label { Text = "频率:" });
        _spawnRateSlider = new HSlider { MinValue = 0.01, MaxValue = 1.0, Step = 0.01, Value = 0.1, CustomMinimumSize = new Vector2(100, 0) };
        _spawnRateSlider.ValueChanged += (v) => _spawnInterval = (float)v;
        controlsHBox.AddChild(_spawnRateSlider);

        // 按钮：清空池
        var btnClear = new Button { Text = "清空池 (销毁闲置)" };
        btnClear.Pressed += () => _pool.Clear();
        controlsHBox.AddChild(btnClear);

        // 按钮：归还所有
        var btnReturnAll = new Button { Text = "回收所有活跃" };
        btnReturnAll.Pressed += ReturnAllActive;
        controlsHBox.AddChild(btnReturnAll);

        mainContainer.AddChild(controls);

        // 4. 池可视化 (展示闲置对象堆栈)
        var poolVisLabel = new Label { Text = "闲置对象堆栈 (可视化):" };
        mainContainer.AddChild(poolVisLabel);

        _poolVisualizerContainer = new HFlowContainer { CustomMinimumSize = new Vector2(0, 30) };
        mainContainer.AddChild(_poolVisualizerContainer);
    }

    private void ReturnAllActive()
    {
        // 遍历父节点路径下的所有子节点并归还
        var parent = ParentManager.Instance.GetParent("VisualTestPool");
        if (parent == null) return;

        var children = parent.GetChildren();
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is VisualTestBullet b)
            {
                ObjectPoolManager.ReturnToPool(b);
            }
        }
    }

    private void UpdateStatsUI()
    {
        var stats = _pool.GetStats();
        _statsLabel.Text = $"闲置: {stats.Count} | 活跃: {stats.ActiveCount} | 总计创建: {stats.TotalCreated} | 已回收: {stats.TotalReleased} | 已丢弃: {stats.TotalDiscarded}";

        _poolLoadBar.Value = stats.ActiveCount;

        // 动态变色进度条
        if (stats.ActiveCount >= _maxPoolSize * 0.9) _poolLoadBar.Modulate = Colors.Red;
        else if (stats.ActiveCount >= _maxPoolSize * 0.5) _poolLoadBar.Modulate = Colors.Yellow;
        else _poolLoadBar.Modulate = Colors.Green;
    }

    private void UpdatePoolVisualizer()
    {
        int count = _pool.Count;
        int currentVisuals = _poolVisualizerContainer.GetChildCount();

        if (count > currentVisuals)
        {
            for (int i = 0; i < count - currentVisuals; i++)
            {
                if (_poolVisualizerContainer.GetChildCount() >= 50) break;
                var rect = new ColorRect
                {
                    CustomMinimumSize = new Vector2(10, 10),
                    Color = Colors.Cyan
                };
                _poolVisualizerContainer.AddChild(rect);
            }
        }
        else if (count < currentVisuals)
        {
            for (int i = 0; i < currentVisuals - count; i++)
            {
                var child = _poolVisualizerContainer.GetChild(0);
                _poolVisualizerContainer.RemoveChild(child);
                child.QueueFree();
            }
        }
    }

    public override void _ExitTree()
    {
        _pool?.Destroy();
        base._ExitTree();
    }
}
