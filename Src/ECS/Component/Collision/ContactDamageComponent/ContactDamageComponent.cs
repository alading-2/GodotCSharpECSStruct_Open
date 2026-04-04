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

    private IEntity? _entity;
    private Data? _data;
    private Team _team;
    private readonly Dictionary<Node2D, GameTimer> _bodyTimers = new();

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _team = _data.Get<Team>(DataKey.Team, Team.Neutral);

        _entity.Events.On<GameEventType.Collision.HurtboxEnteredEventData>(GameEventType.Collision.HurtboxEntered, OnHurtboxEntered);
        _entity.Events.On<GameEventType.Collision.HurtboxExitedEventData>(GameEventType.Collision.HurtboxExited, OnHurtboxExited);

        _log.Debug($"[{entity.Name}] 接触伤害处理组件注册完成，阵营={_team}，开始监听局部碰撞事件。");
    }

    public void OnComponentUnregistered()
    {
        CancelAllBodyTimers();
        _entity = null;
        _data = null;
    }

    private void OnHurtboxEntered(GameEventType.Collision.HurtboxEnteredEventData evt)
    {
        var attacker = evt.Target;
        if (!IsInstanceValid(attacker)) return;

        if (!IsHostile(attacker, evt.TargetEntity))
        {
            _log.Debug($"[ContactCollisionIgnored] victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} attackerEntity={FormatEntityDebug(evt.TargetEntity)} reason=NotHostile distance={FormatDistance(_entity as Node, attacker)}");
            return;
        }

        if (_bodyTimers.ContainsKey(attacker))
        {
            _log.Debug($"[ContactCollisionDuplicate] victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} attackerEntity={FormatEntityDebug(evt.TargetEntity)} distance={FormatDistance(_entity as Node, attacker)}");
            return;
        }

        _log.Debug($"[ContactCollisionEntered] victim={FormatNodeDebug(_entity as Node)} hurtbox={FormatNodeDebug(evt.Hurtbox)} attacker={FormatNodeDebug(attacker)} attackerEntity={FormatEntityDebug(evt.TargetEntity)} distance={FormatDistance(_entity as Node, attacker)}");

        ApplyDamageFrom(attacker, evt.TargetEntity, "EnterImmediate");

        var interval = GetAttackInterval(attacker, evt.TargetEntity);
        var timer = TimerManager.Instance.Loop(interval)
            .OnLoop(() => OnBodyDamageTick(attacker, evt.TargetEntity));

        _bodyTimers[attacker] = timer;
        _log.Debug($"[ContactTimerStarted] victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} interval={interval:F2}s activeTimers={_bodyTimers.Count} distance={FormatDistance(_entity as Node, attacker)}");
    }

    private void OnHurtboxExited(GameEventType.Collision.HurtboxExitedEventData evt)
    {
        _log.Debug($"[ContactCollisionExited] victim={FormatNodeDebug(_entity as Node)} hurtbox={FormatNodeDebug(evt.Hurtbox)} attacker={FormatNodeDebug(evt.Target)} attackerEntity={FormatEntityDebug(evt.TargetEntity)} distance={FormatDistance(_entity as Node, evt.Target)}");
        CancelBodyTimer(evt.Target);
    }

    private void OnBodyDamageTick(Node2D attacker, IEntity? attackerEntity)
    {
        if (_entity == null || _data == null) return;

        if (_data.Get<bool>(DataKey.IsDead))
        {
            _log.Debug($"[ContactTimerStopAll] victim={FormatNodeDebug(_entity as Node)} reason=VictimDead activeTimers={_bodyTimers.Count}");
            CancelAllBodyTimers();
            return;
        }

        if (!IsInstanceValid(attacker))
        {
            _log.Debug($"[ContactTimerCancelled] victim={FormatNodeDebug(_entity as Node)} attacker=<invalid> reason=AttackerInvalid");
            CancelBodyTimer(attacker);
            return;
        }

        _log.Debug($"[ContactTimerTick] victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} distance={FormatDistance(_entity as Node, attacker)} activeTimers={_bodyTimers.Count}");
        ApplyDamageFrom(attacker, attackerEntity, "TimerTick");
    }

    private void ApplyDamageFrom(Node2D attacker, IEntity? attackerEntity, string triggerSource)
    {
        if (_entity == null || _data == null) return;
        if (_entity is not IUnit victimUnit) return;
        if (!IsInstanceValid(attacker)) return;

        var contactDamage = GetContactDamage(attacker, attackerEntity);
        if (contactDamage <= 0f)
        {
            _log.Debug($"[ContactDamageSkipped] trigger={triggerSource} victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} damage={contactDamage:F2} distance={FormatDistance(_entity as Node, attacker)}");
            return;
        }

        _log.Debug($"[ContactDamageApply] trigger={triggerSource} victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} attackerEntity={FormatEntityDebug(attackerEntity)} damage={contactDamage:F2} interval={GetAttackInterval(attacker, attackerEntity):F2}s distance={FormatDistance(_entity as Node, attacker)}");

        var damageInfo = new DamageInfo
        {
            Attacker = attacker,
            Victim = victimUnit,
            Damage = contactDamage,
            Type = DamageType.Physical,
            Tags = DamageTags.Attack
        };

        DamageService.Instance?.Process(damageInfo);
        _log.Debug($"[ContactDamageSubmitted] trigger={triggerSource} damageId={damageInfo.Id} victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} damage={contactDamage:F2} distance={FormatDistance(_entity as Node, attacker)}");
    }

    private void CancelBodyTimer(Node2D attacker)
    {
        if (_bodyTimers.TryGetValue(attacker, out var timer))
        {
            timer.Cancel();
            _bodyTimers.Remove(attacker);
            _log.Debug($"[ContactTimerCancelled] victim={FormatNodeDebug(_entity as Node)} attacker={FormatNodeDebug(attacker)} activeTimers={_bodyTimers.Count} distance={FormatDistance(_entity as Node, attacker)}");
        }
    }

    private void CancelAllBodyTimers()
    {
        foreach (var kv in _bodyTimers)
        {
            kv.Value.Cancel();
        }

        _bodyTimers.Clear();
        _log.Debug($"[ContactTimerCleared] victim={FormatNodeDebug(_entity as Node)}");
    }

    private bool IsHostile(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return false;

        var bodyTeam = entity.Data.Get<Team>(DataKey.Team);
        return bodyTeam != Team.Neutral && bodyTeam != _team;
    }

    private float GetContactDamage(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return 0f;
        return entity.Data.Get<float>(DataKey.FinalAttack);
    }

    private float GetAttackInterval(Node2D attacker, IEntity? attackerEntity)
    {
        var entity = attackerEntity ?? attacker as IEntity;
        if (entity == null) return 1.0f;

        var interval = entity.Data.Get<float>(DataKey.AttackInterval);
        return Mathf.Max(interval, 0.1f);
    }

    private static string FormatEntityDebug(IEntity? entity)
    {
        return entity is Node node ? FormatNodeDebug(node) : "<none>";
    }

    private static string FormatNodeDebug(Node? node)
    {
        if (node == null || !IsInstanceValid(node)) return "<invalid>";

        var name = node.Name.ToString();
        var type = node.GetType().Name;
        var instanceId = node.GetInstanceId();

        if (node is Node2D node2D)
            return $"{name}[{type}#{instanceId}] pos={node2D.GlobalPosition}";

        return $"{name}[{type}#{instanceId}]";
    }

    private static string FormatDistance(Node? source, Node? target)
    {
        if (source is not Node2D sourceNode2D || target is not Node2D targetNode2D) return "n/a";
        if (!IsInstanceValid(sourceNode2D) || !IsInstanceValid(targetNode2D)) return "n/a";
        return sourceNode2D.GlobalPosition.DistanceTo(targetNode2D.GlobalPosition).ToString("F2");
    }
}
