using System.Collections.Generic;
using Godot;

/// <summary>
/// 接触伤害组件 (解耦自原 HurtComponent)
/// <para>
/// 核心职责：
/// 1. 不包含任何原生物理查询
/// 2. 监听本实体抛出的 HurtboxEntered / HurtboxExited 事件
/// 3. 为接触到的对立阵营实体维护独立的循环 Timer (根据攻击者的 AttackInterval)
/// 4. Timer 触发时，向 DamageService 发起伤害请求
/// </para>
/// </summary>
public partial class ContactDamageComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(ContactDamageComponent));

    /// <summary>所属实体引用</summary>
    private IEntity? _entity;

    /// <summary>实体数据容器</summary>
    private Data? _data;

    /// <summary>本实体阵营</summary>
    private Team _team;

    /// <summary>当前仍与本实体 Hurtbox 接触中的目标</summary>
    private readonly Dictionary<Node2D, IEntity?> _contactBodies = new();

    /// <summary>接触目标 -> 伤害计时器的映射表</summary>
    private readonly Dictionary<Node2D, GameTimer> _bodyTimers = new();

    /// <summary>
    /// 组件注册时初始化，订阅受击区碰撞事件
    /// </summary>
    /// <param name="entity">所属实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _team = _data.Get<Team>(DataKey.Team, Team.Neutral);

        _entity.Events.On<GameEventType.Collision.HurtboxEnteredEventData>(GameEventType.Collision.HurtboxEntered, OnHurtboxEntered);
        _entity.Events.On<GameEventType.Collision.HurtboxExitedEventData>(GameEventType.Collision.HurtboxExited, OnHurtboxExited);
        _entity.Events.On<GameEventType.Unit.KilledEventData>(GameEventType.Unit.Killed, OnKilled);
        _entity.Events.On<GameEventType.Unit.RevivedEventData>(GameEventType.Unit.Revived, OnRevived);

        _log.Debug($"[{entity.Name}] 接触伤害处理组件注册完成，阵营={_team}，开始监听局部碰撞事件。");
    }

    /// <summary>
    /// 组件注销时清理，取消所有伤害计时器
    /// </summary>
    public void OnComponentUnregistered()
    {
        CancelAllBodyTimers();
        _contactBodies.Clear();
        _entity = null;
        _data = null;
    }

    /// <summary>
    /// 受击区进入事件处理：
    /// 1. 检查目标是否为敌对阵营
    /// 2. 立即造成一次伤害 (EnterImmediate)
    /// 3. 创建循环计时器，按攻击间隔持续造成伤害
    /// </summary>
    /// <param name="evt">受击区进入事件数据</param>
    private void OnHurtboxEntered(GameEventType.Collision.HurtboxEnteredEventData evt)
    {
        var attacker = evt.Target;
        if (!IsInstanceValid(attacker)) return;

        _contactBodies[attacker] = evt.TargetEntity;

        if (!IsHostile(attacker, evt.TargetEntity))
            return;

        if (!CanDealContactDamage())
            return;

        StartBodyTimer(attacker, evt.TargetEntity, true);
    }

    /// <summary>
    /// 受击区退出事件处理：取消对应目标的伤害计时器
    /// </summary>
    /// <param name="evt">受击区退出事件数据</param>
    private void OnHurtboxExited(GameEventType.Collision.HurtboxExitedEventData evt)
    {
        _contactBodies.Remove(evt.Target);
        CancelBodyTimer(evt.Target);
    }

    /// <summary>
    /// 死亡事件处理：立即暂停所有持续接触伤害，但保留当前接触集合，供复活后恢复
    /// </summary>
    /// <param name="evt">死亡事件数据</param>
    private void OnKilled(GameEventType.Unit.KilledEventData evt)
    {
        CancelAllBodyTimers();
    }

    /// <summary>
    /// 复活事件处理：若复活完成时仍与敌人重叠，重新建立持续接触伤害
    /// </summary>
    /// <param name="evt">复活完成事件数据</param>
    private void OnRevived(GameEventType.Unit.RevivedEventData evt)
    {
        ResumeContactDamage();
    }

    /// <summary>
    /// 伤害计时器 tick 回调：
    /// 1. 检查本实体是否已死亡
    /// 2. 检查攻击者是否仍有效
    /// 3. 触发伤害 (TimerTick)
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体 (可能为 null)</param>
    private void OnBodyDamageTick(Node2D attacker, IEntity? attackerEntity)
    {
        if (_entity == null || _data == null) return;

        if (!CanDealContactDamage())
        {
            CancelAllBodyTimers();
            return;
        }

        if (!IsInstanceValid(attacker))
        {
            _contactBodies.Remove(attacker);
            CancelBodyTimer(attacker);
            return;
        }

        if (!IsHostile(attacker, attackerEntity))
        {
            CancelBodyTimer(attacker);
            return;
        }

        ApplyDamageFrom(attacker, attackerEntity, "TimerTick");
    }

    /// <summary>
    /// 为指定接触目标启动持续伤害计时器
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体</param>
    /// <param name="applyImmediateDamage">是否立即结算一次进入伤害</param>
    private void StartBodyTimer(Node2D attacker, IEntity? attackerEntity, bool applyImmediateDamage)
    {
        if (!IsInstanceValid(attacker)) return;
        if (_bodyTimers.ContainsKey(attacker)) return;

        if (applyImmediateDamage)
        {
            ApplyDamageFrom(attacker, attackerEntity, "EnterImmediate");
        }

        var interval = GetAttackInterval(attacker, attackerEntity);
        var timer = TimerManager.Instance.Loop(interval)
            .OnLoop(() => OnBodyDamageTick(attacker, attackerEntity));

        _bodyTimers[attacker] = timer;
    }

    /// <summary>
    /// 复活后为仍在接触中的敌对目标恢复持续接触伤害
    /// </summary>
    private void ResumeContactDamage()
    {
        // 前置条件检查：必须能造成接触伤害且存在接触目标
        if (!CanDealContactDamage()) return;
        if (_contactBodies.Count == 0) return;

        // 延迟初始化无效目标列表，避免不必要的内存分配
        List<Node2D>? invalidAttackers = null;

        // 遍历所有记录的接触目标
        foreach (var kv in _contactBodies)
        {
            var attacker = kv.Key;           // 攻击者物理节点
            var attackerEntity = kv.Value;   // 攻击者实体

            // 检查攻击者是否仍然有效（未被销毁）
            if (!IsInstanceValid(attacker))
            {
                // 记录无效目标，稍后统一清理
                invalidAttackers ??= new List<Node2D>();
                invalidAttackers.Add(attacker);
                continue;
            }

            // 检查敌对关系：只有敌对目标才会触发接触伤害
            if (!IsHostile(attacker, attackerEntity))
                continue;

            // 立即启动接触伤害计时器
            // applyImmediateDamage=true：复活瞬间立即造成伤害，补偿死亡期间暂停的伤害
            StartBodyTimer(attacker, attackerEntity, true);
        }

        // 清理无效目标（已被销毁的攻击者）
        if (invalidAttackers == null) return;

        foreach (var attacker in invalidAttackers)
        {
            // 从接触记录中移除
            _contactBodies.Remove(attacker);
            // 取消对应的计时器（如果还存在的话）
            CancelBodyTimer(attacker);
        }
    }

    /// <summary>
    /// 向 DamageService 发起伤害请求
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体</param>
    /// <param name="triggerSource">触发来源标识 (EnterImmediate/TimerTick)</param>
    private void ApplyDamageFrom(Node2D attacker, IEntity? attackerEntity, string triggerSource)
    {
        if (_entity == null || _data == null) return;
        if (_entity is not IUnit victimUnit) return;
        if (!IsInstanceValid(attacker)) return;

        var contactDamage = GetContactDamage(attacker, attackerEntity);
        if (contactDamage <= 0f)
            return;

        var damageInfo = new DamageInfo
        {
            Attacker = attacker,
            Victim = victimUnit,
            Damage = contactDamage,
            Type = DamageType.Physical,
            Tags = DamageTags.Attack
        };

        DamageService.Instance?.Process(damageInfo);
    }

    /// <summary>
    /// 当前是否允许接触伤害运行
    /// </summary>
    /// <returns>存活时返回 true，否则返回 false</returns>
    private bool CanDealContactDamage()
    {
        return _entity != null
            && _data != null
            && !_data.Get<bool>(DataKey.IsDead);
    }

    /// <summary>
    /// 取消指定目标的伤害计时器
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    private void CancelBodyTimer(Node2D attacker)
    {
        if (_bodyTimers.TryGetValue(attacker, out var timer))
        {
            timer.Cancel();
            _bodyTimers.Remove(attacker);
        }
    }

    /// <summary>
    /// 取消所有伤害计时器 (组件注销或实体死亡时调用)
    /// </summary>
    private void CancelAllBodyTimers()
    {
        foreach (var kv in _bodyTimers)
        {
            kv.Value.Cancel();
        }

        _bodyTimers.Clear();
    }

    /// <summary>
    /// 检查攻击者是否为敌对阵营
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体</param>
    /// <returns>是否为敌对关系</returns>
    private bool IsHostile(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return false;

        var bodyTeam = entity.Data.Get<Team>(DataKey.Team);
        return bodyTeam != Team.Neutral && bodyTeam != _team;
    }

    /// <summary>
    /// 获取攻击者的接触伤害值 (使用 FinalAttack)
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体</param>
    /// <returns>伤害值，无效目标返回 0</returns>
    private float GetContactDamage(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return 0f;
        return entity.Data.Get<float>(DataKey.FinalAttack);
    }

    /// <summary>
    /// 获取攻击者的攻击间隔 (用于设置伤害计时器周期)
    /// </summary>
    /// <param name="attacker">攻击者节点</param>
    /// <param name="attackerEntity">攻击者实体</param>
    /// <returns>攻击间隔秒数，最小值 0.1f</returns>
    private float GetAttackInterval(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return 1.0f;

        var interval = entity.Data.Get<float>(DataKey.AttackInterval);
        return Mathf.Max(interval, 0.1f);
    }
}
