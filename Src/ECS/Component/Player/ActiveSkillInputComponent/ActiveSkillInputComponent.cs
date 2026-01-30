using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 主动技能输入组件 (Active Skill Input Component)
/// <para>职责：作为玩家实体的输入中转站，将原生的手柄/键盘输入转换为技能系统的施法请求。</para>
/// <para>功能：</para>
/// <list type="bullet">
///   <item>监听 LB/RB 切换当前选中的主动技能。</item>
///   <item>监听 X 键（或对应映射）尝试释放当前选中的主动技能。</item>
///   <item>智能目标解析：根据技能配置（单位/点/混合）自动完成索敌或方向预测。</item>
/// </list>
/// </summary>
public partial class ActiveSkillInputComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(ActiveSkillInputComponent));

    /// <summary>
    /// _entity是PlayerEntity，因为挂载到PlayerPreset.tscn上
    /// </summary>
    private IEntity? _entity;

    /// <summary> 所属实体的数据集引用 </summary>
    private Data? _data;

    // ================= IComponent 生命周期 =================

    /// <summary>
    /// 组件注册时的初始化逻辑
    /// </summary>
    /// <param name="entity">挂载该组件的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            _entity = iEntity;
            _data = iEntity.Data;
            _log.Info($"主动技能输入组件已注册到实体: {entity.Name}");
        }
    }

    /// <summary>
    /// 组件注销时的清理逻辑
    /// </summary>
    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }

    /// <summary>
    /// 对象池复用时的重置逻辑
    /// </summary>
    public void OnComponentReset() { }

    // ================= Godot 生命周期 =================

    /// <summary>
    /// 每帧轮询输入状态
    /// </summary>
    public override void _Process(double delta)
    {
        // 确保实体和数据有效，防止非法访问
        if (_entity == null || _data == null) return;

        HandleActiveAbilityInput();
    }

    // ================= 核心输入处理逻辑 =================

    /// <summary>
    /// 统一入口：处理切换和释放输入。
    /// 分离输入识别与具体逻辑执行，便于未来扩展组合键或输入映射。
    /// </summary>
    private void HandleActiveAbilityInput()
    {
        // 1. 处理技能切换逻辑 (LB/RB) - 改变当前选中的技能索引
        if (InputManager.IsLeftBumper()) CycleActiveAbility(-1);
        if (InputManager.IsRightBumper()) CycleActiveAbility(1);

        // 2. 处理技能释放逻辑 (X 键) - 触发目标解析并请求施法
        if (InputManager.IsX()) TryUseCurrentActiveAbility();
    }

    /// <summary>
    /// 循环切换当前选中的主动技能。
    /// 使用索引管理，确保在多个主动技能间顺滑切换并循环。
    /// </summary>
    /// <param name="direction">-1 表示切换到上一个，1 表示切换到下一个</param>
    private void CycleActiveAbility(int direction)
    {
        var activeAbilities = GetActiveAbilities();
        if (activeAbilities.Count <= 1) return; // 单个或无技能时不执行切换

        // 从实体数据缓存获取当前索引
        int currentIndex = _data!.Get<int>(DataKey.CurrentActiveAbilityIndex);

        // 计算新索引并实现首尾循环 (使用 Mathf.PosMod 确保在负数情况下也能正确取模)
        int newIndex = Mathf.PosMod(currentIndex + direction, activeAbilities.Count);
        _data.Set(DataKey.CurrentActiveAbilityIndex, newIndex);

        var selectedAbility = activeAbilities[newIndex];
        var abilityName = selectedAbility.Data.Get<string>(DataKey.Name);
        _log.Debug($"切换主动技能: {abilityName} (索引: {newIndex})");

        // 发射 UI 事件，通知技能栏高亮切换，注意_entity是PlayerEntity
        _entity!.Events.Emit(
            GameEventType.UI.ActiveSkillSelected,
            new GameEventType.UI.ActiveSkillSelectedEventData(newIndex, abilityName)
        );
    }

    /// <summary>
    /// 执行当前选中的主动技能。
    /// 流程：获取技能 -> 验证释放条件 -> 判断目标类型:
    ///   - Entity类型: 直接触发（自动选目标）
    ///   - Point类型: 进入瞄准模式，等待玩家确认位置
    /// </summary>
    private void TryUseCurrentActiveAbility()
    {
        // 如果正在瞄准中，忽略新的技能请求
        if (TargetingManager.IsTargeting) return;

        var activeAbilities = GetActiveAbilities();
        if (activeAbilities.Count == 0) return;

        // 获取当前索引并从列表提取对应技能实体
        int currentIndex = _data!.Get<int>(DataKey.CurrentActiveAbilityIndex);

        // 防御性检查：防止动态删除技能导致索引越界
        if (currentIndex < 0 || currentIndex >= activeAbilities.Count)
        {
            currentIndex = 0;
            _data.Set(DataKey.CurrentActiveAbilityIndex, 0);
        }

        var ability = activeAbilities[currentIndex];
        var abilityName = ability.Data.Get<string>(DataKey.Name);

        // 1. 先检查技能是否可用（冷却、充能等）
        if (!AbilitySystem.CanUseAbility(ability))
        {
            _log.Debug($"技能不可用: {abilityName}");
            return;
        }

        // 2. 获取目标选择类型
        var targetSelection = (AbilityTargetSelection)ability.Data.Get<int>(DataKey.AbilityTargetSelection);
        _log.Debug($"触发技能: {abilityName}, 目标选择模式: {targetSelection} (int: {(int)targetSelection})");

        // 3. 构建施法上下文
        var context = new CastContext();

        if (_entity is IEntity caster)
        {
            // 4. 根据目标类型决定流程
            if (targetSelection == AbilityTargetSelection.Point)
            {
                // Point类型技能 -> 进入瞄准模式
                var range = ability.Data.Get<float>(DataKey.AbilityRange);
                // 发送开始瞄准事件，由 TargetingManager 处理后续流程
                GlobalEventBus.Global.Emit(
                    GameEventType.Targeting.StartTargeting,
                    new GameEventType.Targeting.StartTargetingEventData(
                        Caster: caster,
                        Ability: ability,
                        Context: context,
                        Range: range
                    )
                );

                _log.Debug($"技能 {abilityName} 进入瞄准模式，射程: {range}");
            }
            else if (targetSelection == AbilityTargetSelection.Entity || targetSelection == AbilityTargetSelection.None)
            {
                // Entity类型技能 -> 直接触发（自动选目标）
                bool success = AbilitySystem.TryTriggerAbility(caster, abilityName, context);

                if (!success)
                {
                    _log.Debug($"技能触发请求被拒绝: {abilityName}");
                }
            }
        }
    }

    /// <summary>
    /// 筛选当前实体拥有的所有支持“手动触发”模式的主动技能。
    /// 过滤规则：
    /// 1. TriggerMode 必须包含 Manual
    /// 2. AbilityType 不能是 Passive (虽然通常 Manual 不会是 Passive，但防错)
    /// </summary>
    /// <returns>符合条件的主动技能实体列表</returns>
    private List<AbilityEntity> GetActiveAbilities()
    {
        if (_entity == null) return new List<AbilityEntity>();

        // 遍历实体拥有的所有技能集
        return EntityManager.GetAbilities(_entity)
            .Where(a =>
            {
                // 获取技能触发模式
                var mode = (AbilityTriggerMode)a.Data.Get<int>(DataKey.AbilityTriggerMode);
                // 获取技能类型
                var type = (AbilityType)a.Data.Get<int>(DataKey.AbilityType);

                // 必须同时满足：非被动技能 且 包含手动触发标记
                return type != AbilityType.Passive && mode.HasFlag(AbilityTriggerMode.Manual);
            })
            .ToList();
    }
}
