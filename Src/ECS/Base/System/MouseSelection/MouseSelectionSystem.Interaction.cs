using Godot;
using System.Collections.Generic;

/// <summary>
/// MouseSelection 的交互层。
/// <para>
/// 这个文件只负责鼠标输入状态机：左键按下、移动、松开，拖拽阈值判断，单击 / 框选收口，以及选择预览事件的广播。
/// </para>
/// <para>
/// 具体拾取、实体过滤和 UI 节点创建分别拆到 Picking 与 SelectionBoxUi 中，避免一个文件同时承担过多职责。
/// </para>
/// </summary>
public partial class MouseSelectionSystem
{
    /// <summary>
    /// 左键是否已经按下并处于本次选择交互中。
    /// <para>用于区分“正在按住鼠标”与“已经松开鼠标”的两个阶段。</para>
    /// </summary>
    private bool _isPointerDown;

    /// <summary>
    /// 当前是否已经进入拖拽框选态。
    /// <para>一旦进入拖拽态，松开鼠标时会走框选完成逻辑，而不是点选逻辑。</para>
    /// </summary>
    private bool _isDragging;

    /// <summary>
    /// 本次选择开始时的屏幕坐标。
    /// <para>用于后续判断拖拽距离，以及构造屏幕矩形与世界矩形。</para>
    /// </summary>
    private Vector2 _startScreenPosition;

    /// <summary>
    /// 处理未被 GUI 吃掉的输入事件
    /// _UnhandledInput 会在 GUI 控件优先处理后才收到事件
    /// <para>这里只处理世界单击 / 框选相关鼠标输入，其他输入直接放行给别的系统。</para>
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        // 技能瞄准等高优先级交互进行中时，普通鼠标选择不插入，避免调试选择打断正式输入状态。
        if (IsWorldSelectionBlocked())
        {
            ResetPointerState();
            return;
        }

        // 鼠标移动只在选择交互中用于判断是否进入拖拽框选态。
        if (@event is InputEventMouseMotion motionEvent)
        {
            HandleMouseMotion(motionEvent);
            return;
        }

        // 这里只处理鼠标左键，其他按键交给更高层或其他系统处理。
        if (@event is not InputEventMouseButton mouseEvent || mouseEvent.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        // 左键按下时，记录本次选择的起点位置；松开时，执行最终选择判定。
        if (mouseEvent.Pressed)
        {
            BeginPointerSelection(mouseEvent.Position);
            return;
        }

        CompletePointerSelection(mouseEvent.Position);
    }

    /// <summary>
    /// 记录一次鼠标按下开始的选择交互。
    /// <para>会重置拖拽状态，并保存起始屏幕坐标，供后续移动与松开事件使用。</para>
    /// </summary>
    private void BeginPointerSelection(Vector2 screenPosition)
    {
        // 进入一次新的鼠标交互周期：先记录按下状态，再重置拖拽标记与起点坐标。
        _isPointerDown = true;
        _isDragging = false;
        _startScreenPosition = screenPosition;
        HideSelectionBoxUi();
    }

    /// <summary>
    /// 处理鼠标移动并判断是否进入拖拽框选态。
    /// <para>只有鼠标按住并超过拖拽阈值后，才会更新框选预览并广播预览事件。</para>
    /// </summary>
    private void HandleMouseMotion(InputEventMouseMotion motionEvent)
    {
        // 没有按下鼠标时，移动事件不属于本次选择交互。
        if (!_isPointerDown)
        {
            return;
        }

        // 只有当鼠标移动距离超过阈值后，才把这次交互升级为框选。
        var distanceSquared = _startScreenPosition.DistanceSquaredTo(motionEvent.Position);
        if (!_isDragging && distanceSquared < DefaultDragThresholdPx * DefaultDragThresholdPx)
        {
            return;
        }

        // 进入拖拽框选态后，每次移动都广播一次预览，供 UI 层实时绘制选框。
        _isDragging = true;
        var screenRect = CreateRect(_startScreenPosition, motionEvent.Position);
        UpdateSelectionBoxUi(screenRect);
        GlobalEventBus.Global.Emit(
            GameEventType.Global.MouseSelectionPreviewUpdated,
            new GameEventType.Global.MouseSelectionPreviewUpdatedEventData(
                _startScreenPosition, // 拖拽起点屏幕坐标
                motionEvent.Position, // 当前屏幕坐标
                screenRect // 当前屏幕框选矩形
            )
        );
    }

