using Godot;
using System;

/// <summary>
/// 属性测试的单行编辑器。
/// <para>
/// 行布局、基础样式和子编辑器全部由场景资产提供，这里只做元数据绑定。
/// </para>
/// </summary>
public partial class AttributeEditorRow : VBoxContainer
{
    [Export] private PackedScene? _booleanEditorScene;
    [Export] private PackedScene? _enumEditorScene;
    [Export] private PackedScene? _numericEditorScene;
    [Export] private PackedScene? _stringEditorScene;
    [Export] private PackedScene? _modifierEditorScene;

    private Label _titleLabel = null!;
    private Label _keyLabel = null!;
    private VBoxContainer _editorHost = null!;
    private VBoxContainer _modifierHost = null!;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("TitleRow/TitleLabel");
        _keyLabel = GetNode<Label>("TitleRow/KeyLabel");
        _editorHost = GetNode<VBoxContainer>("EditorHost");
        _modifierHost = GetNode<VBoxContainer>("ModifierHost");
    }

    /// <summary>
    /// 绑定一行属性编辑器。
    /// </summary>
    internal void Bind(
        IEntity entity,
        DataMeta meta,
        FeatureDebugService featureDebugService,
        Action<DataMeta, object> onValueCommitted,
        Action<string> onStatusChanged,
        Action onRefreshRequested)
    {
        _titleLabel.Text = string.IsNullOrWhiteSpace(meta.DisplayName) ? meta.Key : meta.DisplayName;
        _keyLabel.Text = meta.Key;

        ClearHostChildren(_editorHost);
        ClearHostChildren(_modifierHost);

        BindValueEditor(entity, meta, onValueCommitted);
        BindModifierEditor(entity, meta, featureDebugService, onStatusChanged, onRefreshRequested);
    }

    private void BindValueEditor(IEntity entity, DataMeta meta, Action<DataMeta, object> onValueCommitted)
    {
        if (meta.IsBoolean)
        {
            var editor = InstantiateScene<AttributeBooleanEditor>(_booleanEditorScene, nameof(AttributeBooleanEditor));
            editor.Bind(entity, meta, value => onValueCommitted(meta, value));
            _editorHost.AddChild(editor);
            return;
        }

        if (meta.IsEnum || meta.HasOptions)
        {
            var editor = InstantiateScene<AttributeEnumEditor>(_enumEditorScene, nameof(AttributeEnumEditor));
            editor.Bind(entity, meta, value => onValueCommitted(meta, value));
            _editorHost.AddChild(editor);
            return;
        }

        if (meta.IsNumeric)
        {
            var editor = InstantiateScene<AttributeNumericEditor>(_numericEditorScene, nameof(AttributeNumericEditor));
            editor.Bind(entity, meta, value => onValueCommitted(meta, value));
            _editorHost.AddChild(editor);
            return;
        }

        if (meta.IsString)
        {
            var editor = InstantiateScene<AttributeStringEditor>(_stringEditorScene, nameof(AttributeStringEditor));
            editor.Bind(entity, meta, value => onValueCommitted(meta, value));
            _editorHost.AddChild(editor);
        }
    }

    private void BindModifierEditor(
        IEntity entity,
        DataMeta meta,
        FeatureDebugService featureDebugService,
        Action<string> onStatusChanged,
        Action onRefreshRequested)
    {
        if (!SupportsTemporaryModifier(meta))
        {
            return;
        }

        var editor = InstantiateScene<AttributeModifierEditor>(_modifierEditorScene, nameof(AttributeModifierEditor));
        editor.Bind(
            entity, // owner
            meta, // meta
            featureDebugService, // featureDebugService
            onStatusChanged, // onStatusChanged
            onRefreshRequested // onRefreshRequested
        );
        _modifierHost.AddChild(editor);
    }

    private static bool SupportsTemporaryModifier(DataMeta meta)
    {
        return meta.IsNumeric && meta.SupportModifiers == true && !meta.IsComputed;
    }

    private static void ClearHostChildren(Node host)
    {
        foreach (var child in host.GetChildren())
        {
            child.QueueFree();
        }
    }

    private static T InstantiateScene<T>(PackedScene? scene, string sceneName) where T : Node
    {
        if (scene == null)
        {
            throw new InvalidOperationException($"属性编辑器场景未配置: {sceneName}");
        }

        var instance = scene.Instantiate<T>();
        if (instance == null)
        {
            throw new InvalidOperationException($"属性编辑器场景实例化失败: {sceneName}");
        }

        return instance;
    }
}
