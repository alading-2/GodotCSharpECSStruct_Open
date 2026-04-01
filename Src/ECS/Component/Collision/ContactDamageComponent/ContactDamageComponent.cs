using System.Collections.Generic;
using Godot;

/// <summary>
/// 接触伤害组件 (解耦自原 HurtComponent)
/// <para>
/// 核心职责：
/// 1. 不包含任何原生物理查询
/// 2. 监听本实体抛出的 CollisionEntered / CollisionExited 事件
/// 3. 为接触到的对立阵营实体维护独立的循环 Timer (根据攻击者的 AttackInterval)
/// 4. Timer 触发时，向 DamageService 发起伤害请求
/// </para>
/// </summary>
public partial class ContactDamageComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(ContactDamageComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    /// <summary>本实体的阵营（注册时缓存，避免每帧读 Data）</summary>
    private Team _team;

    // ================= 运行时状态 =================

    /// <summary>每个接触 body 对应的独立循环计时器</summary>
    private readonly Dictionary<Node2D, GameTimer> _bodyTimers = new();

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _team = _data.Get<Team>(DataKey.Team, Team.Neutral);

        // 订阅物理碰撞事件
        _entity.Events.On<GameEventType.Collision.CollisionEnteredEventData>(GameEventType.Collision.CollisionEntered, OnCollisionEntered);
        _entity.Events.On<GameEventType.Collision.CollisionExitedEventData>(GameEventType.Collision.CollisionExited, OnCollisionExited);

        _log.Debug($"[{entity.Name}] 接触伤害处理组件注册完成，阵营={_team}，开始监听局部碰撞事件。");
    }

    /// <summary>
    /// 组件卸载
    /// </summary>
    public void OnComponentUnregistered()
    {
        CancelAllBodyTimers();

        _entity = null;
        _data = null;
    }

    // ================= 事件响应与处理 =================

    /// <summary>
    /// 响应碰撞进入事件：判断敌对、造成即时伤害并开启循环计时器
    /// </summary>
    /// <param name="evt">碰撞进入事件数据负载</param>
    private void OnCollisionEntered(GameEventType.Collision.CollisionEnteredEventData evt)
    {
        if (!IsHurtboxSensor(evt.CollisionType)) return; // 只处理 HurtboxSensor 类型

        var body = evt.Target;

        if (!IsInstanceValid(body)) return; // 目标节点无效
        if (!IsHostile(body)) return; // 目标节点不是敌对

        if (_bodyTimers.ContainsKey(body)) return; // 目标节点已经存在

        // 首次接触立即造成一次伤害
        ApplyDamageFrom(body);

        // 启动该 body 的独立循环计时器
        float interval = GetAttackInterval(body);
        var timer = TimerManager.Instance.Loop(interval)
            .OnLoop(() => OnBodyDamageTick(body));

        _bodyTimers[body] = timer;
    }

    /// <summary>
    /// 响应碰撞离开事件：清理对应的伤害计时器
    /// </summary>
    /// <param name="evt">碰撞离开事件数据负载</param>
    private void OnCollisionExited(GameEventType.Collision.CollisionExitedEventData evt)
    {
        if (!IsHurtboxSensor(evt.CollisionType)) return; // 只处理 HurtboxSensor 类型
        CancelBodyTimer(evt.Target);
    }

    // ================= 伤害循环逻辑 =================

    /// <summary>
    /// 独立计时器触发的伤害心跳逻辑：检查存活状态并执行伤害结算
    /// </summary>
    /// <param name="body">造成伤害的来源节点</param>
    private void OnBodyDamageTick(Node2D body)
    {
        if (_entity == null || _data == null) return;

        // 自身死亡时停止所有计时器
        if (_data.Get<bool>(DataKey.IsDead))
        {
            CancelAllBodyTimers();
            return;
        }

        // body 已失效（死亡/离开场景树）则取消
        if (!IsInstanceValid(body))
        {
            CancelBodyTimer(body);
            return;
        }

        // 移除 attacker 已死亡则取消的拦截，
        // 确保“尸体有毒/留痕毒火”这种环境接触依然能造成伤害，死不等于伤害失效
        /*
        if (body is IEntity attackerEntity && attackerEntity.Data.Get<bool>(DataKey.IsDead))
        {
            CancelBodyTimer(body);
            return;
        }
        */

        ApplyDamageFrom(body);
    }

    /// <summary>
    /// 执行具体的伤害结算请求
    /// </summary>
    /// <param name="body">攻击发起者节点</param>
    private void ApplyDamageFrom(Node2D body)
    {
        if (_entity == null || _data == null) return;
        if (_entity is not IUnit victimUnit) return;
        if (!IsInstanceValid(body)) return;

        float contactDamage = GetContactDamage(body);
        if (contactDamage <= 0f)
        {
            _log.Trace($"[ApplyDamageFrom] body={body.Name} 接触伤害为0，跳过结算");
            return;
        }

        var damageInfo = new DamageInfo
        {
            Attacker = body,
            Victim = victimUnit,
            Damage = contactDamage,
            Type = DamageType.Physical,
            Tags = DamageTags.Physical
        };

        DamageService.Instance?.Process(damageInfo);
        _log.Trace($"[ApplyDamageFrom] {body.Name} -> {(_entity as Node)?.Name} 造成接触伤害={contactDamage}");
    }

    // ================= 计时器管理 =================

    /// <summary>
    /// 取消并移除指定节点的伤害计时器
    /// </summary>
    /// <param name="body">目标节点</param>
    private void CancelBodyTimer(Node2D body)
    {
        if (_bodyTimers.TryGetValue(body, out var timer))
        {
            timer.Cancel();
            _bodyTimers.Remove(body);
        }
    }

    /// <summary>
    /// 停止并清理所有的伤害计时器（用于组件卸载或死亡清理）
    /// </summary>
    private void CancelAllBodyTimers()
    {
        foreach (var kv in _bodyTimers)
        {
            kv.Value.Cancel();
        }
        _bodyTimers.Clear();
    }

    // ================= 阵营与数值查询 =================

    /// <summary>
    /// 判断碰撞类型是否为 HurtboxSensor（受击感应器），ContactDamage 只响应此类碰撞
    /// </summary>
    private static bool IsHurtboxSensor(CollisionType type) =>
        (type & CollisionType.Hurtbox) != 0;

    /// <summary>
    /// 判断目标是否为敌对
    /// </summary>
    private bool IsHostile(Node2D body)
    {
        if (body is not IEntity entity) return false;

        var bodyTeam = entity.Data.Get<Team>(DataKey.Team);
        // 中立不触发伤害；双方阵营不同即为敌对
        return bodyTeam != Team.Neutral && bodyTeam != _team;
    }

    /// <summary>
    /// 从攻击者实体的 Data 容器获取接触伤害数值
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <returns>伤害数值（默认为 FinalAttack）</returns>
    private float GetContactDamage(Node2D attacker)
    {
        if (attacker is not IEntity entity) return 0f;
        // 如果有独立的 ContactDamage (毒圈等环境伤害可能配置这个)，否则回归通用的 FinalAttack
        return entity.Data.Get<float>(DataKey.FinalAttack);
    }

    /// <summary>
    /// 获取攻击频率（间隔秒数），决定伤害触发速率
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <returns>伤害间隔时间（最小限制为 0.1s）</returns>
    private float GetAttackInterval(Node2D attacker)
    {
        if (attacker is not IEntity entity) return 1.0f;

        float interval = entity.Data.Get<float>(DataKey.AttackInterval);
        // 防卡死限制，最小 0.1s
        return Mathf.Max(interval, 0.1f);
    }
}
