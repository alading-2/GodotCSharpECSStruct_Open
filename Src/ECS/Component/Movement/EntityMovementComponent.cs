using Godot;

/// <summary>
/// 通用实体运动组件（策略调度器）- 统一处理 Node2D/Area2D 与 CharacterBody2D 的运动
/// <para>
/// 【核心职责】
/// 1. 监听 <c>MovementStarted</c> 事件切换运动策略，委托当前策略计算运动意图
/// 2. 根据实体节点类型统一执行位移（策略不直接操作 GlobalPosition）
/// 3. 自动维护运动统计数据（已用时间、已移距离），并在满足条件时触发完成事件
/// 4. 策略返回 -1 表示运动完成，由调度器统一触发 OnMoveComplete
/// </para>
/// <para>
/// 【双路径执行（调度器负责）】
/// - Node2D / Area2D：<c>_Process</c> → 策略写 Velocity → 调度器执行 <c>GlobalPosition += Velocity * delta</c>
/// - CharacterBody2D：<c>_PhysicsProcess</c> → 策略写 Velocity → <c>VelocityResolver.Resolve()</c> + <c>MoveAndSlide()</c>
/// - 所有 12 种运动模式对两种节点类型通用，无需区分
/// </para>
/// <para>
/// 【策略切换方式】
/// - 默认模式：Entity 初始化时设置 <c>DataKey.DefaultMoveMode</c>，组件注册时自动进入
/// - 临时运动：业务方通过 <c>Entity.Events.Emit(MovementStarted, ...)</c> 触发切换
/// - 运动完成后自动回退到 <c>DataKey.DefaultMoveMode</c>
/// </para>
/// <para>
/// 【策略扩展方式】
/// 新增运动模式只需：1) 在 MoveMode 枚举添加值 2) 实现 IMovementStrategy 并用 [ModuleInitializer] 自注册
/// </para>
/// </summary>
public partial class EntityMovementComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(EntityMovementComponent));

    // ================= 组件内部状态 =================

    /// <summary>持有的实体引用，用于访问其 Data 容器和 EventBus</summary>
    private IEntity? _entity;

    /// <summary>数据容器缓存，减少每帧通过 _entity 重复获取的开销</summary>
    private Data? _data;

    /// <summary>当前激活的运动策略实例（MoveMode 变化时切换）</summary>
    private IMovementStrategy? _currentStrategy;

    // ================= CharacterBody2D 专用缓存 =================

    /// <summary>是否为物理碰撞体（CharacterBody2D），决定使用哪条运动路径</summary>
    private bool _isPhysicsBody;

    /// <summary>CharacterBody2D 引用缓存（非物理体时为 null）</summary>
    private CharacterBody2D? _body;

    /// <summary>视觉根节点，用于朝向翻转（物理体路径专用）</summary>
    private AnimatedSprite2D? _visualRoot;

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册回调
    /// <para>初始化实体引用、数据容器引用，并检测是否为物理体以决定运动路径。</para>
    /// </summary>
    /// <param name="entity">挂载本组件的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _currentStrategy = null;

        _isPhysicsBody = entity is CharacterBody2D;
        _body = entity as CharacterBody2D;
        _visualRoot = entity.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");

        // 订阅运动开始事件（业务方通过此事件触发临时运动切换）
        _entity.Events.On<GameEventType.Unit.MovementStartedEventData>(
            GameEventType.Unit.MovementStarted, OnMovementStarted);

        // 根据 DefaultMoveMode 初始化默认策略
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        if (defaultMode != MoveMode.None)
        {
            SwitchStrategy(defaultMode);
        }

        _log.Debug($"[{entity.Name}] EntityMovementComponent 注册完成 (物理体={_isPhysicsBody}, 默认模式={defaultMode})");
    }

    /// <inheritdoc/>
    public void OnComponentUnregistered()
    {
        // 退出当前策略
        if (_currentStrategy != null && _entity != null && _data != null)
        {
            _currentStrategy.OnExit(_entity, _data);
        }
        _entity = null;
        _data = null;
        _currentStrategy = null;
        _body = null;
        _visualRoot = null;
        _isPhysicsBody = false;
    }

    // ================= Godot 生命周期 =================

    /// <summary>
    /// 逐帧运动更新 - Node2D / Area2D 实体使用
    /// <para>
    /// 流程：RunMovementLogic（策略写入 Velocity）→ 调度器执行 GlobalPosition += Velocity * delta → 朝向旋转
    /// </para>
    /// </summary>
    /// <param name="delta">帧间隔（秒）</param>
    public override void _Process(double delta)
    {
        if (_isPhysicsBody) return;
        if (_entity == null || _data == null) return;

        RunMovementLogic((float)delta);

        // 调度器统一执行 Node2D / Area2D 位移（策略已将运动意图写入 DataKey.Velocity）
        ApplyNodeMovement((float)delta);
    }

    /// <summary>
    /// 物理帧运动更新 - 仅 CharacterBody2D 实体使用
    /// <para>
    /// 流程：RunMovementLogic（策略写入 Velocity）→ VelocityResolver 分层合成 → MoveAndSlide → 同步速度 → 朝向更新
    /// </para>
    /// </summary>
    /// <param name="delta">帧间隔（秒）</param>
    public override void _PhysicsProcess(double delta)
    {
        if (!_isPhysicsBody || _entity == null || _data == null || _body == null) return;

        // 死亡期间停止移动
        if (_data.Get<bool>(DataKey.IsDead))
        {
            _body.Velocity = Vector2.Zero;
            _body.MoveAndSlide();
            return;
        }

        RunMovementLogic((float)delta);

        // 分层速度合成 + 物理移动
        Vector2 finalVelocity = VelocityResolver.Resolve(_data);
        _body.Velocity = finalVelocity;
        _body.MoveAndSlide();

        // 同步物理修正后的实际速度回 Data
        _data.Set(DataKey.Velocity, _body.Velocity);

        // 根据速度方向更新朝向（统一工具）
        MovementHelper.UpdateOrientation(_entity, _data, _body.Velocity, _isPhysicsBody, _visualRoot);
    }

    // ================= 策略切换（事件驱动） =================

    /// <summary>
    /// 处理运动开始事件（业务方触发临时运动切换）
    /// <para>检查当前策略是否可打断，可打断则切换到新模式。</para>
    /// </summary>
    private void OnMovementStarted(GameEventType.Unit.MovementStartedEventData evt)
    {
        if (_entity == null || _data == null) return;

        // 当前策略不可打断时，拒绝切换
        if (_currentStrategy != null && !_currentStrategy.CanBeInterrupted)
        {
            _log.Debug($"[{(_entity as Node)?.Name}] 当前策略不可打断，拒绝切换到 {evt.Mode}");
            return;
        }

        SwitchStrategy(evt.Mode);
    }

    /// <summary>
    /// 统一策略切换逻辑：退出旧策略 → 重置统计 → 进入新策略
    /// </summary>
    private void SwitchStrategy(MoveMode newMode)
    {
        if (_entity == null || _data == null) return;

        // 退出旧策略
        _currentStrategy?.OnExit(_entity, _data);

        // 重置运行时统计数据
        _data.Set(DataKey.MoveElapsedTime, 0f);
        _data.Set(DataKey.MoveTraveledDistance, 0f);
        _data.Set(DataKey.MoveCompleted, false);

        // 查找并进入新策略
        _currentStrategy = MovementStrategyRegistry.Get(newMode);
        _data.Set(DataKey.MoveMode, newMode);

        if (_currentStrategy != null)
        {
            _currentStrategy.OnEnter(_entity, _data);
            _log.Debug($"[{(_entity as Node)?.Name}] 切换运动策略: {newMode}");
        }
        else
        {
            _log.Warn($"[{(_entity as Node)?.Name}] 未注册的运动模式: {newMode}");
        }
    }

    // ================= 核心逻辑 =================

    /// <summary>
    /// 每帧运动执行：委托当前策略计算运动意图，累计统计并检查结束条件
    /// <para>被 _Process（Node2D）和 _PhysicsProcess（CharacterBody2D）共同调用。</para>
    /// </summary>
    /// <param name="delta">帧间隔（秒）</param>
    private void RunMovementLogic(float delta)
    {
        if (_currentStrategy == null) return;
        if (_data!.Get<bool>(DataKey.MoveCompleted)) return;

        // 委托策略计算运动意图（策略只写 DataKey.Velocity，不直接操作 GlobalPosition）
        float displacement = _currentStrategy.Update(_entity!, _data!, delta);

        // 策略返回 -1 表示运动完成
        if (displacement < 0f)
        {
            OnMoveComplete();
            return;
        }

        // 累计统计 + 结束条件检查
        AccumulateTravel(displacement, delta);
        CheckEndConditions();
    }

    // ================= 位移执行 =================

    /// <summary>
    /// Node2D / Area2D 位移执行：读取策略写入的 Velocity，应用到 GlobalPosition
    /// <para>仅在 _Process 中调用（非物理体路径）。</para>
    /// </summary>
    /// <param name="delta">帧间隔（秒）</param>
    private void ApplyNodeMovement(float delta)
    {
        if (_entity is not Node2D node) return;

        Vector2 velocity = _data!.Get<Vector2>(DataKey.Velocity);
        if (velocity.LengthSquared() < 0.001f) return;

        node.GlobalPosition += velocity * delta;
        MovementHelper.UpdateOrientation(_entity!, _data!, velocity, _isPhysicsBody, _visualRoot);
    }

    // ================= 辅助工具方法 =================

    /// <summary>
    /// 累计轨迹统计数据
    /// <para>更新 Data 中的运行时间和已行驶路程。</para>
    /// </summary>
    /// <param name="distance">本帧产生的实际位移幅度</param>
    /// <param name="delta">本帧时间间隔</param>
    private void AccumulateTravel(float distance, float delta)
    {
        _data!.Set(DataKey.MoveElapsedTime, _data.Get<float>(DataKey.MoveElapsedTime) + delta);
        _data.Set(DataKey.MoveTraveledDistance, _data.Get<float>(DataKey.MoveTraveledDistance) + distance);
    }

    /// <summary>
    /// 检查结束条件
    /// <para>
    /// 支持两种维度（逻辑或关系）：
    /// 1. 时间限制：由 <c>MoveMaxDuration</c> 定义
    /// 2. 距离限制：由 <c>MoveMaxDistance</c> 定义
    /// 值 <c>-1</c> 代表该维度无限制。
    /// </para>
    /// </summary>
    private void CheckEndConditions()
    {
        if (_data == null) return;

        // 时间限制检查
        float maxDuration = _data.Get<float>(DataKey.MoveMaxDuration);
        if (maxDuration >= 0f && _data.Get<float>(DataKey.MoveElapsedTime) >= maxDuration)
        {
            OnMoveComplete();
            return;
        }

        // 距离限制检查
        float maxDistance = _data.Get<float>(DataKey.MoveMaxDistance);
        if (maxDistance >= 0f && _data.Get<float>(DataKey.MoveTraveledDistance) >= maxDistance)
        {
            OnMoveComplete();
        }
    }

    /// <summary>
    /// 触发运动完成流程
    /// <para>执行序列：策略退出 -> 标记位设置 -> 速度清零 -> 抛出全局/局部事件 -> (可选)自动销毁。</para>
    /// </summary>
    private void OnMoveComplete()
    {
        if (_data == null || _entity == null) return;

        var mode = _data.Get<MoveMode>(DataKey.MoveMode);

        // 通知当前策略退出（清空引用，避免 SwitchStrategy 回退时重复 OnExit）
        var completedStrategy = _currentStrategy;
        _currentStrategy = null;
        completedStrategy?.OnExit(_entity, _data);

        // 重置 Basic 类中的临时运行态/一次性参数，保留默认模式、锁定、加速度、朝向等持久配置
        _data.Set(DataKey.Velocity, Vector2.Zero);
        _data.Set(DataKey.VelocityOverride, Vector2.Zero);
        _data.Set(DataKey.VelocityImpulse, Vector2.Zero);
        _data.Set(DataKey.MoveMaxDuration, -1f);
        _data.Set(DataKey.MoveMaxDistance, -1f);
        _data.Set(DataKey.MoveElapsedTime, 0f);
        _data.Set(DataKey.MoveTraveledDistance, 0f);

        // MoveTargetNode 是未注册到 DataRegistry 的 Node 引用，需要手动清理
        _data.Remove(DataKey.MoveTargetNode);
        _data.Set(DataKey.MoveCompleted, true);

        // 批量重置所有策略专用 Category 的运动参数（对象池复用时防止脏数据残留）
        // Basic 类不整体重置（含 DefaultMoveMode 等配置键），由 SwitchStrategy 选择性重置运行时统计
        _data.ResetByCategories(
            DataCategory_Movement.Target,
            DataCategory_Movement.Orbit,
            DataCategory_Movement.Wave,
            DataCategory_Movement.Bezier,
            DataCategory_Movement.Boomerang,
            DataCategory_Movement.Attach);

        // 发布完成事件（在回退前发布，让监听方知道是哪个模式完成的）
        _entity.Events.Emit(
            GameEventType.Unit.MovementCompleted,
            new GameEventType.Unit.MovementCompletedEventData(mode));

        _log.Debug($"[{(_entity as Node)?.Name}] 运动完成 Mode={mode}");

        // 如果配置了自动销毁，则通知 EntityManager 回收本实体
        if (_data.Get<bool>(DataKey.MoveDestroyOnComplete))
        {
            if (_entity is Node entityNode)
                EntityManager.Destroy(entityNode);
            return;
        }

        // 回退到默认运动模式
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        if (defaultMode != MoveMode.None && defaultMode != mode)
        {
            SwitchStrategy(defaultMode);
        }
        else
        {
            _data.Set(DataKey.MoveMode, MoveMode.None);
        }
    }
}
