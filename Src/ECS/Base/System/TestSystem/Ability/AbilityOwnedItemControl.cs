using Godot;
using System;

/// <summary>
/// 当前已拥有技能条目控件。
/// <para>
/// 负责展示启用状态、描述以及操作按钮，
/// 把移除/启停交互从 Tree 右键菜单改为显式场景按钮。
/// </para>
/// </summary>
public partial class AbilityOwnedItemControl : PanelContainer
{
    private static readonly Log _log = new(nameof(AbilityOwnedItemControl));

    /// <summary>
    /// 当用户请求切换技能启用状态时发出。
    /// </summary>
    [Signal] public delegate void ToggleEnabledRequestedEventHandler(string abilityId, bool targetEnabled);

    /// <summary>
    /// 当用户请求移除技能时发出。
    /// </summary>
    [Signal] public delegate void RemoveRequestedEventHandler(string abilityId);

    /// <summary>
    /// 当用户右键技能条目时发出，供模块弹出右键菜单。
    /// </summary>
    [Signal] public delegate void ContextRequestedEventHandler(string abilityId, bool isEnabled, Vector2 globalPosition);

    private Label? _titleLabel;
    private Label? _metaLabel;
    private Label? _descriptionLabel;
    private Button? _toggleButton;
    private Button? _removeButton;
    private string _abilityId = string.Empty;
    private bool _targetEnabled;
    private bool _isEnabled;

    /// <summary>
    /// 配置条目显示。
    /// </summary>
    internal void Configure(AbilityOwnedItemView item)
    {
        _abilityId = item.AbilityId;
        _isEnabled = item.IsEnabled;
        _targetEnabled = !item.IsEnabled;
        GetTitleLabel().Text = item.DisplayName;
        GetMetaLabel().Text = $"{item.AbilityType} / {item.TriggerMode} / {(item.IsEnabled ? "启用" : "禁用")}";
        GetDescriptionLabel().Text = item.Description;
        TooltipText = $"分组: {item.GroupPath}\n类型: {item.AbilityType}\n触发: {item.TriggerMode}\n启用: {(item.IsEnabled ? "是" : "否")}\n\n{item.Description}";
        GetToggleButton().Text = item.IsEnabled ? "禁用" : "启用";
        Modulate = item.IsEnabled ? Colors.White : new Color(0.78f, 0.78f, 0.78f, 1f);
    }

    /// <summary>
    /// 绑定固定按钮事件。
    /// </summary>
    public override void _Ready()
    {
        GetToggleButton().Pressed += OnToggleButtonPressed;
        GetRemoveButton().Pressed += OnRemoveButtonPressed;
    }

    /// <summary>
    /// 兼容旧交互：
    /// <para>左键点击整条右侧技能项直接移除。</para>
    /// <para>右键点击整条右侧技能项弹出上下文菜单。</para>
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseEvent || !mouseEvent.Pressed)
        {
            return;
        }

        // 如果点在按钮上，交给按钮自身处理，避免重复触发
        if (GetToggleButton().GetGlobalRect().HasPoint(mouseEvent.GlobalPosition)
            || GetRemoveButton().GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
        {
            return;
        }

        if (mouseEvent.ButtonIndex == MouseButton.Left)
        {
            _log.Info($"[技能测试UI] 左键点击当前技能条目（整项移除）: abilityId={_abilityId}");
            EmitRemoveRequested();
            return;
        }

        if (mouseEvent.ButtonIndex == MouseButton.Right)
        {
            _log.Info($"[技能测试UI] 右键点击当前技能条目: abilityId={_abilityId} isEnabled={_isEnabled}");
            EmitContextRequested(mouseEvent.GlobalPosition);
        }
    }

    private void OnToggleButtonPressed()
    {
        EmitToggleRequested();
    }

    private void OnRemoveButtonPressed()
    {
        EmitRemoveRequested();
    }

    private void EmitToggleRequested()
    {
        if (!string.IsNullOrWhiteSpace(_abilityId))
        {
            EmitSignal(SignalName.ToggleEnabledRequested, _abilityId, _targetEnabled); // 技能实例Id/目标启用状态
        }
    }

    private void EmitRemoveRequested()
    {
        if (!string.IsNullOrWhiteSpace(_abilityId))
        {
            EmitSignal(SignalName.RemoveRequested, _abilityId); // 技能实例Id
        }
    }

    private void EmitContextRequested(Vector2 globalPosition)
    {
        if (!string.IsNullOrWhiteSpace(_abilityId))
        {
            EmitSignal(SignalName.ContextRequested, _abilityId, _isEnabled, globalPosition); // abilityId/当前启用状态/菜单位置
        }
    }

    private Label GetTitleLabel()
    {
        _titleLabel ??= ResolveRequiredNode<Label>("%TitleLabel", "Margin/Layout/TopRow/TitleLabel", nameof(_titleLabel));
        return _titleLabel;
    }

    private Label GetMetaLabel()
    {
        _metaLabel ??= ResolveRequiredNode<Label>("%MetaLabel", "Margin/Layout/MetaLabel", nameof(_metaLabel));
        return _metaLabel;
    }

    private Label GetDescriptionLabel()
    {
        _descriptionLabel ??= ResolveRequiredNode<Label>("%DescriptionLabel", "Margin/Layout/DescriptionLabel", nameof(_descriptionLabel));
        return _descriptionLabel;
    }

    private Button GetToggleButton()
    {
        _toggleButton ??= ResolveRequiredNode<Button>("%ToggleButton", "Margin/Layout/TopRow/ToggleButton", nameof(_toggleButton));
        return _toggleButton;
    }

    private Button GetRemoveButton()
    {
        _removeButton ??= ResolveRequiredNode<Button>("%RemoveButton", "Margin/Layout/TopRow/RemoveButton", nameof(_removeButton));
        return _removeButton;
    }

    private T ResolveRequiredNode<T>(string uniquePath, string fallbackPath, string cacheName) where T : Node
    {
        var node = GetNodeOrNull<T>(uniquePath);
        if (node != null)
        {
            return node;
        }

        _log.Warn($"[技能测试UI] unique-name 当前技能条目节点未命中，回退普通路径: node={Name} cache={cacheName} unique={uniquePath} fallback={fallbackPath}");

        node = GetNodeOrNull<T>(fallbackPath);
        if (node != null)
        {
            return node;
        }

        throw new InvalidOperationException($"AbilityOwnedItemControl 节点缺失: node={Name}, cache={cacheName}, unique={uniquePath}, fallback={fallbackPath}");
    }
}
