using Godot;
using System.Collections.Generic;

/// <summary>
/// Global 鼠标选择相关事件定义。
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>鼠标选择模式。</summary>
        public enum MouseSelectionMode
        {
            ClickSingle = 0,    // 点击单选
            DragBox = 1,        // 框选
            ClickOrDragBox = 2  // 点击或框选
        }

        /// <summary>选择结果应用模式。</summary>
        public enum MouseSelectionApplyMode
        {
            Replace = 0,    // 替换
            Add = 1,        // 追加
            Toggle = 2      // 切换
        }

        /// <summary>鼠标选择命中来源。</summary>
        public enum MouseSelectionHitKind
        {
            None = 0,           // 无命中
            PhysicsPoint = 1,   // 物理点选
            DistanceFallback = 2, // 距离最近
            BoxRect = 3         // 框选
        }

        /// <summary>请求开始鼠标选择。</summary>
        public const string MouseSelectionStartRequested = "global:mouse_selection:start_requested";
        /// <summary>请求开始鼠标选择事件数据。</summary>
        public readonly record struct MouseSelectionStartRequestedEventData(
            string RequesterId, // 请求者ID
            MouseSelectionMode Mode = MouseSelectionMode.ClickSingle,   // 选择模式
            MouseSelectionApplyMode ApplyMode = MouseSelectionApplyMode.Replace, // 应用模式
            uint CollisionMask = CollisionLayers.SelectionPickable, // 碰撞层掩码
            EntityType TypeFilter = EntityType.None, // 实体类型过滤
            AbilityTargetTeamFilter TeamFilter = AbilityTargetTeamFilter.All, // 队伍过滤
            IEntity? CenterEntity = null, // 中心实体
            bool AllowDistanceFallback = false, // 允许距离回退
            float MaxDistance = 56f, // 最大距离
            float DragThresholdPx = 8f, // 拖拽阈值
            bool ConsumeOnSuccess = true // 成功后是否消费
        );

        /// <summary>请求取消鼠标选择。</summary>
        public const string MouseSelectionCancelRequested = "global:mouse_selection:cancel_requested";
        /// <summary>请求取消鼠标选择事件数据。</summary>
        public readonly record struct MouseSelectionCancelRequestedEventData(string RequesterId);

        /// <summary>鼠标选择完成。</summary>
        public const string MouseSelectionCompleted = "global:mouse_selection:completed";
        /// <summary>鼠标选择完成事件数据。</summary>
        public readonly record struct MouseSelectionCompletedEventData(
            string RequesterId, // 请求者ID
            IReadOnlyList<IEntity> Entities, // 选中的实体列表
            IEntity? PrimaryEntity, // 主选实体
            Vector2 ScreenPosition, // 屏幕位置
            Vector2 WorldPosition, // 世界位置
            Rect2 ScreenRect, // 框选矩形
            MouseSelectionHitKind HitKind, // 命中类型
            MouseSelectionApplyMode ApplyMode // 应用模式
        );

        /// <summary>鼠标框选预览更新。</summary>
        public const string MouseSelectionPreviewUpdated = "global:mouse_selection:preview_updated";
        /// <summary>鼠标框选预览更新事件数据。</summary>
        public readonly record struct MouseSelectionPreviewUpdatedEventData(
            string RequesterId, // 请求者ID
            Vector2 StartScreenPosition, // 起始屏幕位置
            Vector2 CurrentScreenPosition, // 当前屏幕位置
            Rect2 ScreenRect // 框选矩形
        );

        /// <summary>鼠标选择未命中。</summary>
        public const string MouseSelectionMissed = "global:mouse_selection:missed";
        /// <summary>鼠标选择未命中事件数据。</summary>
        public readonly record struct MouseSelectionMissedEventData(
            string RequesterId, // 请求者ID
            Vector2 ScreenPosition, // 屏幕位置
            Vector2 WorldPosition, // 世界位置
            Rect2 ScreenRect, // 框选矩形
            MouseSelectionApplyMode ApplyMode // 应用模式
        );
    }
}
