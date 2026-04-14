using Godot;

/// <summary>
/// MouseSelection 的框选预览 UI 层。
/// <para>
/// 这个文件只负责内置框选矩形的创建、更新与隐藏，不参与目标拾取，也不负责输入状态机。
/// </para>
/// <para>
/// 它的职责是把拖拽过程中的屏幕矩形可视化，方便调试系统、测试系统或任意调用方共享同一套视觉反馈。
/// </para>
/// </summary>
public partial class MouseSelectionSystem
{
    /// <summary>框选预览层级；需要压在世界节点之上，但不参与任何输入处理。</summary>
    private const int SelectionBoxCanvasLayer = 10000;

    /// <summary>
    /// 框选预览 UI 所在的 CanvasLayer。
    /// <para>系统启动时只创建一次，后续拖拽只修改子控件属性，避免反复创建 UI 节点。</para>
    /// </summary>
    private CanvasLayer? _selectionBoxLayer;

    /// <summary>
    /// 框选预览框。
    /// <para>仅用于显示拖拽范围，不参与选择命中，也不拦截 UI 或世界输入。</para>
    /// </summary>
    private Panel? _selectionBoxUi;

    /// <summary>
    /// 确保框选预览 UI 已经创建。
    /// <para>该方法只在 UI 缺失或 Godot 实例失效时创建节点，正常框选过程中不会重复创建。</para>
    /// </summary>
    private void EnsureSelectionBoxUi()
    {
        // 如果 UI 已经存在且仍然有效，就不重复创建，避免拖拽过程中反复挂载节点。
        if (_selectionBoxUi != null && GodotObject.IsInstanceValid(_selectionBoxUi))
        {
            return;
        }

        // 先创建独立的 CanvasLayer，让框选 UI 始终绘制在世界内容之上。
        _selectionBoxLayer = new CanvasLayer
        {
            Name = "MouseSelectionBoxLayer",
            Layer = SelectionBoxCanvasLayer,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_selectionBoxLayer);

        // 用浅蓝色半透明填充 + 深一点的描边，形成通用且不抢眼的框选视觉反馈。
        var selectionBoxStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.65f, 1f, 0.12f),
            BorderColor = new Color(0.2f, 0.65f, 1f, 0.85f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2
        };

        // 实际显示的框选面板默认隐藏，只有拖拽超过阈值后才会被显示出来。
        _selectionBoxUi = new Panel
        {
            Name = "MouseSelectionBox",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ProcessMode = ProcessModeEnum.Always
        };

        // 把自定义样式挂到 panel 主题上，避免依赖场景外部样式资源。
        _selectionBoxUi.AddThemeStyleboxOverride("panel", selectionBoxStyle);
        _selectionBoxLayer.AddChild(_selectionBoxUi);
    }

    /// <summary>
    /// 更新框选预览框属性。
    /// <para>框选 UI 是常驻节点，这里只改位置、尺寸和显隐状态。</para>
    /// </summary>
    private void UpdateSelectionBoxUi(Rect2 screenRect)
    {
        // 先保证节点存在，再写入位置和尺寸，避免首次拖拽时出现空引用。
        EnsureSelectionBoxUi();
        if (_selectionBoxUi == null)
        {
            return;
        }

        // 直接使用屏幕矩形的 position/size 作为 UI 布局数据。
        _selectionBoxUi.Position = screenRect.Position;
        _selectionBoxUi.Size = screenRect.Size;
        _selectionBoxUi.Visible = true;
    }

    /// <summary>隐藏框选预览框。</summary>
    private void HideSelectionBoxUi()
    {
        // 节点不存在或已经失效时不做任何处理，避免退出场景时访问无效实例。
        if (_selectionBoxUi == null || !GodotObject.IsInstanceValid(_selectionBoxUi))
        {
            return;
        }

        // 隐藏预览框但不销毁节点，这样下次拖拽可以直接复用。
        _selectionBoxUi.Visible = false;
    }
}