    /// <summary>
    /// 处理鼠标松开时的最终收口。
    /// <para>根据当前是否已经进入拖拽态，分别走单击拾取、框选拾取或未命中逻辑。</para>
    /// </summary>
    private void CompletePointerSelection(Vector2 screenPosition)
    {
        // 如果没有处于鼠标按下状态，说明这次松开事件不是当前会话的一部分，直接忽略。
        if (!_isPointerDown)
        {
            return;
        }

        // 无论最终命中与否，都会结束本次鼠标按下状态。
        _isPointerDown = false;
        var screenRect = CreateRect(_startScreenPosition, screenPosition);
        var worldPosition = GetWorldPosition(screenPosition);

        // 已经进入拖拽态，则最终按框选逻辑收尾。
        if (_isDragging)
        {
            CompleteBoxSelection(screenPosition, worldPosition, screenRect);
            return;
        }

        // 未进入拖拽态的左键松开按点选逻辑处理。
        CompleteClickSelection(screenPosition, worldPosition, screenRect);
    }

    /// <summary>
    /// 处理单击选择收口。
    /// <para>优先走物理拾取，失败后再走距离兜底。</para>
    /// </summary>
    private void CompleteClickSelection(Vector2 screenPosition, Vector2 worldPosition, Rect2 screenRect)
    {
        // 先通过物理查询命中鼠标下的实体，这是最准确的拾取方式。
        var entity = FindEntityByPhysics(worldPosition, out var hitKind);
        if (entity == null)
        {
            // 如果物理拾取失败，再按距离在全局实体列表中找最近目标，保证调试选择对纯视觉实体也可用。
            entity = FindEntityByDistance(worldPosition, DefaultMaxDistance);
            hitKind = entity == null ? GameEventType.Global.MouseSelectionHitKind.None : GameEventType.Global.MouseSelectionHitKind.DistanceFallback;
        }

        // 仍然没有任何目标，则广播未命中事件，让业务方决定是否取消高亮或保持原状态。
        if (entity == null)
        {
            CompleteWithMiss(
                screenPosition, // 屏幕位置
                worldPosition, // 世界位置
                screenRect, // 选择矩形
                GameEventType.Global.MouseSelectionInteractionKind.Click // 单击交互
            );
            return;
        }

        // 点选只会命中一个主目标，因此把它包装成单元素集合后统一走完成流程。
        var entities = new List<IEntity> { entity };
        CompleteWithEntities(
            entities, // 命中的实体集合
            entity, // 默认主目标
            screenPosition, // 屏幕位置
            worldPosition, // 世界位置
            screenRect, // 选择矩形
            hitKind, // 命中来源
            GameEventType.Global.MouseSelectionInteractionKind.Click // 单击交互
        );
    }

    /// <summary>
    /// 处理框选选择收口。
    /// <para>先把屏幕矩形转换成世界矩形，再筛选落在范围内的实体并确定主目标。</para>
    /// </summary>
    private void CompleteBoxSelection(Vector2 screenPosition, Vector2 worldPosition, Rect2 screenRect)
    {
        // 先把屏幕矩形换算成世界矩形，再在世界空间中筛选位于矩形内的实体。
        var worldRect = CreateWorldRect(screenRect);
        var entities = FindEntitiesInWorldRect(worldRect);
        if (entities.Count == 0)
        {
            // 框选范围内一个目标都没有，则视作未命中。
            CompleteWithMiss(
                screenPosition, // 屏幕位置
                worldPosition, // 世界位置
                screenRect, // 选择矩形
                GameEventType.Global.MouseSelectionInteractionKind.Box // 框选交互
            );
            return;
        }

        // 框选可能命中多个实体，这里按“离框中心更近优先”做稳定排序，保证主目标可预测。
        var worldCenter = worldRect.Position + worldRect.Size * 0.5f;
        entities.Sort((left, right) =>
        {
            var leftDistance = GetEntityPosition(left).DistanceSquaredTo(worldCenter);
            var rightDistance = GetEntityPosition(right).DistanceSquaredTo(worldCenter);
            var distanceCompare = leftDistance.CompareTo(rightDistance);
            if (distanceCompare != 0)
            {
                return distanceCompare;
            }

            return string.Compare(GetEntityStableId(left), GetEntityStableId(right), System.StringComparison.Ordinal);
        });

        CompleteWithEntities(
            entities, // 命中的实体集合
            entities[0], // 默认主目标
            screenPosition, // 屏幕位置
            worldPosition, // 世界位置
            screenRect, // 选择矩形
            GameEventType.Global.MouseSelectionHitKind.BoxRect, // 命中来源
            GameEventType.Global.MouseSelectionInteractionKind.Box // 框选交互
        );
    }

