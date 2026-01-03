using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 数据注册表 - 管理所有数据的元数据和计算规则
/// 静态初始化，无需运行时注册
/// </summary>
public static class DataRegistry
{
    private static readonly Log _log = new("DataRegistry");

    private static readonly Dictionary<string, DataMeta> _metaRegistry = new();
    private static readonly Dictionary<string, ComputedData> _computedRegistry = new();

    static DataRegistry()
    {
        RegisterBasicData();
        RegisterComputedData();
        _log.Info($"数据注册表初始化完成：{_metaRegistry.Count} 个基础数据，{_computedRegistry.Count} 个计算数据");
    }

    private static void RegisterBasicData()
    {
        // === 基础信息（不支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.Name,
            DisplayName = "名称",
            Description = "实体的名称",
            Category = DataCategory.Basic,
            Type = typeof(string),
            DefaultValue = "",
            // SupportModifiers 自动推断为 false (非数值)
        });

        Register(new DataMeta
        {
            Key = DataKey.Level,
            DisplayName = "等级",
            Description = "实体的等级",
            Category = DataCategory.Basic,
            Type = typeof(int),
            DefaultValue = 1,
            MinValue = 1,
            MaxValue = 999,
            SupportModifiers = false // 显式禁用修改器
        });

        // === 生命系统（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.MaxHp,
            DisplayName = "最大生命值",
            Description = "角色的最大生命值",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 1,
            // SupportModifiers 自动推断为 true (数值)
        });

        Register(new DataMeta
        {
            Key = DataKey.HpRegen,
            DisplayName = "生命恢复",
            Description = "每秒恢复的生命值",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = -100,
            MaxValue = 1000,
        });

        Register(new DataMeta
        {
            Key = DataKey.LifeSteal,
            DisplayName = "生命偷取",
            Description = "造成伤害时恢复的生命百分比",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 100,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.Armor,
            DisplayName = "护甲",
            Description = "减少受到的伤害",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 9999,
        });

        // === 攻击系统（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.Damage,
            DisplayName = "伤害",
            Description = "基础攻击伤害",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 10f,
            MinValue = 0,
            MaxValue = 99999,
        });

        Register(new DataMeta
        {
            Key = DataKey.AttackSpeed,
            DisplayName = "攻击速度",
            Description = "攻击速度百分比",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 1,
            MaxValue = 1000,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.CritChance,
            DisplayName = "暴击率",
            Description = "暴击触发概率",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 100,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.CritDamage,
            DisplayName = "暴击伤害",
            Description = "暴击时的伤害倍率",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 150f,
            MinValue = 100,
            MaxValue = 1000,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.Range,
            DisplayName = "攻击范围",
            Description = "攻击的有效距离",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 0,
            MaxValue = 9999,
        });

        Register(new DataMeta
        {
            Key = DataKey.Knockback,
            DisplayName = "击退",
            Description = "击退敌人的力度",
            Category = DataCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 1000,
        });

        // === 防御系统（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.DodgeChance,
            DisplayName = "闪避率",
            Description = "闪避攻击的概率",
            Category = DataCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 90,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.DamageReduction,
            DisplayName = "伤害减免",
            Description = "减少受到伤害的百分比",
            Category = DataCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 90,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.Thorns,
            DisplayName = "反伤",
            Description = "反弹受到伤害的百分比",
            Category = DataCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 500,
            IsPercentage = true,
        });

        // === 移动系统（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.Speed,
            DisplayName = "移动速度",
            Description = "角色的移动速度",
            Category = DataCategory.Movement,
            Type = typeof(float),
            DefaultValue = 300f,
            MinValue = 0,
            MaxValue = 9999,
        });

        // === 资源系统（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.PickupRange,
            DisplayName = "拾取范围",
            Description = "自动拾取物品的范围",
            Category = DataCategory.Resource,
            Type = typeof(float),
            DefaultValue = 50f,
            MinValue = 0,
            MaxValue = 1000,
        });

        Register(new DataMeta
        {
            Key = DataKey.ExpGain,
            DisplayName = "经验获取",
            Description = "经验获取倍率",
            Category = DataCategory.Resource,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 0,
            MaxValue = 1000,
            IsPercentage = true,
        });

        Register(new DataMeta
        {
            Key = DataKey.LuckBonus,
            DisplayName = "幸运值",
            Description = "影响掉落品质和概率",
            Category = DataCategory.Resource,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = -100,
            MaxValue = 1000,
        });

        // === 特殊机制（支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.Pierce,
            DisplayName = "穿透",
            Description = "投射物可穿透的敌人数量",
            Category = DataCategory.Special,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 100,
        });

        Register(new DataMeta
        {
            Key = DataKey.ProjectileCount,
            DisplayName = "投射物数量",
            Description = "每次攻击发射的投射物数量",
            Category = DataCategory.Special,
            Type = typeof(float),
            DefaultValue = 1f,
            MinValue = 1,
            MaxValue = 100,
        });

        Register(new DataMeta
        {
            Key = DataKey.AreaSize,
            DisplayName = "范围倍率",
            Description = "技能和攻击的范围倍率",
            Category = DataCategory.Special,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 10,
            MaxValue = 500,
            IsPercentage = true,
        });

        // === 计算数据（只读，不支持修改器） ===
        Register(new DataMeta
        {
            Key = DataKey.AttackInterval,
            DisplayName = "攻击间隔",
            Description = "两次攻击之间的时间间隔（秒）",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 1f,
            SupportModifiers = false
        });

        Register(new DataMeta
        {
            Key = DataKey.EffectiveHp,
            DisplayName = "有效生命值",
            Description = "考虑减伤后的等效生命值",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 100f,
            SupportModifiers = false
        });

        Register(new DataMeta
        {
            Key = DataKey.DPS,
            DisplayName = "每秒伤害",
            Description = "理论每秒输出伤害",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 10f,
            SupportModifiers = false
        });
    }

    private static void RegisterComputedData()
    {
        // 攻击间隔 = 1.0 / (攻击速度 / 100)
        RegisterComputed(new ComputedData
        {
            Key = DataKey.AttackInterval,
            Dependencies = new[] { DataKey.AttackSpeed },
            Compute = (data) =>
            {
                // 使用 Get 获取最终值（包含修改器效果）
                float attackSpeed = data.Get<float>(DataKey.AttackSpeed, 100f);
                return attackSpeed > 0 ? 1.0f / (attackSpeed / 100f) : 1.0f;
            }
        });

        // 有效生命值 = 最大生命值 / (1 - 伤害减免)
        RegisterComputed(new ComputedData
        {
            Key = DataKey.EffectiveHp,
            Dependencies = new[] { DataKey.MaxHp, DataKey.DamageReduction },
            Compute = (data) =>
            {
                float maxHp = data.Get<float>(DataKey.MaxHp, 100f);
                float damageReduction = data.Get<float>(DataKey.DamageReduction, 0f) / 100f;
                damageReduction = System.Math.Min(damageReduction, 0.9f); // 最大 90% 减伤
                return maxHp / (1f - damageReduction);
            }
        });

        // DPS = 伤害 × 攻击速度 × (1 + 暴击率 × (暴击伤害 - 1))
        RegisterComputed(new ComputedData
        {
            Key = DataKey.DPS,
            Dependencies = new[] { DataKey.Damage, DataKey.AttackSpeed, DataKey.CritChance, DataKey.CritDamage },
            Compute = (data) =>
            {
                float damage = data.Get<float>(DataKey.Damage, 10f);
                float attackSpeed = data.Get<float>(DataKey.AttackSpeed, 100f) / 100f;
                float critChance = data.Get<float>(DataKey.CritChance, 0f) / 100f;
                float critDamage = data.Get<float>(DataKey.CritDamage, 150f) / 100f;

                float avgDamage = damage * (1f + critChance * (critDamage - 1f));
                return avgDamage * attackSpeed;
            }
        });
    }

    private static void Register(DataMeta meta)
    {
        _metaRegistry[meta.Key] = meta;
    }

    private static void RegisterComputed(ComputedData computed)
    {
        _computedRegistry[computed.Key] = computed;
    }

    // === 公共查询接口 ===

    /// <summary>
    /// 获取数据的元数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>元数据，不存在则返回 null</returns>
    public static DataMeta? GetMeta(string key)
    {
        return _metaRegistry.TryGetValue(key, out var meta) ? meta : null;
    }

    /// <summary>
    /// 获取计算数据定义
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>计算数据定义，不存在则返回 null</returns>
    public static ComputedData? GetComputed(string key)
    {
        return _computedRegistry.TryGetValue(key, out var computed) ? computed : null;
    }

    /// <summary>
    /// 检查是否为计算数据
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>是否为计算数据</returns>
    public static bool IsComputed(string key)
    {
        return _computedRegistry.ContainsKey(key);
    }

    /// <summary>
    /// 检查数据是否支持修改器
    /// </summary>
    /// <param name="key">数据键</param>
    /// <returns>是否支持修改器</returns>
    public static bool SupportModifiers(string key)
    {
        var meta = GetMeta(key);
        return meta?.ActualSupportModifiers ?? false;
    }

    /// <summary>
    /// 获取依赖指定数据的所有计算数据键
    /// </summary>
    /// <param name="baseKey">基础数据键</param>
    /// <returns>依赖该数据的计算数据键列表</returns>
    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _computedRegistry
            .Where(kvp => kvp.Value.DependsOn(baseKey))
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// 获取指定分类的所有数据元数据
    /// </summary>
    /// <param name="category">数据分类</param>
    /// <returns>该分类下的所有元数据</returns>
    public static IEnumerable<DataMeta> GetMetaByCategory(DataCategory category)
    {
        return _metaRegistry.Values.Where(m => m.Category == category);
    }

    /// <summary>
    /// 获取所有已注册的数据键
    /// </summary>
    /// <returns>所有数据键</returns>
    public static IEnumerable<string> GetAllKeys()
    {
        return _metaRegistry.Keys;
    }

    /// <summary>
    /// 获取所有计算数据键
    /// </summary>
    /// <returns>所有计算数据键</returns>
    public static IEnumerable<string> GetAllComputedKeys()
    {
        return _computedRegistry.Keys;
    }
}
