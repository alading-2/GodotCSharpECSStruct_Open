using Godot;
using System;

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
        _titleLabel ??= ResolveRequiredNode<Label>("%TitleLabel", "TitleLabel", nameof(_titleLabel));
        return _titleLabel;
    }

    private VBoxContainer GetItemsContainer()
    {
        _itemsContainer ??= ResolveRequiredNode<VBoxContainer>("%ItemsContainer", "ItemsContainer", nameof(_itemsContainer));
        return _itemsContainer;
    }

    private T ResolveRequiredNode<T>(string uniquePath, string fallbackPath, string cacheName) where T : Node
    {
        var node = GetNodeOrNull<T>(uniquePath);
        if (node != null)
        {
            return node;
        }

        node = GetNodeOrNull<T>(fallbackPath);
        if (node != null)
        {
            return node;
        }

        _log.Error($"[技能测试UI] 技能分组节点缺失: section={Name} cache={cacheName} unique={uniquePath} fallback={fallbackPath}");
        throw new InvalidOperationException($"AbilityGroupSection 节点缺失: section={Name}, cache={cacheName}, unique={uniquePath}, fallback={fallbackPath}");
    }
}
