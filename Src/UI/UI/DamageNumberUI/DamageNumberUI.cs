using Godot;

/// <summary>
/// 伤害数字UI
/// 显示一次性的飘字效果，不需要绑定Entity
/// 
/// 核心功能：
/// 1. 显示伤害数值和颜色（普通/暴击）
/// 2. 播放飘字动画
/// 3. 动画结束自动归还对象池
/// </summary>
public partial class DamageNumberUI : Control, IPoolable
{
    private Label _damageLabel = null!;
    private AnimationPlayer _animationPlayer = null!;

    // 颜色配置
    private static readonly Color NORMAL_COLOR = new Color(1, 1, 1); // 白色
    private static readonly Color CRITICAL_COLOR = new Color(1, 0.2f, 0.2f); // 红色
    private static readonly Color HEAL_COLOR = new Color(0.2f, 1, 0.2f); // 绿色

    // ============================================================
    // Godot 生命周期
    // ============================================================

    public override void _Ready()
    {
        _damageLabel = GetNode<Label>("DamageLabel");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // 监听动画结束事件
        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    // ============================================================
    // 公共方法
    // ============================================================

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="position">显示位置（世界坐标）</param>
    /// <param name="isCritical">是否暴击</param>
    public void Show(float damage, Vector2 position, bool isCritical = false)
    {
        // 设置位置
        GlobalPosition = position;

        // 设置文本
        _damageLabel.Text = $"{damage:F0}";

        // 设置颜色和缩放
        if (isCritical)
        {
            _damageLabel.Modulate = CRITICAL_COLOR;
            _damageLabel.Scale = new Vector2(1.5f, 1.5f);
        }
        else
        {
            _damageLabel.Modulate = NORMAL_COLOR;
            _damageLabel.Scale = Vector2.One;
        }

        // 显示并播放动画
        Visible = true;
        _animationPlayer.Play("float_up");
    }

    /// <summary>
    /// 显示治疗数字
    /// </summary>
    public void ShowHeal(float healAmount, Vector2 position)
    {
        GlobalPosition = position;
        _damageLabel.Text = $"+{healAmount:F0}";
        _damageLabel.Modulate = HEAL_COLOR;
        _damageLabel.Scale = Vector2.One;

        Visible = true;
        _animationPlayer.Play("float_up");
    }

    // ============================================================
    // IPoolable 实现
    // ============================================================

    public void OnPoolAcquire()
    {
        // 从池中取出时重置状态
        Visible = false;
    }

    public void OnPoolRelease()
    {
        // 归还时停止动画
        _animationPlayer.Stop();
        Visible = false;
    }

    public void OnPoolReset()
    {
        _damageLabel.Text = "";
        _damageLabel.Scale = Vector2.One;
    }

    // ============================================================
    // 私有方法
    // ============================================================

    /// <summary>
    /// 动画播放完成回调
    /// </summary>
    private void OnAnimationFinished(StringName animName)
    {
        // 自动归还对象池
        ObjectPoolManager.ReturnToPool(this);
    }

    public override void _ExitTree()
    {
        // 清理事件订阅
        if (_animationPlayer != null)
        {
            _animationPlayer.AnimationFinished -= OnAnimationFinished;
        }
    }
}
