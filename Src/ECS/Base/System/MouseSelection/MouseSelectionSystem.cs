using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 通用鼠标选择系统。
/// <para>
/// 通过全局事件驱动进入或退出鼠标选择态，供任意需要世界实体选择的系统复用。
/// </para>
/// <para>
/// 只有收到显式开始请求后才会监听鼠标点击；输入入口使用 _UnhandledInput，确保 GUI 控件先处理点击。
/// </para>
/// </summary>
public partial class MouseSelectionSystem : Node
{
    private static readonly Log _log = new(nameof(MouseSelectionSystem));

    /// <summary>默认的最大距离兜底半径；当物理拾取失败时，用它在实体列表里找最近目标。</summary>
    private const float DefaultMaxDistance = 56f;

    /// <summary>默认拖拽阈值；鼠标移动超过该像素值后，才认为本次点击是框选拖拽而不是普通点击。</summary>
    private const float DefaultDragThresholdPx = 8f;

    /// <summary>
    /// 当前激活的请求方 Id；为空表示当前没有鼠标选择会话。
    /// <para>多个系统可能共用同一个鼠标选择入口，因此必须记录占用方，避免互相抢占结果。</para>
    /// </summary>
    private string _activeRequesterId = string.Empty;

    /// <summary>
    /// 当前选择模式。
    /// <para>决定点击、拖拽框选，或者点击与拖拽都允许的交互方式。</para>
    /// </summary>
    private GameEventType.Global.MouseSelectionMode _mode = GameEventType.Global.MouseSelectionMode.ClickSingle;

    /// <summary>
    /// 当前选择结果应用模式。
    /// <para>用于告诉业务方，这次结果是替换旧选择、追加到旧选择，还是其他策略。</para>
    /// </summary>
    private GameEventType.Global.MouseSelectionApplyMode _applyMode = GameEventType.Global.MouseSelectionApplyMode.Replace;

    /// <summary>
    /// 当前物理拾取层掩码。
    /// <para>只会对这些碰撞层做点选检测，避免误点到无关节点。</para>
    /// </summary>
    private uint _collisionMask = CollisionLayers.SelectionPickable;

    /// <summary>
    /// 当前实体类型过滤。
    /// <para>用于限制本次选择只能命中指定类型的实体，例如只允许选中单位或可交互对象。</para>
    /// </summary>
    private EntityType _typeFilter = EntityType.None;

    /// <summary>
    /// 当前阵营过滤。
    /// <para>用于判断目标是自己、友军、敌军还是中立目标。</para>
    /// </summary>
    private AbilityTargetTeamFilter _teamFilter = AbilityTargetTeamFilter.All;

    /// <summary>
    /// 当前阵营过滤参照实体。
    /// <para>很多阵营判断都需要一个“中心实体”作为比较基准，比如玩家、施法者或测试对象。</para>
    /// </summary>
    private IEntity? _centerEntity;

    /// <summary>
    /// 当前是否允许距离兜底。
    /// <para>当鼠标点到空白区域时，是否仍允许在一定范围内找最近实体。</para>
    /// </summary>
    private bool _allowDistanceFallback;

    /// <summary>
    /// 当前距离兜底半径。
    /// <para>只有在启用距离兜底时才会生效。</para>
    /// </summary>
    private float _maxDistance = DefaultMaxDistance;

    /// <summary>
    /// 当前拖拽阈值，单位为屏幕像素。
    /// <para>该值越小，越容易被识别为框选；该值越大，越偏向于普通点击。</para>
    /// </summary>
    private float _dragThresholdPx = DefaultDragThresholdPx;

    /// <summary>
    /// 当前是否在选中成功后消费点击。
    /// <para>为 true 时，选择成功后会主动标记输入已处理，避免同一点击继续传递给其他系统。</para>
    /// </summary>
    private bool _consumeOnSuccess = true;

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

    /// <summary>检查鼠标选择事件的请求方 Id 是否存在。</summary>
    public bool HasActiveRequesterId => !string.IsNullOrWhiteSpace(_activeRequesterId);

