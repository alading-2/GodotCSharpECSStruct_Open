using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 通用鼠标选择系统。
/// <para>
/// 在全局 `_UnhandledInput` 阶段监听世界单击 / 框选，并把选择事实广播给任意关心的系统。
/// </para>
/// <para>
/// 输入入口使用 _UnhandledInput，确保 GUI 控件先处理点击；业务系统只监听结果，不再向本系统申请独占模式。
/// </para>
/// </summary>
public partial class MouseSelectionSystem : Node
{
    /// <summary>默认物理拾取层；调试选择需要能命中尚未挂 SelectionPickable 的实体，因此默认放宽。</summary>
    private const uint DefaultCollisionMask = CollisionLayers.All;

    /// <summary>默认的最大距离兜底半径；当物理拾取失败时，用它在实体列表里找最近目标。</summary>
    private const float DefaultMaxDistance = 56f;

    /// <summary>默认拖拽阈值；鼠标移动超过该像素值后，才认为本次点击是框选拖拽而不是普通点击。</summary>
    private const float DefaultDragThresholdPx = 8f;

    [ModuleInitializer]
    internal static void Initialize()
    {
        // 将鼠标选择系统注册为调试分组下的自动加载场景，保证运行期选择入口可直接使用。
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(MouseSelectionSystem),
            Scene = ResourceManagement.Load<PackedScene>(nameof(MouseSelectionSystem), ResourceCategory.System),
            Priority = AutoLoad.Priority.Debug,
            ParentPath = "Debug"
        });
    }

    public override void _EnterTree()
    {
        // 系统本身常驻，因此使用 Always 保证调试选择不会被暂停影响。
        ProcessMode = ProcessModeEnum.Always;
        // 创建一次框选预览 UI；后续框选过程只更新位置、尺寸与显隐状态。
        EnsureSelectionBoxUi();
    }

    public override void _ExitTree()
    {
        // 清理交互状态，防止下一次进入场景树时沿用旧的拖拽状态。
        ResetPointerState();
    }
}
