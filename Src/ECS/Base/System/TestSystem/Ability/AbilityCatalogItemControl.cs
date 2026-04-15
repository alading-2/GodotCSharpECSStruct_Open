using Godot;
using System;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 技能库条目控件。
/// <para>
/// 负责展示一个可添加的技能条目，并向外抛出“请求添加”事件。
/// </para>
/// </summary>
public partial class AbilityCatalogItemControl : PanelContainer
{
    private static readonly Log _log = new(nameof(AbilityCatalogItemControl));

    /// <summary>
    /// 当用户点击“添加”按钮时发出，参数为技能资源键。
    /// </summary>
    [Signal] public delegate void AddRequestedEventHandler(string resourceKey);

    private Label? _titleLabel;
    private Label? _metaLabel;
    private Label? _descriptionLabel;
    private Button? _actionButton;
    private string _resourceKey = string.Empty;

    /// <summary>
    /// 配置条目显示。
    /// </summary>
    internal void Configure(AbilityCatalogItemView item)
    {
        _resourceKey = item.ResourceKey;
        GetTitleLabel().Text = item.DisplayName;
        GetMetaLabel().Text = $"{item.AbilityType} / {item.TriggerMode}";
        GetDescriptionLabel().Text = item.Description;
        TooltipText = $"分组: {item.FeatureGroupId}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n\n{item.Description}";

        var actionButton = GetActionButton();
        if (item.IsOwned)
        {
            actionButton.Text = "已拥有";
            actionButton.Disabled = true;
            Modulate = new Color(0.75f, 0.75f, 0.75f, 1f);
        }
        else
        {
            actionButton.Text = "添加";
            actionButton.Disabled = false;
            Modulate = Colors.White;
        }
    }

    /// <summary>
    /// 绑定固定按钮事件。
    /// </summary>
    public override void _Ready()
    {
        GetActionButton().Pressed += OnActionButtonPressed;
    }

    /// <summary>
    /// 兼容旧交互：左键点击整条技能库项即可触发添加。
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent)
        {
            return;
        }

        if (!mouseEvent.Pressed || mouseEvent.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        // 如果命中“添加”按钮区域，交给按钮自身处理，避免重复触发
        if (GetActionButton().GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
        {
            return;
        }

        _log.Info($"[技能测试UI] 左键点击技能库条目（整项添加）: resourceKey={_resourceKey}");
        EmitAddRequested();
    }

    private void OnActionButtonPressed()
    {
        EmitAddRequested();
    }

    private void EmitAddRequested()
    {
        if (!string.IsNullOrWhiteSpace(_resourceKey))
        {
            EmitSignal(SignalName.AddRequested, _resourceKey); // 技能资源键
        }
    }

    private Label GetTitleLabel()
    {
        _titleLabel ??= this.ResolveRequiredNode<Label>("%TitleLabel", "Margin/Layout/TopRow/TitleLabel", nameof(AbilityCatalogItemControl));
        return _titleLabel;
    }

    private Label GetMetaLabel()
    {
        _metaLabel ??= this.ResolveRequiredNode<Label>("%MetaLabel", "Margin/Layout/MetaLabel", nameof(AbilityCatalogItemControl));
        return _metaLabel;
    }

    private Label GetDescriptionLabel()
    {
        _descriptionLabel ??= this.ResolveRequiredNode<Label>("%DescriptionLabel", "Margin/Layout/DescriptionLabel", nameof(AbilityCatalogItemControl));
        return _descriptionLabel;
    }

    private Button GetActionButton()
    {
        _actionButton ??= this.ResolveRequiredNode<Button>("%ActionButton", "Margin/Layout/TopRow/ActionButton", nameof(AbilityCatalogItemControl));
        return _actionButton;
    }
}
