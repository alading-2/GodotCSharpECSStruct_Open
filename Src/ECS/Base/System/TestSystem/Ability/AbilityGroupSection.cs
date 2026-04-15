using Godot;
using System;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 技能测试分组区块。
/// <para>
/// 用于承载同一分组下的多个技能条目，
/// 让技能面板改为可复用场景组合，而不是 TreeItem 代码拼装。
/// </para>
/// </summary>
public partial class AbilityGroupSection : VBoxContainer
{
    private static readonly Log _log = new(nameof(AbilityGroupSection));

    private Label? _titleLabel;
    private VBoxContainer? _itemsContainer;

    /// <summary>
    /// 配置分组标题。
    /// </summary>
    public void SetTitle(string title)
    {
        GetTitleLabel().Text = title;
    }

    /// <summary>
    /// 添加一个技能条目控件。
    /// </summary>
    public void AddItem(Control item)
    {
        GetItemsContainer().AddChild(item);
    }

    private Label GetTitleLabel()
    {
        _titleLabel ??= this.ResolveRequiredNode<Label>("%TitleLabel", "TitleLabel", nameof(AbilityGroupSection));
        return _titleLabel;
    }

    private VBoxContainer GetItemsContainer()
    {
        _itemsContainer ??= this.ResolveRequiredNode<VBoxContainer>("%ItemsContainer", "ItemsContainer", nameof(AbilityGroupSection));
        return _itemsContainer;
    }
}
