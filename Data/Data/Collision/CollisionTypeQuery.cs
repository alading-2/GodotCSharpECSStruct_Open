/// <summary>
/// 碰撞类型查询工具 - 提供 CollisionType 的正向/反向查询方法
/// <para>
/// 此文件手动维护，不会被 ResourceGenerator 覆盖。
/// 原始数据字典位于 CollisionTypeRegistry（自动生成）。
/// </para>
/// <para>
/// 反向查询策略：layer 与 mask 同时匹配才视为成功；
/// 使用 TryGetValue 返回 bool，调用方可显式处理查找失败的情况。
/// </para>
/// </summary>
public static class CollisionTypeQuery
{
    /// <summary>
    /// 通过 (layer, mask) 双条件反向查找 CollisionType（O(1)）
    /// layer 与 mask 需同时与注册表中的预设值完全一致。
    /// layer=0 的类型（如 EffectCollision）同样可以查找。
    /// </summary>
    /// <param name="layer">Area2D / CharacterBody2D 的 collision_layer</param>
    /// <param name="mask">Area2D / CharacterBody2D 的 collision_mask</param>
    /// <param name="type">查找成功时输出对应的 CollisionType，失败时输出 Custom</param>
    /// <returns>true = 找到完全匹配的注册类型；false = 未在注册表中找到（Custom 或未知配置）</returns>
    public static bool TryFromLayerMask(uint layer, uint mask, out CollisionType type) =>
        CollisionTypeRegistry.TypeByLayerMask.TryGetValue((layer, mask), out type);

    /// <summary>
    /// 通过 CollisionType 获取对应的 (Layer, Mask)（O(1)）
    /// </summary>
    /// <param name="type">目标碰撞类型</param>
    /// <param name="layer">输出 collision_layer</param>
    /// <param name="mask">输出 collision_mask</param>
    /// <returns>true = 找到注册数据；false = Custom 或未注册（输出均为 0）</returns>
    public static bool TryGetLayerMask(CollisionType type, out uint layer, out uint mask)
    {
        if (CollisionTypeRegistry.LayerMaskByType.TryGetValue(type, out var lm))
        {
            layer = lm.Layer;
            mask = lm.Mask;
            return true;
        }
        layer = 0u;
        mask = 0u;
        return false;
    }
}
