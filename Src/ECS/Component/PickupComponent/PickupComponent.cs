using System;
using Godot;

/// <summary>
/// 拾取组件 - 实现物品的拾取检测和磁吸效果
/// <para>
/// 继承自 Area2D 以利用引擎的碰撞检测。
/// 所有数据从 Entity.Data 读取。
/// </para>
/// </summary>
public partial class PickupComponent : Area2D, IComponent
{
    private static readonly Log Log = new("PickupComponent");

    // ================= IComponent 实现 =================

    private Data? _data;
    private Node2D? _owner;

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }

        if (entity is Node2D node2D)
        {
            _owner = node2D;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理引用和事件
        PickedUp = null;
        Collector = null;
        _data = null;
        _owner = null;
    }

    // ================= 事件 =================

    /// <summary>
    /// 当物品被拾取时触发
    /// </summary>
    public event Action<Node2D>? PickedUp;

    // ================= 运行时状态 =================

    /// <summary>
    /// 获取磁吸速度
    /// </summary>
    public float MagnetSpeed => _data?.Get<float>(DataKey.MagnetSpeed, 300f) ?? 300f;

    /// <summary>
    /// 获取是否启用磁吸效果
    /// </summary>
    public bool MagnetEnabled
    {
        get => _data?.Get<bool>(DataKey.MagnetEnabled, false) ?? false;
        private set => _data?.Set(DataKey.MagnetEnabled, value);
    }

    /// <summary>
    /// 磁吸目标（采集者）
    /// </summary>
    public Node2D? Collector { get; private set; }

    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        // 懒加载：如果 OnComponentRegistered 未被调用
        if (_data == null)
        {
            _data = EntityManager.GetEntityData(this);
        }

        if (_owner == null)
        {
            var entity = EntityManager.GetEntityByComponent(this);
            if (entity is Node2D node2D)
            {
                _owner = node2D;
            }
        }

        if (_data == null || _owner == null)
        {
            Log.Error("无法获取 Data 容器或所属实体");
            return;
        }

        // 连接信号
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        Log.Debug($"就绪: 磁吸速度={MagnetSpeed}, 磁吸启用={MagnetEnabled}");
    }

    public override void _ExitTree()
    {
        // 断开信号连接
        AreaEntered -= OnAreaEntered;
        BodyEntered -= OnBodyEntered;

        // 清理事件和引用
        PickedUp = null;
        Collector = null;
        _data = null;
        _owner = null;

        Log.Trace("拾取组件退出场景树");
    }

    public override void _PhysicsProcess(double delta)
    {
        // 处理磁吸效果
        if (MagnetEnabled && Collector != null && IsInstanceValid(Collector))
        {
            MoveTowardCollector((float)delta);
        }
    }

    // ================= 公开方法 =================

    /// <summary>
    /// 启用磁吸效果
    /// </summary>
    public void EnableMagnet(Node2D collector)
    {
        Collector = collector;
        MagnetEnabled = true;
        Log.Debug($"磁吸已开启，目标采集者: {collector.Name}");
    }

    /// <summary>
    /// 禁用磁吸效果
    /// </summary>
    public void DisableMagnet()
    {
        MagnetEnabled = false;
        Collector = null;
        Log.Debug("磁吸已禁用");
    }

    // ================= 私有方法 =================

    /// <summary>
    /// 向采集者移动（磁吸效果）
    /// </summary>
    private void MoveTowardCollector(float delta)
    {
        if (_owner == null || Collector == null)
        {
            return;
        }

        Vector2 direction = Collector.GlobalPosition - _owner.GlobalPosition;
        float distance = direction.Length();

        if (distance < 1f)
        {
            // 已到达
            return;
        }

        // 移动向采集者
        Vector2 movement = direction.Normalized() * MagnetSpeed * delta;

        if (movement.Length() > distance)
        {
            // 防止越过目标
            _owner.GlobalPosition = Collector.GlobalPosition;
        }
        else
        {
            _owner.GlobalPosition += movement;
        }
    }

    /// <summary>
    /// 处理 Area2D 进入事件
    /// </summary>
    private void OnAreaEntered(Area2D area)
    {
        // 获取采集者（通过 EntityManager）
        var collector = EntityManager.GetEntityByComponent(area);
        if (collector is Node2D node2D)
        {
            TriggerPickup(node2D);
        }
    }

    /// <summary>
    /// 处理 Body 进入事件
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        TriggerPickup(body);
    }

    /// <summary>
    /// 触发拾取事件
    /// </summary>
    private void TriggerPickup(Node2D collector)
    {
        Log.Debug($"Picked up by: {collector.Name}");
        PickedUp?.Invoke(collector);
    }
}
