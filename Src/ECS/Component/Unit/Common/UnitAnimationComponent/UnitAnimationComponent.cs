using Godot;

/// <summary>
/// 通用动画名称常量
/// 与 SpriteFramesGeneratorPlugin 的 NormalizeName 输出一致
/// 各单位的 SpriteFrames 中实际存在哪些动画取决于美术素材
/// </summary>
public static class Anim
{
    // === 基础动作（所有单位通常都有） ===
    public const string Idle = "idle";
    public const string Run = "run";
    public const string Dead = "dead";

    // === 攻击 ===
    public const string Attack1 = "attack1";
    public const string Attack2 = "attack2";

    // === 受击 ===
    public const string BeAttacked = "beattacked";

    // === 技能/施法 ===
    public const string Skill = "skill";
    public const string CastingIdle = "castingidle";

    // === 其他 ===
    public const string Celebrate = "celebrate";
}

/// <summary>
/// 单位动画组件 - 统一管理 AnimatedSprite2D 的动画播放
///
/// 核心职责：
/// - 缓存 VisualRoot 下的 AnimatedSprite2D 引用
/// - 监听生命周期事件，自动切换死亡动画
/// - 在 _Process 中根据 CharacterBody2D.Velocity 判断 idle/run 切换
/// - 提供 Play(string) 公开接口供外部直接调用（传 Anim 常量）
/// - 防止重复播放同一动画
/// - 死亡动画优先级最高，锁定后不被其他动画打断
///
/// 动画名称：
/// - 直接使用 <see cref="Anim"/> 常量（与 SpriteFramesGeneratorPlugin 输出一致）
/// - 无需枚举映射，调用方直接传字符串：Play(Anim.Attack1)
///
/// 移动判断：
/// - 在 _Process 中读取 CharacterBody2D.Velocity（兼容 Player/Enemy）
/// - Player 通过 VelocityComponent 设置 Velocity
/// - Enemy 通过 FollowComponent + AI 直接设置 CharacterBody2D.Velocity
/// - 两者都最终体现在 CharacterBody2D.Velocity 上
/// </summary>
public partial class UnitAnimationComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(UnitAnimationComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;
    private AnimatedSprite2D? _sprite;
    private CharacterBody2D? _body;

    // ================= 运行时状态 =================

    /// <summary>当前正在播放的动画名称</summary>
    public string CurrentAnimation { get; private set; } = Anim.Idle;

    /// <summary>死亡动画锁定：锁定后不接受任何其他动画请求</summary>
    private bool _deathLocked = false;

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;

        // 缓存 CharacterBody2D 引用（Player/Enemy 都继承自 CharacterBody2D）
        if (entity is CharacterBody2D body)
            _body = body;

        // 查找 VisualRoot 下的 AnimatedSprite2D
        _sprite = FindSprite(entity);
        if (_sprite == null)
        {
            _log.Warn($"[{entity.Name}] 未找到 AnimatedSprite2D，动画组件无效");
        }

        // ✅ 监听生命周期状态变化（Dying/Dead/Alive）
        _entity.Events.On<GameEventType.Unit.StateChangedEventData>(
            GameEventType.Unit.StateChanged, OnStateChanged);

        // ✅ 监听受击事件
        _entity.Events.On<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);

        // 初始播放 Idle
        Play(Anim.Idle);
    }

    public void OnComponentUnregistered()
    {
        _sprite = null;
        _body = null;
        _entity = null;
        _data = null;
    }

    public void OnComponentReset()
    {
        _deathLocked = false;
        CurrentAnimation = Anim.Idle;
    }

    // ================= Godot 生命周期 =================

    public override void _Process(double delta)
    {
        // 死亡锁定期间不做任何处理
        if (_deathLocked) return;
        if (_body == null) return;

        // 只在空闲态时根据速度切换 idle/run，不打断攻击/受击等动画
        if (CurrentAnimation != Anim.Idle && CurrentAnimation != Anim.Run) return;

        bool isMoving = _body.Velocity.LengthSquared() > 1f;
        Play(isMoving ? Anim.Run : Anim.Idle);
    }

    // ================= 公开 API =================

    /// <summary>
    /// 播放指定动画（直接传 Anim 常量）
    /// 用法：Play(Anim.Attack1)、Play(Anim.Dead)
    /// </summary>
    /// <param name="animName">动画名称（使用 Anim 常量）</param>
    /// <param name="forceRestart">为 true 时即使当前已在播放同一动画也重新开始</param>
    public void Play(string animName, bool forceRestart = false)
    {
        // 死亡锁定：只允许 dead 本身通过
        if (_deathLocked && animName != Anim.Dead) return;

        if (_sprite == null) return;

        // 防重复播放
        if (!forceRestart && CurrentAnimation == animName && _sprite.IsPlaying()) return;

        // 检查 SpriteFrames 中是否存在该动画
        if (_sprite.SpriteFrames != null && !_sprite.SpriteFrames.HasAnimation(animName))
        {
            // 不存在则 fallback 到 idle（避免无限递归）
            if (animName != Anim.Idle)
            {
                _log.Warn($"SpriteFrames 中不存在动画 '{animName}'，fallback 到 idle");
                Play(Anim.Idle, forceRestart);
            }
            return;
        }

        CurrentAnimation = animName;
        _sprite.Play(animName);
        _log.Trace($"播放动画: {animName}");
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 从 Entity 节点树中查找 AnimatedSprite2D
    /// 优先查找 VisualRoot（InjectVisualScene 挂载的节点）
    /// </summary>
    private static AnimatedSprite2D? FindSprite(Node entity)
    {
        var visualRoot = entity.GetNodeOrNull("VisualRoot");

        // VisualRoot 本身就是 AnimatedSprite2D（当前所有单位都是这种结构）
        if (visualRoot is AnimatedSprite2D sprite)
        {
            return sprite;
        }
        else
        {
            _log.Warn($"VisualRoot 不是 AnimatedSprite2D，类型: {visualRoot.GetType().Name}");
        }
        return null;
    }

    // ================= 事件处理 =================

    /// <summary>
    /// 生命周期状态变化 → 切换对应动画
    /// </summary>
    private void OnStateChanged(GameEventType.Unit.StateChangedEventData evt)
    {
        if (evt.Key != DataKey.LifecycleState) return;

        if (!System.Enum.TryParse<LifecycleState>(evt.NewValue, out var newState)) return;

        switch (newState)
        {
            case LifecycleState.Dying:
            case LifecycleState.Dead:
                _deathLocked = true;
                Play(Anim.Dead);
                break;

            case LifecycleState.Alive:
                _deathLocked = false;
                Play(Anim.Idle);
                break;
        }
    }

    /// <summary>
    /// 受击事件 → 播放受击动画
    /// </summary>
    private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
    {
        Play(Anim.BeAttacked);
    }

}
