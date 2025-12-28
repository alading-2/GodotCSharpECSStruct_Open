using Godot;
using System;

namespace BrotatoMy.Test;

/// <summary>
/// 测试用特效对象
/// 演示自动播放动画并自动销毁
/// </summary>
public partial class TestEffect : Node2D, IPoolable
{
    private ColorRect _visual;
    private float _lifetime;
    private float _maxLifetime = 1.0f;

    public override void _Ready()
    {
        _visual = new ColorRect
        {
            Size = new Vector2(30, 30),
            Position = new Vector2(-15, -15),
            Color = Colors.Magenta
        };
        AddChild(_visual);
    }

    public void Init(Vector2 position)
    {
        Position = position;
        _lifetime = 0;
        Scale = Vector2.One;
        Modulate = Colors.White;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _lifetime += dt;

        // 简单的缩放和淡出动画
        float progress = _lifetime / _maxLifetime;
        Scale = Vector2.One * (1.0f + progress * 2.0f); // 变大
        Modulate = new Color(1, 1, 1, 1.0f - progress); // 变透明

        if (_lifetime >= _maxLifetime)
        {
            ObjectPoolManager.ReturnToPool(this);
        }
    }

    public void OnPoolAcquire()
    {
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
        Scale = Vector2.One;
    }
}
