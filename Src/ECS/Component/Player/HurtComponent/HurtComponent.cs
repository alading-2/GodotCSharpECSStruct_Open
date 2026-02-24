using System.Collections.Generic;
using Godot;

/// <summary>
/// 通用受伤感应组件 - 检测对立阵营碰撞并造成持续接触伤害
/// <para>
/// 核心设计：
/// - 继承 Area2D，可挂在任意 IUnit 实体上（玩家/敌人/召唤物均可）
/// - 碰撞层在 OnComponentRegistered 时根据 Entity 的 Team 自动配置，场景文件无需硬编码
/// - body_entered 时立即造成一次伤害，并为该 body 启动独立循环计时器
/// - 每个 body 的计时间隔 = 该 attacker 的 AttackInterval（攻击间隔）
/// - body_exited 时取消该 body 的计时器，互不干扰
/// - 伤害走完整 DamageService 管道（可触发闪避/护甲等）
/// </para>
/// <para>
/// 碰撞层自动配置规则（见 CollisionLayers）：
/// - Team.Player → Layer=PlayerHurtbox(8),  Mask=Enemy(4)|Projectile(32)
/// - Team.Enemy  → Layer=EnemyHurtbox(64),  Mask=Player(2)|WeaponHitbox(128)
/// </para>
/// </summary>
public partial class HurtComponent : Area2D, IComponent
{
    private static readonly Log _log = new(nameof(HurtComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    /// <summary>本实体的阵营（注册时缓存，避免每帧读 Data）</summary>
    private Team _ownerTeam = Team.Neutral;

    // ================= 运行时状态 =================

    /// <summary>每个接触 body 对应的独立循环计时器</summary>
    private readonly Dictionary<Node2D, GameTimer> _bodyTimers = new();

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _ownerTeam = _data.Get<Team>(DataKey.Team);

        ConfigureCollisionByTeam(_ownerTeam);

        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;

        // 重新激活碰撞检测
        Monitoring = true;

        _log.Debug($"[{entity.Name}] 受伤感应组件注册完成，阵营={_ownerTeam}");
    }

    public void OnComponentUnregistered()
    {
        Monitoring = false;
        CancelAllBodyTimers();

        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;

        _entity = null;
        _data = null;
        _ownerTeam = Team.Neutral;
    }

    // ================= 碰撞事件 =================

    /// <summary>
    /// 对立阵营物理体进入感应区：立即造成一次伤害，并启动该 body 的独立循环计时器
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        if (!IsInstanceValid(body)) return;
        if (!IsHostile(body)) return;

        if (_bodyTimers.ContainsKey(body)) return;

        ApplyDamageFrom(body);

        float interval = GetAttackInterval(body);

        var timer = TimerManager.Instance.Loop(interval)
            .OnLoop(() => OnBodyDamageTick(body));
        _bodyTimers[body] = timer;
    }

    /// <summary>
    /// 对立阵营物理体离开感应区：取消该 body 的计时器
    /// </summary>
    private void OnBodyExited(Node2D body)
    {
        CancelBodyTimer(body);
    }

    // ================= 伤害逻辑 =================

    /// <summary>
    /// 某个 body 的循环计时器触发：再次造成伤害
    /// </summary>
    private void OnBodyDamageTick(Node2D body)
    {
        if (_entity == null || _data == null) return;

        // 自身死亡时停止所有计时器
        if (_data.Get<bool>(DataKey.IsDead))
        {
            CancelAllBodyTimers();
            Monitoring = false;
            return;
        }

        // body 已失效（死亡/离开场景树）则取消
        if (!IsInstanceValid(body))
        {
            CancelBodyTimer(body);
            return;
        }

        // attacker 已死亡则取消
        if (body is IEntity attackerEntity && attackerEntity.Data.Get<bool>(DataKey.IsDead))
        {
            CancelBodyTimer(body);
            return;
        }

        // 防御性校验：如果当前并未真实重叠，停止该 body 的持续伤害计时器
        if (!IsBodyCurrentlyOverlapping(body))
        {
            CancelBodyTimer(body);
            return;
        }

        ApplyDamageFrom(body);
    }

