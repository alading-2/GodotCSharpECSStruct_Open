using System;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;


/// <summary>
/// 全局恢复系统 (RecoverySystem)
/// 
/// 该系统遵循 ECS (Entity-Component-System) 架构中的 System 职责，负责统一驱动所有具有恢复能力的实体。
/// 
/// 【核心设计思想】：
/// 1. 集中驱动：不再让每个单位都持有一个 Timer，而是由全局系统使用单一 Timer 批量轮询，大幅降低 CPU 开销。
/// 2. 响应式注册：只有当实体的恢复属性（HP/Mana Regen）大于 0 时，才会进入处理列表；属性归零时自动移除。
/// 3. 安全性：在遍历过程中通过 `IsInstanceValid` 检测，防止处理已销毁的 Godot 对象。
/// 
/// 【协作组件】：
/// - RecoveryComponent: 负责在实体生命周期内向本系统发起注册/注销请求。
/// - HealthComponent: 本系统通过调用该组件的接口来实施真正的数值变更。
/// </summary>
public partial class RecoverySystem : Node
{
    /// <summary>
    /// 模块初始化器。
    /// 在程序启动时自动执行，将 RecoverySystem 注册到 AutoLoad 管理器中。
    /// 设置优先级为 System 级别，并声明对 TimerManager 的依赖。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(RecoverySystem),
            Scene = ResourceManagement.Load<PackedScene>(nameof(RecoverySystem), ResourceCategory.System),
            Priority = AutoLoad.Priority.System,
        });
    }

    /// <summary>全局单例访问点</summary>
    public static RecoverySystem Instance = null!;

    /// <summary>日志工具实例，用于输出该系统的运行信息</summary>
    private static readonly Log _log = new("RecoverySystem");

    // ================= 核心数据区域 =================

    /// <summary>
    /// 当前受恢复系统监管的实体集合。
    /// 使用 HashSet 实现，以确保：
    /// 1. 每个实体只会被注册一次（去重）。
    /// 2. 检查、注销操作的效率为 O(1)。
    /// </summary>
    private readonly HashSet<IEntity> _registeredEntities = new();

    /// <summary>
    /// 底层驱动计时器。
    /// 依托于 TimerManager 运行，循环触发行恢复处理函数。
    /// </summary>
    private GameTimer? _recoveryTimer;

    /// <summary>
    /// 恢复计算的时间步长。
    /// 默认为 1.0 秒，即每秒执行一次恢复逻辑。
    /// </summary>
    public float RecoveryInterval { get; set; } = 1.0f;

    // ================= Godot 生命周期回调 =================

    public override void _EnterTree()
    {
        // 建立单例引用
        Instance = this;
    }

    public override void _Ready()
    {
        // 初始化并启动恢复计时器
        StartRecoveryTimer();
        _log.Info($"RecoverySystem 已启动，恢复间隔: {RecoveryInterval}s");
    }

    public override void _ExitTree()
    {
        // 清理资，防止内存泄漏或脏数据
        StopRecoveryTimer();
        _registeredEntities.Clear();
        Instance = null!;
    }

    // ================= 注册管理逻辑 =================

    /// <summary>
    /// 将一个实体纳入恢复系统的计算队列。
    /// 通常由 RecoveryComponent 在实体生成或属性提升时调用。
    /// </summary>
    /// <param name="entity">目标实体接口</param>
    public void Register(IEntity entity)
    {
        if (entity == null)
        {
            _log.Warn("尝试注册 null 实体");
            return;
        }

        // 避免重复注册
        if (_registeredEntities.Contains(entity))
        {
            _log.Debug($"实体 {(entity as Node)?.Name} 已在注册列表中，跳过重复操作");
            return;
        }

        _registeredEntities.Add(entity);
        _log.Debug($"注册实体: {(entity as Node)?.Name}，当前待处理总数: {_registeredEntities.Count}");
    }

    /// <summary>
    /// 从恢复系统的计算队列中移除指定实体。
    /// 通常由 RecoveryComponent 在实体死亡、移除或属性降为 0 时调用。
    /// </summary>
    /// <param name="entity">目标实体接口</param>
    public void Unregister(IEntity entity)
    {
        if (entity == null) return;

        if (_registeredEntities.Remove(entity))
        {
            _log.Debug($"注销实体: {(entity as Node)?.Name}，当前剩余总数: {_registeredEntities.Count}");
        }
    }

    /// <summary>
    /// 外部接口：查询某实体当前是否正在接受恢复逻辑驱动。
    /// </summary>
    /// <param name="entity">目标实体</param>
    /// <returns>已注册则返回 true</returns>
    public bool IsRegistered(IEntity entity)
    {
        return _registeredEntities.Contains(entity);
    }

    // ================= 计时器核心管理 =================

    /// <summary>
    /// 配置并启动循环计时器。
    /// </summary>
    private void StartRecoveryTimer()
    {
        if (RecoveryInterval <= 0)
        {
            _log.Error("恢复间隔配置非法，必须大于 0");
            return;
        }

        // 使用 TimerManager 启动一个循环调用的计时器，回调指向 ProcessRecovery
        _recoveryTimer = TimerManager.Instance?.Loop(RecoveryInterval)
            .OnLoop(ProcessRecovery);

        _log.Debug($"恢复计时器已启动，配置间隔: {RecoveryInterval}s");
    }

    /// <summary>
    /// 停止并重置计时器。
    /// </summary>
    private void StopRecoveryTimer()
    {
        _recoveryTimer?.Cancel();
        _recoveryTimer = null;
    }

    // ================= 批量恢复执行逻辑 =================

    /// <summary>
    /// 定时器回调函数：负责对所有注册实体进行统一的恢复处理。
    /// </summary>
    private void ProcessRecovery()
    {
        if (_registeredEntities.Count == 0) return;

        // 【关键点】：创建集合副本进行遍历。
        // 因为在 ProcessEntityRecovery 过程中，可能会触发 Unregister 操作修改 _registeredEntities 集合，
        // 直接遍历原集合会导致 "Collection was modified" 异常。
        var entitiesToProcess = new List<IEntity>(_registeredEntities);

        foreach (var entity in entitiesToProcess)
        {
            // 安全预检：Godot 节点可能在计时器间隔期间被 QueueFree。
            // 转换为 Node 并检查实例有效性。
            if (!IsInstanceValid(entity as Node))
            {
                _registeredEntities.Remove(entity);
                continue;
            }

            // 处理具体的恢复计算
            ProcessEntityRecovery(entity);
        }
    }

    /// <summary>
    /// 对单一实体执行具体的恢复动作。
    /// 包括属性读取、合法性检查（是否死亡、是否被禁疗）以及最终应用。
    /// </summary>
    /// <param name="entity">待处理实体</param>
    private void ProcessEntityRecovery(IEntity entity)
    {
        var data = entity.Data;

        // 1. 获取当前最终恢复属性（已计算装备、天赋等加成后的最终值）
        float hpRegen = data.Get<float>(DataKey.FinalHpRegen);
        float manaRegen = data.Get<float>(DataKey.FinalManaRegen);

        // 2. 智能性能优化：如果该单位目前没有任何恢复需求，自动从本系统中注销。
        // 这将减少下一帧遍历带来的开销。
        if (hpRegen <= 0 && manaRegen <= 0)
        {
            Unregister(entity);
            _log.Debug($"实体 {(entity as Node)?.Name} 当前恢复属性均为 0，已自动解除监管");
            return;
        }

        // 3. 基本逻辑状态检查：死亡单位停止恢复。
        if (data.Get<bool>(DataKey.IsDead))
        {
            return;
        }

        // 4. [生命恢复逻辑]
        // ✅ 通过事件触发治疗，解耦 RecoverySystem 与 HealthComponent
        if (hpRegen > 0)
        {
            // 检查是否受"禁疗"状态影响
            if (!data.Get<bool>(DataKey.IsDisableHealthRecovery))
            {
                // ✅ 发送治疗请求事件，由 HealthComponent 监听处理
                // IsFullHp 检查在 HealthComponent.ApplyHeal 内部处理
                entity.Events.Emit(GameEventType.Unit.HealRequest,
                    new GameEventType.Unit.HealRequestEventData(hpRegen, HealSource.Regen));
            }
        }

        // 5. [魔法恢复]
        if (manaRegen > 0)
        {
            // 检查魔法禁疗状态
            if (!data.Get<bool>(DataKey.IsDisableManaRecovery))
            {
                float currentMana = data.Get<float>(DataKey.CurrentMana);
                float maxMana = data.Get<float>(DataKey.FinalMana);

                // 仅在未满魔时恢复
                if (currentMana < maxMana)
                {
                    float newMana = currentMana + manaRegen;
                    // 手动处理上限 Clamp (下限已由 DataMeta MinValue 保证，但 Math.Clamp 可更保险)
                    newMana = Math.Clamp(newMana, 0, maxMana);

                    data.Set(DataKey.CurrentMana, newMana);
                }
            }
        }
    }


    // ================= 辅助 & 调试 =================

    /// <summary>
    /// 获取当前正在运行恢复逻辑的实体总数。
    /// 可用于性能监控或 UI 调试显示。
    /// </summary>
    /// <returns>实体的数量</returns>
    public int GetRegisteredCount() => _registeredEntities.Count;
}
