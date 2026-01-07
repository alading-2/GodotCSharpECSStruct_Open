using Godot;
using System;

/// <summary>
/// 吸血处理器
/// <para>处理攻击者的吸血逻辑。</para>
/// </summary>
public class LifestealProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.FinalDamage <= 0) return;
        if (info.Instigator is not Node instigatorNode) return; // Instigator usually is Node (Player/Enemy implements IUnit which is usually Node?)
                                                                // IUnit 不一定是 Node，但在此项目中 Player/Enemy 都是 Node。
                                                                // 我们需要访问 Instigator 的 Data 和 HealthComponent。

        // 尝试将 Instigator 转换为 Node 以获取组件
        // 或者 IUnit 应该提供获取 Entity/Data 的方法？
        // 目前 IEntity 包含 Data。 Player/Enemy 实现 IEntity。

        if (info.Instigator is IEntity instigatorEntity)
        {
            float lifestealChance = instigatorEntity.Data.Get<float>(DataKey.LifeSteal, 0);

            // Brotato 逻辑：LifeSteal 是触发回血 1 点的概率？还是百分比吸血？
            // Brotato Wiki: Life Steal is a chance to heal 1 HP when damaging an enemy. Max 10HP/wave cap usually?
            // 这里假设是 概率回复 1 点血。

            if (lifestealChance > 0)
            {
                if (GD.Randf() * 100 < lifestealChance)
                {
                    // 回血 1 点
                    if (instigatorEntity is Node entityNode)
                    {
                        var health = EntityManager.GetComponent<HealthComponent>(entityNode);
                        if (health != null)
                        {
                            health.ModifyHealth(1);
                            info.AddLog("Lifesteal triggered (+1 HP)");
                        }
                    }
                }
            }
        }
    }
}
