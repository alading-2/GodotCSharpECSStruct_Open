using Godot;

/// <summary>
/// 运动策略接口 - 定义一种具体的运动轨迹实现
/// <para>
/// 每种运动模式（直线、追踪、环绕、贝塞尔等）实现此接口。
/// EntityMovementComponent 作为调度器，根据 MoveMode 选择对应策略执行。
/// </para>
/// <para>
/// 【策略职责边界（纯计算层）】
/// - 只负责计算本帧运动意图，将结果写入 <c>DataKey.Velocity</c>（速度向量）
/// - <b>禁止直接操作节点位置</b>（不得写 <c>GlobalPosition</c>），位移执行由调度器统一处理：
///   - Node2D / Area2D：调度器在 <c>_Process</c> 中执行 <c>GlobalPosition += Velocity * delta</c>
///   - CharacterBody2D：调度器在 <c>_PhysicsProcess</c> 中执行 <c>VelocityResolver + MoveAndSlide</c>
/// - 不负责结束条件判断和统计累计（由调度器统一处理）
/// - 不负责决定"去哪儿"（参数由上层写入 Data）
/// </para>
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// 当前策略是否可被外部打断切换（默认 true）
    /// <para>
    /// 若为 false，调度器在检测到 MoveMode 变化时会拒绝切换，保持当前策略继续执行。
    /// 典型用法：冲锋/击退等不可打断的运动，完成后由 OnMoveComplete 自动回退到 DefaultMoveMode。
    /// </para>
    /// </summary>
    bool CanBeInterrupted => true;

    /// <summary>
    /// 策略进入时回调（可选，用于初始化轨道角度等一次性计算）
    /// <para>在 MoveMode 首次生效或切换到本策略时调用一次</para>
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    void OnEnter(IEntity entity, Data data) { }

    /// <summary>
    /// 每帧运动更新（纯计算，将运动意图写入 <c>DataKey.Velocity</c>，禁止直接操作 GlobalPosition）
    /// <para>调度器根据实体节点类型（Node2D/Area2D vs CharacterBody2D）统一执行位移。</para>
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="delta">帧间隔（秒）</param>
    /// <returns>本帧估算位移量（用于调度器累计统计）；返回 -1 表示运动完成</returns>
    float Update(IEntity entity, Data data, float delta);

    /// <summary>
    /// 策略退出时回调（可选，用于清理临时状态）
    /// </summary>
    /// <param name="entity">运动实体</param>
    /// <param name="data">实体数据容器</param>
    void OnExit(IEntity entity, Data data) { }
}
