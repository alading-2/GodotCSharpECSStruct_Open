using System;
using Godot;

/// <summary>
/// 拾取组件 - 实现物品的拾取检测和磁吸效果。
/// 继承自 Area2D 以利用引擎的碰撞检测。
/// </summary>
public partial class PickupComponent : Area2D
{
    private static readonly Log Log = new("PickupComponent");

    // ================= Export Properties =================

    // ================= Private State =================

    /// <summary>
    /// 当物品被拾取时触发的事件。
    /// </summary>
    public event Action<Node2D>? PickedUp;

    /// <summary>
    /// 父实体的动态数据容器。
    /// </summary>
    private Data _data = null!;

    // ================= Runtime State =================

    /// <summary>
    /// 获取磁吸速度。
    /// </summary>
    public float MagnetSpeed => _data.Get<float>("MagnetSpeed", 300f);

    /// <summary>
    /// 获取是否启用磁吸效果。
    /// </summary>
    public bool MagnetEnabled
    {
        get => _data.Get<bool>("MagnetEnabled", false);
        private set => _data.Set("MagnetEnabled", value);
    }

    /// <summary>
    /// 磁吸目标（采集者）。
    /// </summary>
    public Node2D? Collector { get; private set; }

    /// <summary>
    /// 获取所属实体（父节点）。
    /// </summary>
    private Node2D? OwnerEntity => GetParent<Node2D>();

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        var parent = GetParent();
        if (parent == null)
        {
            Log.Error("PickupComponent 错误: 必须作为实体 (Node) 的子节点存在。");
            return;
        }
        _data = parent.GetData();

        // 连接信号
        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;

        Log.Debug($"拾取组件初始化完成: 磁吸速度={MagnetSpeed}, 磁吸启用={MagnetEnabled}");
    }

    public override void _ExitTree()
    {
        // 断开信号连接
        AreaEntered -= OnAreaEntered;
        BodyEntered -= OnBodyEntered;

        // 清理事件和引用
        PickedUp = null;
        Collector = null;

        Log.Trace("拾取组件退出场景树，已清理信号连接和引用。");
    }

    public override void _PhysicsProcess(double delta)
    {
        // 处理磁吸效果：当磁吸开启且有有效采集者时，向采集者移动
        if (MagnetEnabled && Collector != null && IsInstanceValid(Collector))
        {
            MoveTowardCollector((float)delta);
        }
    }


    // ================= 公开方法 =================

    /// <summary>
    /// 启用磁吸效果。
    /// </summary>
    /// <param name="collector">采集者节点（通常是玩家）。</param>
    public void EnableMagnet(Node2D collector)
    {
        Collector = collector;
        MagnetEnabled = true;
        Log.Debug($"磁吸已开启，目标采集者: {collector.Name}");
    }

    /// <summary>
    /// 禁用磁吸效果。
    /// </summary>
    public void DisableMagnet()
    {
        MagnetEnabled = false;
        Collector = null;
        Log.Debug("磁吸已禁用。");
    }

    // ================= Private Methods =================

    /// <summary>
    /// 向采集者移动（磁吸效果）。
    /// </summary>
    private void MoveTowardCollector(float delta)
    {
        var owner = OwnerEntity;
        if (owner == null || Collector == null)
        {
            return;
        }

        Vector2 direction = Collector.GlobalPosition - owner.GlobalPosition;
        float distance = direction.Length();

        if (distance < 1f)
        {
            // 已到达，触发拾取
            return;
        }

        // 移动向采集者
        Vector2 movement = direction.Normalized() * MagnetSpeed * delta;

        if (movement.Length() > distance)
        {
            // 防止越过目标
            owner.GlobalPosition = Collector.GlobalPosition;
        }
        else
        {
            owner.GlobalPosition += movement;
        }
    }

    /// <summary>
    /// 处理 Area2D 进入事件。
    /// </summary>
    private void OnAreaEntered(Area2D area)
    {
        // 检查是否为采集者的检测区域
        var collector = area.GetParent<Node2D>();
        if (collector != null)
        {
            TriggerPickup(collector);
        }
    }

    /// <summary>
    /// 处理 Body 进入事件。
    /// </summary>
    private void OnBodyEntered(Node2D body)
    {
        TriggerPickup(body);
    }

    /// <summary>
    /// 触发拾取事件。
    /// </summary>
    private void TriggerPickup(Node2D collector)
    {
        Log.Debug($"Picked up by: {collector.Name}");
        PickedUp?.Invoke(collector);
    }
}
