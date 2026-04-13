using System;
using System.Collections.Generic;
using Godot;

namespace Slime.Test
{
    internal partial class MovementComponentTestScene : Node2D
    {
        private enum DemoId
        {
            Charge,
            Orbit,
            SineWave,
            BezierCurve,
            Boomerang,
            AttachToHost,
            PlayerInput,
            AIControlled,
            Parabola,
            CircularArc,
        }

        private sealed class DemoSettings
        {
            public float Speed = 280f;
            public float Duration = 3f;
            public float Distance = -1f;
            public float ReachDistance = 16f;
            public float Radius = 160f;
            public float AngularSpeed = 120f;
            public float Amplitude = 60f;
            public float Frequency = 2f;
            public float Height = 120f;
            public float Phase;
            public float Pause = 0.15f;
            public float ReturnMultiplier = 1.35f;
            public float Angle;
            public bool TrackTarget;
            public bool WorldUp;
            public bool Clockwise;
        }

        private sealed class DemoState
        {
            public DemoId Id;
            public string Name = string.Empty;
            public Vector2 SpawnPosition;
            public Color Color;
            public DemoSettings Settings = new DemoSettings();
            public MovementTestEntity? Entity;
        }

        [Export] private PackedScene? _movementTestEntityScene;

        private readonly Dictionary<DemoId, DemoState> _demos = new Dictionary<DemoId, DemoState>();
        private readonly Dictionary<DemoId, GameTimer?> _restartTimers = new Dictionary<DemoId, GameTimer?>();

        private Node2D? _demoRoot;
        private Camera2D? _camera;
        private Marker2D? _orbitCenterMarker;
        private Marker2D? _chargeTargetMarker;
        private Marker2D? _curveTargetMarker;
        private Marker2D? _arcTargetMarker;
        private Marker2D? _boomerangTargetMarker;
        private Marker2D? _boomerangReturnMarker;
        private Marker2D? _aiCenterMarker;

        private PlayerEntity? _player;
        private EnemyEntity? _enemy;

        private OptionButton? _presetOption;
        private Label? _summaryLabel;
        private CheckButton? _autoReplayCheck;
        private CheckButton? _trackTargetCheck;
        private CheckButton? _worldUpCheck;
        private CheckButton? _clockwiseCheck;
        private SpinBox? _speedBox;
        private SpinBox? _durationBox;
        private SpinBox? _distanceBox;
        private SpinBox? _reachBox;
        private SpinBox? _radiusBox;
        private SpinBox? _angularSpeedBox;
        private SpinBox? _amplitudeBox;
        private SpinBox? _frequencyBox;
        private SpinBox? _heightBox;
        private SpinBox? _phaseBox;
        private SpinBox? _pauseBox;
        private SpinBox? _returnBox;
        private SpinBox? _angleBox;

        public override void _Ready()
        {
            GlobalEventBus.TriggerGameStart();
            _demoRoot = new Node2D();
            _demoRoot.Name = "DemoRoot";
            AddChild(_demoRoot);

            BuildMarkers();
            InitDemos();
            BuildCamera();
            BuildUi();
            SpawnUnits();
            SpawnAllDemos();
            LoadUi(GetSelectedId());
            QueueRedraw();
        }

        public override void _ExitTree()
        {
            foreach (var timer in _restartTimers.Values)
            {
                timer?.Cancel();
            }

            foreach (var demo in _demos.Values)
            {
                if (GodotObject.IsInstanceValid(demo.Entity))
                {
                    EntityManager.Destroy(demo.Entity!);
                }
            }

            if (GodotObject.IsInstanceValid(_player))
            {
                EntityManager.Destroy(_player!);
            }

            if (GodotObject.IsInstanceValid(_enemy))
            {
                EntityManager.Destroy(_enemy!);
            }

            base._ExitTree();
        }

