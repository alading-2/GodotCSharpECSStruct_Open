using Godot;

/// <summary>
/// 充能组件 - 管理技能的充能次数。
/// 
/// 核心逻辑：
/// 1. 仅用于主动技能：被动技能没有"使用"的概念，因此不需要充能。
/// 2. 多次充能：支持技能积累多次使用次数 (例如：可以连续释放 2 次冲刺)。
/// 3. 自动恢复：当充能未满时，使用 TimerManager.Loop() 自动恢复。
/// 4. 数据解耦：所有状态（当前充能）都存储在 Entity.Data 中。
/// </summary>
public partial class ChargeComponent : Node, IComponent
{
    private static readonly Log _log = new("ChargeComponent");

    // ================= 标准字段 =================

    /// <summary>实体数据引用</summary>
    private Data? _data;

    /// <summary>所属实体引用</summary>
    private IEntity? _entity;

    /// <summary>充能恢复计时器（由 TimerManager 管理）</summary>
    private GameTimer? _chargeTimer;

    // ================= 属性访问 =================

    private AbilityType AbilityType => _data.Get<AbilityType>(DataKey.AbilityType);
    private string AbilityName => _data.Get<string>(DataKey.Name);
    private int CurrentCharges => _data.Get<int>(DataKey.AbilityCurrentCharges);
    private int MaxCharges => _data.Get<int>(DataKey.AbilityMaxCharges);

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册时的初始化逻辑
    /// </summary>
    /// <param name="entity">所属的 Godot 节点（应实现 IEntity）</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
            _entity = iEntity;

            // 检查是否启用了充能系统
            // 如果未启用，则不初始化充能数据，也不订阅事件
            if (!_data.Get<bool>(DataKey.IsAbilityUsesCharges))
            {
                return;
            }

