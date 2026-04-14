using Godot;
using System.Collections.Generic;

/// <summary>
/// Global 鼠标选择相关事件定义。
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>鼠标选择交互类型。</summary>
        public enum MouseSelectionInteractionKind
        {
            Click = 0,  // 单击
            Box = 1     // 框选
        }

        /// <summary>鼠标选择命中来源。</summary>
        public enum MouseSelectionHitKind
        {
            None = 0,           // 无命中
            PhysicsPoint = 1,   // 物理点选
            DistanceFallback = 2, // 距离最近
            BoxRect = 3         // 框选
        }

        /// <summary>鼠标选择完成。</summary>
        public const string MouseSelectionCompleted = "global:mouse_selection:completed";
        /// <summary>鼠标选择完成事件数据。</summary>
        public readonly record struct MouseSelectionCompletedEventData(
            IReadOnlyList<IEntity> Entities, // 选中的实体列表
            IEntity? PrimaryEntity, // 主选实体
            Vector2 ScreenPosition, // 屏幕位置
            Vector2 WorldPosition, // 世界位置
            Rect2 ScreenRect, // 选择矩形
            MouseSelectionHitKind HitKind, // 命中类型
            MouseSelectionInteractionKind InteractionKind // 交互类型
        );

        /// <summary>鼠标框选预览更新。</summary>
        public const string MouseSelectionPreviewUpdated = "global:mouse_selection:preview_updated";
        /// <summary>鼠标框选预览更新事件数据。</summary>
        public readonly record struct MouseSelectionPreviewUpdatedEventData(
            Vector2 StartScreenPosition, // 起始屏幕位置
            Vector2 CurrentScreenPosition, // 当前屏幕位置
            Rect2 ScreenRect // 框选矩形
        );

        /// <summary>鼠标选择未命中。</summary>
        public const string MouseSelectionMissed = "global:mouse_selection:missed";
        /// <summary>鼠标选择未命中事件数据。</summary>
        public readonly record struct MouseSelectionMissedEventData(
            Vector2 ScreenPosition, // 屏幕位置
            Vector2 WorldPosition, // 世界位置
            Rect2 ScreenRect, // 选择矩形
            MouseSelectionInteractionKind InteractionKind // 交互类型
        );
    }
}
