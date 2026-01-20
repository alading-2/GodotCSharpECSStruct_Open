using Godot;

/// <summary>
/// 暴击处理器
/// <para>负责判定攻击是否触发暴击，并根据攻击者的暴击倍率调整最终伤害。</para>
/// </summary>
public class CritProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("CritProcessor");
    public int Priority { get; set; }

    /// <summary>
    /// 处理暴击逻辑
    /// </summary>
    /// <param name="info">伤害上下文信息</param>
    public void Process(DamageInfo info)
    {
        if (info.Attacker == null) return;

        // 查找攻击者IUnit实体（自身或沿 PARENT 向上）
        var attackerEntity = EntityRelationshipManager.FindAncestorOfType<IUnit>(info.Attacker);
        if (attackerEntity == null)
        {
            _log.Error($"暴击处理失败：无法找到攻击者实体，Attacker={info.Attacker}");
            return;
        }

        // 从攻击者数据中获取暴击率 (0-100)
        float critChance = attackerEntity.Data.Get<float>(DataKey.CritRate);

        // 执行随机判定
        if (MyMath.CheckProbability(critChance))
        {
            // 获取暴击伤害
            float critMultiplier = attackerEntity.Data.Get<float>(DataKey.CritDamage);
            critMultiplier /= 100f;
            info.IsCritical = true;
            info.FinalDamage *= critMultiplier;
            info.AddLog($"暴击(倍率: {critMultiplier}) -> {info.FinalDamage}");
        }
    }
}

