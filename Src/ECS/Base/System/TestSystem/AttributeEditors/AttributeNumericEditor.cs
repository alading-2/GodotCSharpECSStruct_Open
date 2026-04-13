using Godot;
using System;

/// <summary>
/// 数值属性编辑器。
/// </summary>
public partial class AttributeNumericEditor : HBoxContainer
{
    private SpinBox _valueSpinBox = null!;

    public override void _Ready()
    {
        _valueSpinBox = GetNode<SpinBox>("ValueSpinBox");
    }

    /// <summary>
    /// 绑定数值属性。
    /// </summary>
    public void Bind(IEntity entity, DataMeta meta, Action<object> onValueCommitted)
    {
        _valueSpinBox.MinValue = meta.MinValue ?? -999999;
        _valueSpinBox.MaxValue = meta.MaxValue ?? 999999;
        _valueSpinBox.Step = meta.IsInteger ? 1 : 0.1;
        _valueSpinBox.Value = meta.IsInteger
            ? entity.Data.Get<int>(meta.Key)
            : entity.Data.Get<float>(meta.Key);

        _valueSpinBox.ValueChanged += value =>
        {
            if (meta.IsInteger)
            {
                onValueCommitted((int)Math.Round(value));
                return;
            }

            onValueCommitted((float)value);
        };
    }
}
