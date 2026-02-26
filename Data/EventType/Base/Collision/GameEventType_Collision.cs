using Godot;

/// <summary>
/// 物理碰撞感应局部事件
/// 用于 Entity.Events 总线发布
/// </summary>
public static partial class GameEventType
{
    public static class Collision
    {
        /// <summary>请求治疗（命令事件：外部 -> HealthComponent）</summary>
        public const string CollisionEntered = "collision:collision_entered";
        /// <summary>请求治疗事件数据</summary>
        public readonly record struct CollisionEnteredEventData(
            IEntity Source,
            Node2D Target
            );

        /// <summary>请求治疗（命令事件：外部 -> HealthComponent）</summary>
        public const string CollisionExited = "collision:collision_exited";
        /// <summary>请求治疗事件数据</summary>
        public readonly record struct CollisionExitedEventData(
            IEntity Source,
            Node2D Target
            );
    }
}
