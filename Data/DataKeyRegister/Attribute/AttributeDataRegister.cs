
using Godot;
using System.Runtime.CompilerServices;

// 说明：百分比存的是1-100，需要除以100再使用
public partial class AttributeDataRegister : Node
{
    private static readonly Log _log = new Log("AttributeDataRegister");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "AttributeDataRegister",
            Path = "res://Data/DataKeyRegister/Attribute/AttributeDataRegister.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("AttributeDataRegister注册属性数据...");
        // ========================================
        // 生命相关 (Health)
        // ========================================
        // 基础生命值
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseHp,
            DisplayName = "基础生命值",
            Description = "基础生命值",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 10f,
            MinValue = 0
        });
        // 生命值加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.HpBonus,
            DisplayName = "生命值加成",
            Description = "生命值百分比加成",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终生命值
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalHp,
            DisplayName = "生命值",
            Description = "生命值",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseHp, DataKey.HpBonus],
            Compute = (data) =>
            {
                float baseHp = data.Get<float>(DataKey.BaseHp);
                float bonus = data.Get<float>(DataKey.HpBonus);
                return MyMath.AttributeBonusCalculation(baseHp, bonus);
            }
        });
        // 基础生命恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseHpRegen,
            DisplayName = "基础生命恢复",
            Description = "每秒恢复的基础生命值",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f
        }); // 恢复可为负
        // 生命恢复加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.HpRegenBonus,
            DisplayName = "生命恢复加成",
            Description = "生命恢复百分比加成",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 百分比生命恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.PercentHpRegen,
            DisplayName = "百分比生命恢复",
            Description = "每秒基于最大生命值的百分比恢复",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxPercentRegen,
            IsPercentage = true
        });
        // 最终生命恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalHpRegen,
            DisplayName = "生命恢复", // UI显示出来不用带最终两个字
            Description = "每秒恢复的生命值",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseHpRegen, DataKey.HpRegenBonus, DataKey.PercentHpRegen, DataKey.FinalHp],
            Compute = (data) =>
            {
                float baseRegen = data.Get<float>(DataKey.BaseHpRegen);
                float bonus = data.Get<float>(DataKey.HpRegenBonus);
                float baseRecovery = MyMath.AttributeBonusCalculation(baseRegen, bonus);
                float finalHp = data.Get<float>(DataKey.FinalHp);
                float percentRegen = data.Get<float>(DataKey.PercentHpRegen);
                float percentRecovery = finalHp * (percentRegen * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });
        // 吸血百分比
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.LifeSteal,
            DisplayName = "吸血百分比",
            Description = "吸血百分比",
            Category = AttributeCategory.Health,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // ========================================
        // 魔法相关 (Mana)
        // ========================================
        // 基础魔法值
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseMana,
            DisplayName = "基础魔法值",
            Description = "基础魔法值",
            Category = AttributeCategory.Mana,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 魔法加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.ManaBonus,
            DisplayName = "魔法加成",
            Description = "魔法值百分比加成",
            Category = AttributeCategory.Mana,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终魔法值
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalMana,
            DisplayName = "魔法值",
            Description = "魔法值",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseMana, DataKey.ManaBonus],
            Compute = (data) =>
            {
                float baseMana = data.Get<float>(DataKey.BaseMana);
                float bonus = data.Get<float>(DataKey.ManaBonus);
                return MyMath.AttributeBonusCalculation(baseMana, bonus);
            }
        });
        // 基础魔法恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseManaRegen,
            DisplayName = "基础魔法恢复",
            Description = "每秒恢复的基础魔法值",
            Category = AttributeCategory.Mana,
            Type = typeof(float),
            DefaultValue = 0f
        }); // 恢复可为负
        // 魔法恢复加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.ManaRegenBonus,
            DisplayName = "魔法恢复加成",
            Description = "魔法恢复百分比加成",
            Category = AttributeCategory.Mana,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 百分比魔法恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.PercentManaRegen,
            DisplayName = "百分比魔法恢复",
            Description = "基于最大魔法值的百分比恢复",
            Category = AttributeCategory.Mana,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxPercentRegen,
            IsPercentage = true
        });
        // 最终魔法恢复
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalManaRegen,
            DisplayName = "魔法恢复",
            Description = "每秒恢复的魔法值（基础恢复 + 百分比恢复）",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseManaRegen, DataKey.ManaRegenBonus, DataKey.PercentManaRegen, DataKey.FinalMana],
            Compute = (data) =>
            {
                float baseRegen = data.Get<float>(DataKey.BaseManaRegen);
                float bonus = data.Get<float>(DataKey.ManaRegenBonus);
                float baseRecovery = MyMath.AttributeBonusCalculation(baseRegen, bonus);
                float finalMana = data.Get<float>(DataKey.FinalMana);
                float percentRegen = data.Get<float>(DataKey.PercentManaRegen);
                float percentRecovery = finalMana * (percentRegen * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });
        // ========================================
        // 攻击相关 (Attack)
        // ========================================
        // 基础攻击力
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseAttack,
            DisplayName = "基础攻击力",
            Description = "基础攻击力",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 攻击力加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.AttackBonus,
            DisplayName = "攻击力加成",
            Description = "攻击力百分比加成",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终攻击力
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalAttack,
            DisplayName = "攻击力",
            Description = "攻击力",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseAttack, DataKey.AttackBonus],
            Compute = (data) =>
            {
                float baseAttack = data.Get<float>(DataKey.BaseAttack);
                float bonus = data.Get<float>(DataKey.AttackBonus);
                return MyMath.AttributeBonusCalculation(baseAttack, bonus);
            }
        });
        // ========================================
        // 攻速相关 (AttackSpeed)
        // ========================================
        // 基础攻速
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseAttackSpeed,
            DisplayName = "基础攻速",
            Description = "基础攻击速度",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 0,
            MaxValue = 1000
        });
        // 攻速加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.AttackSpeedBonus,
            DisplayName = "攻速加成",
            Description = "攻击速度百分比加成",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终攻速
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalAttackSpeed,
            DisplayName = "攻速",
            Description = "攻击速度",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseAttackSpeed, DataKey.AttackSpeedBonus],
            Compute = (data) =>
            {
                float baseSpeed = data.Get<float>(DataKey.BaseAttackSpeed);
                float bonus = data.Get<float>(DataKey.AttackSpeedBonus);
                return MyMath.AttributeBonusCalculation(baseSpeed, bonus);
            }
        });
        // 攻击间隔
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.AttackInterval,
            DisplayName = "攻击间隔",
            Description = "攻击间隔：攻击一次需要的时间（秒）",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 1f,
            SupportModifiers = false,
            Dependencies = [DataKey.FinalAttackSpeed],
            Compute = (data) =>
            {
                float speed = data.Get<float>(DataKey.FinalAttackSpeed);
                return 1f / (speed / 100f);
            }
        });
        // 伤害增幅
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.DamageAmplification,
            DisplayName = "伤害增幅",
            Description = "伤害增幅百分比",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 护甲穿透
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Penetration,
            DisplayName = "护甲穿透",
            Description = "护甲穿透值",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 攻击范围
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Range,
            DisplayName = "攻击范围",
            Description = "攻击范围",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 0
        });
        // 击退
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Knockback,
            DisplayName = "击退",
            Description = "击退敌人的距离",
            Category = AttributeCategory.Attack,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 1000
        });


        // ========================================
        // 防御相关 (Defense)
        // ========================================
        // 基础防御
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseDefense,
            DisplayName = "基础防御",
            Description = "基础防御力",
            Category = AttributeCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 防御加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.DefenseBonus,
            DisplayName = "防御加成",
            Description = "防御力百分比加成",
            Category = AttributeCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终防御
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalDefense,
            DisplayName = "防御",
            Description = "防御值",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseDefense, DataKey.DefenseBonus],
            Compute = (data) =>
            {
                float baseDefense = data.Get<float>(DataKey.BaseDefense);
                float bonus = data.Get<float>(DataKey.DefenseBonus);
                return MyMath.AttributeBonusCalculation(baseDefense, bonus);
            }
        });
        // 伤害减免
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.DamageReduction,
            DisplayName = "伤害减免",
            Description = "伤害减免百分比",
            Category = AttributeCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxDamageReduction,
            IsPercentage = true
        });
        // 护盾
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Shield,
            DisplayName = "护盾",
            Description = "护盾值",
            Category = AttributeCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 反伤
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.Thorns,
            DisplayName = "反伤",
            Description = "反弹受到伤害的百分比",
            Category = AttributeCategory.Defense,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = 500,
            IsPercentage = true
        });
        // ========================================
        // 技能相关 (Skill)
        // ========================================
        // 基础技能伤害
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.BaseSkillDamage,
            DisplayName = "基础技能伤害",
            Description = "基础技能伤害百分比",
            Category = AttributeCategory.Skill,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0
        });
        // 技能伤害加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.SkillDamageBonus,
            DisplayName = "技能伤害加成",
            Description = "技能伤害百分比加成",
            Category = AttributeCategory.Skill,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终技能伤害
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalSkillDamage,
            DisplayName = "技能伤害",
            Description = "技能伤害百分比",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            IsPercentage = true,
            Dependencies = [DataKey.BaseSkillDamage, DataKey.SkillDamageBonus],
            Compute = (data) =>
            {
                float baseSkillDamage = data.Get<float>(DataKey.BaseSkillDamage);
                float bonus = data.Get<float>(DataKey.SkillDamageBonus);
                return MyMath.AttributeBonusCalculation(baseSkillDamage, bonus);
            }
        });
        // 技能冷却缩减
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.CooldownReduction,
            DisplayName = "技能冷却缩减",
            Description = "技能冷却缩减百分比",
            Category = AttributeCategory.Skill,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxCooldownReduction,
            IsPercentage = true
        });
        // ========================================
        // 移动相关 (Movement)
        // ========================================
        // 移动速度
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.MoveSpeed,
            DisplayName = "移动速度",
            Description = "移动速度",
            Category = AttributeCategory.Movement,
            Type = typeof(float),
            DefaultValue = 100f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxMoveSpeed
        });
        // 移动速度加成
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.MoveSpeedBonus,
            DisplayName = "移动速度加成",
            Description = "移动速度百分比加成",
            Category = AttributeCategory.Movement,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            IsPercentage = true
        });
        // 最终移动速度
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalMoveSpeed,
            DisplayName = "移动速度",
            Description = "移动速度",
            Category = AttributeCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.MoveSpeed, DataKey.MoveSpeedBonus],
            Compute = (data) =>
            {
                float moveSpeed = data.Get<float>(DataKey.MoveSpeed);
                float bonus = data.Get<float>(DataKey.MoveSpeedBonus);
                return MyMath.AttributeBonusCalculation(moveSpeed, bonus);
            }
        });
        // ========================================
        // 闪避相关 (Dodge)
        // ========================================
        // 闪避几率
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.DodgeChance,
            DisplayName = "闪避几率",
            Description = "闪避几率",
            Category = AttributeCategory.Dodge,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxDodgeChance,
            IsPercentage = true
        });
        // ========================================
        // 暴击相关 (Crit)
        // ========================================
        // 暴击率
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.CritRate,
            DisplayName = "暴击率",
            Description = "暴击率",
            Category = AttributeCategory.Crit,
            Type = typeof(float),
            DefaultValue = 0f,
            MinValue = 0,
            MaxValue = GlobalConfig.MaxCritRate,
            IsPercentage = true
        });
        // 暴击伤害
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.CritDamage,
            DisplayName = "暴击伤害",
            Description = "暴击伤害百分比",
            Category = AttributeCategory.Crit,
            Type = typeof(float),
            DefaultValue = 100f,    //默认100%
            MinValue = 0,
            IsPercentage = true
        });
    }
}
