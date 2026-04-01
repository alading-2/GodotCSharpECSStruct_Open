using Godot;

/// <summary>
/// Unit 运动相关事件定义（由 EntityMovementComponent 发出）
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>运动开始/切换事件 Key</summary>
        public const string MovementStarted = "unit:movement:started";
        /// <summary>运动开始/切换事件数据</summary>
        public readonly record struct MovementStartedEventData(global::MoveMode Mode, global::MovementParams Params);

        /// <summary>运动完成事件 Key（时间/距离到达阈值，或到达目标点/目标实体）</summary>
        public const string MovementCompleted = "unit:movement:completed";
        /// <summary>运动完成事件数据</summary>
        public readonly record struct MovementCompletedEventData(
            global::MoveMode Mode,
            float ElapsedTime,
            float TraveledDistance);

        /// <summary>
        /// 运动中碰撞事件 Key
        /// <para>
        /// 仅在非默认运动模式（非 AIControlled/PlayerInput）发生碰撞时发布。
        /// Area2D 实体通过 CollisionComponent 的 CollisionEntered 信号触发；
        /// CharacterBody2D 实体通过 MoveAndSlide 首次碰撞触发。
        /// </para>
        /// <para>
        /// 订阅示例（技能组件发射炮弹后监听命中）：
        /// <code>
        /// bullet.Events.On&lt;GameEventType.Unit.MovementCollisionEventData&gt;(
        ///     GameEventType.Unit.MovementCollision, OnBulletHit);
        /// </code>
        /// </para>
        /// </summary>
        public const string MovementCollision = "unit:movement:collision";
        /// <summary>运动中碰撞事件数据</summary>
        public readonly record struct MovementCollisionEventData(
            global::MoveMode Mode,
            Node2D? Target,
            global::CollisionType CollisionType);
    }
}
