using Godot;

namespace Slime.Test
{
    internal partial class MovementTestEntity : Area2D, IEntity
    {
        public EventBus Events { get; } = new EventBus();
        public Data Data { get; private set; }

        [Export] public string DisplayName { get; set; } = "Movement";
        [Export] public Color DrawColor { get; set; } = Colors.White;
        [Export] public float DrawRadius { get; set; } = 12f;

        private Label? _label;

        public MovementTestEntity()
        {
            Data = new Data(this);
            Data.Set("DefaultMoveMode", MoveMode.None);
            Data.Set("MoveMode", MoveMode.None);
            Data.Set("Velocity", Vector2.Zero);
            Data.Set("FinalMoveSpeed", 260f);
            Data.Set("Acceleration", 10f);
            Data.Set("AIMoveDirection", Vector2.Zero);
            Data.Set("AIMoveSpeedMultiplier", 0f);
            Data.Set("EffectOffset", Vector2.Zero);
            Data.Set("IsDead", false);
        }

        public override void _Ready()
        {
            EnsureLabel();
            EntityManager.Register(this);
            EntityManager.RegisterComponents(this);
            QueueRedraw();
        }

        public override void _ExitTree()
        {
            var id = Data.Get<string>("Id");
            if (!string.IsNullOrEmpty(id) && EntityManager.GetEntityById(id) != null)
            {
                EntityManager.UnregisterEntity(this);
            }

            base._ExitTree();
        }

        public override void _Process(double delta)
        {
            if (_label != null)
            {
                _label.Text = DisplayName;
                _label.Position = new Vector2(-48f, -34f);
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, DrawRadius, DrawColor);
            DrawArc(Vector2.Zero, DrawRadius + 2f, 0f, Mathf.Tau, 24, Colors.Black, 2f);

            var velocity = Data.Get<Vector2>("Velocity");
            if (velocity.LengthSquared() < 1f)
            {
                return;
            }

            var dir = velocity.Normalized();
            var tip = dir * (DrawRadius + 18f);
            DrawLine(Vector2.Zero, tip, Colors.White, 2f);
            DrawLine(tip, tip + dir.Rotated(Mathf.DegToRad(150f)) * 8f, Colors.White, 2f);
            DrawLine(tip, tip + dir.Rotated(Mathf.DegToRad(-150f)) * 8f, Colors.White, 2f);
        }

        private void EnsureLabel()
        {
            _label = GetNodeOrNull<Label>("Label");
            if (_label != null)
            {
                return;
            }

            _label = new Label();
            _label.Name = "Label";
            _label.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(_label);
        }
    }
}
