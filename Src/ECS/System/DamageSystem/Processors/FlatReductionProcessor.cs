using Godot;

/// <summary>
/// 固定减伤处理器
/// <para>最后的硬减伤（如“格挡 10 点伤害”）。</para>
/// </summary>
public class FlatReductionProcessor : IDamageProcessor
{
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.IsDodged || info.FinalDamage <= 0) return;
        if (info.Victim is not IEntity victimEntity) return;

        // 假设有 FlatDamageReduction 属性，暂时用 DamageReduction 替代或新增?
        // 在 DataKey 中我们有 DamageReduction，但通常那是百分比。
        // 假设 DataKey.DamageReduction 是百分比，我们需要一个新的 key?
        // 为了简化，假设 DataKey.Armor 同时提供通过公式计算的百分比减伤。
        // 此处如果有 "FlatDamageReduction" (暂无定义)，则处理。
        // 目前 DataKey.DamageReduction 如果是百分比，是否已经处理了？
        // 之前在 DefenseProcessor 中处理了 Armor。
        // 如果 DataKey.DamageReduction 是额外的百分比减伤，应该在 DamageAmplificationProcessor 类似的 Defense 中处理。
        // 这里如果是 "Flat Reduction"，比如减少 5 点伤害。

        // 假设 DataKey 中没有特定的 Flat Reduction，暂时跳过或者用一个硬编码值测试
        // 或者检查是否有类似 "Block" 的机制。
        // 由于没有明确需求，暂时留空或作为扩展点。

        // 如果需要实现，假设 Key 为 "FlatDefense"
        // float flatDef = victimEntity.Data.Get<float>("FlatDefense", 0);
        // if (flatDef > 0) { ... }
    }
}
