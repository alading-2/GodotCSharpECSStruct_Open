using Godot;

/// <summary>
/// AI 组件 - 持有行为树运行器
/// <para>
/// 核心职责：
/// - 每帧构建 AIContext 并驱动行为树 Tick
/// - 管理行为树生命周期
/// </para>
/// <para>
/// 使用方式：
/// 1. 在 Entity 场景中挂载此组件
/// 2. 调用 SetBehaviorTree() 设置行为树
/// 3. 组件在 _Process 中自动 Tick
/// </para>
/// </summary>
public partial class AIComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(AIComponent));

    // ================= 组件依赖 =================

    private IEntity? _entity;
    private Data? _data;

    // ================= 行为树 =================

    /// <summary>行为树运行器</summary>
    public BehaviorTreeRunner Runner { get; private set; }

    // ================= 运行时上下文（避免每帧 new） =================

    private readonly AIContext _context = new();

    // ================= IComponent 实现 =================

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;

        // 记录出生位置（用于巡逻基准点）
        if (_entity is CharacterBody2D body)
        {
            _data.Set(DataKey.SpawnPosition, body.GlobalPosition);
        }

        // 默认启用 AI
        if (!_data.Has(DataKey.AIEnabled))
        {
            _data.Set(DataKey.AIEnabled, true);
        }

        _log.Debug($"[{entity.Name}] AI 组件注册完成");
    }

    /// <summary>
    /// 组件被注销时的清理（释放所有缓存的引用）
    /// </summary>
    public void OnComponentUnregistered()
    {
        Runner?.Reset();

        _entity = null;
        _data = null;
    }

    // ================= 公开 API =================

    /// <summary>
    /// 设置行为树（通常在 Entity 初始化时调用）
    /// </summary>
    public void SetBehaviorTree(BehaviorNode root)
    {
        if (Runner == null)
            Runner = new BehaviorTreeRunner(root);
        else
            Runner.SetTree(root);

        _log.Debug($"[{(_entity as Node)?.Name}] 行为树已设置: {root.NodeName}");
    }

    // ================= Godot 生命周期 =================

    /// <summary>
    /// 每帧执行 AI 逻辑，驱动行为树 Tick
    /// </summary>
    public override void _Process(double delta)
    {
        // 前置检查
        if (Runner == null) return;
        if (_data == null) return;
        if (!_data.Get<bool>(DataKey.AIEnabled, true)) return;

        // 检查生命周期状态（死亡/濒死不执行 AI）
        var lifecycleState = _data.Get<string>(DataKey.LifecycleState, "");
        if (lifecycleState == nameof(LifecycleState.Dead) ||
            lifecycleState == nameof(LifecycleState.Dying))
        {
            return;
        }

        // 构建上下文（复用对象，避免 GC）
        _context.Entity = _entity;
        _context.Data = _data;
        _context.Events = _entity.Events;
        _context.Body = _entity as CharacterBody2D;
        _context.DeltaTime = (float)delta;

        // 执行行为树
        Runner.Tick(_context);
    }
}
