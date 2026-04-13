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
    private static readonly Log _log = new(nameof(ActiveSkillSlotUI), LogLevel.Debug);

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
        // Removed incorrect subscription to GameEventType.UI.ActiveSkillSelected
        // ActiveSkillSlotUI should NOT change its ability based on global selection.
        // The selection highlight is handled by ActiveSkillBarUI.

        // 初始化显示（如果节点已就绪）
        if (_skillIcon != null)
        {
            InitializeDisplay();
        }
    }

    protected override void OnUnbind()
    {
        UnsubscribeAbilityEvents(); // Ensure events are unsubscribed
        _currentAbility = null;
        Visible = false;
    }



    // ============================================================
    // 事件处理
    // ============================================================

    // Removed OnActiveSkillSelected - this was causing the bug where all slots showed the selected skill

    // ============================================================
    // 核心逻辑
    // ============================================================

    /// <summary>
    /// 初始化显示：不再自动获取，等待 ActiveSkillBarUI 分配
    /// </summary>
    private void InitializeDisplay()
    {
        // Keep empty or minimal. ActiveSkillBarUI will call UpdateSlot.
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
        // 使用 Scale 而不是 Size，以避免被 Container 布局强制重置
        // 关键：设置基准点为底部中心 (或左下角)，使缩放产生 "向下流逝" 或 "向上揭开" 的效果
        // 这里我们希望遮罩是黑色的，随着冷却结束，遮罩变小。
        // 为了让遮罩 "向下消退" (露出顶部)，Pivot 应该在底部? 
        // 不，如果 Scale Y 从 1 变 0:
        // Pivot Top(0): Bottom edge moves Up. (Draining up)
        // Pivot Bottom(1): Top edge moves Down. (Draining down)

        // 强制设置 Pivot为左下角 (0, Size.Y)
        _cooldownOverlay.PivotOffset = new Vector2(0, _cooldownOverlay.Size.Y);

        _cooldownOverlay.Scale = new Vector2(1f, 1f - progress);
        // _cooldownOverlay.Size = new Vector2(_cooldownOverlay.Size.X, overlayHeight);
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
    /// 清空并重置槽位显示
    /// </summary>
    /// <summary>
    /// 清空并重置槽位显示
    /// </summary>
    public void ClearSlot()
    {
        UnsubscribeAbilityEvents();
        _currentAbility = null;

        // 重置为默认状态
        _skillIcon.Texture = null;
        _skillNameLabel.Text = "";
        _chargeLabel.Visible = false;
        _cooldownOverlay.Visible = false;

        // 关键修改：空槽位保持背景显示，作为占位符
        if (_background != null)
        {
            _background.Visible = true;
            // 确保重置为非高亮样式
            SetHighlight(false);
        }
    }

    /// <summary>
    /// 设置高亮状态
    /// </summary>
    public void SetHighlight(bool highlighted)
    {
        // _log.Debug($"SetHighlight: {highlighted} (Ability: {_skillNameLabel.Text})"); // Optional excessive logging

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
            else
            {
                stylebox.BorderWidthLeft = 0;
                stylebox.BorderWidthRight = 0;
                stylebox.BorderWidthTop = 0;
                stylebox.BorderWidthBottom = 0;
            }

            _background.AddThemeStyleboxOverride("panel", stylebox);
        }
        else
        {
            _log.Error("SetHighlight Failed: _background is null!");
        }
    }
}