        public override void _Process(double delta)
        {
            float t = Time.GetTicksMsec() / 1000f;

            if (_chargeTargetMarker != null)
            {
                _chargeTargetMarker.GlobalPosition = new Vector2(-260f + Mathf.Cos(t) * 90f, -250f + Mathf.Sin(t * 1.4f) * 50f);
            }

            if (_curveTargetMarker != null)
            {
                _curveTargetMarker.GlobalPosition = new Vector2(180f + Mathf.Cos(t * 0.8f) * 90f, -20f + Mathf.Sin(t * 1.1f) * 50f);
            }

            if (_arcTargetMarker != null)
            {
                _arcTargetMarker.GlobalPosition = new Vector2(520f + Mathf.Cos(t * 0.9f) * 80f, 150f + Mathf.Sin(t * 1.3f) * 55f);
            }

            if (_boomerangTargetMarker != null)
            {
                _boomerangTargetMarker.GlobalPosition = new Vector2(120f + Mathf.Cos(t * 0.6f) * 42f, 24f + Mathf.Sin(t * 0.85f) * 16f);
            }

            UpdateEnemyAi();
            UpdateSummary();
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawGrid();
            DrawMarker(_orbitCenterMarker, Colors.Yellow);
            DrawMarker(_chargeTargetMarker, Colors.Orange);
            DrawMarker(_curveTargetMarker, Colors.DeepSkyBlue);
            DrawMarker(_arcTargetMarker, Colors.MediumPurple);
            DrawMarker(_boomerangTargetMarker, Colors.GreenYellow);
            DrawMarker(_boomerangReturnMarker, Colors.LightGreen);
            DrawMarker(_aiCenterMarker, Colors.Red);
        }

        private void BuildMarkers()
        {
            _orbitCenterMarker = AddMarker("OrbitCenter", new Vector2(-240f, -70f));
            _chargeTargetMarker = AddMarker("ChargeTarget", new Vector2(-260f, -250f));
            _curveTargetMarker = AddMarker("CurveTarget", new Vector2(180f, -20f));
            _arcTargetMarker = AddMarker("ArcTarget", new Vector2(520f, 150f));
            _boomerangTargetMarker = AddMarker("BoomerangTarget", new Vector2(120f, 24f));
            _boomerangReturnMarker = AddMarker("BoomerangReturn", new Vector2(-240f, 30f));
            _aiCenterMarker = AddMarker("AiCenter", new Vector2(240f, 250f));
        }

        private void InitDemos()
        {
            AddDemo(DemoId.Charge, "冲刺", new Vector2(-720f, -250f), Colors.OrangeRed, new DemoSettings { Speed = 340f, Duration = 3f, ReachDistance = 20f, TrackTarget = true });
            AddDemo(DemoId.Orbit, "环绕", new Vector2(-80f, -70f), Colors.Gold, new DemoSettings { Radius = 160f, AngularSpeed = 140f, Duration = 6f, Distance = 1080f });
            AddDemo(DemoId.SineWave, "正弦波", new Vector2(-720f, -10f), Colors.Cyan, new DemoSettings { Speed = 260f, Distance = 620f, Amplitude = 72f, Frequency = 2.2f });
            AddDemo(DemoId.BezierCurve, "贝塞尔曲线", new Vector2(-680f, 120f), Colors.DeepSkyBlue, new DemoSettings { Duration = 3.4f, TrackTarget = true });
            AddDemo(DemoId.Boomerang, "回旋", new Vector2(-260f, 30f), Colors.GreenYellow, new DemoSettings { Speed = 360f, Pause = 0.08f, ReturnMultiplier = 1.2f, Height = 28f, Clockwise = false });
            AddDemo(DemoId.AttachToHost, "附着宿主", new Vector2(-520f, 260f), Colors.HotPink, new DemoSettings { Duration = -1f });
            AddDemo(DemoId.PlayerInput, "玩家控制", new Vector2(-520f, 260f), Colors.White, new DemoSettings { Speed = 260f, Amplitude = 12f });
            AddDemo(DemoId.AIControlled, "AI控制", new Vector2(240f, 250f), Colors.IndianRed, new DemoSettings { Speed = 170f });
            AddDemo(DemoId.Parabola, "抛物线", new Vector2(20f, 20f), Colors.MediumPurple, new DemoSettings { Speed = 300f, Height = 130f, TrackTarget = true, WorldUp = true });
            AddDemo(DemoId.CircularArc, "圆弧", new Vector2(320f, 150f), Colors.SandyBrown, new DemoSettings { Speed = 280f, Radius = 220f, ReachDistance = 20f, TrackTarget = true, Clockwise = true });
        }