    public override void _EnterTree()
    {
        // 进入场景树时注册全局事件监听；系统本身常驻，因此使用 Always 保证输入不会被暂停影响。
        ProcessMode = ProcessModeEnum.Always;
        // 创建一次框选预览 UI；后续框选过程只更新位置、尺寸与显隐状态。
        EnsureSelectionBoxUi();
        // 监听“开始选择”请求：只有业务方显式发起请求后，系统才开始接收鼠标输入。
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionStartRequestedEventData>(
            GameEventType.Global.MouseSelectionStartRequested,
            OnStartRequested
        );
        // 监听“取消选择”请求：用于业务方主动中断当前会话，例如切换界面或关闭面板。
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCancelRequestedEventData>(
            GameEventType.Global.MouseSelectionCancelRequested,
            OnCancelRequested
        );
    }

    public override void _ExitTree()
    {
        // 离开场景树前主动注销监听，避免节点销毁后事件仍回调到失效实例。
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionStartRequestedEventData>(
            GameEventType.Global.MouseSelectionStartRequested,
            OnStartRequested
        );
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCancelRequestedEventData>(
            GameEventType.Global.MouseSelectionCancelRequested,
            OnCancelRequested
        );
        // 清理会话状态，防止下一次进入场景树时沿用旧的请求方或拖拽状态。
        ResetSelectionState();
    }

    /// <summary>
    /// 处理鼠标选择开始请求
    /// </summary>
    private void OnStartRequested(GameEventType.Global.MouseSelectionStartRequestedEventData evt)
    {
        // 空请求方没有上下文，直接忽略，避免把空字符串误当成有效会话。
        if (string.IsNullOrWhiteSpace(evt.RequesterId))
        {
            _log.Warn("收到空 RequesterId 的鼠标选择请求，已忽略");
            return;
        }

        // 当前会话若已被别的请求方占用，则拒绝新的请求，避免两个系统同时抢同一套鼠标输入。
        if (HasActiveRequesterId && _activeRequesterId != evt.RequesterId)
        {
            _log.Warn($"鼠标选择已被 {_activeRequesterId} 占用，忽略来自 {evt.RequesterId} 的请求");
            return;
        }

        // 接管当前选择会话，并把请求携带的参数写入运行时状态。
        _activeRequesterId = evt.RequesterId;
        _mode = evt.Mode;
        _applyMode = evt.ApplyMode;
        _collisionMask = evt.CollisionMask;
        _typeFilter = evt.TypeFilter;
        _teamFilter = evt.TeamFilter;
        _centerEntity = evt.CenterEntity;
        _allowDistanceFallback = evt.AllowDistanceFallback;
        _maxDistance = evt.MaxDistance <= 0f ? DefaultMaxDistance : evt.MaxDistance;
        _dragThresholdPx = evt.DragThresholdPx <= 0f ? DefaultDragThresholdPx : evt.DragThresholdPx;
        _consumeOnSuccess = evt.ConsumeOnSuccess;

        // 新会话开始时，必须清空鼠标按下与拖拽状态，确保本次交互从干净状态启动。
        _isPointerDown = false;
        _isDragging = false;
    }

    /// <summary>
    /// 处理鼠标选择取消请求
    /// </summary>
    private void OnCancelRequested(GameEventType.Global.MouseSelectionCancelRequestedEventData evt)
    {
        // 如果没有请求方，取消请求没有意义，直接忽略。
        if (!HasActiveRequesterId)
        {
            return;
        }

        // 如果取消请求带了 RequesterId，则只允许取消自己的会话，避免误取消其他系统的选择任务。
        if (!string.IsNullOrWhiteSpace(evt.RequesterId) && evt.RequesterId != _activeRequesterId)
        {
            return;
        }

        // 统一清空当前选择状态，让系统回到空闲态。
        ResetSelectionState();
    }

    /// <summary>
    /// 重置选择状态
    /// </summary>
    private void ResetSelectionState()
    {
        // 把所有运行时参数恢复到默认值，确保下一次选择会话不会继承上一次的上下文。
        _activeRequesterId = string.Empty;
        _mode = GameEventType.Global.MouseSelectionMode.ClickSingle;
        _applyMode = GameEventType.Global.MouseSelectionApplyMode.Replace;
        _collisionMask = CollisionLayers.SelectionPickable;
        _typeFilter = EntityType.None;
        _teamFilter = AbilityTargetTeamFilter.All;
        _centerEntity = null;
        _allowDistanceFallback = false;
        _maxDistance = DefaultMaxDistance;
        _dragThresholdPx = DefaultDragThresholdPx;
        _consumeOnSuccess = true;
        _isPointerDown = false;
        _isDragging = false;
        _startScreenPosition = Vector2.Zero;
        HideSelectionBoxUi();
    }
}
