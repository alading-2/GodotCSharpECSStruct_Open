using Godot;

/// <summary>
/// 伤害数字UI
/// 显示一次性的飘字效果，不需要绑定Entity
/// 
/// 核心功能：
/// 1. 显示伤害数值和颜色（普通/暴击/魔法/真实/治疗/闪避）
/// 2. 播放飘字动画（普通上飘 / 暴击弹跳放大）
/// 3. 动画结束自动归还对象池
/// 4. 随机水平偏移防止数字堆叠
/// </summary>
public partial class DamageNumberUI : Control, IPoolable
{
    private Label _damageLabel = null!;
    private AnimationPlayer _animationPlayer = null!;

    // 颜色配置（统一引用 GameTheme）

    // 随机水平偏移范围（防止数字完全重叠）
    private static readonly System.Random _rng = new();
    private const float RANDOM_OFFSET_X = 30f;
    private const float BASE_OFFSET_Y = -20f; // 稍微向上偏移，对齐敌人中心上方

    // ============================================================
    // Godot 生命周期
    // ============================================================

    public override void _Ready()
    {
        _damageLabel = GetNode<Label>("DamageLabel");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        _animationPlayer.AnimationFinished += OnAnimationFinished;
    }

    // ============================================================
    // 公共方法
    // ============================================================

    /// <summary>
    /// 显示伤害数字（核心入口）
    /// </summary>
    /// <param name="damage">最终伤害值</param>
    /// <param name="worldPosition">受击单位的世界坐标</param>
    /// <param name="isCritical">是否暴击</param>
    /// <param name="damageType">伤害类型（决定颜色）</param>
    public void Show(float damage, Vector2 worldPosition, bool isCritical = false, DamageType damageType = DamageType.Physical)
    {
        _damageLabel.Text = $"{damage:F0}";

        if (isCritical)
        {
            _damageLabel.AddThemeFontSizeOverride("font_size", 32);
            _damageLabel.Modulate = GameTheme.DamageCritical;
            _damageLabel.Scale = new Vector2(1.3f, 1.3f);
            PlayAt(worldPosition, "float_up_crit");
        }
        else
        {
            _damageLabel.AddThemeFontSizeOverride("font_size", 22);
            _damageLabel.Modulate = damageType switch
            {
                DamageType.Magical => GameTheme.DamageMagical,
                DamageType.True => GameTheme.DamageTrue,
                _ => GameTheme.DamagePhysical,
            };
            _damageLabel.Scale = Vector2.One;
            PlayAt(worldPosition, "float_up");
        }
    }

    /// <summary>
    /// 显示闪避（MISS）
    /// </summary>
    public void ShowMiss(Vector2 worldPosition)
    {
        _damageLabel.AddThemeFontSizeOverride("font_size", 20);
        _damageLabel.Text = "MISS";
        _damageLabel.Modulate = GameTheme.Miss;
        _damageLabel.Scale = Vector2.One;
        PlayAt(worldPosition, "float_up");
    }

    /// <summary>
    /// 显示治疗数字
    /// </summary>
    public void ShowHeal(float healAmount, Vector2 worldPosition)
    {
        _damageLabel.AddThemeFontSizeOverride("font_size", 22);
        _damageLabel.Text = $"+{healAmount:F0}";
        _damageLabel.Modulate = GameTheme.Heal;
        _damageLabel.Scale = Vector2.One;
        PlayAt(worldPosition, "float_up");
    }

    // ============================================================
    // IPoolable 实现
    // ============================================================

    public void OnPoolAcquire()
    {
        Visible = false;
    }

    public void OnPoolRelease()
    {
        if (_animationPlayer != null) _animationPlayer.Stop();
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
    /// 设置位置（加随机偏移）并播放动画
    /// </summary>
    private void PlayAt(Vector2 worldPosition, string animName)
    {
        float offsetX = (float)(_rng.NextDouble() * 2 - 1) * RANDOM_OFFSET_X;
        GlobalPosition = worldPosition + new Vector2(offsetX, BASE_OFFSET_Y);
        Modulate = Colors.White; // 重置父节点 alpha（动画控制父节点 modulate 做渐隐）
        Visible = true;
        _animationPlayer.Play(animName);
    }

    private void OnAnimationFinished(StringName animName)
    {
        ObjectPoolManager.ReturnToPool(this);
    }

    public override void _ExitTree()
    {
        if (_animationPlayer != null)
            _animationPlayer.AnimationFinished -= OnAnimationFinished;
    }
}
