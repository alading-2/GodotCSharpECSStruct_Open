
using Godot;
using System.Runtime.CompilerServices;

// 说明：百分比存的是1-100，需要除以100再使用
public partial class DataRegister_Attribute : Node
{
    private static readonly Log _log = new Log("DataRegister_Attribute");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "DataRegister_Attribute",
            Path = "res://Data/DataKeyRegister/Attribute/DataRegister_Attribute.cs",
            Priority = AutoLoad.Priority.Core,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("DataRegister_Attribute注册属性数据...");
        // ========================================
        // 生命相关 (Health)
        // ========================================
        // ========================================
        // 生命相关 (Health)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseHp, DisplayName = "基础生命值", Description = "基础生命值", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 10f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.HpBonus, DisplayName = "生命值加成", Description = "生命值百分比加成", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });
        // 最终生命值 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalHp,
            DisplayName = "生命值",
            Description = "生命值",
            Category = DataCategory_Attribute.Computed,
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

        DataRegistry.Register(new DataMeta { Key = DataKey.BaseHpRegen, DisplayName = "基础生命恢复", Description = "每秒恢复的基础生命值", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.HpRegenBonus, DisplayName = "生命恢复加成", Description = "生命恢复百分比加成", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.PercentHpRegen, DisplayName = "百分比生命恢复", Description = "每秒基于最大生命值的百分比恢复", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxPercentRegen, IsPercentage = true, SupportModifiers = true });
        // 最终生命恢复 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalHpRegen,
            DisplayName = "生命恢复",
            Description = "每秒恢复的生命值",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseHpRegen, DataKey.HpRegenBonus, DataKey.PercentHpRegen, DataKey.FinalHp],
            Compute = (data) =>
            {
                // 基础恢复+百分比恢复
                float baseRecovery = MyMath.AttributeBonusCalculation(data.Get<float>(DataKey.BaseHpRegen), data.Get<float>(DataKey.HpRegenBonus));
                float percentRecovery = data.Get<float>(DataKey.FinalHp) * (data.Get<float>(DataKey.PercentHpRegen) * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });
        DataRegistry.Register(new DataMeta { Key = DataKey.LifeSteal, DisplayName = "吸血百分比", Description = "吸血百分比", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });

        // ========================================
        // 魔法相关 (Mana)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseMana, DisplayName = "基础魔法值", Description = "基础魔法值", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true, MinValue = 0 });
        DataRegistry.Register(new DataMeta { Key = DataKey.ManaBonus, DisplayName = "魔法加成", Description = "魔法值百分比加成", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });
        // 最终魔法值 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalMana,
            DisplayName = "魔法值",
            Description = "魔法值",
            Category = DataCategory_Attribute.Computed,
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

        DataRegistry.Register(new DataMeta { Key = DataKey.BaseManaRegen, DisplayName = "基础魔法恢复", Description = "每秒恢复的基础魔法值", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.ManaRegenBonus, DisplayName = "魔法恢复加成", Description = "魔法恢复百分比加成", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.PercentManaRegen, DisplayName = "百分比魔法恢复", Description = "基于最大魔法值的百分比恢复", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true, MinValue = 0, MaxValue = GlobalConfig.MaxPercentRegen, IsPercentage = true });
        // 最终魔法恢复 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalManaRegen,
            DisplayName = "魔法恢复",
            Description = "每秒恢复的魔法值（基础恢复 + 百分比恢复）",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [DataKey.BaseManaRegen, DataKey.ManaRegenBonus, DataKey.PercentManaRegen, DataKey.FinalMana],
            Compute = (data) =>
            {
                float baseRecovery = MyMath.AttributeBonusCalculation(data.Get<float>(DataKey.BaseManaRegen), data.Get<float>(DataKey.ManaRegenBonus));
                float percentRecovery = data.Get<float>(DataKey.FinalMana) * (data.Get<float>(DataKey.PercentManaRegen) * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });

        // ========================================
        // 攻击相关 (Attack)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseAttack, DisplayName = "基础攻击力", Description = "基础攻击力", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AttackBonus, DisplayName = "攻击力加成", Description = "攻击力百分比加成", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        // 最终攻击力 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalAttack,
            DisplayName = "攻击力",
            Description = "攻击力",
            Category = DataCategory_Attribute.Computed,
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
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseAttackSpeed, DisplayName = "基础攻速", Description = "基础攻击速度", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 100f, MinValue = 0, MaxValue = 1000, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.AttackSpeedBonus, DisplayName = "攻速加成", Description = "攻击速度百分比加成", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        // 最终攻速 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalAttackSpeed,
            DisplayName = "攻速",
            Description = "攻击速度",
            Category = DataCategory_Attribute.Computed,
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
        // 攻击间隔 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.AttackInterval,
            DisplayName = "攻击间隔",
            Description = "攻击间隔：攻击一次需要的时间（秒）",
            Category = DataCategory_Attribute.Computed,
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

        DataRegistry.Register(new DataMeta { Key = DataKey.DamageAmplification, DisplayName = "伤害增幅", Description = "伤害增幅百分比", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.Penetration, DisplayName = "护甲穿透", Description = "护甲穿透值", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.Range, DisplayName = "攻击范围", Description = "攻击范围", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 100f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.Knockback, DisplayName = "击退", Description = "击退敌人的距离", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = 1000, SupportModifiers = true });

        // ========================================
        // 防御相关 (Defense)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseDefense, DisplayName = "基础防御", Description = "基础防御力", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.DefenseBonus, DisplayName = "防御加成", Description = "防御力百分比加成", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        // 最终防御 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalDefense,
            DisplayName = "防御",
            Description = "防御值",
            Category = DataCategory_Attribute.Computed,
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
        DataRegistry.Register(new DataMeta { Key = DataKey.DamageReduction, DisplayName = "伤害减免", Description = "伤害减免百分比", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxDamageReduction, IsPercentage = true, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.Shield, DisplayName = "护盾", Description = "护盾值", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.Thorns, DisplayName = "反伤", Description = "反弹受到伤害的百分比", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = 500, IsPercentage = true, SupportModifiers = true });

        // ========================================
        // 技能相关 (Skill)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.BaseSkillDamage, DisplayName = "基础技能伤害", Description = "基础技能伤害百分比", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.SkillDamageBonus, DisplayName = "技能伤害加成", Description = "技能伤害百分比加成", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        // 最终技能伤害 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalSkillDamage,
            DisplayName = "技能伤害",
            Description = "技能伤害百分比",
            Category = DataCategory_Attribute.Computed,
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
        DataRegistry.Register(new DataMeta { Key = DataKey.CooldownReduction, DisplayName = "技能冷却缩减", Description = "技能冷却缩减百分比", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxCooldownReduction, IsPercentage = true, SupportModifiers = true });

        // ========================================
        // 移动相关 (Movement)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.MoveSpeed, DisplayName = "移动速度", Description = "移动速度", Category = DataCategory_Attribute.Movement, Type = typeof(float), DefaultValue = 100f, MinValue = 0, MaxValue = GlobalConfig.MaxMoveSpeed, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.MoveSpeedBonus, DisplayName = "移动速度加成", Description = "移动速度百分比加成", Category = DataCategory_Attribute.Movement, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
        // 最终移动速度 (Computed)
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.FinalMoveSpeed,
            DisplayName = "移动速度",
            Description = "移动速度",
            Category = DataCategory_Attribute.Computed,
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
        DataRegistry.Register(new DataMeta { Key = DataKey.DodgeChance, DisplayName = "闪避几率", Description = "闪避几率", Category = DataCategory_Attribute.Dodge, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxDodgeChance, IsPercentage = true, SupportModifiers = true });

        // ========================================
        // 暴击相关 (Crit)
        // ========================================
        DataRegistry.Register(new DataMeta { Key = DataKey.CritRate, DisplayName = "暴击率", Description = "暴击率", Category = DataCategory_Attribute.Crit, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxCritRate, IsPercentage = true, SupportModifiers = true });
        DataRegistry.Register(new DataMeta { Key = DataKey.CritDamage, DisplayName = "暴击伤害", Description = "暴击伤害百分比", Category = DataCategory_Attribute.Crit, Type = typeof(float), DefaultValue = 100f, MinValue = 0, IsPercentage = true, SupportModifiers = true });
    }
}
