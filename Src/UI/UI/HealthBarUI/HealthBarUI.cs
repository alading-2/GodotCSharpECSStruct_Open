using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 头顶血条UI
/// 跟随Entity显示生命值，响应式更新
/// 
/// 核心功能：
/// 1. Bind到Entity后自动监听HP变化事件
/// 2. 每帧更新位置跟随Entity
/// 3. 支持平滑动画过渡
/// 4. 根据阵营/品阶自动改变颜色
/// </summary>
public partial class HealthBarUI : UIBase
{
    // 静态日志（用于系统级事件监听）
    private static readonly Log _log = new("HealthBarUI", LogLevel.Debug);

    private ProgressBar _healthBar = null!;

    // 平滑插值参数
    private float _displayedHpPercent;
    private const float SMOOTH_SPEED = 10f;

    [ModuleInitializer]
    public static void Initialize()
    {
        _log.Info("[ModuleInitializer] HealthBarUI.Initialize() 开始执行");

        // 订阅全局实体生成事件
        GlobalEventBus.Global.On<GameEventType.Global.EntitySpawnedEventData>(
            GameEventType.Global.EntitySpawned,
            OnUnitCreated
        );

        // 订阅全局单位销毁事件
        GlobalEventBus.Global.On<GameEventType.Unit.DestroyedEventData>(
            GameEventType.Unit.Destroyed,
            OnUnitDestroyed
        );

        _log.Info("[ModuleInitializer] HealthBarUI 全局事件监听已初始化完成");
    }

    // ============================================================
    // Godot 生命周期
    // ============================================================

    public override void _Ready()
    {
        // 关键：在这里获取子节点，因为 _Ready 保证了子节点已准备就绪
        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthBar.ShowPercentage = false;

        // 提高层级，防止被玩家遮挡
        ZIndex = 100;

        // 初始状态隐藏
        Visible = false;

        // 如果在 _Ready 之前就已经执行了 OnBind (此时 _entity 已赋值)，则补上样式更新
        if (_entity != null)
        {
            ApplyInitialState();
        }
    }

    /// <summary>
    /// 应用初始视觉状态
    /// 解决 Godot 时序问题：确保在 _healthBar 就绪后再执行视觉更新
    /// </summary>
    private void ApplyInitialState()
    {
        if (_healthBar == null) return;

        UpdateStyle();
        UpdateHealthBar();
        Visible = true;
    }

    public override void _Process(double delta)
    {
        if (_entity == null) return;

        // 1. 更新位置跟随Entity
        UpdatePosition();

        // 2. 平滑插值血量显示
        SmoothUpdateHealthBar((float)delta);
    }

    // ============================================================
    // 事件
    // ============================================================

    /// <summary>
    /// 单位创建事件处理：自动为新单位绑定血条
    /// </summary>
    private static void OnUnitCreated(GameEventType.Global.EntitySpawnedEventData evt)
    {
        var entity = evt.Entity;
        var entityTypeName = entity.GetType().Name;
        _log.Info($"[事件回调] OnUnitCreated 被调用: EntityType={entityTypeName}");

        // 只处理 IUnit 类型的实体
        if (entity is not IUnit)
        {
            _log.Debug($"[事件回调] 跳过非 IUnit 实体: {entityTypeName}");
            return;
        }

        // 只为有 HP 的单位创建血条
        if (!entity.Data.Has(DataKey.CurrentHp))
        {
            _log.Debug($"[事件回调] 跳过无 HP 的实体: {entityTypeName}");
            return;
        }

        _log.Info($"[事件回调] 准备为 {entityTypeName} 绑定血条");

        // 使用 UIManager 从对象池获取并绑定血条
        var healthBar = UIManager.BindUI<HealthBarUI>(entity, ObjectPoolNames.HealthBarPool);
        if (healthBar == null)
        {
            _log.Error($"绑定血条失败: Entity {entity.Data.Get<string>(DataKey.Id)}");
        }
        else
        {
            _log.Info($"[事件回调] 成功为 {entityTypeName} 绑定血条");
        }
    }

