using Godot;
using System.Collections.Generic;


/// <summary>
/// 主动技能栏UI - 显示4个技能槽位
/// 固定显示在屏幕下方，自动监听技能添加事件并更新显示
/// </summary>
public partial class ActiveSkillBarUI : UIBase
{
    private static readonly Log _log = new("ActiveSkillBarUI", LogLevel.Debug);

    private const int MAX_SKILL_SLOTS = 4;

    private List<ActiveSkillSlotUI> _skillSlots = new();
    private HBoxContainer _slotContainer = null!;

    public override void _Ready()
    {
        _slotContainer = GetNode<HBoxContainer>("%SlotContainer");

        // 获取场景中静态放置的槽位
        foreach (var child in _slotContainer.GetChildren())
        {
            if (child is ActiveSkillSlotUI slot)
            {
                _skillSlots.Add(slot);
                slot.Visible = true; // 初始显示，但在 InitializeDisplay 前可能是空的
                slot.ClearSlot();   // 初始清空
            }
        }

        _log.Debug($"已通过静态布局加载 {_skillSlots.Count} 个技能槽位");

        // 如果已绑定实体，初始化显示
        if (_entity != null)
        {
            InitializeDisplay();
        }
    }

    /// <summary>
    /// 绑定实体时的初始化
    /// </summary>
    protected override void OnBind()
    {
        // 订阅技能添加事件
        _entity!.Events.On<GameEventType.Ability.AddedEventData>(
            GameEventType.Ability.Added,
            OnAbilityAdded
        );

        // 订阅技能移除事件
        _entity!.Events.On<GameEventType.Ability.RemovedEventData>(
            GameEventType.Ability.Removed,
            OnAbilityRemoved
        );

        // 订阅技能切换事件
        _entity!.Events.On<GameEventType.UI.ActiveSkillSelectedEventData>(
            GameEventType.UI.ActiveSkillSelected,
            OnActiveSkillSelected
        );

        // 初始化显示
        if (_slotContainer != null)
        {
            InitializeDisplay();
        }
    }

    /// <summary>
    /// 解绑实体时的清理
    /// </summary>
    protected override void OnUnbind()
    {
        ClearAllSlots();
    }



    private void InitializeDisplay()
    {
        if (_entity == null) return;

        UpdateAllSlots();
        Visible = true;
    }

    private void OnAbilityAdded(GameEventType.Ability.AddedEventData evt)
    {
        var abilityName = evt.Ability.Data.Get<string>(DataKey.Name);
        _log.Debug($"检测到技能添加: {abilityName}");
        UpdateAllSlots();
    }

    private void OnAbilityRemoved(GameEventType.Ability.RemovedEventData evt)
    {
        _log.Debug($"检测到技能移除: {evt.abilityName}");
        UpdateAllSlots();
    }

    private void OnActiveSkillSelected(GameEventType.UI.ActiveSkillSelectedEventData evt)
    {
        _log.Debug($"收到 ActiveSkillSelected 事件: Index {evt.SlotIndex}, Name: {evt.AbilityName}");
        HighlightSelectedSlot(evt.SlotIndex);
    }

    private void UpdateAllSlots()
    {
        if (_entity == null) return;

        var activeAbilities = GetActiveAbilities();
        _log.Debug($"更新技能槽位，共 {activeAbilities.Count} 个主动技能");

        // 更新每个槽位
        for (int i = 0; i < MAX_SKILL_SLOTS; i++)
        {
            // 确保槽位可见
            _skillSlots[i].Visible = true;

            if (i < activeAbilities.Count)
            {
                // 有技能，显示并绑定到技能实体
                var ability = activeAbilities[i];
                _skillSlots[i].UpdateSlot(ability);
            }
            else
            {
                // 无技能，清空显示但保持占位
                _skillSlots[i].ClearSlot();
            }
        }

        // 高亮当前选中的技能
        int currentIndex = _entity.Data.Get<int>(DataKey.CurrentActiveAbilityIndex);
        HighlightSelectedSlot(currentIndex);
    }

    private void HighlightSelectedSlot(int index)
    {
        _log.Debug($"尝试高亮槽位: {index}");
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            _skillSlots[i].SetHighlight(i == index);
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in _skillSlots)
        {
            slot.ClearSlot();
            // 不再隐藏槽位，保持布局
            // slot.Visible = false; 
        }
    }

    private List<AbilityEntity> GetActiveAbilities()
    {
        if (_entity == null) return new List<AbilityEntity>();
        return EntityManager.GetManualAbilities(_entity);
    }
}