        private void BuildCamera()
        {
            _camera = new Camera2D();
            _camera.Zoom = new Vector2(0.85f, 0.85f);
            _camera.Position = new Vector2(-80f, 40f);
            AddChild(_camera);
            _camera.MakeCurrent();
        }

        private void BuildUi()
        {
            var layer = new CanvasLayer();
            AddChild(layer);

            var panel = new PanelContainer();
            panel.Position = new Vector2(16f, 16f);
            panel.Size = new Vector2(340f, 680f);
            layer.AddChild(panel);

            var root = new VBoxContainer();
            panel.AddChild(root);

            root.AddChild(new Label { Text = "Movement 综合测试", HorizontalAlignment = HorizontalAlignment.Center });

            _presetOption = new OptionButton();
            foreach (var demo in _demos.Values)
            {
                _presetOption.AddItem(demo.Name, (int)demo.Id);
            }
            _presetOption.ItemSelected += _ => LoadUi(GetSelectedId());
            root.AddChild(_presetOption);

            root.AddChild(CreateButtonRow(("应用到选中", ApplySelected), ("重播选中", RestartSelected), ("重播全部", SpawnAllDemos)));

            _autoReplayCheck = new CheckButton { Text = "自动重播", ButtonPressed = true };
            _trackTargetCheck = new CheckButton { Text = "跟踪目标" };
            _worldUpCheck = new CheckButton { Text = "弓形朝上" };
            _clockwiseCheck = new CheckButton { Text = "顺时针" };
            root.AddChild(_autoReplayCheck);
            root.AddChild(_trackTargetCheck);
            root.AddChild(_worldUpCheck);
            root.AddChild(_clockwiseCheck);

            _speedBox = AddSpin(root, "速度", 0, 1200, 10, 280);
            _durationBox = AddSpin(root, "持续时间", -1, 12, 0.1, 3);
            _distanceBox = AddSpin(root, "距离/总角度", -1, 2000, 10, -1);
            _reachBox = AddSpin(root, "到达距离", 0, 200, 1, 16);
            _radiusBox = AddSpin(root, "半径", 0, 600, 5, 160);
            _angularSpeedBox = AddSpin(root, "角速度", 0, 720, 5, 120);
            _amplitudeBox = AddSpin(root, "振幅/加速度", 0, 240, 2, 60);
            _frequencyBox = AddSpin(root, "频率", 0, 8, 0.1, 2);
            _heightBox = AddSpin(root, "高度", -240, 240, 5, 120);
            _phaseBox = AddSpin(root, "相位", -360, 360, 5, 0);
            _pauseBox = AddSpin(root, "停顿时长", 0, 3, 0.05, 0.2);
            _returnBox = AddSpin(root, "返回倍率", 0.1, 4, 0.05, 1.35);
            _angleBox = AddSpin(root, "出发角度", -180, 180, 5, 0);

            _summaryLabel = new Label();
            _summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            root.AddChild(_summaryLabel);
        }

