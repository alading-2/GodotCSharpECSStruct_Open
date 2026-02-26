using Godot;

/// <summary>
/// 攻击状态枚举
/// <para>Idle: 空闲，可接受攻击请求</para>
/// <para>WindUp: 前摇（播放攻击动画，等待伤害判定时机）</para>
/// <para>Recovery: 后摇（伤害已判定，动画后半段，可被打断）</para>
/// </summary>
public enum AttackState
{
    /// <summary>空闲状态</summary>
    Idle,
    /// <summary>前摇阶段（发力/蓄力）</summary>
    WindUp,
    /// <summary>后摇阶段（收招/硬直）</summary>
    Recovery
}

/// <summary>
/// 攻击组件 - 状态机 + 双 Timer 管理单位的攻击行为
/// <para>
/// 核心设计：
/// - 状态机驱动（Idle → WindUp → Recovery → Idle）
/// - 阶段计时器（Phase Timer）：推进状态转换，控制前摇时长和后摇时长
/// - 校验计时器（Validation Timer）：每 0.2s 验证目标/自身有效性，确证攻击意图的持续合法性
/// - 无 _Process：由于攻击是离散事件，全 Timer 驱动比每帧轮询更具性能优势且逻辑更清晰
/// </para>
/// <para>
/// 通用框架兼容：
/// - WindUpTime=0, RecoveryTime=0 → 即时模式（适用于 Brotato/吸血鬼幸存者类游戏的自动攻击）
/// - WindUpTime>0, RecoveryTime>0 → 完整前后摇（适用于 ACT/RTS 类游戏，增强打击感与博弈深度）
/// </para>
/// <para>
/// 前后摇与攻击间隔(CD)的核心解惑：
/// - 攻击间隔 (AttackInterval) 由【外部控制】，决定“多久尝试发一次攻击请求”。
/// - 前后摇 (WindUp + Recovery) 由【本组件控制】，决定“本次攻击动作占用单位多久的时间”。
/// - 正常情况 (前后摇时长短于攻击间隔)：打完一套动作回到 Idle，发呆等下一次 CD。
/// - 攻速溢出 (前后摇时长超出攻击间隔)：CD 已经转好但由于本次动作尚未做完（非 Idle），新请求会被直接丢弃。实际攻速被强制拉长至前后摇总时长。
/// </para>
/// </summary>
public partial class AttackComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(AttackComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;
    private CharacterBody2D? _body;

    // ================= 运行时状态 =================

    /// <summary>当前内部攻击状态</summary>
    private AttackState _state = AttackState.Idle;

    /// <summary>当前正在攻击的目标引用</summary>
    private Node2D? _currentTarget;

    /// <summary>阶段推进计时器：处理前摇->命中、后摇->空闲的跳转</summary>
    private GameTimer? _phaseTimer;

    /// <summary>校验计时器：攻击期间定期检查目标是否跑掉或死亡</summary>
    private GameTimer? _validationTimer;

    /// <summary>校验计时器间隔（秒）- 兼顾性能与响应速度。0.2s 是人类反应级别的两倍，足够灵敏且开销极低</summary>
    private const float ValidationInterval = 0.2f;

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册回调：初始化依赖并订阅攻击事件
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;

        // 缓存物理车体，用于范围校验（比每次取组件快）
        if (entity is CharacterBody2D body)
            _body = body;

        // // 监听来自 AI 节点或玩家输入的攻击请求
        // _entity.Events.On<GameEventType.Attack.RequestedEventData>(
        //     GameEventType.Attack.Requested, OnAttackRequested);

        // // 监听来自外部（如眩晕 Buff、强制位移等）的中断请求
        // _entity.Events.On<GameEventType.Attack.CancelRequestedEventData>(
        //     GameEventType.Attack.CancelRequested, OnCancelRequested);
    }

    /// <summary>
    /// 组件注销回调：确保清理所有运行中的计时器，防止闭包引起的内存泄漏或逻辑幽灵
    /// </summary>
    public void OnComponentUnregistered()
    {
        // 悄悄清理所有计时任务，不发出取消事件（因为组件已不在生命周期内）
        CleanupTimers();
        _state = AttackState.Idle;
        _currentTarget = null;

        _entity = null;
        _data = null;
        _body = null;
    }

    // ================= 事件处理 =================

    /// <summary>
    /// 处理具体的攻击请求逻辑
    /// </summary>
    private void OnAttackRequested(GameEventType.Attack.RequestedEventData evt)
    {
        if (_data == null || _entity == null) return;

        // 状态锁：只有处于 Idle 状态时才能发起新攻击。
        // 这同时也隐含地遵循了 AttackInterval（由外部 AI 节点根据 CD 来决定何时发 Requested 事件）
        if (_state != AttackState.Idle) return;

        var target = evt.Target;

        // === 攻击前置校验（例如：已经死了、被晕了、目标不在范围内） ===
        if (!ValidateCanAttack(target)) return;

        _currentTarget = target;

        // 从实体数据中心读取攻击时长配置。
        // 若为 0，则该框架表现为即时判定的轻量模式。
        float windUpTime = _data.Get<float>(DataKey.AttackWindUpTime);

        // 发出 Started 通知，告知外部（如 UI 进度条、特效、AI 记录）攻击已正式进入准备阶段
        _entity.Events.Emit(GameEventType.Attack.Started,
            new GameEventType.Attack.StartedEventData(target));

        // 统一走 WindUp 流程（WindUpTime=0 时 Timer 会在下一帧立即触发 OnWindUpComplete）
        // 这保证动画播放、校验计时器等逻辑只维护一处
        EnterWindUp(windUpTime);
    }

    /// <summary>
    /// 响应外部对攻击的中断请求（如 AI 决定临时撤退，或者单位进入控制状态）
    /// </summary>
    private void OnCancelRequested(GameEventType.Attack.CancelRequestedEventData evt)
    {
        if (_state == AttackState.Idle) return;
        CancelAttack(AttackCancelReason.ExternalCancel);
    }

    // ================= 状态转换核心（State Machine Logic） =================

    /// <summary>
    /// 转换为 WindUp 状态：播放前摇动画，并设置阶段切换计划
    /// </summary>
    private void EnterWindUp(float windUpTime)
    {
        _state = AttackState.WindUp;
        _data?.Set(DataKey.AttackState, AttackState.WindUp);
        _log.Trace($"状态转换: Idle → WindUp (预计耗时: {windUpTime:F2}s)");

        // 触发动画播放：攻击间隔越大，对应的动画通常播放得越慢。
        float attackInterval = _data.Get<float>(DataKey.AttackInterval);

        // 从可用动画列表中随机选择攻击动画（支持 attack1, attack2 等多个攻击动画）
        string attackAnim = SelectRandomAttackAnimation();

        _entity?.Events.Emit(GameEventType.Unit.PlayAnimationRequested,
            new GameEventType.Unit.PlayAnimationRequestedEventData(
                attackAnim, ForceRestart: true, Duration: attackInterval));

        // 设置 Delay 定时器：当 WindUp 时间结束时，触发动作完成（命中）回调
        _phaseTimer = TimerManager.Instance.Delay(windUpTime)
            .OnComplete(OnWindUpComplete);

        // 设置 Loop 定时器：在准备期间，每隔一小段时间检查一次目标是否还在，
        // 避免"蓄力很久对方跑了，但我依然原地砍出伤害"的问题。
        _validationTimer = TimerManager.Instance.Loop(ValidationInterval)
            .OnLoop(ValidateAttackContext);
    }

    /// <summary>
    /// 前摇结束的回调（动作判定的时刻发生点）
    /// </summary>
    private void OnWindUpComplete()
    {
        // 多重防御性检查：因为计时器是异步的，触发时可能已发生大变故
        if (_state != AttackState.WindUp || _data == null || _entity == null) return;

        // 最终致命的一刻进行最后一次校验（例如目标刚好在这一毫秒死掉）
        if (!ValidateTargetForStrike())
        {
            return; // 内部已异步进入 Cancel 流程
        }

        ExecuteStrikeAndProceed();
    }

    /// <summary>
    /// 执行最终伤害，并判断是否需要进入后摇缓解阶段
    /// </summary>
    private void ExecuteStrikeAndProceed()
    {
        if (_data == null || _entity == null) return;

        // 判定伤害
        bool didHit = ExecuteDamage(_currentTarget);

        // 检查是否有后摇配置（例如挥大剑会有很大的硬直）
        float recoveryTime = _data.Get<float>(DataKey.AttackRecoveryTime);

        if (recoveryTime > 0f)
        {
            // === 进后摇：此时伤害已经产生，但单位仍处于忙碌状态不可发起新请求 ===
            EnterRecovery(recoveryTime);
        }
        else
        {
            // === 无后摇配置：直接结束攻击回到待机 ===
            FinishAttack(didHit);
        }
    }

    /// <summary>
    /// 转换为 Recovery 状态：维持校验计时器防止自身状态变异，直到收招结束
    /// </summary>
    private void EnterRecovery(float recoveryTime)
    {
        _state = AttackState.Recovery;
        _data?.Set(DataKey.AttackState, AttackState.Recovery);
        _log.Trace($"状态转换: WindUp → Recovery (收招硬直: {recoveryTime:F2}s)");

        // 取消前摇计时任务（虽然已经到期了，但 Cleanup 时逻辑更稳），设置新的收招计时任务
        _phaseTimer?.Cancel();
        _phaseTimer = TimerManager.Instance.Delay(recoveryTime)
            .OnComplete(OnRecoveryComplete);

        // 如果是即时模式进入的（没有前摇周期），我们需要补启动校验循环
        if (_validationTimer == null)
        {
            _validationTimer = TimerManager.Instance.Loop(ValidationInterval)
                .OnLoop(ValidateAttackContext);
        }
    }

    /// <summary>
    /// 收招结束回调
    /// </summary>
    private void OnRecoveryComplete()
    {
        if (_state != AttackState.Recovery) return;
        FinishAttack(true); // 顺利走完流程
    }

    /// <summary>
    /// 流程完结：重置状态机，发出 Finished 事件
    /// </summary>
    private void FinishAttack(bool didHit)
    {
        if (_data == null || _entity == null) return;

        // 计算剩余冷却时间 = AttackInterval - WindUp - Recovery
        // 目的：让 AttackState 在整个攻击间隔内保持非 Idle，
        // 使得 AI 的 IsAttackReady（检查 AttackState==Idle）自然生效，无需外部 CD 计时器
        float attackInterval = _data.Get<float>(DataKey.AttackInterval);
        float windUpTime = _data.Get<float>(DataKey.AttackWindUpTime);
        float recoveryTime = _data.Get<float>(DataKey.AttackRecoveryTime);
        float remainingCooldown = attackInterval - windUpTime - recoveryTime;

        if (remainingCooldown > 0.001f)
        {
            // 进入追加冷却阶段（复用 Recovery 状态，语义等同于「出完刀还没到下次攻击时机」）
            _state = AttackState.Recovery;
            _data.Set(DataKey.AttackState, AttackState.Recovery);

            _phaseTimer?.Cancel();
            _phaseTimer = TimerManager.Instance.Delay(remainingCooldown)
                .OnComplete(() => CompleteFinishAttack(didHit));
            return;
        }

        CompleteFinishAttack(didHit);
    }

    /// <summary>
    /// 真正执行状态回 Idle 并发出 Finished 事件（被 FinishAttack 调用，可能有延迟）
    /// </summary>
    private void CompleteFinishAttack(bool didHit)
    {
        var target = _currentTarget;
        CleanupTimers();
        _state = AttackState.Idle;
        _data?.Set(DataKey.AttackState, AttackState.Idle);
        _currentTarget = null;

        _log.Trace($"攻击完结: → Idle (已上报 Finished 事件)");

        _entity?.Events.Emit(GameEventType.Attack.Finished,
            new GameEventType.Attack.FinishedEventData(target, didHit));
    }

    /// <summary>
    /// 流程夭折：重置状态机，并发出中断通知（如停止动画、告知 AI/UI 任务失败）
    /// </summary>
    private void CancelAttack(AttackCancelReason reason)
    {
        if (_state == AttackState.Idle) return;

        var oldState = _state;
        CleanupTimers();
        _state = AttackState.Idle;
        _data?.Set(DataKey.AttackState, AttackState.Idle);
        _currentTarget = null;

        _log.Trace($"攻击异常中断: {oldState} → Idle (原因: {reason})");

        // 如果是在动作中被打断，通常需要立刻将模型强制扯回 Idle 动画
        _entity?.Events.Emit(GameEventType.Unit.StopAnimationRequested,
            new GameEventType.Unit.StopAnimationRequestedEventData());

        // 通知业务层该次攻击尝试已非法中断
        _entity?.Events.Emit(GameEventType.Attack.Cancelled,
            new GameEventType.Attack.CancelledEventData(reason));
    }

    // ================= 决策逻辑 (Verification Logic) =================

    /// <summary>
    /// 检查首帧攻击条件：包含自身合规性、目标合规性、距离合规性
    /// </summary>
    private bool ValidateCanAttack(Node2D? target)
    {
        if (_data == null) return false;

        // 自身合规
        if (_data.Get<bool>(DataKey.IsDead)) return false;
        if (_data.Get<bool>(DataKey.IsStunned)) return false;

        // 目标合规
        if (target == null || !GodotObject.IsInstanceValid(target)) return false;

        if (target is IEntity targetEntity)
        {
            if (targetEntity.Data.Get<bool>(DataKey.IsDead)) return false;
        }

        // 物理距离合规
        if (_body != null)
        {
            float attackRange = _data.Get<float>(DataKey.AttackRange);
            float distance = _body.GlobalPosition.DistanceTo(target.GlobalPosition);
            if (distance > attackRange) return false;
        }

        return true;
    }

    /// <summary>
    /// 攻击运行中（WindUp或Recovery时）的周期性合法性检查。相当于 _Process 的低频替代品。
    /// </summary>
    private void ValidateAttackContext()
    {
        if (_data == null || _entity == null || _state == AttackState.Idle)
        {
            CleanupTimers();
            return;
        }

        // 1. 检查自身状态突变
        if (_data.Get<bool>(DataKey.IsDead))
        {
            CancelAttack(AttackCancelReason.SelfDead);
            return;
        }

        if (_data.Get<bool>(DataKey.IsStunned))
        {
            CancelAttack(AttackCancelReason.SelfDisabled);
            return;
        }

        // 2. 检查目标对象合规性
        if (_currentTarget == null || !GodotObject.IsInstanceValid(_currentTarget))
        {
            CancelAttack(AttackCancelReason.TargetInvalid);
            return;
        }

        // 3. 检查目标生命状态
        if (_currentTarget is IEntity targetEntity && targetEntity.Data.Get<bool>(DataKey.IsDead))
        {
            CancelAttack(AttackCancelReason.TargetDead);
            return;
        }

        // 4. 只有在前摇蓄力期才强制要求距离。
        // （设计点：后摇期间由于伤害已经爆发，通常允许对方跑开而不需要打断收招动画）
        if (_state == AttackState.WindUp && _body != null)
        {
            float attackRange = _data.Get<float>(DataKey.AttackRange);
            float distance = _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);

            // 使用攻击距离的 1.5 倍作为"容差中断位点"。
            // 避免微小的浮点数误差或极其轻微的位移导致攻击频繁因"差了一像素"而取消。
            if (distance > attackRange * 1.5f)
            {
                CancelAttack(AttackCancelReason.TargetOutOfRange);
                return;
            }
        }
    }

    /// <summary>
    /// 在命中判定的那一瞬间做的终极安全检查。
    /// </summary>
    private bool ValidateTargetForStrike()
    {
        if (_currentTarget == null || !GodotObject.IsInstanceValid(_currentTarget))
        {
            CancelAttack(AttackCancelReason.TargetInvalid);
            return false;
        }

        if (_currentTarget is IEntity targetEntity && targetEntity.Data.Get<bool>(DataKey.IsDead))
        {
            CancelAttack(AttackCancelReason.TargetDead);
            return false;
        }

        if (_data != null && _data.Get<bool>(DataKey.IsDead))
        {
            CancelAttack(AttackCancelReason.SelfDead);
            return false;
        }

        return true;
    }

    // ================= 动作执行 (Action Execution) =================

    /// <summary>
    /// 真正的伤害发生点。通过向 DamageService 获取接口调用来实现解耦。
    /// </summary>
    private bool ExecuteDamage(Node2D? target)
    {
        if (target == null || !GodotObject.IsInstanceValid(target)) return false;

        // 最后一次确保目标没死（双活规则）
        if (target is IEntity targetEntity && targetEntity.Data.Get<bool>(DataKey.IsDead))
            return false;

        if (target is IUnit victimUnit && _body != null)
        {
            float finalAttack = _data.Get<float>(DataKey.FinalAttack);

            // 构造伤害载体并掷入 Pipeline 处理管道
            var damageInfo = new DamageInfo
            {
                Attacker = _body,
                Victim = victimUnit,
                Damage = finalAttack,
                Type = DamageType.Physical
            };

            DamageService.Instance?.Process(damageInfo);
            return true;
        }

        return false;
    }

    // ================= 计时器管理 (Timer Management) =================

    /// <summary>
    /// 安全地停止并在主循环中销毁所有相关的计时任务。
    /// </summary>
    private void CleanupTimers()
    {
        _phaseTimer?.Cancel();
        _phaseTimer = null;
        _validationTimer?.Cancel();
        _validationTimer = null;
    }

    // ================= 动画选择 (Animation Selection) =================

    /// <summary>
    /// 从可用动画列表中随机选择一个攻击动画。
    /// 如果有多个攻击动画（如 attack1, attack2），会随机选择一个。
    /// 如果没有可用动画列表或没有攻击动画，返回默认的 Attack1。
    /// </summary>
    private string SelectRandomAttackAnimation()
    {
        if (_data == null) return Anim.Attack1;

        // 从 Data 中获取可用动画列表
        var availableAnims = _data.Get<System.Collections.Generic.List<string>>(DataKey.AvailableAnimations);
        if (availableAnims == null || availableAnims.Count == 0)
        {
            return Anim.Attack1;
        }

        // 筛选出所有以 "attack" 开头的动画
        var attackAnims = new System.Collections.Generic.List<string>();
        foreach (var anim in availableAnims)
        {
            if (anim.StartsWith("attack"))
            {
                attackAnims.Add(anim);
            }
        }

        // 如果没有找到攻击动画，返回默认值
        if (attackAnims.Count == 0)
        {
            return Anim.Attack1;
        }

        // 如果只有一个攻击动画，直接返回
        if (attackAnims.Count == 1)
        {
            return attackAnims[0];
        }

        // 随机选择一个攻击动画
        int randomIndex = (int)GD.RandRange(0, attackAnims.Count - 1);
        return attackAnims[randomIndex];
    }
}
