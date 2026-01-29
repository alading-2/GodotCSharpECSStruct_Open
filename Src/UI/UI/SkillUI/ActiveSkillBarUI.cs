using Godot;
using System.Collections.Generic;
using System.Linq;


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

        // 创建4个技能槽位
        CreateSkillSlots();

        // 如果已绑定实体，初始化显示
        if (_entity != null)
        {
            InitializeDisplay();
        }
    }

    protected override void OnBind()
    {
        // 订阅技能添加事件
        _entity!.Events.On<GameEventType.Ability.AddedEventData>(
            GameEventType.Ability.Added,
            OnAbilityAdded
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

    protected override void OnUnbind()
    {
        ClearAllSlots();
    }

    public override void OnPoolReset()
    {
        base.OnPoolReset();
        ClearAllSlots();
    }

    private void CreateSkillSlots()
    {
        var slotScene = ResourceManagement.Load<PackedScene>(nameof(ActiveSkillSlotUI), ResourceCategory.UI);
        if (slotScene == null)
        {
            _log.Error("无法加载 ActiveSkillSlotUI.tscn");
            return;
        }

        for (int i = 0; i < MAX_SKILL_SLOTS; i++)
        {
            var slot = slotScene.Instantiate<ActiveSkillSlotUI>();
            _slotContainer.AddChild(slot);
            _skillSlots.Add(slot);
            slot.Visible = false;
        }

        _log.Debug($"创建了 {MAX_SKILL_SLOTS} 个技能槽位");
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

    private void OnActiveSkillSelected(GameEventType.UI.ActiveSkillSelectedEventData evt)
    {
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
            if (i < activeAbilities.Count)
            {
                // 有技能，显示并绑定到技能实体
                var ability = activeAbilities[i];
                _skillSlots[i].Visible = true;
                _skillSlots[i].UpdateSlot(ability);  // 直接更新槽位显示的技能
            }
            else
            {
                // 无技能，隐藏
                _skillSlots[i].Visible = false;
            }
        }

        // 高亮当前选中的技能
        int currentIndex = _entity.Data.Get<int>(DataKey.CurrentActiveAbilityIndex);
        HighlightSelectedSlot(currentIndex);
    }

    private void HighlightSelectedSlot(int index)
    {
        for (int i = 0; i < _skillSlots.Count; i++)
        {
            if (_skillSlots[i].Visible)
            {
                _skillSlots[i].SetHighlight(i == index);
            }
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in _skillSlots)
        {
            slot.Visible = false;
        }
    }

    private List<AbilityEntity> GetActiveAbilities()
    {
        if (_entity == null) return new List<AbilityEntity>();

        return EntityManager.GetAbilities(_entity)
            .Where(a =>
            {
                var mode = (AbilityTriggerMode)a.Data.Get<int>(DataKey.AbilityTriggerMode);
                return mode.HasFlag(AbilityTriggerMode.Manual);
            })
            .ToList();
    }
}