        private void SpawnUnits()
        {
            var playerConfig = ResourceManagement.Load<Slime.Config.Units.PlayerConfig>(ResourcePaths.DataUnit_deluyi, ResourceCategory.DataUnit);
            if (playerConfig != null)
            {
                var playerSpawn = new EntitySpawnConfig
                {
                    Config = playerConfig,
                    UsingObjectPool = false,
                    Position = _demos[DemoId.PlayerInput].SpawnPosition
                };
                _player = EntityManager.Spawn<PlayerEntity>(playerSpawn);
                if (_player != null)
                {
                    _player.Data.Set("FinalMoveSpeed", _demos[DemoId.PlayerInput].Settings.Speed);
                    _player.Data.Set("Acceleration", _demos[DemoId.PlayerInput].Settings.Amplitude);
                }
            }

            var enemyConfig = ResourceManagement.Load<Resource>(ResourcePaths.DataUnit_chailangren, ResourceCategory.DataUnit);
            if (enemyConfig != null)
            {
                var enemySpawn = new EntitySpawnConfig
                {
                    Config = enemyConfig,
                    UsingObjectPool = false,
                    Position = _demos[DemoId.AIControlled].SpawnPosition
                };
                _enemy = EntityManager.Spawn<EnemyEntity>(enemySpawn);
                if (_enemy != null)
                {
                    _enemy.Data.Set("FinalMoveSpeed", _demos[DemoId.AIControlled].Settings.Speed);
                    _enemy.Data.Set("AIMoveSpeedMultiplier", 1f);
                }
            }
        }

        private void SpawnAllDemos()
        {
            foreach (var demo in _demos.Values)
            {
                SpawnDemo(demo.Id);
            }
        }

        private void SpawnDemo(DemoId id)
        {
            if (id == DemoId.PlayerInput || id == DemoId.AIControlled)
            {
                ApplyUnitSettings(id);
                return;
            }

            if (_movementTestEntityScene == null || _demoRoot == null)
            {
                return;
            }

            var demo = _demos[id];
            _restartTimers[id]?.Cancel();
            if (GodotObject.IsInstanceValid(demo.Entity))
            {
                EntityManager.Destroy(demo.Entity!);
            }

            var entity = _movementTestEntityScene.Instantiate<MovementTestEntity>();
            entity.Name = demo.Id.ToString();
            entity.DisplayName = demo.Name;
            entity.DrawColor = demo.Color;
            entity.GlobalPosition = GetSpawnPosition(id);
            _demoRoot.AddChild(entity);
            demo.Entity = entity;

            if (id == DemoId.AttachToHost)
            {
                entity.Data.Set("EffectOffset", new Vector2(0f, -54f));
            }

            entity.Events.On<GameEventType.Unit.MovementCompletedEventData>(GameEventType.Unit.MovementCompleted, _ => OnDemoCompleted(id));
            StartDemo(id);
        }

        private void StartDemo(DemoId id)
        {
            var demo = _demos[id];
            if (!GodotObject.IsInstanceValid(demo.Entity))
            {
                return;
            }

            demo.Entity!.GlobalPosition = GetSpawnPosition(id);
            demo.Entity.Data.Set("Velocity", Vector2.Zero);
            demo.Entity.Events.Emit(GameEventType.Unit.MovementStarted, new GameEventType.Unit.MovementStartedEventData(BuildMode(id), BuildParams(id)));
        }

        private void OnDemoCompleted(DemoId id)
        {
            if (_autoReplayCheck?.ButtonPressed != true || id == DemoId.AttachToHost)
            {
                return;
            }

            _restartTimers[id]?.Cancel();
            _restartTimers[id] = TimerManager.Instance.Delay(0.45f).OnComplete(() => StartDemo(id));
        }

