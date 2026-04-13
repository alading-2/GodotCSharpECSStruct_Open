/// <summary>
/// 运动策略接口，定义一种具体的运动轨迹计算方式。
/// <para>
/// 每种 <c>MoveMode</c> 都对应一个 <c>IMovementStrategy</c> 实现。调度器 <c>EntityMovementComponent</c>
/// 在切换运动模式时通过工厂创建新实例，策略可安全持有私有运行时状态字段。
/// </para>
/// <para>
/// 职责边界：
/// 1. 只负责计算本帧的运动意图，并把结果写入 <c>DataKey.Velocity</c>
/// 2. 不直接修改 <c>GlobalPosition</c>，真正位移始终由调度器统一执行
/// 3. 不负责通用结束条件与累计统计，时间和距离限制统一由组件处理
/// 4. 如需将“面向方向”与“位移方向”解耦，可通过 <c>MovementUpdateResult</c> 显式返回本帧朝向意图
/// 5. 输入参数通过 <c>in MovementParams</c> 传入，运行时状态存于策略私有字段
/// </para>
/// </summary>
public interface IMovementStrategy
{
    /// <summary>
    /// 当前策略是否允许被新的 <c>MovementStarted</c> 打断，默认值为 <c>true</c>。
    /// <para>
    /// 当返回 <c>false</c> 时，组件会拒绝切换到新模式，直到当前策略自然完成。
    /// 常用于冲锋、击退、关键位移技能等不可中断的运动。
    /// </para>
    /// </summary>
    bool CanBeInterrupted => true;

    /// <summary>
    /// 策略是否要求在 <c>_PhysicsProcess</c> 中执行，默认值为 <c>false</c>。
    /// <para>
    /// 返回 <c>true</c> 通常表示该策略依赖固定帧率，例如玩家输入或 AI 持续移动。
    /// 返回 <c>false</c> 适合纯轨迹型运动，例如直线、曲线、环绕。
    /// </para>
    /// </summary>
    bool UsePhysicsProcess => false;

    /// <summary>
    /// 进入策略时调用一次，可选。
    /// 适合做一次性初始化：记录起点、计算初始角度、构建运行时缓存（存于策略私有字段）。
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="data">实体数据容器（只读跨系统属性如 Velocity、MoveSpeed）</param>
    /// <param name="params">本次运动上下文参数</param>
    void OnEnter(IEntity entity, Data data, MovementParams @params) { }

    /// <summary>
    /// 每帧更新一次运动意图，将结果写入 <c>DataKey.Velocity</c>，禁止直接修改节点位置。
    /// 如当前轨迹的视觉朝向不应直接取 <c>Velocity</c>（例如正弦波/曲线路径的切线方向），
    /// 可通过返回值显式附带本帧朝向意图。
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="delta">本帧时间（秒）</param>
    /// <param name="params">本次运动上下文参数（包含 ElapsedTime/TraveledDistance 统计）</param>
    /// <returns>
    /// <c>Continue(distance)</c> 继续运动，把本帧估算位移距离交给组件累计统计；
    /// <c>Complete()</c> 策略主动完成，组件进入统一完成流程。
    /// </returns>
    MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params);

    /// <summary>
    /// 策略停止时调用一次，可选。
    /// <para>
    /// 无论是自然完成、碰撞完成、被新运动打断还是组件注销，都会走统一的停止回调。
    /// 典型用途：清理外部状态、根据停止原因决定是否结算命中或返回效果。
    /// </para>
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="context">停止上下文，包含停止原因、最终统计、碰撞目标与下一模式等信息</param>
    void OnStop(IEntity entity, Data data, in MovementStopContext context) { }
}
