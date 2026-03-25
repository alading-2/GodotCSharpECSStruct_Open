using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 附着跟随策略 - 每帧将实体位置同步到宿主位置 + 偏移
/// <para>
/// 读取参数：
/// - <c>DataKey.MoveTargetNode</c>：宿主 Node2D 引用
/// - <c>DataKey.EffectOffset</c>：附着偏移向量
/// </para>
/// <para>
/// 行为：
/// - 每帧设置 GlobalPosition = 宿主.GlobalPosition + Offset
/// - 宿主无效（null 或已销毁）时返回 -1 触发运动完成
/// - 不产生实际位移量（返回 0），不参与时间/距离结束条件
/// </para>
/// </summary>
public class AttachToHostStrategy : IMovementStrategy
{
    /// <summary>
    /// 注册附着跟随策略到全局注册表
    /// </summary>
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.AttachToHost, new AttachToHostStrategy());
    }

    /// <summary>
    /// 进入策略时回调（附着模式无需初始化）
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    public void OnEnter(IEntity entity, Data data)
    {
        // 附着模式无需初始化，宿主引用由 EffectComponent/EffectTool 在生成时写入
    }

    /// <summary>
    /// 每帧跟随宿主位置并应用偏移
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="delta">帧间隔（秒）</param>
    /// <returns>
    /// 0：本帧不计入位移统计；
    /// -1：宿主失效，触发运动完成
    /// </returns>
    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D selfNode) return -1f;

        var hostNode = data.Get<Node2D>(DataKey.MoveTargetNode);

        // 宿主无效 → 运动完成
        if (hostNode == null || !GodotObject.IsInstanceValid(hostNode))
        {
            return -1f;
        }

        // 附着跟随：直接设绝对位置（无需碰撞，不走 Velocity 路径）
        var offset = data.Get<Vector2>(DataKey.EffectOffset);
        // 改为计算速度让调度器执行
        Vector2 toTarget = hostNode.GlobalPosition + offset - selfNode.GlobalPosition;
        Vector2 velocity = toTarget / Mathf.Max(delta, 0.001f);
        data.Set(DataKey.Velocity, velocity);

        // 附着跟随不产生位移统计
        return 0f;
    }

    /// <summary>
    /// 退出策略时回调（无状态，无需清理）
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    public void OnExit(IEntity entity, Data data)
    {
        // 无实例级状态需要清理
    }
}