        private MovementParams BuildParams(DemoId id)
        {
            var settings = _demos[id].Settings;
            var spawn = GetSpawnPosition(id);

            switch (id)
            {
                case DemoId.Charge:
                    return new MovementParams
                    {
                        Mode = MoveMode.Charge,
                        ActionSpeed = settings.Speed,
                        MaxDuration = settings.Duration,
                        MaxDistance = settings.Distance,
                        ReachDistance = settings.ReachDistance,
                        TargetNode = settings.TrackTarget ? _chargeTargetMarker : null,
                        TargetPoint = _chargeTargetMarker != null ? _chargeTargetMarker.GlobalPosition : Vector2.Zero,
                        isTrackTarget = settings.TrackTarget,
                        Angle = settings.Angle
                    };
                case DemoId.Orbit:
                    return new MovementParams
                    {
                        Mode = MoveMode.Orbit,
                        OrbitCenter = _orbitCenterMarker != null ? _orbitCenterMarker.GlobalPosition : Vector2.Zero,
                        OrbitRadius = settings.Radius,
                        OrbitAngularSpeed = settings.AngularSpeed,
                        OrbitTotalAngle = settings.Distance,
                        MaxDuration = settings.Duration,
                        IsOrbitClockwise = settings.Clockwise
                    };
                case DemoId.SineWave:
                    return new MovementParams
                    {
                        Mode = MoveMode.SineWave,
                        ActionSpeed = settings.Speed,
                        MaxDuration = settings.Duration,
                        MaxDistance = settings.Distance,
                        WaveAmplitude = settings.Amplitude,
                        WaveFrequency = settings.Frequency,
                        WavePhase = settings.Phase,
                        Angle = settings.Angle
                    };
                case DemoId.BezierCurve:
                    return new MovementParams
                    {
                        Mode = MoveMode.BezierCurve,
                        MaxDuration = Mathf.Max(0.5f, settings.Duration),
                        TargetNode = settings.TrackTarget ? _curveTargetMarker : null,
                        TargetPoint = _curveTargetMarker != null ? _curveTargetMarker.GlobalPosition : Vector2.Zero,
                        isTrackTarget = settings.TrackTarget,
                        ReachDistance = settings.ReachDistance,
                        BezierPoints = new[]
                        {
                            spawn,
                            spawn + new Vector2(180f, -180f),
                            spawn + new Vector2(380f, 120f),
                            _curveTargetMarker != null ? _curveTargetMarker.GlobalPosition : spawn + new Vector2(520f, 0f)
                        }
                    };
                case DemoId.Boomerang:
                    return new MovementParams
                    {
                        Mode = MoveMode.Boomerang,
                        TargetPoint = _boomerangTargetMarker != null ? _boomerangTargetMarker.GlobalPosition : spawn + new Vector2(360f, 0f),
                        TargetNode = _boomerangReturnMarker,
                        ActionSpeed = settings.Speed,
                        MaxDuration = settings.Duration,
                        BoomerangPauseTime = settings.Pause,
                        BoomerangReturnSpeedMultiplier = settings.ReturnMultiplier,
                        BoomerangArcHeight = Mathf.Abs(settings.Height),
                        BoomerangIsClockwise = settings.Clockwise
                    };
                case DemoId.AttachToHost:
                    return new MovementParams
                    {
                        Mode = MoveMode.AttachToHost,
                        TargetNode = _player,
                        MaxDuration = settings.Duration
                    };
                case DemoId.Parabola:
                    return new MovementParams
                    {
                        Mode = MoveMode.Parabola,
                        TargetNode = settings.TrackTarget ? _arcTargetMarker : null,
                        TargetPoint = _arcTargetMarker != null ? _arcTargetMarker.GlobalPosition : spawn + new Vector2(260f, 0f),
                        isTrackTarget = settings.TrackTarget,
                        ActionSpeed = settings.Speed,
                        ParabolaApexHeight = settings.Height,
                        ReachDistance = settings.ReachDistance,
                        BowWorldUp = settings.WorldUp,
                        MaxDuration = settings.Duration
                    };
                case DemoId.CircularArc:
                    return new MovementParams
                    {
                        Mode = MoveMode.CircularArc,
                        TargetNode = settings.TrackTarget ? _arcTargetMarker : null,
                        TargetPoint = _arcTargetMarker != null ? _arcTargetMarker.GlobalPosition : spawn + new Vector2(260f, 0f),
                        isTrackTarget = settings.TrackTarget,
                        ActionSpeed = settings.Speed,
                        CircularArcRadius = settings.Radius,
                        CircularArcClockwise = settings.Clockwise,
                        ReachDistance = settings.ReachDistance,
                        BowWorldUp = settings.WorldUp,
                        MaxDuration = settings.Duration
                    };
                default:
                    return new MovementParams { Mode = BuildMode(id) };
            }
        }

