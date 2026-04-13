using Godot;
using System;

/// <summary>
/// 枚举 / 选项属性编辑器。
/// </summary>
public partial class AttributeEnumEditor : HBoxContainer
{
    private OptionButton _valueOptions = null!;

    public override void _Ready()
    {
        _valueOptions = GetNode<OptionButton>("ValueOptions");
    }

    /// <summary>
    /// 绑定枚举属性。
    /// </summary>
    public void Bind(IEntity entity, DataMeta meta, Action<object> onValueCommitted)
    {
        _valueOptions.Clear();

        if (meta.IsEnum)
        {
            var values = Enum.GetValues(meta.Type);
            var currentValue = entity.Data.Get<int>(meta.Key);
            var selectedIndex = 0;

            for (int index = 0; index < values.Length; index++)
            {
                var option = values.GetValue(index);
                _valueOptions.AddItem(option?.ToString() ?? string.Empty);
                if (option != null && Convert.ToInt32(option) == currentValue)
                {
                    selectedIndex = index;
                }
            }

            _valueOptions.Selected = selectedIndex;
            _valueOptions.ItemSelected += idx =>
            {
                var rawValue = values.GetValue((int)idx);
                if (rawValue != null)
                {
                    onValueCommitted(rawValue);
                }
            };
            return;
        }

        if (meta.HasOptions && meta.Options != null)
        {
            for (int index = 0; index < meta.Options.Count; index++)
            {
                _valueOptions.AddItem(meta.Options[index]);
            }

            _valueOptions.Selected = entity.Data.Get<int>(meta.Key);
            _valueOptions.ItemSelected += idx => onValueCommitted((int)idx);
        }
    }
}
