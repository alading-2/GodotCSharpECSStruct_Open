using Godot;
using System;

namespace BrotatoMy.Test;

/// <summary>
/// 测试用投射物
/// 包含移动、边界反弹、生命周期管理和复用计数显示
/// </summary>
public partial class TestProjectile : Node2D, IPoolable
{
    private Label _label;
    private ColorRect _visual;
    private Vector2 _velocity;
    private float _lifetime;
    private float _maxLifetime = 3.0f;
    private int _reuseCount = 0;
    private Rect2 _bounds;

    public override void _Ready()
    {
        // 动态构建视觉元素
        _visual = new ColorRect
        {
            Size = new Vector2(20, 20),
            Position = new Vector2(-10, -10), // Center
            Color = Colors.Green
        };
        AddChild(_visual);

        _label = new Label
        {
            Text = "0",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Position = new Vector2(-20, -30),
            Size = new Vector2(40, 20),
            Modulate = Colors.White
        };
        AddChild(_label);
    }

    public void Init(Vector2 position, Vector2 velocity, Rect2 bounds)
    {
        Position = position;
        _velocity = velocity;
        _bounds = bounds;
        _lifetime = 0;
        
        // 重置视觉
        Modulate = Colors.White;
        _visual.Color = Colors.Green.Lerp(Colors.Blue, (_reuseCount % 10) / 10f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Position += _velocity * dt;
        _lifetime += dt;

        // 边界反弹
        if (Position.X < _bounds.Position.X || Position.X > _bounds.End.X)
        {
            _velocity.X = -_velocity.X;
            Position = new Vector2(Mathf.Clamp(Position.X, _bounds.Position.X, _bounds.End.X), Position.Y);
        }
        if (Position.Y < _bounds.Position.Y || Position.Y > _bounds.End.Y)
        {
            _velocity.Y = -_velocity.Y;
            Position = new Vector2(Position.X, Mathf.Clamp(Position.Y, _bounds.Position.Y, _bounds.End.Y));
        }

        // 旋转效果
        Rotation += 5.0f * dt;

        // 生命周期结束
        if (_lifetime >= _maxLifetime)
        {
            ObjectPoolManager.ReturnToPool(this);
        }
    }

    public void OnPoolAcquire()
    {
        _reuseCount++;
        if (_label != null) _label.Text = _reuseCount.ToString();
        SetProcess(true);
        Show();
    }

    public void OnPoolRelease()
    {
        SetProcess(false);
    }

    public void OnPoolReset()
    {
        _lifetime = 0;
        _velocity = Vector2.Zero;
    }
}