    /// <summary>
    /// 对本实体造成来自指定 body 的接触伤害
    /// </summary>
    private void ApplyDamageFrom(Node2D body)
    {
        if (_entity == null || _data == null) return;
        if (_entity is not IUnit victimUnit) return;
        if (!IsInstanceValid(body)) return;

        float contactDamage = GetContactDamage(body);
        if (contactDamage <= 0f)
        {
            _log.Trace($"[ApplyDamageFrom] body={body.Name} 接触伤害=0，跳过");
            return;
        }

        var damageInfo = new DamageInfo
        {
            Attacker = body,
            Victim = victimUnit,
            Damage = contactDamage,
            Type = DamageType.Physical,
            Tags = DamageTags.Melee | DamageTags.Persistent
        };

        DamageService.Instance?.Process(damageInfo);
        _log.Trace($"[ApplyDamageFrom] {body.Name} → {(_entity as Node)?.Name}  伤害={contactDamage}");
    }

    // ================= 计时器管理 =================

    /// <summary>取消指定 body 的计时器并从字典移除</summary>
    private void CancelBodyTimer(Node2D body)
    {
        if (_bodyTimers.TryGetValue(body, out var timer))
        {
            timer.Cancel();
            _bodyTimers.Remove(body);
        }
    }

    /// <summary>取消所有 body 的计时器并清空字典</summary>
    private void CancelAllBodyTimers()
    {
        foreach (var kv in _bodyTimers)
        {
            kv.Value.Cancel();
        }
        _bodyTimers.Clear();
    }

    // ================= 辅助方法 =================

    /// <summary>
    /// 根据 Entity 阵营自动配置碰撞层级
    /// 场景文件（.tscn）中无需硬编码，统一在此处设置
    /// </summary>
    private void ConfigureCollisionByTeam(Team team)
    {
        switch (team)
        {
            case Team.Player:
                CollisionLayer = CollisionLayers.PlayerHurtbox;
                CollisionMask = CollisionLayers.PlayerHurtboxMask;
                _log.Debug($"[ConfigureCollisionByTeam] Player: Layer={CollisionLayer}, Mask={CollisionMask}");
                break;
            case Team.Enemy:
                CollisionLayer = CollisionLayers.EnemyHurtbox;
                CollisionMask = CollisionLayers.EnemyHurtboxMask;
                _log.Debug($"[ConfigureCollisionByTeam] Enemy: Layer={CollisionLayer}, Mask={CollisionMask}");
                break;
            default:
                CollisionLayer = 0;
                CollisionMask = 0;
                _log.Warn($"HurtComponent: 未知阵营 {team}，碰撞层已清零");
                break;
        }
    }

    /// <summary>
    /// 判断节点是否为对立阵营（通过 IEntity 的 Team 数据判断）
    /// </summary>
    private bool IsHostile(Node2D body)
    {
        if (body is not IEntity entity) return false;

        var bodyTeam = entity.Data.Get<Team>(DataKey.Team);
        // 中立不触发伤害；双方阵营不同即为敌对
        return bodyTeam != Team.Neutral && bodyTeam != _ownerTeam;
    }

    private bool IsBodyCurrentlyOverlapping(Node2D body)
    {
        var bodies = GetOverlappingBodies();
        for (int i = 0; i < bodies.Count; i++)
        {
            if (ReferenceEquals(bodies[i], body))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 获取接触单位的接触伤害值
    /// 优先读取 ContactDamage 专属字段，回退到 FinalAttack
    /// </summary>
    private float GetContactDamage(Node2D attacker)
    {
        if (attacker is not IEntity entity) return 0f;

        if (entity.Data.Has(DataKey.ContactDamage))
            return entity.Data.Get<float>(DataKey.ContactDamage);

        return entity.Data.Get<float>(DataKey.FinalAttack, 5f);
    }

    /// <summary>
    /// 获取 attacker 的攻击间隔（秒）
    /// 优先读取 AttackInterval，回退到默认 1.0f
    /// </summary>
    private float GetAttackInterval(Node2D attacker)
    {
        if (attacker is not IEntity entity) return 1.0f;

        float interval = entity.Data.Get<float>(DataKey.AttackInterval, 1.0f);
        // 防止间隔过短导致性能问题，最小 0.1s
        return Mathf.Max(interval, 0.1f);
    }
}
