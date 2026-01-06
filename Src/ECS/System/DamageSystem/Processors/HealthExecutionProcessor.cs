using Godot;

/// <summary>
/// 生命值结算处理器
/// <para>Priority: 500</para>
/// <para>实际执行扣血逻辑。</para>
/// </summary>
public class HealthExecutionProcessor : IDamageProcessor
{
    public int Priority => 500;

    public void Process(DamageInfo info)
    {
        // 即使 FinalDamage 为 0，如果伤害被闪避了，就不会走到这里 (因为 IsDodged 检查)
        // 但如果是被护盾完全吸收，FinalDamage = 0，此时不应扣血，但可能需要触发 "0 伤害" 事件?
        // 根据逻辑，ModifyHealth(0) 应该也没问题。

        if (info.IsDodged) return;

        var healthComp = EntityManager.GetComponent<HealthComponent>(info.Victim);
        if (healthComp != null)
        {
            // 执行扣血
            // 注意：HealthComponent.ModifyHealth 接受负数表示伤害? 
            // 通常 ModifyHealth(float amount) -> amount > 0 heal, amount < 0 damage?
            // 或者 ModifyHealth(float delta) -> current += delta.
            // 所以伤害应该是负数。

            if (info.FinalDamage > 0)
            {
                healthComp.ModifyHealth(-info.FinalDamage);
                info.AddLog($"Health Executed: -{info.FinalDamage}");
            }
        }
        else
        {
            // Victim 没有生命组件，无法扣血
            info.AddLog("No HealthComponent on Victim");
        }
    }
}
