using Godot;
using System;

/// <summary>
/// 字符串属性编辑器。
/// </summary>
public partial class AttributeStringEditor : HBoxContainer
{
    private LineEdit _valueLineEdit = null!;

    public override void _Ready()
    {
        _valueLineEdit = GetNode<LineEdit>("ValueLineEdit");
    }

    /// <summary>
    /// 绑定字符串属性。
    /// </summary>
    public void Bind(IEntity entity, DataMeta meta, Action<object> onValueCommitted)
    {
        _valueLineEdit.Text = entity.Data.Get<string>(meta.Key);
        _valueLineEdit.TextSubmitted += text => onValueCommitted(text);
        _valueLineEdit.FocusExited += () => onValueCommitted(_valueLineEdit.Text);
    }
}
