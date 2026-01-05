using Godot;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Data 系统初始化
/// 负责 DataRegistry 的业务元数据和计算逻辑注册
/// </summary>
public partial class DataInit : Node
{
    private static readonly Log _log = new("DataInit");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register("DataInit", "res://Src/Tools/Data/DataInit.cs", AutoLoad.Priority.Core);
    }

    public override void _EnterTree()
    {
        InitializeDataRegistry();
    }

    private void InitializeDataRegistry()
    {
        _log.Info("开始注册全局 Data 元数据...");

        // === 基础信息 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Name,
            DisplayName = "名称",
            Description = "实体的名称",
            Category = DataCategory.Basic,
            Type = typeof(string),
            DefaultValue = "",
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Level,
            DisplayName = "等级",
            Description = "实体的等级",
            Category = DataCategory.Basic,
            Type = typeof(int),
            DefaultValue = 1,
            MinValue = 1,
            MaxValue = 999,
            SupportModifiers = false
        });

        // === 生命系统 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.MaxHp,
            DisplayName = "最大生命值",
            Description = "角色的最大生命值",
            Category = DataCategory.Health,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 1,
        });

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        // === 攻击系统 ===
        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        // === 防御系统 ===
        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        // === 移动系统 ===
        DataRegistry.Register(new DataMeta
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

        // === 资源系统 ===
        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        // === 特殊机制 ===
        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        DataRegistry.Register(new DataMeta
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

        // === 计算属性元数据 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.AttackInterval,
            DisplayName = "攻击间隔",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 1f,
            SupportModifiers = false
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.EffectiveHp,
            DisplayName = "有效生命值",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 100f,
            SupportModifiers = false
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.DPS,
            DisplayName = "每秒伤害",
            Category = DataCategory.Computed,
            Type = typeof(float),
            DefaultValue = 10f,
            SupportModifiers = false
        });

        // --- 注册计算逻辑 ---

        DataRegistry.RegisterComputed(new ComputedData
        {
            Key = DataKey.AttackInterval,
            Dependencies = new[] { DataKey.AttackSpeed },
            Compute = (data) =>
            {
                float attackSpeed = data.Get<float>(DataKey.AttackSpeed, 100f);
                return attackSpeed > 0 ? 1.0f / (attackSpeed / 100f) : 1.0f;
            }
        });

        DataRegistry.RegisterComputed(new ComputedData
        {
            Key = DataKey.EffectiveHp,
            Dependencies = new[] { DataKey.MaxHp, DataKey.DamageReduction },
            Compute = (data) =>
            {
                float maxHp = data.Get<float>(DataKey.MaxHp, 100f);
                float damageReduction = data.Get<float>(DataKey.DamageReduction, 0f) / 100f;
                damageReduction = System.Math.Min(damageReduction, 0.9f);
                return maxHp / (1f - damageReduction);
            }
        });

        DataRegistry.RegisterComputed(new ComputedData
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

        var keyCount = DataRegistry.GetAllKeys().Count();
        _log.Success($"DataRegistry 业务数据注册完成：共 {keyCount} 个键");
    }
}
