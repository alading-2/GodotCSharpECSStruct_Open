using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// TODO 这个组件应该做完简单UI再做，现在不知道怎么做
/// 主动技能输入组件 (Active Skill Input Component)
/// 职责：通过监听手柄/键盘输入，触发主动技能。
/// 通过监听手柄/键盘输入，触发主动技能（LB/RB 切换、X 释放、右摇杆/左摇杆/默认方向目标解析）。
/// </summary>
public partial class ActiveSkillInputComponent : Node, IComponent
{
    private static readonly Log _log = new("ActiveSkillInput");

    /// <summary> 所属实体引用 </summary>
    private IEntity? _entity;

    /// <summary> 所属实体的数据集引用 </summary>
    private Data? _data;

    // ================= IComponent 生命周期 =================

    /// <summary>
    /// 组件注册时的初始化逻辑
    /// </summary>
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

    public override void _Process(double delta)
    {
        // 确保实体和数据有效
        if (_entity == null || _data == null) return;

        HandleActiveAbilityInput();
    }

    // ================= 核心输入处理逻辑 =================

    /// <summary>
    /// 统一入口：处理切换和释放输入
    /// </summary>
    private void HandleActiveAbilityInput()
    {
        // 1. 处理技能切换逻辑 (LB/RB)
        if (InputManager.IsLeftBumper()) CycleActiveAbility(-1);
        if (InputManager.IsRightBumper()) CycleActiveAbility(1);

        // 2. 处理技能释放逻辑 (X 键)
        if (InputManager.IsX()) TryUseCurrentActiveAbility();
    }

    /// <summary>
    /// 循环切换当前选中的主动技能
    /// </summary>
    /// <param name="direction">-1 表示上一个，1 表示下一个</param>
    private void CycleActiveAbility(int direction)
    {
        var activeAbilities = GetActiveAbilities();
        if (activeAbilities.Count <= 1) return; // 0 或 1 个技能无需切换

        int currentIndex = _data!.Get<int>(DataKey.CurrentActiveAbilityIndex);

        // 计算新索引并实现首尾循环
        int newIndex = (currentIndex + direction + activeAbilities.Count) % activeAbilities.Count;
        _data.Set(DataKey.CurrentActiveAbilityIndex, newIndex);

        var selectedAbility = activeAbilities[newIndex];
        var abilityName = selectedAbility.Data.Get<string>(DataKey.Name);
        _log.Debug($"切换主动技能: {abilityName} (索引: {newIndex})");

        // TODO: 此处可发射事件用于通知 UI 更新（如：GameEventType.UI.ActiveSkillChanged）
    }

    /// <summary>
    /// 执行当前选中的主动技能
    /// </summary>
    private void TryUseCurrentActiveAbility()
    {
        var activeAbilities = GetActiveAbilities();
        if (activeAbilities.Count == 0) return;

        // 获取当前索引并进行越界检查保护
        int currentIndex = _data!.Get<int>(DataKey.CurrentActiveAbilityIndex);
        if (currentIndex < 0 || currentIndex >= activeAbilities.Count)
        {
            currentIndex = 0;
            _data.Set(DataKey.CurrentActiveAbilityIndex, 0);
        }

        var ability = activeAbilities[currentIndex];
        var abilityName = ability.Data.Get<string>(DataKey.Name);

        // 1. 构建施法上下文（在此处完成目标解析，AbilitySystem 只负责验证和执行）
        var context = BuildCastContext(ability);

        // 2. 直接调用 AbilitySystem 触发技能流程 (Validation -> CD -> Cost -> Execute)
        if (_entity is IEntity caster)
        {
            // 使用带上下文的重载，确保目标信息已填充
            bool success = AbilitySystem.TryTriggerAbility(caster, abilityName, context);

            if (!success)
            {
                // 触发失败逻辑可以在此处理，例如播放冷却中的音效或显示提示
                // _log.Debug($"主动技能 '{abilityName}' 释放请求被拦截（可能在 CD 中或条件不足）");
            }
        }
    }

