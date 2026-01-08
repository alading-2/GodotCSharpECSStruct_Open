using Godot;

/// <summary>
/// 暴击处理器
/// <para>负责判定攻击是否触发暴击，并根据攻击者的暴击倍率调整最终伤害。</para>
/// </summary>
public class CritProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    /// <summary>
    /// 处理暴击逻辑
    /// </summary>
    /// <param name="info">伤害上下文信息</param>
    public void Process(DamageInfo info)
    {
        // 只有当攻击者是实体（具有数据容器）时才处理暴击
        if (info.Instigator is not IEntity instigatorEntity) return;

        // 从攻击者数据中获取暴击率 (0-100)
        // 使用 DataKey.CritChance 确保键名统一
        float critChance = instigatorEntity.Data.Get<float>(DataKey.CritRate);

        // 执行随机判定：如果随机数 (0.0-1.0) * 100 小于暴击率，则触发暴击
        if (GD.Randf() * 100 <= critChance)
        {
            // 获取暴击伤害
            float critMultiplier = instigatorEntity.Data.Get<float>(DataKey.CritDamage, 1.5f);
            critMultiplier /= 100f; //暴击伤害采用百分比
            // 标记此伤害为暴击（UI 可能会根据此标志显示大字或特殊特效）
            info.IsCritical = true;

            // 应用暴击加成
            info.FinalDamage *= critMultiplier;

            // 记录计算日志
            info.AddLog($"暴击(x{critMultiplier}) -> {info.FinalDamage}");
        }
    }
}