    /// <summary>
    /// 单位销毁事件处理：自动解绑并回收血条
    /// </summary>
    private static void OnUnitDestroyed(GameEventType.Unit.DestroyedEventData evt)
    {
        // 解绑所有 UI（包括血条）
        UIManager.UnbindAllUI(evt.Entity);
    }

    // ============================================================
    // UIBase 
    // ============================================================

    protected override void OnBind()
    {
        // 订阅HP变化事件
        _entity!.Events.On<GameEventType.Data.HealthChangedEventData>(
            GameEventType.Data.HealthChanged,
            OnHealthChanged
        );

        // 如果节点还未就绪（_healthBar 为空），ApplyInitialState 将在 _Ready 中被调用
        // 如果节点已就绪（对象池复用），则手动调用
        if (_healthBar != null)
        {
            ApplyInitialState();
        }
    }

    protected override void OnUnbind()
    {
        // EventBus会自动清理订阅，这里可选
        // 隐藏UI
        Visible = false;
    }

    public override void OnPoolReset()
    {
        base.OnPoolReset();
        _displayedHpPercent = 0;
        _healthBar.Value = 0;
        _healthBar.SelfModulate = Colors.White; // 重置颜色
    }

    // ============================================================
    // 事件处理
    // ============================================================

    private void OnHealthChanged(GameEventType.Data.HealthChangedEventData evt)
    {
        UpdateHealthBar();
    }

    // ============================================================
    // 私有方法
    // ============================================================

    /// <summary>
    /// 更新位置跟随Entity
    /// </summary>
    private void UpdatePosition()
    {
        if (_entity is not Node2D entityNode) return;

        // 将Entity的世界坐标转换为UI坐标
        var worldPos = entityNode.GlobalPosition;

        // 偏移到Entity头顶
        worldPos.Y -= _entity.Data.Get<float>(DataKey.HealthBarHeight);

        GlobalPosition = worldPos;
    }

    /// <summary>
    /// 更新样式（颜色）
    /// </summary>
    private void UpdateStyle()
    {
        if (_entity == null || _healthBar == null) return;

        // 获取阵营和等级
        // 注意：Data.Get需要确保类型匹配，如果没有设置这这些值，需要有默认处理
        // 假设 Data 系统对于枚举存储为 Enum 或 Int

        // 尝试获取 Team
        Team team = Team.Neutral;
        try { team = _entity.Data.Get<Team>(DataKey.Team); }
        catch { /* ignored, use default */ }

        // 尝试获取 UnitRank
        UnitRank rank = UnitRank.Normal;
        try { rank = _entity.Data.Get<UnitRank>(DataKey.UnitRank); }
        catch { /* ignored, use default */ }

        // 获取并应用颜色
        var color = GameTheme.GetEntityColor(team, rank);
        _healthBar.SelfModulate = color;
    }

    /// <summary>
    /// 更新血条显示（立即）
    /// </summary>
    private void UpdateHealthBar()
    {
        if (_entity == null || _healthBar == null) return;

        // 使用计算属性 HpPercent（0-100）
        var hpPercent = _entity.Data.Get<float>(DataKey.HpPercent);

        // 设置目标值（会通过平滑插值过渡）
        _displayedHpPercent = hpPercent;

        // 仅在首次或差异过大时直接设置
        if (Mathf.Abs((float)_healthBar.Value - hpPercent) > 50f)
        {
            _healthBar.Value = hpPercent;
        }
    }

    /// <summary>
    /// 平滑更新血条
    /// </summary>
    private void SmoothUpdateHealthBar(float delta)
    {
        var currentValue = (float)_healthBar.Value;
        var targetValue = _displayedHpPercent;

        // 平滑插值
        var newValue = Mathf.Lerp(currentValue, targetValue, SMOOTH_SPEED * delta);
        _healthBar.Value = newValue;

        // 距离很小时直接设置为目标值
        if (Mathf.Abs(newValue - targetValue) < 0.1f)
        {
            _healthBar.Value = targetValue;
        }
    }
}
