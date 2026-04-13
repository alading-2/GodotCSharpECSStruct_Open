using Godot;
using System;

/// <summary>
/// 已拥有技能条目卡片。
/// </summary>
public partial class AbilityOwnedCard : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _metaLabel = null!;
    private Label _stateLabel = null!;
    private Label _descriptionLabel = null!;
    private Button _toggleButton = null!;
    private Button _removeButton = null!;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("Margin/Layout/Header/NameLabel");
        _metaLabel = GetNode<Label>("Margin/Layout/Header/MetaLabel");
        _stateLabel = GetNode<Label>("Margin/Layout/Header/StateLabel");
        _descriptionLabel = GetNode<Label>("Margin/Layout/DescriptionLabel");
        _toggleButton = GetNode<Button>("Margin/Layout/Actions/ToggleButton");
        _removeButton = GetNode<Button>("Margin/Layout/Actions/RemoveButton");
    }

    /// <summary>
    /// 绑定已拥有技能条目。
    /// </summary>
    internal void Bind(
        AbilityOwnedItemView item,
        Action<string, bool> onToggleRequested,
        Action<string> onRemoveRequested)
    {
        _nameLabel.Text = item.DisplayName;
        _metaLabel.Text = $"{item.AbilityType} / {item.TriggerMode}";
        _stateLabel.Text = item.IsEnabled ? "启用" : "禁用";
        _descriptionLabel.Text = item.Description;
        _toggleButton.Text = item.IsEnabled ? "禁用" : "启用";
        _toggleButton.Pressed += () => onToggleRequested(item.AbilityId, !item.IsEnabled);
        _removeButton.Pressed += () => onRemoveRequested(item.AbilityId);
        TooltipText = $"{item.DisplayName}\n分组: {item.GroupPath}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n启用: {(item.IsEnabled ? "是" : "否")}\n\n{item.Description}";
    }
}
