using Godot;
using System;

/// <summary>
/// 吸血处理器
/// <para>处理攻击者的吸血逻辑，向角色（IUnit）发送治疗请求。</para>
/// </summary>
public class LifestealProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("LifestealProcessor");
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.FinalDamage <= 0) return;
        if (info.Attacker == null) return;

        // 查找归属的 IUnit（自身或沿 PARENT 向上）
        var targetUnit = EntityRelationshipManager.FindAncestorOfType<IUnit>(info.Attacker);
        if (targetUnit == null)
        {
            _log.Error($"吸血处理失败：无法找到归属的 IUnit，Attacker={info.Attacker}");
            return;
        }

        // 获取吸血概率并判定
        float lifestealChance = targetUnit.Data.Get<float>(DataKey.LifeSteal);

        // Brotato 逻辑：LifeSteal 是触发回血 1 点的概率
        if (lifestealChance > 0 && GD.Randf() * 100 < lifestealChance)
        {
            // 发送治疗请求事件到正确的 IUnit（角色）
            targetUnit.Events.Emit(GameEventType.Unit.HealRequest,
                new GameEventType.Unit.HealRequestEventData(1, HealSource.Lifesteal));
            info.AddLog("Lifesteal triggered (+1 HP to Unit)");
        }
    }
}

