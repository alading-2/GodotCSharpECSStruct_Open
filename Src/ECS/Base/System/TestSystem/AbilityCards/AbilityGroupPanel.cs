using Godot;

/// <summary>
/// 技能分组面板。
/// </summary>
public partial class AbilityGroupPanel : VBoxContainer
{
    private Label _groupTitleLabel = null!;
    private VBoxContainer _itemsContainer = null!;

    public override void _Ready()
    {
        _groupTitleLabel = GetNode<Label>("GroupTitleLabel");
        _itemsContainer = GetNode<VBoxContainer>("ItemsContainer");
    }

    /// <summary>
    /// 设置分组标题。
    /// </summary>
    public void SetTitle(string title)
    {
        _groupTitleLabel.Text = title;
    }

    /// <summary>
    /// 添加一个分组条目。
    /// </summary>
    public void AddItem(Control item)
    {
        _itemsContainer.AddChild(item);
    }
}
