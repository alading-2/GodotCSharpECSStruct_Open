using Godot;
using System.Collections.Generic;

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
/// 动画播放模式：区分循环动画和一次性动画的锁定行为
/// </summary>
public enum AnimPlayMode
{
    /// <summary>循环动画（idle/run），_Process 可自由切换</summary>
    Loop,
    /// <summary>一次性动画（attack/beattacked/dead），播放期间锁定，由 OnAnimationFinished 解锁</summary>
    OneShot,
}

/// <summary>
/// 单位动画组件 - 统一管理 AnimatedSprite2D 的动画播放
///
/// 核心职责：
/// - 缓存 VisualRoot 下的 AnimatedSprite2D 引用
/// - 监听生命周期事件，自动切换死亡动画
/// - 在 _Process 中根据 CharacterBody2D.Velocity 判断 idle/run 切换
/// - 监听 GameEventType.Unit.PlayAnimationRequested 事件，外部通过事件触发动画播放
/// - 防止重复播放同一动画
/// - 死亡动画优先级最高，锁定后不被其他动画打断
///
/// 动画名称：
/// - 直接使用 <see cref="Anim"/> 常量（与 SpriteFramesGeneratorPlugin 输出一致）
/// - 通过触发 GameEventType.Unit.PlayAnimationRequested 完成播放
///
/// 移动判断：
/// - 在 _Process 中读取 CharacterBody2D.Velocity（兼容 Player/Enemy）
/// - Player 通过 VelocityComponent 设置 Velocity
/// - Enemy 通过 FollowComponent + AI 直接设置 CharacterBody2D.Velocity
/// - 两者都最终体现在 CharacterBody2D.Velocity 上
/// </summary>
public partial class UnitAnimationComponent : Node2D, IComponent
{
    private static readonly Log _log = new(nameof(UnitAnimationComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;
    private AnimatedSprite2D? _sprite;
    private CharacterBody2D? _body;

    // ================= 运行时状态 =================

    /// <summary>当前正在播放的动画名称</summary>
    private string CurrentAnimation { get; set; } = Anim.Idle;

    /// <summary>
    /// 当前动画的播放模式：循环动画（idle/run）可被 _Process 自由切换；
    /// 一次性动画（attack/beattacked/dead）播放期间锁定，由 OnAnimationFinished 解锁。
    /// </summary>
    private AnimPlayMode _playMode = AnimPlayMode.Loop;

    /// <summary>是否处于死亡中（Dead 或 Reviving），此期间锁定动画不被打断</summary>
    private bool IsDeadOrReviving
    {
        get
        {
            if (_data == null) return false;
            var state = _data.Get<LifecycleState>(DataKey.LifecycleState);
            return state == LifecycleState.Dead || state == LifecycleState.Reviving;
        }
    }

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
        else
        {
            // 读取并缓存所有可用动画名称到 Data（供其他组件使用，如 AttackComponent 随机选择攻击动画）
            var spriteFrames = _sprite.SpriteFrames;
            if (spriteFrames != null)
            {
                var animNames = new List<string>();
                foreach (var animName in spriteFrames.GetAnimationNames())
                {
                    animNames.Add(animName);
                }
                _data.Set(DataKey.AvailableAnimations, animNames);
                _log.Debug($"[{entity.Name}] 缓存了 {animNames.Count} 个可用动画: {string.Join(", ", animNames.ToArray())}");
            }
        }

        // ✅ 监听生命周期状态变化（Dead/Reviving/Alive）
        _entity.Events.On<GameEventType.Unit.KilledEventData>(
            GameEventType.Unit.Killed, OnKilled);

        // ✅ 监听受击事件
        _entity.Events.On<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);

        // ✅ 监听外部发来的动画播放请求事件
        _entity.Events.On<GameEventType.Unit.PlayAnimationRequestedEventData>(
            GameEventType.Unit.PlayAnimationRequested, OnPlayAnimationRequested);

        // ✅ 监听停止动画请求（攻击取消等）
        _entity.Events.On<GameEventType.Unit.StopAnimationRequestedEventData>(
            GameEventType.Unit.StopAnimationRequested, OnStopAnimationRequested);

        // 初始播放 Idle
        Play(Anim.Idle);

        // ✅ 监听动画播放完毕信号（用于一次性动画结束后自动回退到 idle/run）
        if (_sprite != null)
            _sprite.AnimationFinished += OnAnimationFinished;
    }

    public void OnComponentUnregistered()
    {
        if (_sprite != null)
            _sprite.AnimationFinished -= OnAnimationFinished;

        _sprite = null;
        _body = null;
        _entity = null;
        _data = null;
    }

