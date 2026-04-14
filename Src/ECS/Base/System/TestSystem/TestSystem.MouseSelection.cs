/// <summary>
/// TestSystem 对通用鼠标选择系统的事件适配。
/// </summary>
public partial class TestSystem
{
    /// <summary>
    /// 绑定通用鼠标选择系统的结果事件。
    /// </summary>
    private void BindMouseSelectionEvents()
    {
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
    }

    /// <summary>
    /// 解绑通用鼠标选择系统的结果事件。
    /// </summary>
    private void UnbindMouseSelectionEvents()
    {
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
    }

    /// <summary>
    /// 根据当前面板显隐和开关状态，同步鼠标选择请求。
    /// </summary>
    private void SyncMouseSelectionRequest()
    {
        if (_panelVisible && _selectionToggle.ButtonPressed)
        {
            GlobalEventBus.Global.Emit(
                GameEventType.Global.MouseSelectionStartRequested,
                new GameEventType.Global.MouseSelectionStartRequestedEventData(
                    RequesterId: nameof(TestSystem), // 请求方
                    Mode: GameEventType.Global.MouseSelectionMode.ClickOrDragBox, // 调试面板允许单击或框选
                    ApplyMode: GameEventType.Global.MouseSelectionApplyMode.Replace, // 替换当前选中实体
                    CollisionMask: CollisionLayers.All, // 调试模式允许查询全部物理层
                    TypeFilter: EntityType.None, // 调试模式不按实体类型过滤
                    TeamFilter: AbilityTargetTeamFilter.All, // 调试模式不按阵营过滤
                    CenterEntity: null, // 调试模式无阵营参照实体
                    AllowDistanceFallback: true, // 调试模式允许纯视觉实体距离兜底
                    MaxDistance: 56f, // 距离兜底半径
                    DragThresholdPx: 8f, // 单击模式保留默认拖拽阈值
                    ConsumeOnSuccess: true // 成功选中后消费本次点击
                )
            );
            return;
        }

        CancelMouseSelectionRequest();
    }

    /// <summary>
    /// 取消由 TestSystem 发起的鼠标选择请求。
    /// </summary>
    private static void CancelMouseSelectionRequest()
    {
        GlobalEventBus.Global.Emit(
            GameEventType.Global.MouseSelectionCancelRequested,
            new GameEventType.Global.MouseSelectionCancelRequestedEventData(nameof(TestSystem))
        );
    }

    /// <summary>
    /// 通用鼠标选择完成后，把结果回写到 TestSystem 当前选中实体。
    /// </summary>
    private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompletedEventData evt)
    {
        if (evt.RequesterId != nameof(TestSystem))
        {
            return;
        }

        SetSelectedEntity(evt.PrimaryEntity ?? (evt.Entities.Count > 0 ? evt.Entities[0] : null));
        SyncMouseSelectionRequest(); // 保持“选择实体”开关开启时可连续点选多个实体
    }

    /// <summary>
    /// 通用鼠标选择未命中时保持当前选中实体不变，并恢复连续选择请求。
    /// </summary>
    private void OnMouseSelectionMissed(GameEventType.Global.MouseSelectionMissedEventData evt)
    {
        if (evt.RequesterId != nameof(TestSystem))
        {
            return;
        }

        SyncMouseSelectionRequest(); // 保持“选择实体”开关开启时可连续点选多个实体
    }
}
