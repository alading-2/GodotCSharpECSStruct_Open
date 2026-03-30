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
    /// 策略自然完成时调用一次（区别于 <c>OnExit</c>：<c>OnEnd</c> 仅在运动正常完成时触发；
    /// 被强制打断切换时只触发 <c>OnExit</c>，不触发 <c>OnEnd</c>）。
    /// <para>
    /// 典型用途：回旋镖返回后触发拾取效果、追及结束后触发攻击判定。
    /// <c>@params</c> 携带本次运动最终统计（<c>ElapsedTime</c>、<c>TraveledDistance</c>）。
    /// </para>
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="data">实体数据容器</param>
    /// <param name="params">本次运动上下文参数（含最终 ElapsedTime / TraveledDistance）</param>
    void OnEnd(IEntity entity, Data data, MovementParams @params) { }

    /// <summary>
    /// 退出策略时调用一次，可选（强制打断和自然完成均会触发）。
    /// 策略私有状态随实例销毁自动清理，此处只做必要的外部清理。
    /// </summary>
    /// <param name="entity">当前运动实体</param>
    /// <param name="data">实体数据容器</param>
    void OnExit(IEntity entity, Data data) { }
}
