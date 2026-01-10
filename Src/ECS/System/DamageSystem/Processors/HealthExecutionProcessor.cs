using Godot;

/// <summary>
/// 生命值结算处理器
/// <para>实际执行扣血逻辑，并触发死亡检测。</para>
/// </summary>
public class HealthExecutionProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.IsDodged) return;
        if (info.FinalDamage <= 0) return;

        var victim = info.Victim as Node;
        if (victim == null) return;

        // 获取 Entity 的 Data
        var data = EntityManager.GetEntityData(victim);
        if (data == null)
        {
            info.AddLog("No Data on Victim");
            return;
        }

        // 获取 HealthComponent
        var health = EntityManager.GetComponent<HealthComponent>(victim);
        if (health != null)
        {
            health.ApplyDamage(info.FinalDamage, info.Attacker as IEntity, info.Type);
            info.AddLog($"Health Executed via HealthComponent: -{info.FinalDamage}");
        }
        else
        {
            info.AddLog("Victim has no HealthComponent");

            // 回退逻辑：如果没 HealthComponent 但有 Data，尝试手动修改（可选，建议强制要求 HealthComponent）
            float currentHp = data.Get<float>(DataKey.CurrentHp);
            data.Set(DataKey.CurrentHp, Mathf.Max(0, currentHp - info.FinalDamage));
        }
    }
}