        private MoveMode BuildMode(DemoId id)
        {
            switch (id)
            {
                case DemoId.Charge: return MoveMode.Charge;
                case DemoId.Orbit: return MoveMode.Orbit;
                case DemoId.SineWave: return MoveMode.SineWave;
                case DemoId.BezierCurve: return MoveMode.BezierCurve;
                case DemoId.Boomerang: return MoveMode.Boomerang;
                case DemoId.AttachToHost: return MoveMode.AttachToHost;
                case DemoId.PlayerInput: return MoveMode.PlayerInput;
                case DemoId.AIControlled: return MoveMode.AIControlled;
                case DemoId.Parabola: return MoveMode.Parabola;
                default: return MoveMode.CircularArc;
            }
        }

        private Vector2 GetSpawnPosition(DemoId id)
        {
            if (id == DemoId.Orbit && _orbitCenterMarker != null)
            {
                return _orbitCenterMarker.GlobalPosition + new Vector2(_demos[id].Settings.Radius, 0f);
            }

            return _demos[id].SpawnPosition;
        }

        private DemoId GetSelectedId()
        {
            return (DemoId)(_presetOption?.GetSelectedId() ?? 0);
        }

        private void ApplySelected()
        {
            var settings = _demos[GetSelectedId()].Settings;
            settings.Speed = (float)(_speedBox?.Value ?? settings.Speed);
            settings.Duration = (float)(_durationBox?.Value ?? settings.Duration);
            settings.Distance = (float)(_distanceBox?.Value ?? settings.Distance);
            settings.ReachDistance = (float)(_reachBox?.Value ?? settings.ReachDistance);
            settings.Radius = (float)(_radiusBox?.Value ?? settings.Radius);
            settings.AngularSpeed = (float)(_angularSpeedBox?.Value ?? settings.AngularSpeed);
            settings.Amplitude = (float)(_amplitudeBox?.Value ?? settings.Amplitude);
            settings.Frequency = (float)(_frequencyBox?.Value ?? settings.Frequency);
            settings.Height = (float)(_heightBox?.Value ?? settings.Height);
            settings.Phase = (float)(_phaseBox?.Value ?? settings.Phase);
            settings.Pause = (float)(_pauseBox?.Value ?? settings.Pause);
            settings.ReturnMultiplier = (float)(_returnBox?.Value ?? settings.ReturnMultiplier);
            settings.Angle = (float)(_angleBox?.Value ?? settings.Angle);
            settings.TrackTarget = _trackTargetCheck?.ButtonPressed == true;
            settings.WorldUp = _worldUpCheck?.ButtonPressed == true;
            settings.Clockwise = _clockwiseCheck?.ButtonPressed == true;
            SpawnDemo(GetSelectedId());
        }

        private void RestartSelected()
        {
            SpawnDemo(GetSelectedId());
        }

        private void LoadUi(DemoId id)
        {
            var settings = _demos[id].Settings;
            if (_presetOption != null) _presetOption.Select((int)id);
            if (_speedBox != null) _speedBox.Value = settings.Speed;
            if (_durationBox != null) _durationBox.Value = settings.Duration;
            if (_distanceBox != null) _distanceBox.Value = settings.Distance;
            if (_reachBox != null) _reachBox.Value = settings.ReachDistance;
            if (_radiusBox != null) _radiusBox.Value = settings.Radius;
            if (_angularSpeedBox != null) _angularSpeedBox.Value = settings.AngularSpeed;
            if (_amplitudeBox != null) _amplitudeBox.Value = settings.Amplitude;
            if (_frequencyBox != null) _frequencyBox.Value = settings.Frequency;
            if (_heightBox != null) _heightBox.Value = settings.Height;
            if (_phaseBox != null) _phaseBox.Value = settings.Phase;
            if (_pauseBox != null) _pauseBox.Value = settings.Pause;
            if (_returnBox != null) _returnBox.Value = settings.ReturnMultiplier;
            if (_angleBox != null) _angleBox.Value = settings.Angle;
            if (_trackTargetCheck != null) _trackTargetCheck.ButtonPressed = settings.TrackTarget;
            if (_worldUpCheck != null) _worldUpCheck.ButtonPressed = settings.WorldUp;
            if (_clockwiseCheck != null) _clockwiseCheck.ButtonPressed = settings.Clockwise;
        }

