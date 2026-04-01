using Godot;

/// <summary>
/// 通用实体运动组件（策略调度器）- 统一处理所有节点类型（Node2D/Area2D/CharacterBody2D）的运动
/// <para>
/// 【核心职责】
/// 1. 监听 <c>MovementStarted</c> 事件切换运动策略，委托当前策略计算运动意图
/// 2. 统一执行位移：所有实体经 <c>VelocityResolver</c> 合成速度后应用位移（策略不直接操作 GlobalPosition）
/// 3. 自动维护运动统计数据（已用时间、已移距离），并在满足条件时触发完成事件
/// 4. 策略返回 Complete 表示运动完成，由调度器统一触发 OnMoveComplete
/// </para>
/// <para>
/// 【帧率选择（由策略 UsePhysicsProcess 声明，与节点类型无关）】
/// - UsePhysicsProcess=false（默认）：在 <c>_Process</c>（可变帧率）中执行
/// - UsePhysicsProcess=true：在 <c>_PhysicsProcess</c>（固定帧率）中执行
/// - 两条路径执行完全相同的逻辑：策略写 Velocity → VelocityResolver 合成 → 位移执行 → 朝向更新
/// - CharacterBody2D 实体额外调用 <c>MoveAndSlide()</c> 处理碰撞，其他节点用 <c>GlobalPosition +=</c>
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

    /// <summary>当前激活的运动策略实例（MoveMode 变化时新建）</summary>
    private IMovementStrategy? _currentStrategy;

    /// <summary>本次运动的输入参数（由 MovementStarted 事件传入，策略只读访问）</summary>
    private MovementParams _params;

    /// <summary>本次运动是否已完成（组件内部标志，防止重复触发）</summary>
    private bool _moveCompleted;

    /// <summary>当前帧显式朝向意图（由策略通过 MovementUpdateResult 返回；Zero = 回退到 Velocity 方向）</summary>
    private Vector2 _facingDirection;

    /// <summary>本次运动是否已触发过碰撞事件（CharacterBody2D 防止连续帧重复触发 MovementCollision）</summary>
    private bool _hasCollided;

    // ================= 节点类型缓存 =================

    /// <summary>CharacterBody2D 引用缓存（非 CharacterBody2D 实体时为 null）</summary>
    private CharacterBody2D? _body;

    /// <summary>视觉根节点，用于角色朝向翻转（有此节点时用 FlipH，否则用 Rotation）</summary>
    private AnimatedSprite2D? _visualRoot;

    // ================= IComponent 实现 =================

    /// <summary>
    /// 组件注册回调
    /// <para>初始化实体引用、数据容器引用，缓存节点类型信息。</para>
    /// </summary>
    /// <param name="entity">挂载本组件的实体节点</param>
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _currentStrategy = null;
        _params = default;
        _moveCompleted = false;
        _facingDirection = Vector2.Zero;

        _body = entity as CharacterBody2D;
        _visualRoot = entity.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");

        // 订阅运动开始/切换事件（业务方通过此事件触发临时运动切换）
        _entity.Events.On<GameEventType.Unit.MovementStartedEventData>(
            GameEventType.Unit.MovementStarted, OnMovementStarted);

        // 订阅碰撞事件（Area2D 路径；CharacterBody2D 路径在 ApplyMovement 中通过 MoveAndSlide 检测）
        _entity.Events.On<GameEventType.Collision.CollisionEnteredEventData>(
            GameEventType.Collision.CollisionEntered, OnCollisionDetected);

        // 根据 DefaultMoveMode 初始化默认策略（无 MovementParams，使用空参数）
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        if (defaultMode != MoveMode.None)
        {
            SwitchStrategy(new MovementParams { Mode = defaultMode });
        }

        _log.Debug($"[{entity.Name}] EntityMovementComponent 注册完成 (CharacterBody2D={_body != null}, 默认模式={defaultMode})");
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
        _params = default;
        _body = null;
        _visualRoot = null;
        _facingDirection = Vector2.Zero;
        _hasCollided = false;
    }

    // ================= Godot 生命周期 =================

    /// <summary>
    /// 判断当前是否应走物理帧路径（纯策略声明，与节点类型无关）
    /// </summary>
    private bool ShouldUsePhysicsProcess =>
        _currentStrategy?.UsePhysicsProcess == true;

    /// <summary>
    /// 可变帧率运动更新 - 策略 UsePhysicsProcess=false 时使用
    /// </summary>
    public override void _Process(double delta)
    {
        if (ShouldUsePhysicsProcess) return;
        UpdateMovement((float)delta);
    }

    /// <summary>
    /// 固定帧率运动更新 - 策略 UsePhysicsProcess=true 时使用
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (!ShouldUsePhysicsProcess) return;
        UpdateMovement((float)delta);
    }

    /// <summary>
    /// 统一运动更新入口（_Process 和 _PhysicsProcess 执行完全相同的逻辑）
    /// <para>流程：死亡检查 → 策略写 Velocity/可选 Facing → VelocityResolver 合成 → 位移执行 → 朝向更新</para>
    /// </summary>
    private void UpdateMovement(float delta)
    {
        if (_entity == null || _data == null) return;

        // 死亡期间停止移动
        if (_data.Get<bool>(DataKey.IsDead))
        {
            _data.Set(DataKey.Velocity, Vector2.Zero);
            _facingDirection = Vector2.Zero;
            if (_body != null)
            {
                _body.Velocity = Vector2.Zero;
                _body.MoveAndSlide();
            }
            return;
        }

        RunMovementLogic(delta);
        ApplyMovement(delta);
    }

    // ================= 策略切换（事件驱动） =================

    /// <summary>
    /// 处理运动开始/切换事件（业务方触发临时运动切换）
    /// <para>当前为默认策略时可直接切换；非默认策略需满足可打断条件。</para>
    /// </summary>
    private void OnMovementStarted(GameEventType.Unit.MovementStartedEventData evt)
    {
        if (_entity == null || _data == null) return;

        MoveMode currentMode = _data.Get<MoveMode>(DataKey.MoveMode);
        MoveMode defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        bool isCurrentDefaultMode = currentMode == defaultMode;

        if (!isCurrentDefaultMode && _currentStrategy != null && !_currentStrategy.CanBeInterrupted)
        {
            _log.Debug($"[{(_entity as Node)?.Name}] 当前策略不可打断，拒绝切换到 {evt.Mode}");
            return;
        }

        SwitchStrategy(evt.Params);
    }

    /// <summary>
    /// 统一策略切换逻辑：退出旧策略 → 完整重置运动状态 → 进入新策略
    /// <para>切换等同于强制结束当前运动，无论是中途切换还是运动结束后回退，均做完整清理。</para>
    /// </summary>
    private void SwitchStrategy(MovementParams newParams)
    {
        if (_entity == null || _data == null) return;

        MoveMode newMode = newParams.Mode;

        // 退出旧策略
        _currentStrategy?.OnExit(_entity, _data);
        _currentStrategy = null;

        // 重置运动状态
        ResetMovementState();

        // 存储新参数，并统一推导 ActionSpeed（三选二：ActionSpeed / MaxDistance+MaxDuration）
        _params = newParams with { ActionSpeed = MovementHelper.ResolveActionSpeed(newParams) };

        // 创建新策略实例并进入
        _currentStrategy = MovementStrategyRegistry.Create(newMode);
        _data.Set(DataKey.MoveMode, newMode);

        if (_currentStrategy != null)
        {
            _currentStrategy.OnEnter(_entity, _data, _params);
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
    /// <para>被 _Process 和 _PhysicsProcess 根据策略声明分别调用。</para>
    /// </summary>
    /// <param name="delta">帧间隔（秒）</param>
    private void RunMovementLogic(float delta)
    {
        if (_currentStrategy == null) return;
        if (_moveCompleted) return;

        // 委托策略计算运动意图（策略只写 DataKey.Velocity，不直接操作 GlobalPosition）
        MovementUpdateResult result = _currentStrategy.Update(_entity!, _data!, delta, _params);
        _facingDirection = result.HasFacingDirection ? result.FacingDirection : Vector2.Zero;

        // 策略主动完成
        if (result.IsCompleted)
        {
            OnMoveComplete();
            return;
        }

        // 累计统计 + 结束条件检查
        AccumulateTravel(result.Distance, delta);
        CheckEndConditions();
    }

    // ================= 位移执行 =================

    /// <summary>
    /// 统一位移执行：VelocityResolver 合成速度 → 应用位移 → 朝向更新
    /// <para>
    /// - CharacterBody2D：VelocityResolver → MoveAndSlide → 同步碰撞修正后的速度回 Data
    /// - 其他 Node2D：VelocityResolver → GlobalPosition += velocity * delta
    /// </para>
    /// </summary>
    private void ApplyMovement(float delta)
    {
        if (_entity is not Node2D node) return;

        // 分层速度合成（眩晕/击退/冲量对所有实体通用）
        Vector2 finalVelocity = VelocityResolver.Resolve(_data!);
        // 朝向优先取策略显式提供的方向；未提供时回退到策略意图速度（合成前）
        Vector2 intentVelocity = _data!.Get<Vector2>(DataKey.Velocity);
        Vector2 facingDirection = _facingDirection.LengthSquared() >= 0.001f ? _facingDirection : intentVelocity;

        if (_body != null)
        {
            // CharacterBody2D：MoveAndSlide 处理碰撞
            _body.Velocity = finalVelocity;
            _body.MoveAndSlide();
            // 同步碰撞修正后的实际速度回 Data
            _data.Set(DataKey.Velocity, _body.Velocity);

            // CharacterBody2D 碰撞检测：手动模拟 Area2D body_entered 的"首次进入"语义
            // （Area2D 路径由 OnCollisionDetected 订阅 CollisionEntered 事件处理，此处仅处理 CharacterBody2D）
            if (_body.GetSlideCollisionCount() > 0  // MoveAndSlide 本帧检测到物理碰撞
                && !_moveCompleted                   // 运动尚未因策略/时间/距离提前完成，避免重复触发
                && !_hasCollided)                    // 同一次运动内只响应第一次碰撞，防止持续推墙时每帧刷事件
            {
                var curMode = _data.Get<MoveMode>(DataKey.MoveMode);
                var defMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
                // 仅在非默认运动模式（如 FixedDirection 冲锋/发射）下响应；
                // 默认移动模式 AI/Player 移动撞墙属正常现象，不应触发 MovementCollision 事件
                if (curMode != defMode && curMode != MoveMode.None)
                {
                    _hasCollided = true; // 立即标记，确保后续帧不再重入
                    // 取第一个碰撞结果；Collider 可能已释放（地形 StaticBody2D 等），as Node2D 自动返回 null
                    var slideCollision = _body.GetSlideCollision(0);
                    // CharacterBody2D 无 layer 语义，传 Custom 作为占位类型；业务方可在事件数据中区分
                    HandleMovementCollision(slideCollision.GetCollider() as Node2D, CollisionType.Custom);
                }
            }
        }
        else
        {
            // Node2D/Area2D：直接位移
            if (finalVelocity.LengthSquared() >= 0.001f)
            {
                node.GlobalPosition += finalVelocity * delta;
            }
        }

        // 根据策略显式朝向或意图速度更新朝向（从 _params 读取 RotateToVelocity）
        MovementHelper.UpdateOrientation(_entity!, _params, facingDirection, _visualRoot);
    }

    // ================= 辅助工具方法 =================

    /// <summary>
    /// 完整重置运动状态（切换策略时调用，防止脏数据污染新策略）
    /// <para>重置所有一次性运行参数、统计数据及策略专用 Category 参数，保留 DefaultMoveMode 等持久配置。</para>
    /// </summary>
    private void ResetMovementState()
    {
        if (_data == null) return;

        // 重置跨系统共享的速度状态
        _data.Set(DataKey.Velocity, Vector2.Zero);
        _data.Set(DataKey.VelocityOverride, Vector2.Zero);
        _data.Set(DataKey.VelocityImpulse, Vector2.Zero);
        _facingDirection = Vector2.Zero;

        // 重置组件内部完成标志
        _moveCompleted = false;
        _hasCollided = false;
        // _params 由 SwitchStrategy 在调用此方法后立即替换，无需在此清零
    }

    /// <summary>
    /// 累计轨迹统计数据
    /// <para>更新 Data 中的运行时间和已行驶路程。</para>
    /// </summary>
    /// <param name="distance">本帧产生的实际位移幅度</param>
    /// <param name="delta">本帧时间间隔</param>
    private void AccumulateTravel(float distance, float delta)
    {
        _params.ElapsedTime += delta;
        _params.TraveledDistance += distance;
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

        if (_params.MaxDuration >= 0f && _params.ElapsedTime >= _params.MaxDuration)
        {
            OnMoveComplete();
            return;
        }

        if (_params.MaxDistance >= 0f && _params.TraveledDistance >= _params.MaxDistance)
        {
            OnMoveComplete();
        }
    }

    /// <summary>
    /// 触发运动完成流程
    /// <para>执行序列：记录完成模式 -> 标记完成 -> 发送事件 -> (可选)自动销毁 -> 回退默认模式。</para>
    /// <para>数据清理由后续调用的 SwitchStrategy 统一负责，此处只处理完成语义。</para>
    /// </summary>
    /// <param name="byCollision">
    /// true = 由碰撞触发的完成，额外检查 <c>DestroyOnCollision</c>；
    /// false（默认）= 时间/距离/策略主动触发，只检查 <c>DestroyOnComplete</c>。
    /// </param>
    private void OnMoveComplete(bool byCollision = false)
    {
        if (_data == null || _entity == null) return;

        var mode = _data.Get<MoveMode>(DataKey.MoveMode);

        // 标记完成（防止本帧重复触发）
        _moveCompleted = true;

        // 调用策略自然完成回调（OnEnd 仅自然完成触发；强制打断只走 OnExit）
        _currentStrategy?.OnEnd(_entity, _data, _params);

        // 发送 MovementCompleted 事件，携带本次运动统计数据
        _entity.Events.Emit(
            GameEventType.Unit.MovementCompleted,
            new GameEventType.Unit.MovementCompletedEventData(mode, _params.ElapsedTime, _params.TraveledDistance));

        _log.Debug($"[{(_entity as Node)?.Name}] 运动完成 Mode={mode} byCollision={byCollision}");

        // 如果配置了自动销毁（按完成 or 按碰撞），则通知 EntityManager 回收本实体
        if (_params.DestroyOnComplete || (byCollision && _params.DestroyOnCollision))
        {
            if (_entity is Node entityNode)
                EntityManager.Destroy(entityNode);
            return;
        }

        // 回退到默认运动模式
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        if (defaultMode != MoveMode.None && defaultMode != mode)
        {
            SwitchStrategy(new MovementParams { Mode = defaultMode });
        }
    }

    // ================= 碰撞处理 =================

    /// <summary>
    /// Area2D 碰撞进入回调（由 CollisionComponent 通过 Entity.Events 转发）
    /// <para>仅在非默认运动模式下响应，避免常驻 AI/Player 模式产生噪声事件。</para>
    /// </summary>
    private void OnCollisionDetected(GameEventType.Collision.CollisionEnteredEventData evt)
    {
        if (_entity == null || _data == null) return;
        if (_moveCompleted) return;

        var mode = _data.Get<MoveMode>(DataKey.MoveMode);
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        if (mode == defaultMode || mode == MoveMode.None) return;

        HandleMovementCollision(evt.Target, evt.CollisionType);
    }

    /// <summary>
    /// 运动碰撞统一处理：发布 MovementCollision 事件，若配置了 DestroyOnCollision 则触发完成流程。
    /// <para>
    /// 设计意图：碰撞事件与销毁逻辑解耦。
    /// 业务方（技能/子弹组件）订阅 <c>MovementCollision</c> 事件执行伤害/特效，
    /// <c>DestroyOnCollision=true</c> 仅控制实体回收，无需业务方关心生命周期。
    /// </para>
    /// </summary>
    /// <param name="target">碰撞目标节点（可能为 null，例如 CharacterBody2D 碰到地形时 Collider 已释放）</param>
    /// <param name="collisionType">碰撞类型（Area2D 路径由 CollisionComponent 识别；CharacterBody2D 路径传 Custom）</param>
    private void HandleMovementCollision(Node2D? target, CollisionType collisionType)
    {
        if (_moveCompleted) return;
        if (_entity == null || _data == null) return;

        var moveMode = _data.Get<MoveMode>(DataKey.MoveMode);

        _entity.Events.Emit(
            GameEventType.Unit.MovementCollision,
            new GameEventType.Unit.MovementCollisionEventData(moveMode, target, collisionType));

        _log.Debug($"[{(_entity as Node)?.Name}] 运动碰撞 Mode={moveMode}, Target={target?.Name}, CollisionType={collisionType}");

        if (_params.DestroyOnCollision)
        {
            OnMoveComplete(byCollision: true);
        }
    }
}
