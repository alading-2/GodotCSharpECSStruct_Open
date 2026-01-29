using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 主动技能槽位UI
/// 显示当前选中的主动技能，包括图标、冷却遮罩、充能层数
/// 
/// 核心功能：
/// 1. Bind到PlayerEntity后自动监听技能切换事件
/// 2. 显示冷却进度（遮罩从上到下收缩）
/// 3. 显示充能层数（如 "2/3"）
/// 4. 显示按键提示（X）
/// </summary>
public partial class ActiveSkillSlotUI : UIBase
{
    private static readonly Log _log = new("ActiveSkillSlotUI", LogLevel.Debug);

    // ============================================================
    // UI 节点引用
    // ============================================================

    private TextureRect _skillIcon = null!;
    private ColorRect _cooldownOverlay = null!;
    private Label _chargeLabel = null!;
    private Label _keyHintLabel = null!;
    private Label _skillNameLabel = null!;
    private Panel _background = null!;

    // ============================================================
    // 状态
    // ============================================================

    private AbilityEntity? _currentAbility;
    private float _cooldownOverlayMaxHeight;
    private Color _normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private Color _highlightColor = new Color(0.4f, 0.6f, 1.0f, 0.9f);
    private const string DEFAULT_SKILL_ICON = "res://icon.svg";

    // ============================================================
    // Godot 生命周期
    // ============================================================

    public override void _Ready()
    {
        _skillIcon = GetNode<TextureRect>("%SkillIcon");
        _cooldownOverlay = GetNode<ColorRect>("%CooldownOverlay");
        _chargeLabel = GetNode<Label>("%ChargeLabel");
        _keyHintLabel = GetNode<Label>("%KeyHintLabel");
        _skillNameLabel = GetNode<Label>("%SkillNameLabel");
        _background = GetNode<Panel>("%Background");

        // 记录遮罩初始高度
        _cooldownOverlayMaxHeight = _cooldownOverlay.Size.Y;

        // 初始状态
        _keyHintLabel.Text = "X";
        _chargeLabel.Visible = false;
        _cooldownOverlay.Visible = false;

        // 如果已绑定实体，初始化显示
        if (_entity != null)
        {
            InitializeDisplay();
        }
    }

    public override void _Process(double delta)
    {
        if (_entity == null || _currentAbility == null) return;

        // 更新冷却遮罩动画
        UpdateCooldownOverlay();
    }

    // ============================================================
    // UIBase 重写
    // ============================================================

    protected override void OnBind()
    {
        // 订阅技能切换事件
        _entity!.Events.On<GameEventType.UI.ActiveSkillSelectedEventData>(
            GameEventType.UI.ActiveSkillSelected,
            OnActiveSkillSelected
        );

        // 初始化显示（如果节点已就绪）
        if (_skillIcon != null)
        {
            InitializeDisplay();
        }
    }

    protected override void OnUnbind()
    {
        _currentAbility = null;
        Visible = false;
    }

    public override void OnPoolReset()
    {
        base.OnPoolReset();
        UnsubscribeAbilityEvents();
        _currentAbility = null;
        _cooldownOverlay.Visible = false;
        _chargeLabel.Visible = false;
    }

    // ============================================================
    // 事件处理
    // ============================================================

    private void OnActiveSkillSelected(GameEventType.UI.ActiveSkillSelectedEventData evt)
    {
        _log.Debug($"技能切换事件: {evt.AbilityName} (索引: {evt.SlotIndex})");
        UpdateCurrentAbility(evt.AbilityName);
    }

    // ============================================================
    // 核心逻辑
    // ============================================================

    /// <summary>
    /// 初始化显示：获取第一个主动技能
    /// </summary>
    private void InitializeDisplay()
    {
        if (_entity == null) return;

        // 获取所有主动技能
        var activeAbilities = GetActiveAbilities();
        if (activeAbilities.Count == 0)
        {
            _log.Debug("无主动技能，隐藏UI");
            Visible = false;
            return;
        }

        // 获取当前选中索引
        int currentIndex = _entity.Data.Get<int>(DataKey.CurrentActiveAbilityIndex);
        if (currentIndex < 0 || currentIndex >= activeAbilities.Count)
        {
            currentIndex = 0;
            _entity.Data.Set(DataKey.CurrentActiveAbilityIndex, 0);
        }

        var ability = activeAbilities[currentIndex];
        var abilityName = ability.Data.Get<string>(DataKey.Name);
        UpdateCurrentAbility(abilityName);

        Visible = true;
    }

    /// <summary>
    /// 更新当前显示的技能
    /// </summary>
    private void UpdateCurrentAbility(string abilityName)
    {
        if (_entity == null) return;

        // 查找技能实体
        _currentAbility = EntityManager.GetAbilityByName(_entity, abilityName);
        if (_currentAbility == null)
        {
            _log.Warn($"找不到技能: {abilityName}");
            return;
        }

        // 更新图标（支持 Texture2D 或路径字符串）
        Texture2D? iconTexture = null;
        var iconValue = _currentAbility.Data.GetBase<object?>(DataKey.AbilityIcon, null);
        if (iconValue is Texture2D texture)
        {
            iconTexture = texture;
        }
        else if (iconValue is string iconPath && !string.IsNullOrEmpty(iconPath))
        {
            iconTexture = GD.Load<Texture2D>(iconPath);
        }

        _skillIcon.Texture = iconTexture ?? GD.Load<Texture2D>(DEFAULT_SKILL_ICON);

        // 更新技能名称
        _skillNameLabel.Text = abilityName;

        // 更新充能显示
        UpdateChargeDisplay();

        // 订阅当前技能的事件
        SubscribeAbilityEvents();

        _log.Debug($"切换到技能: {abilityName}");
    }

