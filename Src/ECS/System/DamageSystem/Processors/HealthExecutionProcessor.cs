using Godot;

/// <summary>
/// 生命值结算处理器
/// <para>实际执行扣血逻辑，调用受击方 HealthComponent。</para>
/// </summary>
public class HealthExecutionProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("HealthExecutionProcessor");

    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        var victim = info.Victim as Node;
        if (victim == null) return;

        // ✅ 获取 HealthComponent（由其负责扣血和致死判定）
        var health = EntityManager.GetComponent<HealthComponent>(victim);
        if (health != null)
        {
            if (info.IsSimulation)
            {
                info.AddLog($"[模拟] 将通过 HealthComponent 执行伤害: -{info.FinalDamage}");
            }
            else
            {
                health.ApplyDamage(info);
                info.AddLog($"通过 HealthComponent 执行伤害: -{info.FinalDamage}");
            }
        }
        else
        {
            // ❌ 没有 HealthComponent 是严重错误，不应该有回退逻辑
            // 原因：
            // 1. 有 HealthComponent：会触发 Damaged/Kill 事件，统计系统正常工作
            // 2. 无 HealthComponent：直接修改 Data，不触发任何事件，统计数据丢失
            // 3. 两种路径结果差异巨大，会导致难以排查的 bug
            _log.Error($"受害者 {info.Victim} 没有 HealthComponent，无法执行伤害！这是架构错误，所有可受击单位必须有 HealthComponent。");
            info.AddLog("错误：受害者没有 HealthComponent");
        }
    }
}
