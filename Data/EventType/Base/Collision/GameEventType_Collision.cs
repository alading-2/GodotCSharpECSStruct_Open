using Godot;

/// <summary>
/// 物理碰撞感应局部事件
/// 用于 Entity.Events 总线发布
/// </summary>
public static partial class GameEventType
{
    public static class Collision
    {
        /// <summary>碰撞进入事件（仅由 Area2D Entity 根节点触发，即视觉体碰撞）</summary>
        public const string CollisionEntered = "collision:collision_entered";
        /// <summary>
        /// 碰撞进入事件数据
        /// 由 CollisionComponent 在 Entity 根节点为 Area2D 时发出，供 EntityMovementComponent 等视觉体碰撞消费者使用
        /// </summary>
        public readonly record struct CollisionEnteredEventData(
            IEntity Source,
            Node2D Target
            );

        /// <summary>碰撞离开事件（仅由 Area2D Entity 根节点触发）</summary>
        public const string CollisionExited = "collision:collision_exited";
        /// <summary>碰撞离开事件数据</summary>
        public readonly record struct CollisionExitedEventData(
            IEntity Source,
            Node2D Target
            );

        /// <summary>受击区进入事件</summary>
        public const string HurtboxEntered = "collision:hurtbox_entered";
        /// <summary>
        /// 受击区进入事件数据
        /// Hurtbox 表示本方被触发的受击区节点；Target / TargetEntity 表示进入受击区的目标
        /// 该事件通常由 ContactDamageComponent 等业务组件消费。
        /// </summary>
        public readonly record struct HurtboxEnteredEventData(
            IEntity Source,
            Area2D Hurtbox,
            Node2D Target,
            IEntity? TargetEntity = null
            );

        /// <summary>受击区离开事件</summary>
        public const string HurtboxExited = "collision:hurtbox_exited";
        /// <summary>
        /// 受击区离开事件数据
        /// Hurtbox 表示本方被触发的受击区节点；Target / TargetEntity 表示离开受击区的目标
        /// 对象池碰撞实体复用时，应通过统一的激活时序避免伪 entered / exited 事件。
        /// </summary>
        public readonly record struct HurtboxExitedEventData(
            IEntity Source,
            Area2D Hurtbox,
            Node2D Target,
            IEntity? TargetEntity = null
            );
    }
}