    /// <summary>
    /// 筛选当前实体拥有的所有支持“手动触发”模式的主动技能
    /// </summary>
    private List<AbilityEntity> GetActiveAbilities()
    {
        if (_entity == null) return new List<AbilityEntity>();

        // 遍历实体拥有的所有技能实体
        return EntityManager.GetAbilities(_entity)
            .Where(a =>
            {
                var mode = (AbilityTriggerMode)a.Data.Get<int>(DataKey.AbilityTriggerMode);
                // 仅保留包含 Manual 标记的主动技能
                return mode.HasFlag(AbilityTriggerMode.Manual);
            })
            .ToList();
    }

    /// <summary>
    /// 解析技能目标配置并构建上下文
    /// </summary>
    private CastContext BuildCastContext(AbilityEntity ability)
    {
        var context = new CastContext();
        var selection = ability.Data.Get<AbilityTargetSelection>(DataKey.AbilityTargetSelection);

        if (_entity is not Node2D casterNode) return context;
        Vector2 origin = casterNode.GlobalPosition;

        switch (selection)
        {
            case AbilityTargetSelection.Unit:
                // 【模式：单位目标】执行智能索敌，选取范围内最近的敌方单位
                var range = ability.Data.Get<float>(DataKey.AbilityRange);
                context.Targets = TargetSelector.Query(new TargetSelectorQuery
                {
                    Geometry = AbilityTargetGeometry.Circle,
                    Origin = origin,
                    Range = range > 0 ? range : 300f, // 若无范围配置则默认 300
                    CenterEntity = _entity,
                    TeamFilter = AbilityTargetTeamFilter.Enemy, // TODO: 需根据技能配置动态确定团队过滤
                    Sorting = AbilityTargetSorting.Nearest,
                    MaxTargets = 1
                });
                break;

            case AbilityTargetSelection.Point:
                // 【模式：位置目标】解析当前瞄准方向（右摇杆优先 > 移动方向 > 默认方向）
                var pointRange = ability.Data.Get<float>(DataKey.AbilityRange);
                context.TargetPosition = origin + GetCastDirection() * (pointRange > 0 ? pointRange : 200f);
                break;

            case AbilityTargetSelection.UnitOrPoint:
                // 【模式：混合】优先尝试索敌，若无目标则降级为位置释放
                var unitRange = ability.Data.Get<float>(DataKey.AbilityRange);
                var targets = TargetSelector.Query(new TargetSelectorQuery
                {
                    Geometry = AbilityTargetGeometry.Circle,
                    Origin = origin,
                    Range = unitRange > 0 ? unitRange : 300f,
                    CenterEntity = _entity,
                    TeamFilter = AbilityTargetTeamFilter.Enemy,
                    Sorting = AbilityTargetSorting.Nearest,
                    MaxTargets = 1
                });

                if (targets.Count > 0)
                {
                    context.Targets = targets;
                }
                else
                {
                    context.TargetPosition = origin + GetCastDirection() * (unitRange > 0 ? unitRange : 200f);
                }
                break;
        }

        return context;
    }

    /// <summary>
    /// 计算施法解析方向
    /// 优先级：右摇杆 (Aim) > 左摇杆 (Move) > 默认朝向 (Right)
    /// </summary>
    private Vector2 GetCastDirection()
    {
        // 1. 尝试获取右摇杆瞄准输入
        var aim = InputManager.GetAimInput();

        // 2. 如果无瞄准输入，尝试获取左摇杆移动输入
        if (aim.LengthSquared() < 0.1f)
            aim = InputManager.GetMoveInput();

        // 3. 如果仍没有任何输入，使用默认方向
        if (aim.LengthSquared() < 0.1f)
            aim = Vector2.Right;

        return aim.Normalized();
    }
}
