using Godot;
using System;
using BrotatoMy.Test;

namespace BrotatoMy.Test
{
    /// <summary>
    /// 敌人生成系统测试场景
    /// 用于验证 SpawnSystem 的批量生成功能及各种生成策略
    /// </summary>
    public partial class SpawnTestScene : Node2D
    {
        private static readonly Log _log = new Log("SpawnTestScene");

        // UI 组件
        private OptionButton _strategyOption;
        private Label _fpsLabel;
        private Label _countLabel;
        private CheckButton _debugDrawCheck;
        private Camera2D _camera;

        // 状态
        private SpawnPositionStrategy _currentStrategy = SpawnPositionStrategy.Rectangle;
        private readonly SpawnPositionParams _previewParams = new(); // 用于绘图预览参数，保持默认值与 SpawnSystem 一致
        private EnemyResource _testEnemy;

        public override void _Ready()
        {
            // 1. 初始化环境
            SetupEnvironment();

            // 2. 构建 UI
            BuildUI();

            _log.Info("生成测试场景初始化完成");
        }

        private void SetupEnvironment()
        {
            // 加载测试用敌人资源
            _testEnemy = GD.Load<EnemyResource>("res://Data/Resources/Enemy/豺狼人/豺狼人.tres");
            if (_testEnemy == null) _log.Error("Failed to load test enemy resource!");

            // 添加相机以便观察 Offscreen 策略
            _camera = new Camera2D();
            _camera.Zoom = new Vector2(0.5f, 0.5f); // 缩小一点以便看到更大范围
            AddChild(_camera);
        }

        private void BuildUI()
        {
            var canvas = new CanvasLayer { Layer = 100 };
            AddChild(canvas);

            var panel = new PanelContainer();
            // 设置 UI 在左上角，并有一些边距
            panel.Position = new Vector2(20, 20);
            canvas.AddChild(panel);

            var vbox = new VBoxContainer();
            panel.AddChild(vbox);

            // 标题
            var title = new Label { Text = "敌人生成测试", HorizontalAlignment = HorizontalAlignment.Center };
            vbox.AddChild(title);

            vbox.AddChild(new HSeparator());

            // 策略选择
            var hBoxStrategy = new HBoxContainer();
            hBoxStrategy.AddChild(new Label { Text = "策略:" });
            _strategyOption = new OptionButton();
            foreach (var strategy in Enum.GetValues<SpawnPositionStrategy>())
            {
                _strategyOption.AddItem(strategy.ToString(), (int)strategy);
            }
            // 默认选中 Random
            _strategyOption.Selected = (int)_currentStrategy;
            _strategyOption.ItemSelected += OnStrategySelected;
            hBoxStrategy.AddChild(_strategyOption);
            vbox.AddChild(hBoxStrategy);

            vbox.AddChild(new HSeparator());

            // 生成按钮组
            var gridBtn = new GridContainer { Columns = 2 };
            vbox.AddChild(gridBtn);

            var btnSpawn10 = new Button { Text = "生成 10 个" };
            btnSpawn10.Pressed += () => SpawnEnemies(10);
            gridBtn.AddChild(btnSpawn10);

            var btnSpawn100 = new Button { Text = "生成 100 个" };
            btnSpawn100.Pressed += () => SpawnEnemies(100);
            gridBtn.AddChild(btnSpawn100);

            vbox.AddChild(new HSeparator());

            // 清除按钮
            var btnClear = new Button { Text = "清除所有 (Kill All)", Modulate = Colors.Red };
            btnClear.Pressed += ClearEnemies;
            vbox.AddChild(btnClear);

            vbox.AddChild(new HSeparator());

            // 调试选项
            _debugDrawCheck = new CheckButton { Text = "显示生成范围", ButtonPressed = true };
            _debugDrawCheck.Pressed += QueueRedraw;
            vbox.AddChild(_debugDrawCheck);

            // 状态显示
            _fpsLabel = new Label { Text = "FPS: 0" };
            vbox.AddChild(_fpsLabel);

            _countLabel = new Label { Text = "Count: 0" };
            vbox.AddChild(_countLabel);

            // 提示信息
            var hint = new Label
            {
                Text = "WASD/箭头 移动相机\n滚轮 缩放",
                Modulate = new Color(1, 1, 1, 0.7f),
                ThemeTypeVariation = "HeaderSmall"
            };
            vbox.AddChild(hint);
        }

        private void OnStrategySelected(long index)
        {
            _currentStrategy = (SpawnPositionStrategy)index;
            QueueRedraw(); // 刷新绘图
        }

