using Godot;
using System.Linq;

/// <summary>
/// 特效组件 - 完整特效管理
///
/// 核心职责：
/// - 附着模式下跟随宿主位置
/// - MaxLifeTime 计时器自动销毁
/// - 查找动画节点并直接播放默认动画
/// - 非循环动画播完后自动销毁（无 MaxLifeTime 时）
/// - 监听宿主销毁事件自动销毁附着特效
/// - 配置播放速率
///
/// 职责分工：
/// - 视觉加载 + 缩放/偏移 → EffectTool.Spawn
/// - 其他所有逻辑 → EffectComponent
/// </summary>
public partial class EffectComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(EffectComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    // ================= Data 透传 =================

    /// <summary>是否附着到宿主</summary>
    public bool IsAttached => _data.Get<bool>(DataKey.EffectIsAttached);

    /// <summary>附着偏移</summary>
    public Vector2 Offset => _data.Get<Vector2>(DataKey.EffectOffset);

    /// <summary>是否循环播放</summary>
    public bool IsLooping => _data.Get<bool>(DataKey.EffectIsLooping);

    /// <summary>播放速率</summary>
    public float PlayRate => _data.Get<float>(DataKey.EffectPlayRate);

    /// <summary>最大生存时间</summary>
    public float MaxLifeTime => _data.Get<float>(DataKey.MaxLifeTime);

    // ================= 附着跟随 =================

    /// <summary>宿主节点引用（附着模式下缓存）</summary>
    private Node2D? _hostNode;

    /// <summary>动画精灵引用（用于监听动画结束信号）</summary>
    private AnimatedSprite2D? _sprite;

    /// <summary>生命周期计时器</summary>
    private GameTimer? _lifeTimer;

    /// <summary>当前动画名</summary>
    private string _currentAnimation = string.Empty;

    // ================= IComponent 实现 =================

    /// <inheritdoc/>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;

        // 查找动画节点并直接播放默认动画
        SetupAnimation(entity);

        // 启动生命周期计时器
        StartLifeTimer();

        // 缓存宿主引用（附着模式）并监听宿主销毁
        SetupAttachment();
    }

    /// <inheritdoc/>
    public void OnComponentUnregistered()
    {
        if (_sprite != null)
            _sprite.AnimationFinished -= OnAnimationFinished;

        _lifeTimer?.Cancel();
        _lifeTimer = null;

        // 取消宿主销毁事件监听
        GlobalEventBus.Global.Off<GameEventType.Global.EntityDestroyedEventData>(
            GameEventType.Global.EntityDestroyed, OnHostDestroyed);

        _sprite = null;
        _hostNode = null;
        _entity = null;
        _data = null;
    }

    // ================= Godot 生命周期 =================

    /// <inheritdoc/>
    public override void _Process(double delta)
    {
        if (_hostNode == null) return;
        if (_entity is not Node2D selfNode) return;

        if (!GodotObject.IsInstanceValid(_hostNode))
        {
            _log.Debug("宿主已销毁，特效自动销毁");
            DestroySelf();
            return;
        }

        selfNode.GlobalPosition = _hostNode.GlobalPosition + Offset;
    }

    // ================= 初始化 =================

    /// <summary>
    /// 设置附着模式：查找宿主节点、缓存引用、监听宿主销毁事件
    /// </summary>
    private void SetupAttachment()
    {
        _hostNode = null;
        if (!IsAttached) return;
        if (_entity is not Node effectNode) return;

        var parentIds = EntityRelationshipManager.GetParentEntitiesByChildAndType(
            effectNode.GetInstanceId().ToString(),
            EntityRelationshipType.ENTITY_TO_EFFECT);

        var hostEntityId = parentIds.FirstOrDefault();
        if (string.IsNullOrEmpty(hostEntityId))
        {
            _log.Warn("附着模式但未找到宿主关系");
            return;
        }

        var hostNode = EntityManager.GetEntityById(hostEntityId);
        if (hostNode is Node2D host2D)
        {
            _hostNode = host2D;
            _log.Debug($"附着到宿主: {hostNode.Name}");

            // 监听宿主销毁事件
            GlobalEventBus.Global.On<GameEventType.Global.EntityDestroyedEventData>(
                GameEventType.Global.EntityDestroyed, OnHostDestroyed);
        }
        else
        {
            _log.Warn($"未找到宿主 Entity: {hostEntityId}");
        }
    }

    /// <summary>
    /// 宿主销毁事件处理：如果是自己的宿主，则自动销毁
    /// </summary>
    private void OnHostDestroyed(GameEventType.Global.EntityDestroyedEventData evt)
    {
        if (_hostNode == null) return;
        if (evt.Entity is not Node destroyedNode) return;

        // 检查是否是自己的宿主
        if (destroyedNode.GetInstanceId() == _hostNode.GetInstanceId())
        {
            _log.Debug("宿主已销毁，特效自动销毁");
            DestroySelf();
        }
    }

    /// <summary>
    /// 查找 AnimatedSprite2D、直接播放默认动画、监听动画结束信号
    /// </summary>
    private void SetupAnimation(Node entity)
    {
        if (_entity is not Node entityNode) return;

        var visualRoot = entityNode.GetNodeOrNull("VisualRoot");
        if (visualRoot == null)
        {
            _log.Warn("未找到 VisualRoot");
            return;
        }

        _sprite = visualRoot is AnimatedSprite2D s ? s : FindAnimatedSprite(visualRoot);

        if (_sprite == null)
        {
            _log.Warn("VisualRoot 中未找到 AnimatedSprite2D");
            return;
        }

        // 监听动画结束信号
        _sprite.AnimationFinished += OnAnimationFinished;

        // 直接播放默认动画（idle 优先）
        PlayDefaultAnimation();
    }

    /// <summary>
    /// 直接播放默认动画：优先 idle，否则取第一个非 default 动画
    /// </summary>
    private void PlayDefaultAnimation()
    {
        if (_sprite?.SpriteFrames == null) return;

        var animName = ResolveDefaultAnimation();
        if (string.IsNullOrEmpty(animName)) return;

        _entity.Events.Emit(
            GameEventType.Unit.PlayAnimationRequested,
            new GameEventType.Unit.PlayAnimationRequestedEventData(animName));
    }

    /// <summary>
    /// 解析默认动画名：优先 idle，否则第一个非 default 动画
    /// </summary>
    private string ResolveDefaultAnimation()
    {
        if (_sprite?.SpriteFrames == null) return string.Empty;

        // 优先使用显式指定的动画名
        string specifiedAnim = _data?.Get<string>(DataKey.EffectAnimationName) ?? string.Empty;
        if (!string.IsNullOrEmpty(specifiedAnim) && _sprite.SpriteFrames.HasAnimation(specifiedAnim))
        {
            return specifiedAnim;
        }

        var animNames = _sprite.SpriteFrames.GetAnimationNames();
        if (animNames.Length == 0) return string.Empty;
        if (animNames.Contains(Anim.Effect)) return Anim.Effect;

        foreach (var name in animNames)
        {
            if (name != "default") return name;
        }

        return animNames[0];
    }

    /// <summary>
    /// 递归查找第一个 AnimatedSprite2D
    /// </summary>
    private static AnimatedSprite2D? FindAnimatedSprite(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is AnimatedSprite2D sprite)
                return sprite;
            var found = FindAnimatedSprite(child);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>
    /// 启动生命周期计时器
    /// MaxLifeTime 大于 0：到时间自动销毁
    /// MaxLifeTime 小于等于 0 且非循环：由动画结束信号触发销毁
    /// MaxLifeTime 小于等于 0 且循环：永久播放，需外部调用 EffectTool 销毁
    /// </summary>
    private void StartLifeTimer()
    {
        if (MaxLifeTime > 0)
        {
            _lifeTimer = TimerManager.Instance?.Delay(MaxLifeTime)
                .OnComplete(DestroySelf);
            _log.Debug($"启动生命周期计时器: {MaxLifeTime}s");
        }
    }

    // ================= 信号处理 =================

    /// <summary>
    /// 动画播放完毕回调
    /// 非循环动画播完后自动销毁（除非有 MaxLifeTime 计时器管理）
    /// </summary>
    private void OnAnimationFinished()
    {

        if (IsLooping) return;

        // 非循环动画播完，且没有设置 MaxLifeTime，则自动销毁
        if (MaxLifeTime <= 0)
        {
            _log.Debug("动画播放完毕，自动销毁");
            DestroySelf();
        }
    }


    /// <summary>
    /// 销毁自身特效实体
    /// </summary>
    private void DestroySelf()
    {
        if (_entity is Node entityNode)
        {
            EntityManager.Destroy(entityNode);
        }
    }
}
