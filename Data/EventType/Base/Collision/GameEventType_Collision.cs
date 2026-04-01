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
        /// CollisionType 标识被触发的碰撞节点类型（由 CollisionComponent 统一写入，绑定时由节点自身 layer/mask 反查确定）
        /// 消费者在 On 订阅时通过 CollisionType 过滤：ContactDamageComponent 只处理 HurtboxSensor 类型，EntityMovementComponent 只处理 VisualBody 类型
        /// </summary>
        public readonly record struct CollisionEnteredEventData(
            IEntity Source,
            Node2D Target,
            CollisionType CollisionType = CollisionType.None
            );

        /// <summary>碰撞离开事件</summary>
        public const string CollisionExited = "collision:collision_exited";
        /// <summary>碰撞离开事件数据</summary>
        public readonly record struct CollisionExitedEventData(
            IEntity Source,
            Node2D Target,
            CollisionType CollisionType = CollisionType.None
            );
    }
}
