using Godot;
using System;

/// <summary>
/// 临时 Modifier 编辑器。
/// </summary>
public partial class AttributeModifierEditor : HBoxContainer
{
    private Label _modifierLabel = null!;
    private SpinBox _modifierValueSpinBox = null!;
    private Button _applyButton = null!;
    private Button _clearButton = null!;

    public override void _Ready()
    {
        _modifierLabel = GetNode<Label>("ModifierLabel");
        _modifierValueSpinBox = GetNode<SpinBox>("ModifierValueSpinBox");
        _applyButton = GetNode<Button>("ApplyButton");
        _clearButton = GetNode<Button>("ClearButton");
    }

    /// <summary>
    /// 绑定临时 Modifier 编辑器。
    /// </summary>
    internal void Bind(
        IEntity entity,
        DataMeta meta,
        FeatureDebugService featureDebugService,
        Action<string> onStatusChanged,
        Action onRefreshRequested)
    {
        _modifierLabel.Text = "临时加成";

        var maxAbs = Math.Max(Math.Abs(meta.MinValue ?? 0d), Math.Abs(meta.MaxValue ?? 0d));
        if (maxAbs < 9999d)
        {
            maxAbs = 9999d;
        }

        _modifierValueSpinBox.MinValue = -maxAbs;
        _modifierValueSpinBox.MaxValue = maxAbs;
        _modifierValueSpinBox.Step = meta.IsInteger ? 1 : 0.1;
        _modifierValueSpinBox.Value = featureDebugService.GetTemporaryModifierValue(entity, meta.Key);

        _applyButton.Pressed += () =>
        {
            var result = featureDebugService.ApplyTemporaryModifier(
                entity, // owner
                meta.Key, // dataKey
                GetMetaDisplayName(meta), // displayName
                meta.IsPercentage, // isPercentage
                (float)_modifierValueSpinBox.Value // value
            );
            onStatusChanged(result.Message);
            onRefreshRequested();
        };

        _clearButton.Pressed += () =>
        {
            var result = featureDebugService.ClearTemporaryModifier(
                entity, // owner
                meta.Key, // dataKey
                GetMetaDisplayName(meta) // displayName
            );
            onStatusChanged(result.Message);
            onRefreshRequested();
        };
    }

    private static string GetMetaDisplayName(DataMeta meta)
    {
        return string.IsNullOrWhiteSpace(meta.DisplayName) ? meta.Key : meta.DisplayName;
    }
}
