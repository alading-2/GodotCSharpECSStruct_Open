using Godot;
using System;

/// <summary>
/// 可添加技能条目卡片。
/// </summary>
public partial class AbilityCatalogCard : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _metaLabel = null!;
    private Label _descriptionLabel = null!;
    private Button _addButton = null!;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("Margin/Layout/Header/NameLabel");
        _metaLabel = GetNode<Label>("Margin/Layout/Header/MetaLabel");
        _descriptionLabel = GetNode<Label>("Margin/Layout/DescriptionLabel");
        _addButton = GetNode<Button>("Margin/Layout/Header/AddButton");
    }

    /// <summary>
    /// 绑定技能库条目。
    /// </summary>
    internal void Bind(AbilityCatalogItemView item, bool canAdd, Action<string> onAddRequested)
    {
        _nameLabel.Text = item.DisplayName;
        _metaLabel.Text = $"{item.AbilityType} / {item.TriggerMode}";
        _descriptionLabel.Text = item.Description;
        _addButton.Text = item.IsOwned ? "已拥有" : "添加";
        _addButton.Disabled = !canAdd || item.IsOwned;
        _addButton.Pressed += () => onAddRequested(item.ResourceKey);
        TooltipText = $"{item.DisplayName}\n分组: {item.GroupPath}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n\n{item.Description}";
    }
}
