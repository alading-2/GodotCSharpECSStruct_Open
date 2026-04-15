using Godot;
using System;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 属性测试的单行词条容器。
/// <para>
/// 负责承载标题区、主编辑控件宿主、临时加成控件宿主，
/// 让具体编辑控件全部走场景复用，而不是在模块里手写布局。
/// </para>
/// </summary>
public partial class AttributeEditorRow : VBoxContainer
{
    private static readonly Log _log = new(nameof(AttributeEditorRow));

    private Label? _titleLabel;
    private Label? _keyLabel;
    private VBoxContainer? _editorHost;
    private VBoxContainer? _modifierHost;

    /// <summary>
    /// 配置词条标题与 DataKey 文本。
    /// </summary>
    public void ConfigureHeader(string title, string key)
    {
        GetTitleLabel().Text = title;
        GetKeyLabel().Text = key;
    }

    /// <summary>
    /// 设置主编辑控件。
    /// </summary>
    public void SetEditor(Control? editor)
    {
        ReplaceHostChild(GetEditorHost(), editor);
    }

    /// <summary>
    /// 设置临时加成编辑控件。
    /// </summary>
    public void SetModifierEditor(Control? editor)
    {
        ReplaceHostChild(GetModifierHost(), editor);
        GetModifierHost().Visible = editor != null;
    }

    private Label GetTitleLabel()
    {
        _titleLabel ??= this.ResolveRequiredNode<Label>("%TitleLabel", "Header/TitleLabel", nameof(AttributeEditorRow));
        return _titleLabel;
    }

    private Label GetKeyLabel()
    {
        _keyLabel ??= this.ResolveRequiredNode<Label>("%KeyLabel", "Header/KeyLabel", nameof(AttributeEditorRow));
        return _keyLabel;
    }

    private VBoxContainer GetEditorHost()
    {
        _editorHost ??= this.ResolveRequiredNode<VBoxContainer>("%EditorHost", "EditorHost", nameof(AttributeEditorRow));
        return _editorHost;
    }

    private VBoxContainer GetModifierHost()
    {
        _modifierHost ??= this.ResolveRequiredNode<VBoxContainer>("%ModifierHost", "ModifierHost", nameof(AttributeEditorRow));
        return _modifierHost;
    }

    /// <summary>
    /// 用新的子控件替换宿主中的旧内容。
    /// </summary>
    private static void ReplaceHostChild(VBoxContainer host, Control? child)
    {
        foreach (Node oldChild in host.GetChildren())
        {
            oldChild.QueueFree();
        }

        if (child != null)
        {
            host.AddChild(child);
        }
    }
}
