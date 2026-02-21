using System;
using Godot;

/// <summary>
/// 受击判定组件 - 检测并响应来自 Hitbox 的攻击。
/// 继承自 Area2D，仅负责触发 HitReceived 事件，不直接依赖 HealthComponent。
/// 伤害转发由实体（父节点）负责协调。
/// </summary>
public partial class HurtboxComponent : Area2D, IComponent
{
    private static readonly Log Log = new(nameof(HurtboxComponent));

    // ================= IComponent 实现 =================

    private Data? _data;

    public void OnComponentRegistered(Node entity)
    {
        // 组件注册时缓存 Data 引用
        if (entity is IEntity iEntity)
        {
            _data = iEntity.Data;
        }
    }

    public void OnComponentUnregistered()
    {
        // 清理引用和事件
        HitReceived = null;
        _data = null;
    }



    // ================= Runtime State =================

    /// <summary>
    /// 获取无敌时间。
    /// </summary>
    public float InvincibilityTime => _data?.Get<float>(DataKey.InvincibilityTime, 0f) ?? 0f;

    // ================= C# Events =================

    /// <summary>
    /// 受到攻击时触发（无论是否有 HealthComponent）。
    /// 参数: 攻击的 HitboxComponent。
    /// 实体应订阅此事件并自行处理伤害转发。
    /// </summary>
    public event Action<HitboxComponent>? HitReceived;

    // ================= Private State =================

    /// <summary>
    /// 无敌计时器（从 Data 容器读取）
    /// </summary>
    private float InvincibilityTimerValue => _data.Get<float>(DataKey.InvincibilityTimer);

    /// <summary>
    /// 是否处于无敌状态
    /// </summary>
    public bool IsInvincible => InvincibilityTimerValue > 0f;

    // ================= Godot Lifecycle =================

    public override void _Ready()
    {
        // 连接 area_entered 信号
        AreaEntered += OnAreaEntered;

        Log.Debug($"受击判定组件初始化完成: 无敌时间={InvincibilityTime}");
    }

    public override void _ExitTree()
    {
        // 断开信号连接
        AreaEntered -= OnAreaEntered;

        // 清理事件订阅，防止内存泄漏
        HitReceived = null;

        Log.Trace("受击判定组件退出场景树，已清理信号连接和事件订阅。");
    }

    public override void _Process(double delta)
    {
        // 更新无敌计时器
        float time = InvincibilityTimerValue;
        if (time > 0f)
        {
            time -= (float)delta;
            if (time <= 0f)
            {
                time = 0f;
                Log.Trace("无敌状态结束。");
            }
            // ✅ 通过 Data 更新计时器（符合纯数据驱动规范）
            _data?.Set(DataKey.InvincibilityTimer, time);
        }
    }

    // ================= 信号处理 =================

    /// <summary>
    /// 处理 Area2D 进入事件。
    /// </summary>
    private void OnAreaEntered(Area2D area)
    {
        // 检查是否为 HitboxComponent
        if (area is not HitboxComponent hitbox)
        {
            return;
        }

        // 检查是否处于无敌状态
        if (IsInvincible)
        {
            Log.Trace("忽略受击: 当前处于无敌状态。");
            return;
        }

        Log.Debug($"检测到来自 HitboxComponent 的攻击: 伤害={hitbox.Damage}");

        // 启动无敌时间
        if (InvincibilityTime > 0f)
        {
            // ✅ 通过 Data 设置无敌计时器
            _data.Set(DataKey.InvincibilityTimer, InvincibilityTime);
            Log.Trace($"无敌状态开始: 持续 {InvincibilityTime} 秒");
        }

        // 触发 HitReceived 事件，由实体负责处理伤害转发
        HitReceived?.Invoke(hitbox);
    }
}
