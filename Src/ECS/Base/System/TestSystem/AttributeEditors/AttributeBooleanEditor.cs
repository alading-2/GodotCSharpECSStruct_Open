using Godot;

/// <summary>
/// 布尔属性编辑器。
/// </summary>
public partial class AttributeBooleanEditor : HBoxContainer
{
    private CheckButton _valueToggle = null!;

    public override void _Ready()
    {
        _valueToggle = GetNode<CheckButton>("ValueToggle");
    }

    /// <summary>
    /// 绑定布尔属性。
    /// </summary>
    public void Bind(IEntity entity, DataMeta meta, System.Action<object> onValueCommitted)
    {
        _valueToggle.ButtonPressed = entity.Data.Get<bool>(meta.Key);
        _valueToggle.Text = _valueToggle.ButtonPressed ? "已开启" : "已关闭";
        _valueToggle.Toggled += pressed =>
        {
            _valueToggle.Text = pressed ? "已开启" : "已关闭";
            onValueCommitted(pressed);
        };
    }
}