    // ================= Godot 生命周期 =================

    public override void _Process(double delta)
    {
        // Dead/Reviving 期间锁定动画，Alive 允许正常切换
        if (IsDeadOrReviving) return;
        if (_body == null || _sprite == null) return;

        // 一次性动画（攻击/受击等）播放期间不打断，由 OnAnimationFinished 信号负责回退
        if (_playMode == AnimPlayMode.OneShot) return;

        bool isMoving = _body.Velocity.LengthSquared() > 1f;
        Play(isMoving ? Anim.Run : Anim.Idle);
    }

    // ================= 公开 API =================

    /// <summary>
    /// 播放指定动画（私有方法）
    /// 用法受限：外部通过 GameEventType.Unit.PlayAnimationRequested 事件调用
    /// <param name="animName">动画名称</param>
    /// <param name="forceRestart">是否强制重新播放</param>
    /// <param name="duration">动画持续时间</param>
    /// </summary>
    private void Play(string animName, bool forceRestart = false, float duration = -1f)
    {
        // Dead/Reviving 期间只允许 dead 动画本身通过
        if (IsDeadOrReviving && animName != Anim.Dead) return;

        if (_sprite == null) return;

        // 防重复播放
        if (!forceRestart && CurrentAnimation == animName && _sprite.IsPlaying()) return;

        // 检查 SpriteFrames 中是否存在该动画
        var availableAnims = _data.Get<System.Collections.Generic.List<string>>(DataKey.AvailableAnimations);
        if (!availableAnims.Contains(animName))
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
        _playMode = _sprite.SpriteFrames?.GetAnimationLoop(animName) == true
            ? AnimPlayMode.Loop
            : AnimPlayMode.OneShot;

        // 重置动画速度
        _sprite.SpeedScale = 1.0f;

        // 计算并设置持续时间对应的拉伸速度
        if (duration > 0 && _sprite.SpriteFrames != null)
        {
            int frameCount = _sprite.SpriteFrames.GetFrameCount(animName);
            double fps = _sprite.SpriteFrames.GetAnimationSpeed(animName);
            if (fps > 0 && frameCount > 0)
            {
                float naturalDuration = frameCount / (float)fps;
                _sprite.SpeedScale = naturalDuration / duration;
            }
        }

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

    // ================= 信号处理 =================

    /// <summary>
    /// AnimatedSprite2D.AnimationFinished 信号回调
    /// 一次性动画（攻击/受击等）播放完毕后，根据当前速度自动回退到 idle 或 run
    /// </summary>
    private void OnAnimationFinished()
    {
        // idle/run 是循环动画，不需要回退
        if (_playMode == AnimPlayMode.Loop) return;

        if (CurrentAnimation == Anim.Dead)
        {
            // 死亡动画播完：发出通用事件（携带动画名），LifecycleComponent 监听后处理
            _entity.Events.Emit(GameEventType.Unit.AnimationFinished,
                new GameEventType.Unit.AnimationFinishedEventData(Anim.Dead));
            // 暂停在最后一帧（Pause 不重置帧，Stop 会回到第0帧）
            _sprite?.Pause();
        }

        _playMode = AnimPlayMode.Loop; // 解锁，让 Play() 能正常切换
        return;
    }

    // ================= 事件处理 =================

    /// <summary>
    /// 生命周期状态变化 → 切换对应动画
    /// </summary>
    private void OnKilled(GameEventType.Unit.KilledEventData evt)
    {
        // IsDead 由 LifecycleComponent 写入 Data，Play() 内部会检查
        Play(Anim.Dead);
    }

    /// <summary>
    /// 受击事件 → 播放受击动画
    /// </summary>
    private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
    {
        Play(Anim.BeAttacked);
    }

    /// <summary>
    /// 收到动画播放请求事件 -> 直接播放
    /// </summary>
    private void OnPlayAnimationRequested(GameEventType.Unit.PlayAnimationRequestedEventData evt)
    {
        Play(evt.AnimName, evt.ForceRestart, evt.Duration);
    }

    /// <summary>
    /// 收到停止动画请求 -> 立即中断当前动画并回到 idle
    /// </summary>
    private void OnStopAnimationRequested(GameEventType.Unit.StopAnimationRequestedEventData evt)
    {
        if (_sprite == null) return;
        if (IsDeadOrReviving) return; // Dead/Reviving 期间死亡动画不被中断

        _sprite.Stop();
        _playMode = AnimPlayMode.Loop;
        _sprite.SpeedScale = 1.0f;
        _sprite.Play(Anim.Idle);
        _log.Trace("动画中断 → idle");
    }
}
