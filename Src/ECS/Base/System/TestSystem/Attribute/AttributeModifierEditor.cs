using Godot;
using System;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 属性测试的临时 Modifier 编辑器。
/// <para>
/// 这个控件只负责固定 UI 结构与信号暴露，
/// 真正的业务动作仍由 AttributeTestModule 调用 FeatureDebugService。
/// </para>
/// </summary>
public partial class AttributeModifierEditor : HBoxContainer
{
    private static readonly Log _log = new(nameof(AttributeModifierEditor));

    private Label? _titleLabel;
    private SpinBox? _valueSpinBox;
    private Button? _applyButton;
    private Button? _clearButton;

    /// <summary>
    /// 当前输入值。
    /// </summary>
    public double Value
    {
        get => GetValueSpinBox().Value;
        set => GetValueSpinBox().Value = value;
    }

    /// <summary>
    /// 设置标题文本。
    /// </summary>
    public void SetTitle(string title)
    {
        GetTitleLabel().Text = title;
    }

    /// <summary>
    /// 配置数值范围。
    /// </summary>
    public void ConfigureRange(double minValue, double maxValue, double step)
    {
        var spinBox = GetValueSpinBox();
        spinBox.MinValue = minValue;
        spinBox.MaxValue = maxValue;
        spinBox.Step = step;
    }

    /// <summary>
    /// 订阅应用按钮事件。
    /// </summary>
    public void BindApply(System.Action callback)
    {
        GetApplyButton().Pressed += callback;
    }

    /// <summary>
    /// 订阅清除按钮事件。
    /// </summary>
    public void BindClear(System.Action callback)
    {
        GetClearButton().Pressed += callback;
    }

    private Label GetTitleLabel()
    {
        _titleLabel ??= this.ResolveRequiredNode<Label>("%TitleLabel", "TitleLabel", nameof(AttributeModifierEditor));
        return _titleLabel;
    }

    private SpinBox GetValueSpinBox()
    {
        _valueSpinBox ??= this.ResolveRequiredNode<SpinBox>("%ValueSpinBox", "ValueSpinBox", nameof(AttributeModifierEditor));
        return _valueSpinBox;
    }

    private Button GetApplyButton()
    {
        _applyButton ??= this.ResolveRequiredNode<Button>("%ApplyButton", "ApplyButton", nameof(AttributeModifierEditor));
        return _applyButton;
    }

    private Button GetClearButton()
    {
        _clearButton ??= this.ResolveRequiredNode<Button>("%ClearButton", "ClearButton", nameof(AttributeModifierEditor));
        return _clearButton;
    }
}
