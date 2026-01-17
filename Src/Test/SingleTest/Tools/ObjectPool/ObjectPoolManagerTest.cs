using Godot;
using System;
using System.Collections.Generic;

namespace BrotatoMy.Test;

/// <summary>
/// ObjectPoolManager 全局管理器测试场景
/// 演示多对象池管理、全局统计和混合使用
/// </summary>
public partial class ObjectPoolManagerTest : Control
{
    // 持有池引用以防止被 GC（尽管 ObjectPoolManager 会持有引用，但为了类型安全访问建议持有）
    private ObjectPool<TestProjectile> _projectilePool;
    private ObjectPool<TestEffect> _effectPool;

    private Node2D _gameContainer;
    private Rect2 _spawnBounds;

    // UI References
    private VBoxContainer _statsContainer;

    // State
    private bool _autoSpawnProjectile = false;
    private bool _autoSpawnEffect = false;
    private float _timer = 0;

    public override void _Ready()
    {
        // 1. 初始化容器 (ZIndex=1 确保在 UI 之上显示)
        _gameContainer = new Node2D { Name = "GameContainer", ZIndex = 1 };
        AddChild(_gameContainer);

        // 2. 初始化对象池
        InitializePools();

        // 3. 构建 UI
        BuildUI();

        // 4. 设置生成区域
        _spawnBounds = new Rect2(250, 50, GetViewportRect().Size.X - 300, GetViewportRect().Size.Y - 100);

        // 绘制边界
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

    private void InitializePools()
    {
        // 1. 投射物池
        // 演示：自定义 ParentPath
        _projectilePool = new ObjectPool<TestProjectile>(
            createFunc: () =>
            {
                var p = new TestProjectile();
                p.Name = "Projectile";
                return p;
            },
            config: new ObjectPoolConfig
            {
                Name = "ProjectilePool",
                InitialSize = 20,
                MaxSize = 200,
                ParentPath = $"{Name}/GameContainer", // 挂载到当前的 GameContainer 下
                EnableStats = true
            }
        );

        // 2. 特效池
        // 演示：共享同一个 ParentPath
        _effectPool = new ObjectPool<TestEffect>(
            createFunc: () =>
            {
                var e = new TestEffect();
                e.Name = "Effect";
                return e;
            },
            config: new ObjectPoolConfig
            {
                Name = "EffectPool",
                InitialSize = 10,
                MaxSize = 50,
                ParentPath = $"{Name}/GameContainer",
                EnableStats = true
            }
        );
    }

    public override void _Process(double delta)
    {
        _timer += (float)delta;

        // 自动生成逻辑
        if (_timer > 0.1f)
        {
            _timer = 0;
            if (_autoSpawnProjectile) SpawnProjectile();
            if (_autoSpawnEffect) SpawnEffect();
        }

        // 刷新统计信息 (每帧刷新可能太快，但为了演示流畅度先这样)
        UpdateStats();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            if (_spawnBounds.HasPoint(mb.Position))
            {
                if (mb.ButtonIndex == MouseButton.Left) SpawnProjectile(mb.Position);
                else if (mb.ButtonIndex == MouseButton.Right) SpawnEffect(mb.Position);
            }
        }
    }

    private void SpawnProjectile(Vector2? pos = null)
    {
        var p = _projectilePool.Spawn();
        var position = pos ?? GetRandomPos();
        var angle = (float)GD.RandRange(0, Math.PI * 2);
        var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 200;
        p.Init(position, velocity, _spawnBounds);
    }

    private void SpawnEffect(Vector2? pos = null)
    {
        var e = _effectPool.Spawn();
        var position = pos ?? GetRandomPos();
        e.Init(position);
    }

    private Vector2 GetRandomPos()
    {
        return new Vector2(
            (float)GD.RandRange(_spawnBounds.Position.X, _spawnBounds.End.X),
            (float)GD.RandRange(_spawnBounds.Position.Y, _spawnBounds.End.Y)
        );
    }

    private void BuildUI()
    {
        // 左侧面板：统计信息
        var leftPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(240, 0),
            AnchorBottom = 1,
            OffsetRight = 240
        };
        AddChild(leftPanel);

        var leftVBox = new VBoxContainer();
        leftPanel.AddChild(leftVBox);

        leftVBox.AddChild(new Label
        {
            Text = "对象池管理器概览",
            LabelSettings = new LabelSettings { FontSize = 20, FontColor = Colors.Cyan }
        });

        _statsContainer = new VBoxContainer();
        leftVBox.AddChild(_statsContainer);

        leftVBox.AddChild(new HSeparator());

        // 控制区
        leftVBox.AddChild(new Label { Text = "控制面板" });

        // Projectile Controls
        leftVBox.AddChild(new Label { Text = "[投射物池]", Modulate = Colors.Green });
        var btnSpawnP = new Button { Text = "生成投射物 (左键)" };
        btnSpawnP.Pressed += () => SpawnProjectile();
        leftVBox.AddChild(btnSpawnP);

        var chkAutoP = new CheckButton { Text = "自动生成投射物" };
        chkAutoP.Toggled += (on) => _autoSpawnProjectile = on;
        leftVBox.AddChild(chkAutoP);

        // Effect Controls
        leftVBox.AddChild(new Label { Text = "[特效池]", Modulate = Colors.Magenta });
        var btnSpawnE = new Button { Text = "生成特效 (右键)" };
        btnSpawnE.Pressed += () => SpawnEffect();
        leftVBox.AddChild(btnSpawnE);

        var chkAutoE = new CheckButton { Text = "自动生成特效" };
        chkAutoE.Toggled += (on) => _autoSpawnEffect = on;
        leftVBox.AddChild(chkAutoE);

        leftVBox.AddChild(new HSeparator());

        // Global Controls
        var btnClean = new Button { Text = "清理闲置 (保留10个)" };
        btnClean.Pressed += () => ObjectPoolManager.CleanupAll(10);
        leftVBox.AddChild(btnClean);

        var btnDestroy = new Button { Text = "销毁所有池" };
        btnDestroy.Pressed += () =>
        {
            ObjectPoolManager.DestroyAll();
            // 重新初始化以防崩溃
            InitializePools();
        };
        leftVBox.AddChild(btnDestroy);

        var btnReleaseAll = new Button { Text = "回收所有活跃" };
        btnReleaseAll.Pressed += () =>
        {
            _projectilePool.ReleaseAll();
            _effectPool.ReleaseAll();
        };
        leftVBox.AddChild(btnReleaseAll);
    }

    private void UpdateStats()
    {
        // 清除旧的 Label
        foreach (var child in _statsContainer.GetChildren())
        {
            child.QueueFree();
        }

        var allStats = ObjectPoolManager.GetAllStats();

        foreach (var kvp in allStats)
        {
            var name = kvp.Key;
            var stats = kvp.Value;

            var statsStr = $"[{name}]\n" +
                           $"闲置: {stats.Count} | 活跃: {stats.ActiveCount}\n" +
                           $"总创建: {stats.TotalCreated} | 总回收: {stats.TotalReleased}\n" +
                           $"利用率: {stats.HitRate:P0}";

            var label = new Label
            {
                Text = statsStr,
                Modulate = name.Contains("Projectile") ? Colors.Green : Colors.Magenta
            };
            _statsContainer.AddChild(label);
            _statsContainer.AddChild(new HSeparator());
        }
    }

    public override void _ExitTree()
    {
        ObjectPoolManager.DestroyAll();
    }
}