            // 订阅事件驱动请求
            SubscribeEvents();
        }
    }

    /// <summary>
    /// 组件重置逻辑 (通常用于对象池回收)
    /// </summary>
    public void OnComponentReset()
    {
        if (_data == null) return;

        // 停止恢复计时
        StopChargeRecovery();
    }

    /// <summary>
    /// 组件注销时的清理逻辑
    /// </summary>
    public void OnComponentUnregistered()
    {
        StopChargeRecovery();
        _data = null;
        _entity = null;
    }

    // ================= 事件驱动 =================

    /// <summary>订阅请求事件</summary>
    private void SubscribeEvents()
    {
        if (_entity == null) return;
        // 监听请求检查可用性事件
        _entity.Events.On<GameEventType.Ability.CheckCanUseEventData>(
            GameEventType.Ability.CheckCanUse,
            OnCheckCanUse
        );
        // 监听使用技能消耗充能事件
        _entity.Events.On<GameEventType.Ability.ConsumeChargeEventData>(
            GameEventType.Ability.ConsumeCharge,
            OnRequestConsumeCharge
        );
        // 监听增加充能事件
        _entity.Events.On<GameEventType.Ability.AddChargeEventData>(
            GameEventType.Ability.AddCharge,
            OnRequestAddCharge
        );
    }

    /// <summary>响应可用性检查请求</summary>
    private void OnCheckCanUse(GameEventType.Ability.CheckCanUseEventData eventData)
    {
        // 仅主动技能需要检查充能
        if (AbilityType != AbilityType.Active) return;

        if (!HasCharge())
        {
            eventData.Context.SetFailed("充能不足");
        }
    }

    /// <summary>响应消耗充能请求</summary>
    private void OnRequestConsumeCharge(GameEventType.Ability.ConsumeChargeEventData eventData)
    {
        // 仅主动技能需要消耗充能
        if (AbilityType != AbilityType.Active) return;

        // 调用消耗方法并在 Context 中报告结果
        bool success = ConsumeCharge();
        if (!success)
        {
            eventData.Context.SetFailed("充能不足");
        }
    }

    /// <summary>响应增加充能请求</summary>
    private void OnRequestAddCharge(GameEventType.Ability.AddChargeEventData eventData)
    {
        InternalAddCharges(eventData.Amount);
    }

    public override void _ExitTree()
    {
        StopChargeRecovery();
    }

    // ================= 充能恢复逻辑 =================

    /// <summary>
    /// 启动充能自动恢复计时器
    /// </summary>
    private void StartChargeRecovery()
    {
        if (_data == null) return;

        // 如果已经在恢复中，不重复启动
        if (_chargeTimer != null) return;

        // 计算充能时间间隔 (应用冷却缩减)
        // 充能回复速度某种意义上也是冷却时间，但跟冷却时间是不同的概念
        float baseChargeTime = _data.Get<float>(DataKey.AbilityChargeTime);
        float reduction = _data.Get<float>(DataKey.CooldownReduction);
        float chargeTime = MyMath.CalculateFinalCooldownTime(baseChargeTime, reduction);

        // 如果充能时间间隔<=0，不启动充能恢复计时器
        if (chargeTime <= 0f) return;

        // 使用 TimerManager.Loop() 创建循环计时器
        _chargeTimer = TimerManager.Instance.Loop(chargeTime)
            .WithTag("AbilityCharge")
            .OnLoop(RecoverOneCharge);

        _log.Debug($"启动充能恢复: {AbilityName}, 间隔: {chargeTime:F2}s");
    }

    /// <summary>
    /// 恢复一次充能（由 Timer 回调触发）
    /// </summary>
    private void RecoverOneCharge()
    {
        InternalAddCharges(1);
    }



    /// <summary>
    /// 内部统一的充能增加逻辑（带上限检查和事件发送）
    /// </summary>
    /// <param name="amount">增加数量</param>
    /// <returns>实际增加的数量</returns>
    private int InternalAddCharges(int amount)
    {
        if (_data == null || amount <= 0) return 0;

        // ✅ 统一的上限检查 - 防止充能超过最大值
        int actualAdd = System.Math.Min(amount, MaxCharges - CurrentCharges);
        if (actualAdd <= 0) return 0;

        int newCharges = CurrentCharges + actualAdd;
        _data.Set(DataKey.AbilityCurrentCharges, newCharges);

        _log.Debug($"充能增加: {AbilityName}, +{actualAdd}, 当前: {newCharges}/{MaxCharges}");

        // ✅ 充能已满时停止自动恢复计时器
        if (newCharges >= MaxCharges)
        {
            StopChargeRecovery();
        }

        // ✅ 发送统一的充能恢复事件
        _entity?.Events.Emit(
            GameEventType.Ability.ChargeRestored,
            new GameEventType.Ability.ChargeRestoredEventData(newCharges, MaxCharges)
        );

        return actualAdd;
    }



    // ================= 公共接口 =================

    /// <summary>
    /// 检查是否有可用的充能次数
    /// </summary>
    /// <returns>true 表示至少有 1 次充能</returns>
    public bool HasCharge()
    {
        if (_data == null) return false;
        return CurrentCharges > 0;
    }

    /// <summary>
    /// 消耗一次充能。
    /// 在技能实际激活前调用。
    /// </summary>
    /// <returns>消耗成功返回 true，充能不足返回 false</returns>
    public bool ConsumeCharge()
    {
        if (_data == null) return false;

        if (CurrentCharges <= 0)
        {
            _log.Debug($"技能 {AbilityName} 充能不足");
            return false;
        }

        // 减少一次充能
        int newCharges = CurrentCharges - 1;
        _data.Set(DataKey.AbilityCurrentCharges, newCharges);

        _log.Debug($"消耗充能: {AbilityName}, 剩余: {newCharges}/{MaxCharges}");

        // 从满充能变为非满，启动恢复计时器
        if (newCharges == MaxCharges - 1)
        {
            StartChargeRecovery();
        }

        return true;
    }



    /// <summary>
    /// 获取当前可用的充能次数
    /// </summary>
    public int GetCurrentCharges()
    {
        if (_data == null) return 0;
        return CurrentCharges;
    }

    /// <summary>
    /// 获取最大充能次数配置
    /// </summary>
    public int GetMaxCharges()
    {
        if (_data == null) return 1;
        return MaxCharges;
    }

    /// <summary>
    /// 获取下一次充能恢复的百分比进度 (0~1)
    /// 常用于 UI 显示充能条或冷却转圈。
    /// </summary>
    public float GetChargeProgress()
    {
        if (_data == null) return 1f;

        // 充能已满，进度为 1
        if (CurrentCharges >= MaxCharges) return 1f;

        // 如果计时器存在，返回其进度
        if (_chargeTimer != null)
        {
            return _chargeTimer.Progress;
        }

        return 0f;
    }

    /// <summary>
    /// 立即恢复所有充能次数到最大值。
    /// 常用于关卡开始或特殊增益效果。
    /// </summary>
    public void RestoreAllCharges()
    {
        if (_data == null) return;

        _data.Set(DataKey.AbilityCurrentCharges, MaxCharges);

        // 停止恢复计时
        StopChargeRecovery();

        _log.Debug($"技能充能完全恢复: {AbilityName}");
    }

    /// <summary>
    /// 停止充能恢复计时器
    /// </summary>
    private void StopChargeRecovery()
    {
        if (_chargeTimer != null)
        {
            _chargeTimer.Cancel();
            _chargeTimer = null;
        }
    }
}
