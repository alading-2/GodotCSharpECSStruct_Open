using Godot;

/// <summary>
/// 物理碰撞感应局部事件
/// 用于 Entity.Events 总线发布
/// </summary>
public static partial class GameEventType
{
    public static class Collision
    {
        /// <summary>碰撞进入事件</summary>
        public const string CollisionEntered = "collision:collision_entered";
        /// <summary>
        /// 碰撞进入事件数据
        /// CollisionType 标识碰撞来源类型（由 CollisionSensorComponent / CollisionComponent 写入）
        /// 消费者可据此过滤：ContactDamageComponent 只处理 HurtboxSensor 类型，DestroyOnCollision 只处理 VisualBody 类型
        /// </summary>
        public readonly record struct CollisionEnteredEventData(
            IEntity Source,
            Node2D Target,
            CollisionType CollisionType = CollisionType.Custom
            );

        /// <summary>碰撞离开事件</summary>
        public const string CollisionExited = "collision:collision_exited";
        /// <summary>碰撞离开事件数据</summary>
        public readonly record struct CollisionExitedEventData(
            IEntity Source,
            Node2D Target,
            CollisionType CollisionType = CollisionType.Custom
            );
    }
}