    /// <summary>
    /// 广播一次成功的鼠标选择结果。
    /// <para>在事件发出前会先清空当前交互状态，避免回调过程中复用脏数据。</para>
    /// </summary>
    private void CompleteWithEntities(
        IReadOnlyList<IEntity> entities,
        IEntity primaryEntity,
        Vector2 screenPosition,
        Vector2 worldPosition,
        Rect2 screenRect,
        GameEventType.Global.MouseSelectionHitKind hitKind,
        GameEventType.Global.MouseSelectionInteractionKind interactionKind)
    {
        // 在广播结果前先清空鼠标交互状态，避免事件回调里再次进入脏状态。
        ResetPointerState();

        // 统一广播成功事件：业务层只关心命中的实体集合、主目标和命中来源即可。
        GlobalEventBus.Global.Emit(
            GameEventType.Global.MouseSelectionCompleted,
            new GameEventType.Global.MouseSelectionCompletedEventData(
                entities, // 命中的实体集合
                primaryEntity, // 默认主目标
                screenPosition, // 完成时的屏幕坐标
                worldPosition, // 完成时的世界坐标
                screenRect, // 本次点击或框选的屏幕矩形
                hitKind, // 命中来源
                interactionKind // 交互类型
            )
        );

        // 选中成功后消费输入，避免同一次点击继续进入 Godot 物理对象拾取等更低层处理。
        GetViewport().SetInputAsHandled();
    }

    /// <summary>
    /// 广播一次未命中的鼠标选择结果。
    /// <para>未命中不等于出错，而是让调用方决定是否维持当前选择状态。</para>
    /// </summary>
    private void CompleteWithMiss(
        Vector2 screenPosition,
        Vector2 worldPosition,
        Rect2 screenRect,
        GameEventType.Global.MouseSelectionInteractionKind interactionKind)
    {
        // 未命中也要清空鼠标交互状态，由监听方决定是否保持现有选择。
        ResetPointerState();

        // 广播未命中事件，由上层决定是关闭选择模式、保持当前选择，还是播放提示反馈。
        GlobalEventBus.Global.Emit(
            GameEventType.Global.MouseSelectionMissed,
            new GameEventType.Global.MouseSelectionMissedEventData(
                screenPosition, // 未命中时的屏幕坐标
                worldPosition, // 未命中时的世界坐标
                screenRect, // 本次点击或框选的屏幕矩形
                interactionKind // 交互类型
            )
        );
    }

    /// <summary>
    /// 判断当前是否应暂停普通世界选择。
    /// </summary>
    private static bool IsWorldSelectionBlocked()
    {
        // 技能异步瞄准是项目里已有的高优先级输入状态；此时普通鼠标选择不参与本次输入。
        return TargetingManager.IsTargeting;
    }

    /// <summary>
    /// 重置鼠标按下与拖拽状态。
    /// </summary>
    private void ResetPointerState()
    {
        _isPointerDown = false;
        _isDragging = false;
        _startScreenPosition = Vector2.Zero;
        HideSelectionBoxUi();
    }

    private Rect2 CreateWorldRect(Rect2 screenRect)
    {
        // 把屏幕矩形的两个对角点分别转成世界坐标，再重新组装成世界矩形。
        var start = GetWorldPosition(screenRect.Position);
        var end = GetWorldPosition(screenRect.Position + screenRect.Size);
        return CreateRect(start, end);
    }

    private static Rect2 CreateRect(Vector2 start, Vector2 end)
    {
        // 无论起点和终点的拖动方向如何，都统一生成左上角为 position、宽高为正值的矩形。
        var position = new Vector2(Mathf.Min(start.X, end.X), Mathf.Min(start.Y, end.Y));
        var size = new Vector2(Mathf.Abs(start.X - end.X), Mathf.Abs(start.Y - end.Y));
        return new Rect2(position, size);
    }

    private static Vector2 GetEntityPosition(IEntity entity)
    {
        // 框选排序需要比较实体位置；只有 Node2D 才具备世界坐标，其他类型视为零点。
        return entity is Node2D node2D ? node2D.GlobalPosition : Vector2.Zero;
    }

    // 获取实体的稳定 ID，用于排序和去重
    private static string GetEntityStableId(IEntity entity)
    {
        // 优先使用 Data 中的业务 Id，保证排序在跨运行期或重复实例下也更稳定。
        var id = entity.Data.Get<string>(DataKey.Id.Key);
        if (!string.IsNullOrEmpty(id))
        {
            return id;
        }

        // 如果没有业务 Id，则退化到实例 Id，至少能保证本次运行内稳定。
        return entity is Node node ? node.GetInstanceId().ToString() : string.Empty;
    }
}