        private void ApplyUnitSettings(DemoId id)
        {
            var settings = _demos[id].Settings;

            if (id == DemoId.PlayerInput && _player != null)
            {
                _player.Data.Set("FinalMoveSpeed", settings.Speed);
                _player.Data.Set("Acceleration", settings.Amplitude);
            }

            if (id == DemoId.AIControlled && _enemy != null)
            {
                _enemy.Data.Set("FinalMoveSpeed", settings.Speed);
                _enemy.Data.Set("AIMoveSpeedMultiplier", 1f);
            }
        }

        private void UpdateEnemyAi()
        {
            if (_enemy == null || _aiCenterMarker == null)
            {
                return;
            }

            float t = Time.GetTicksMsec() / 1000f;
            Vector2 target = _aiCenterMarker.GlobalPosition + new Vector2(Mathf.Cos(t * 0.8f), Mathf.Sin(t * 1.1f)) * 110f;
            _enemy.Data.Set("AIMoveDirection", _enemy.GlobalPosition.DirectionTo(target));
            _enemy.Data.Set("AIMoveSpeedMultiplier", 1f);
        }

        private void UpdateSummary()
        {
            if (_summaryLabel == null)
            {
                return;
            }

            var selected = _demos[GetSelectedId()];
            _summaryLabel.Text = $"当前: {selected.Name}\n玩家: WASD/方向键\n敌人: 自动 AIControlled\n附着体会跟随玩家\nCharge/Bezier/Parabola/CircularArc 可切换 TrackTarget\nDistance 在 Orbit 中表示 TotalAngle";
        }

        private void AddDemo(DemoId id, string name, Vector2 spawn, Color color, DemoSettings settings)
        {
            _demos[id] = new DemoState
            {
                Id = id,
                Name = name,
                SpawnPosition = spawn,
                Color = color,
                Settings = settings
            };
            _restartTimers[id] = null;
        }

        private Marker2D AddMarker(string name, Vector2 position)
        {
            var marker = new Marker2D();
            marker.Name = name;
            marker.GlobalPosition = position;
            AddChild(marker);
            return marker;
        }

        private HBoxContainer CreateButtonRow((string text, Action action) a, (string text, Action action) b, (string text, Action action) c)
        {
            var row = new HBoxContainer();
            foreach (var item in new[] { a, b, c })
            {
                var button = new Button();
                button.Text = item.text;
                button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                button.Pressed += item.action;
                row.AddChild(button);
            }
            return row;
        }

        private SpinBox AddSpin(VBoxContainer root, string label, double min, double max, double step, double value)
        {
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(130f, 0f) });
            var spin = new SpinBox();
            spin.MinValue = min;
            spin.MaxValue = max;
            spin.Step = step;
            spin.Value = value;
            spin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(spin);
            root.AddChild(row);
            return spin;
        }

        private void DrawGrid()
        {
            for (int x = -800; x <= 800; x += 80)
            {
                DrawLine(new Vector2(x, -420), new Vector2(x, 420), new Color(1f, 1f, 1f, 0.05f), 1f);
            }

            for (int y = -400; y <= 400; y += 80)
            {
                DrawLine(new Vector2(-900, y), new Vector2(900, y), new Color(1f, 1f, 1f, 0.05f), 1f);
            }

            DrawLine(new Vector2(-900, 0), new Vector2(900, 0), new Color(1f, 1f, 1f, 0.15f), 2f);
            DrawLine(new Vector2(0, -420), new Vector2(0, 420), new Color(1f, 1f, 1f, 0.15f), 2f);
        }

        private void DrawMarker(Node2D? marker, Color color)
        {
            if (marker == null)
            {
                return;
            }

            DrawCircle(marker.GlobalPosition, 8f, color);
            DrawArc(marker.GlobalPosition, 12f, 0f, Mathf.Tau, 20, Colors.Black, 2f);
        }
    }
}