    /// <summary>
    /// 订阅当前技能的事件
    /// </summary>
    private void SubscribeAbilityEvents()
    {
        if (_currentAbility == null) return;

        // 先取消之前的订阅(如果有)
        UnsubscribeAbilityEvents();

        // 监听充能变化
        _currentAbility.Events.On<GameEventType.Ability.ChargeRestoredEventData>(
            GameEventType.Ability.ChargeRestored,
            OnChargeRestored
        );

        // 监听冷却完成
        _currentAbility.Events.On<GameEventType.Ability.ReadyEventData>(
            GameEventType.Ability.Ready,
            OnAbilityReady
        );

        // 监听技能激活
        _currentAbility.Events.On<GameEventType.Ability.ActivatedEventData>(
            GameEventType.Ability.Activated,
            OnAbilityActivated
        );
    }

    /// <summary>
    /// 取消订阅当前技能的事件
    /// </summary>
    private void UnsubscribeAbilityEvents()
    {
        if (_currentAbility == null) return;

        _currentAbility.Events.Off<GameEventType.Ability.ChargeRestoredEventData>(
            GameEventType.Ability.ChargeRestored,
            OnChargeRestored
        );

        _currentAbility.Events.Off<GameEventType.Ability.ReadyEventData>(
            GameEventType.Ability.Ready,
            OnAbilityReady
        );

        _currentAbility.Events.Off<GameEventType.Ability.ActivatedEventData>(
            GameEventType.Ability.Activated,
            OnAbilityActivated
        );
    }

    private void OnChargeRestored(GameEventType.Ability.ChargeRestoredEventData evt)
    {
        UpdateChargeDisplay();
    }

    private void OnAbilityReady(GameEventType.Ability.ReadyEventData evt)
    {
        // 冷却完成，隐藏遮罩
        _cooldownOverlay.Visible = false;
    }

    private void OnAbilityActivated(GameEventType.Ability.ActivatedEventData evt)
    {
        // 技能激活，更新充能和冷却显示
        UpdateChargeDisplay();
        _cooldownOverlay.Visible = true;
    }

    /// <summary>
    /// 更新充能显示
    /// </summary>
    private void UpdateChargeDisplay()
    {
        if (_currentAbility == null) return;

        // 检查是否使用充能系统
        bool usesCharges = _currentAbility.Data.Get<bool>(DataKey.IsAbilityUsesCharges);
        if (!usesCharges)
        {
            _chargeLabel.Visible = false;
            return;
        }

        int currentCharges = _currentAbility.Data.Get<int>(DataKey.AbilityCurrentCharges);
        int maxCharges = _currentAbility.Data.Get<int>(DataKey.AbilityMaxCharges);

        _chargeLabel.Text = $"{currentCharges}/{maxCharges}";
        _chargeLabel.Visible = true;
    }

    /// <summary>
    /// 更新冷却遮罩
    /// </summary>
    private void UpdateCooldownOverlay()
    {
        if (_currentAbility == null) return;

        // 获取冷却组件
        var cooldownComponent = EntityManager.GetComponent<CooldownComponent>(_currentAbility);
        if (cooldownComponent == null) return;

        // 如果技能就绪，隐藏遮罩
        if (cooldownComponent.IsReady())
        {
            _cooldownOverlay.Visible = false;
            return;
        }

        // 显示遮罩并更新高度
        _cooldownOverlay.Visible = true;

        // 进度 0.0 = 刚开始冷却, 1.0 = 冷却完成
        float progress = cooldownComponent.GetCooldownProgress();

        // 遮罩高度：从满到空
        float overlayHeight = _cooldownOverlayMaxHeight * (1f - progress);
        _cooldownOverlay.Size = new Vector2(_cooldownOverlay.Size.X, overlayHeight);
    }

    /// <summary>
    /// 获取所有主动技能
    /// </summary>
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

    // ============================================================
    // 公共方法 (供 ActiveSkillBarUI 调用)
    // ============================================================

    /// <summary>
    /// 直接更新槽位显示的技能
    /// </summary>
    public void UpdateSlot(AbilityEntity ability)
    {
        // 先取消旧技能的事件订阅
        UnsubscribeAbilityEvents();

        _currentAbility = ability;
        var abilityName = ability.Data.Get<string>(DataKey.Name);

        // 更新图标
        Texture2D? iconTexture = null;
        var iconValue = ability.Data.GetBase<object?>(DataKey.AbilityIcon, null);
        if (iconValue is Texture2D texture)
        {
            iconTexture = texture;
        }
        else if (iconValue is string iconPath && !string.IsNullOrEmpty(iconPath))
        {
            iconTexture = GD.Load<Texture2D>(iconPath);
        }

        _skillIcon.Texture = iconTexture ?? GD.Load<Texture2D>("res://icon.svg");

        // 更新技能名称
        _skillNameLabel.Text = abilityName;

        // 更新充能显示
        UpdateChargeDisplay();

        // 订阅新技能的事件
        SubscribeAbilityEvents();
    }

    /// <summary>
    /// 设置高亮状态
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        if (_background != null)
        {
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = highlighted ? _highlightColor : _normalColor;

            if (highlighted)
            {
                stylebox.BorderWidthLeft = 2;
                stylebox.BorderWidthRight = 2;
                stylebox.BorderWidthTop = 2;
                stylebox.BorderWidthBottom = 2;
                stylebox.BorderColor = new Color(1, 1, 1, 1);
            }

            _background.AddThemeStyleboxOverride("panel", stylebox);
        }
    }
}