        private void SpawnEnemies(int count)
        {
            if (_testEnemy == null) return;
            _log.Info($"请求生成 {count} 个敌人，策略: {_currentStrategy}");

            // 为了测试不同的生成策略，我们临时修改测试资源的策略
            _testEnemy.DefaultStrategy = _currentStrategy;
            SpawnSystem.Instance.SpawnBatch(count, _testEnemy);
        }

        private void ClearEnemies()
        {
            SpawnSystem.Instance.KillAllEnemies();
        }

        public override void _Process(double delta)
        {
            // 更新 UI 统计
            _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";

            var pool = ObjectPoolManager.GetPool<Enemy>(PoolNames.EnemyPool);
            if (pool != null)
            {
                var stats = pool.GetStats();
                _countLabel.Text = $"Active: {stats.ActiveCount} | Pool: {stats.Count}";
            }
            else
            {
                _countLabel.Text = $"{PoolNames.EnemyPool} Not Found!";
            }

            // 简单的相机移动控制 (方便查看 Offscreen 生成)
            var moveSpeed = 500f * (float)delta / _camera.Zoom.X;
            if (Input.IsActionPressed("ui_right")) _camera.Position += new Vector2(moveSpeed, 0);
            if (Input.IsActionPressed("ui_left")) _camera.Position -= new Vector2(moveSpeed, 0);
            if (Input.IsActionPressed("ui_down")) _camera.Position += new Vector2(0, moveSpeed);
            if (Input.IsActionPressed("ui_up")) _camera.Position -= new Vector2(0, moveSpeed);

            // 调试绘图每帧刷新（如果相机移动）
            if (_debugDrawCheck.ButtonPressed)
            {
                QueueRedraw();
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            // 简单的缩放控制
            if (@event is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.WheelUp)
                {
                    _camera.Zoom *= 1.1f;
                }
                else if (mb.ButtonIndex == MouseButton.WheelDown)
                {
                    _camera.Zoom *= 0.9f;
                }
            }
        }

        public override void _Draw()
        {
            if (!_debugDrawCheck.ButtonPressed) return;

            // 绘制辅助线
            var color = new Color(1, 1, 0, 0.3f); // 半透明黄色
            var boundaryColor = new Color(0, 1, 0, 0.5f); // 绿色边界

            switch (_currentStrategy)
            {
                case SpawnPositionStrategy.Rectangle:
                    // 绘制矩形范围
                    DrawRect(new Rect2(_previewParams.MinX, _previewParams.MinY, _previewParams.MaxX - _previewParams.MinX, _previewParams.MaxY - _previewParams.MinY), color, true);
                    DrawRect(new Rect2(_previewParams.MinX, _previewParams.MinY, _previewParams.MaxX - _previewParams.MinX, _previewParams.MaxY - _previewParams.MinY), boundaryColor, false, 2.0f);
                    break;

                case SpawnPositionStrategy.Circle:
                    // 绘制圆形
                    DrawCircle(_previewParams.Center, _previewParams.Radius, color);
                    DrawArc(_previewParams.Center, _previewParams.Radius, 0, Mathf.Tau, 64, boundaryColor, 2.0f);
                    break;

                case SpawnPositionStrategy.Offscreen:
                    // 绘制屏幕边界和 Offscreen 距离
                    var viewportRect = GetViewportRect();
                    var cameraPos = _camera.GlobalPosition;
                    // 计算视口在世界空间的矩形
                    var size = viewportRect.Size / _camera.Zoom;
                    var worldRect = new Rect2(cameraPos - size / 2, size);

                    DrawRect(worldRect, new Color(0, 0, 1, 0.1f), true); // 屏幕可见区(淡蓝)
                    DrawRect(worldRect, boundaryColor, false, 2.0f);

                    var offRect = worldRect.Grow(_previewParams.ViewportPadding);
                    DrawRect(offRect, new Color(1, 0, 0, 0.5f), false, 2.0f); // 红色生成边界
                    break;

                case SpawnPositionStrategy.Grid:
                    // 绘制网格起点
                    var origin = _previewParams.GridOrigin ?? Vector2.Zero;
                    DrawCircle(origin, 10, boundaryColor);
                    // 简单示意一下网格方向
                    DrawLine(origin, origin + new Vector2(100, 0), boundaryColor, 2.0f);
                    DrawLine(origin, origin + new Vector2(0, 100), boundaryColor, 2.0f);
                    break;

                case SpawnPositionStrategy.Cluster:
                    // Cluster 是动态的，这里画一个中心区域示意
                    // 由于 Cluster 也是基于 Offscreen 的中心点，这里就简单画个圈
                    DrawCircle(Vector2.Zero, _previewParams.ClusterRadius, color);
                    break;
            }
        }
    }
}
