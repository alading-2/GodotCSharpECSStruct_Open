/// <summary>
/// 数据键定义 - 属性域
/// 所有键均为 DataMeta 静态实例，直接嵌入注册信息
/// </summary>
public static partial class DataKey
{
    // ========================================
    // 生命相关 (Health)
    // ========================================
    // 基础生命值
    public static readonly DataMeta BaseHp = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseHp), DisplayName = "基础生命值", Description = "基础生命值", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 10f, MinValue = 0, SupportModifiers = true });

    // 生命值加成
    public static readonly DataMeta HpBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(HpBonus), DisplayName = "生命值加成", Description = "生命值百分比加成", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });

    // 生命值
    public static readonly DataMeta FinalHp = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalHp),
            DisplayName = "生命值",
            Description = "生命值",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseHp), nameof(HpBonus)],
            Compute = (data) =>
            {
                float baseHp = data.Get<float>(nameof(BaseHp));
                float bonus = data.Get<float>(nameof(HpBonus));
                return MyMath.AttributeBonusCalculation(baseHp, bonus);
            }
        });

    // 生命百分比
    public static readonly DataMeta HpPercent = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(HpPercent),
            DisplayName = "生命百分比",
            Description = "当前生命值百分比",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(CurrentHp), nameof(FinalHp)],
            Compute = (data) =>
            {
                float current = data.Get<float>(nameof(CurrentHp));
                float max = data.Get<float>(nameof(FinalHp));
                return max > 0 ? (current / max) * 100f : 0f;
            }
        });

    // 当前生命值
    public static readonly DataMeta CurrentHp = DataRegistry.Register(
        new DataMeta { Key = nameof(CurrentHp), DisplayName = "当前生命值", Description = "当前生命值", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    // 基础生命恢复
    public static readonly DataMeta BaseHpRegen = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseHpRegen), DisplayName = "基础生命恢复", Description = "每秒恢复的基础生命值", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true });

    // 生命恢复加成
    public static readonly DataMeta HpRegenBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(HpRegenBonus), DisplayName = "生命恢复加成", Description = "生命恢复百分比加成", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 百分比生命恢复
    public static readonly DataMeta PercentHpRegen = DataRegistry.Register(
        new DataMeta { Key = nameof(PercentHpRegen), DisplayName = "百分比生命恢复", Description = "每秒基于最大生命值的百分比恢复", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxPercentRegen, IsPercentage = true, SupportModifiers = true });

    // 生命恢复
    public static readonly DataMeta FinalHpRegen = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalHpRegen),
            DisplayName = "生命恢复",
            Description = "每秒恢复的生命值",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseHpRegen), nameof(HpRegenBonus), nameof(PercentHpRegen), nameof(FinalHp)],
            Compute = (data) =>
            {
                float baseRecovery = MyMath.AttributeBonusCalculation(data.Get<float>(nameof(BaseHpRegen)), data.Get<float>(nameof(HpRegenBonus)));
                float percentRecovery = data.Get<float>(nameof(FinalHp)) * (data.Get<float>(nameof(PercentHpRegen)) * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });

    // 吸血百分比
    public static readonly DataMeta LifeSteal = DataRegistry.Register(
        new DataMeta { Key = nameof(LifeSteal), DisplayName = "吸血百分比", Description = "吸血百分比", Category = DataCategory_Attribute.Health, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });

    // ========================================
    // 魔法相关 (Mana)
    // ========================================
    // 基础魔法值
    public static readonly DataMeta BaseMana = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseMana), DisplayName = "基础魔法值", Description = "基础魔法值", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true, MinValue = 0 });

    // 魔法加成
    public static readonly DataMeta ManaBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(ManaBonus), DisplayName = "魔法加成", Description = "魔法值百分比加成", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });

    // 魔法值
    public static readonly DataMeta FinalMana = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalMana),
            DisplayName = "魔法值",
            Description = "魔法值",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseMana), nameof(ManaBonus)],
            Compute = (data) =>
            {
                float baseMana = data.Get<float>(nameof(BaseMana));
                float bonus = data.Get<float>(nameof(ManaBonus));
                return MyMath.AttributeBonusCalculation(baseMana, bonus);
            }
        });

    // 当前法力值
    public static readonly DataMeta CurrentMana = DataRegistry.Register(
        new DataMeta { Key = nameof(CurrentMana), DisplayName = "当前法力值", Description = "当前法力值", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    // 魔法百分比
    public static readonly DataMeta ManaPercent = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(ManaPercent),
            DisplayName = "魔法百分比",
            Description = "当前魔法值百分比",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(CurrentMana), nameof(FinalMana)],
            Compute = (data) =>
            {
                float current = data.Get<float>(nameof(CurrentMana));
                float max = data.Get<float>(nameof(FinalMana));
                return max > 0 ? (current / max) * 100f : 0f;
            }
        });

    // 基础魔法恢复
    public static readonly DataMeta BaseManaRegen = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseManaRegen), DisplayName = "基础魔法恢复", Description = "每秒恢复的基础魔法值", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true });

    // 魔法恢复加成
    public static readonly DataMeta ManaRegenBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(ManaRegenBonus), DisplayName = "魔法恢复加成", Description = "魔法恢复百分比加成", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true, IsPercentage = true });

    // 百分比魔法恢复
    public static readonly DataMeta PercentManaRegen = DataRegistry.Register(
        new DataMeta { Key = nameof(PercentManaRegen), DisplayName = "百分比魔法恢复", Description = "基于最大魔法值的百分比恢复", Category = DataCategory_Attribute.Mana, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true, MinValue = 0, MaxValue = GlobalConfig.MaxPercentRegen, IsPercentage = true });

    // 魔法恢复
    public static readonly DataMeta FinalManaRegen = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalManaRegen),
            DisplayName = "魔法恢复",
            Description = "每秒恢复的魔法值（基础恢复 + 百分比恢复）",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseManaRegen), nameof(ManaRegenBonus), nameof(PercentManaRegen), nameof(FinalMana)],
            Compute = (data) =>
            {
                float baseRecovery = MyMath.AttributeBonusCalculation(data.Get<float>(nameof(BaseManaRegen)), data.Get<float>(nameof(ManaRegenBonus)));
                float percentRecovery = data.Get<float>(nameof(FinalMana)) * (data.Get<float>(nameof(PercentManaRegen)) * 0.01f);
                return baseRecovery + percentRecovery;
            }
        });

    // ========================================
    // 攻击相关 (Attack)
    // ========================================
    // 基础攻击力
    public static readonly DataMeta BaseAttack = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseAttack), DisplayName = "基础攻击力", Description = "基础攻击力", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 攻击力加成
    public static readonly DataMeta AttackBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackBonus), DisplayName = "攻击力加成", Description = "攻击力百分比加成", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 攻击力
    public static readonly DataMeta FinalAttack = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalAttack),
            DisplayName = "攻击力",
            Description = "攻击力",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseAttack), nameof(AttackBonus)],
            Compute = (data) =>
            {
                float baseAttack = data.Get<float>(nameof(BaseAttack));
                float bonus = data.Get<float>(nameof(AttackBonus));
                return MyMath.AttributeBonusCalculation(baseAttack, bonus);
            }
        });

    // 基础攻速
    public static readonly DataMeta BaseAttackSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseAttackSpeed), DisplayName = "基础攻速", Description = "基础攻击速度", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 100f, MinValue = 0, MaxValue = 1000, SupportModifiers = true });

    // 攻速加成
    public static readonly DataMeta AttackSpeedBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackSpeedBonus), DisplayName = "攻速加成", Description = "攻击速度百分比加成", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 攻速
    public static readonly DataMeta FinalAttackSpeed = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalAttackSpeed),
            DisplayName = "攻速",
            Description = "攻击速度",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseAttackSpeed), nameof(AttackSpeedBonus)],
            Compute = (data) =>
            {
                float baseSpeed = data.Get<float>(nameof(BaseAttackSpeed));
                float bonus = data.Get<float>(nameof(AttackSpeedBonus));
                return MyMath.AttributeBonusCalculation(baseSpeed, bonus);
            }
        });

    // 攻击间隔
    public static readonly DataMeta AttackInterval = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(AttackInterval),
            DisplayName = "攻击间隔",
            Description = "攻击间隔：攻击一次需要的时间（秒）",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 1f,
            SupportModifiers = false,
            Dependencies = [nameof(FinalAttackSpeed)],
            Compute = (data) =>
            {
                float speed = data.Get<float>(nameof(FinalAttackSpeed));
                return 1f / (speed / 100f);
            }
        });

    // 伤害增幅
    public static readonly DataMeta DamageAmplification = DataRegistry.Register(
        new DataMeta { Key = nameof(DamageAmplification), DisplayName = "伤害增幅", Description = "伤害增幅百分比", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 护甲穿透
    public static readonly DataMeta Penetration = DataRegistry.Register(
        new DataMeta { Key = nameof(Penetration), DisplayName = "护甲穿透", Description = "护甲穿透值", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 攻击范围
    public static readonly DataMeta AttackRange = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackRange), DisplayName = "攻击范围", Description = "攻击范围", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 100f, MinValue = 0, SupportModifiers = true });

    // 攻击前摇
    public static readonly DataMeta AttackWindUpTime = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackWindUpTime), DisplayName = "攻击前摇", Description = "攻击前摇时间", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 攻击后摇
    public static readonly DataMeta AttackRecoveryTime = DataRegistry.Register(
        new DataMeta { Key = nameof(AttackRecoveryTime), DisplayName = "攻击后摇", Description = "攻击后摇时间", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 击退
    public static readonly DataMeta Knockback = DataRegistry.Register(
        new DataMeta { Key = nameof(Knockback), DisplayName = "击退", Description = "击退敌人的距离", Category = DataCategory_Attribute.Attack, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = 1000, SupportModifiers = true });

    // ========================================
    // 防御相关 (Defense)
    // ========================================
    // 基础防御
    public static readonly DataMeta BaseDefense = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseDefense), DisplayName = "基础防御", Description = "基础防御力", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 防御加成
    public static readonly DataMeta DefenseBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(DefenseBonus), DisplayName = "防御加成", Description = "防御力百分比加成", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 防御
    public static readonly DataMeta FinalDefense = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalDefense),
            DisplayName = "防御",
            Description = "防御值",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(BaseDefense), nameof(DefenseBonus)],
            Compute = (data) =>
            {
                float baseDefense = data.Get<float>(nameof(BaseDefense));
                float bonus = data.Get<float>(nameof(DefenseBonus));
                return MyMath.AttributeBonusCalculation(baseDefense, bonus);
            }
        });

    // 伤害减免
    public static readonly DataMeta DamageReduction = DataRegistry.Register(
        new DataMeta { Key = nameof(DamageReduction), DisplayName = "伤害减免", Description = "伤害减免百分比", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxDamageReduction, IsPercentage = true, SupportModifiers = true });

    // 受伤倍率（1=正常，<1减伤，>1易伤）
    public static readonly DataMeta DamageTakenMultiplier = DataRegistry.Register(
        new DataMeta { Key = nameof(DamageTakenMultiplier), DisplayName = "受伤倍率", Description = "最终受到伤害倍率", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 1f, MinValue = 0f, SupportModifiers = true });

    // 护盾
    public static readonly DataMeta Shield = DataRegistry.Register(
        new DataMeta { Key = nameof(Shield), DisplayName = "护盾", Description = "护盾值", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 反伤
    public static readonly DataMeta Thorns = DataRegistry.Register(
        new DataMeta { Key = nameof(Thorns), DisplayName = "反伤", Description = "反弹受到伤害的百分比", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = 500, IsPercentage = true, SupportModifiers = true });

    // 护甲
    public static readonly DataMeta Armor = DataRegistry.Register(
        new DataMeta { Key = nameof(Armor), DisplayName = "护甲", Description = "护甲（兼容旧系统）", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 无敌时间
    public static readonly DataMeta InvincibilityTime = DataRegistry.Register(
        new DataMeta { Key = nameof(InvincibilityTime), DisplayName = "无敌时间", Description = "无敌时间", Category = DataCategory_Attribute.Defense, Type = typeof(float), DefaultValue = 0f, MinValue = 0 });

    // ========================================
    // 技能相关 (Skill)
    // ========================================
    // 基础技能伤害
    public static readonly DataMeta BaseSkillDamage = DataRegistry.Register(
        new DataMeta { Key = nameof(BaseSkillDamage), DisplayName = "基础技能伤害", Description = "基础技能伤害百分比", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 技能伤害加成
    public static readonly DataMeta SkillDamageBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(SkillDamageBonus), DisplayName = "技能伤害加成", Description = "技能伤害百分比加成", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 技能伤害
    public static readonly DataMeta FinalSkillDamage = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalSkillDamage),
            DisplayName = "技能伤害",
            Description = "技能伤害百分比",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            IsPercentage = true,
            Dependencies = [nameof(BaseSkillDamage), nameof(SkillDamageBonus)],
            Compute = (data) =>
            {
                float baseSkillDamage = data.Get<float>(nameof(BaseSkillDamage));
                float bonus = data.Get<float>(nameof(SkillDamageBonus));
                return MyMath.AttributeBonusCalculation(baseSkillDamage, bonus);
            }
        });

    // 技能冷却缩减
    public static readonly DataMeta CooldownReduction = DataRegistry.Register(
        new DataMeta { Key = nameof(CooldownReduction), DisplayName = "技能冷却缩减", Description = "技能冷却缩减百分比", Category = DataCategory_Attribute.Skill, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxCooldownReduction, IsPercentage = true, SupportModifiers = true });

    // ========================================
    // 移动相关 (Movement)
    // ========================================
    // 移动速度
    public static readonly DataMeta MoveSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveSpeed), DisplayName = "移动速度", Description = "移动速度", Category = DataCategory_Attribute.Movement, Type = typeof(float), DefaultValue = 100f, MinValue = 0, MaxValue = GlobalConfig.MaxMoveSpeed, SupportModifiers = true });

    // 移动速度加成
    public static readonly DataMeta MoveSpeedBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(MoveSpeedBonus), DisplayName = "移动速度加成", Description = "移动速度百分比加成", Category = DataCategory_Attribute.Movement, Type = typeof(float), DefaultValue = 0f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 移动速度
    public static readonly DataMeta FinalMoveSpeed = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(FinalMoveSpeed),
            DisplayName = "移动速度",
            Description = "移动速度",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(MoveSpeed), nameof(MoveSpeedBonus)],
            Compute = (data) =>
            {
                float moveSpeed = data.Get<float>(nameof(MoveSpeed));
                float bonus = data.Get<float>(nameof(MoveSpeedBonus));
                return MyMath.AttributeBonusCalculation(moveSpeed, bonus);
            }
        });

    // ========================================
    // 闪避相关 (Dodge)
    // ========================================
    // 闪避几率
    public static readonly DataMeta DodgeChance = DataRegistry.Register(
        new DataMeta { Key = nameof(DodgeChance), DisplayName = "闪避几率", Description = "闪避几率", Category = DataCategory_Attribute.Dodge, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxDodgeChance, IsPercentage = true, SupportModifiers = true });

    // ========================================
    // 暴击相关 (Crit)
    // ========================================
    // 暴击率
    public static readonly DataMeta CritRate = DataRegistry.Register(
        new DataMeta { Key = nameof(CritRate), DisplayName = "暴击率", Description = "暴击率", Category = DataCategory_Attribute.Crit, Type = typeof(float), DefaultValue = 0f, MinValue = 0, MaxValue = GlobalConfig.MaxCritRate, IsPercentage = true, SupportModifiers = true });

    // 暴击伤害
    public static readonly DataMeta CritDamage = DataRegistry.Register(
        new DataMeta { Key = nameof(CritDamage), DisplayName = "暴击伤害", Description = "暴击伤害百分比", Category = DataCategory_Attribute.Crit, Type = typeof(float), DefaultValue = 100f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // ========================================
    // 资源系统 (Resource)
    // ========================================
    // 拾取范围
    public static readonly DataMeta PickupRange = DataRegistry.Register(
        new DataMeta { Key = nameof(PickupRange), DisplayName = "拾取范围", Description = "拾取范围", Category = DataCategory_Attribute.Resource, Type = typeof(float), DefaultValue = 300f, MinValue = 0, SupportModifiers = true });

    // 经验倍率
    public static readonly DataMeta ExpGain = DataRegistry.Register(
        new DataMeta { Key = nameof(ExpGain), DisplayName = "经验倍率", Description = "经验倍率", Category = DataCategory_Attribute.Resource, Type = typeof(float), DefaultValue = 1f, MinValue = 0, IsPercentage = true, SupportModifiers = true });

    // 幸运值
    public static readonly DataMeta LuckBonus = DataRegistry.Register(
        new DataMeta { Key = nameof(LuckBonus), DisplayName = "幸运值", Description = "幸运值", Category = DataCategory_Attribute.Resource, Type = typeof(float), DefaultValue = 0f, SupportModifiers = true });

    // 磁铁吸附速度
    public static readonly DataMeta MagnetSpeed = DataRegistry.Register(
        new DataMeta { Key = nameof(MagnetSpeed), DisplayName = "磁铁吸附速度", Description = "磁铁吸附速度", Category = DataCategory_Attribute.Resource, Type = typeof(float), DefaultValue = 0f, MinValue = 0, SupportModifiers = true });

    // 磁铁开关
    public static readonly DataMeta MagnetEnabled = DataRegistry.Register(
        new DataMeta { Key = nameof(MagnetEnabled), DisplayName = "磁铁开关", Description = "磁铁开关", Category = DataCategory_Attribute.Resource, Type = typeof(bool), DefaultValue = false, SupportModifiers = true });

    // ========================================
    // 计算数据（只读） (Computed)
    // ========================================
    // 有效生命
    public static readonly DataMeta EffectiveHp = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(EffectiveHp),
            DisplayName = "有效生命",
            Description = "有效生命值（考虑防御后的生命值）",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(FinalHp), nameof(FinalDefense)],
            Compute = (data) =>
            {
                float hp = data.Get<float>(nameof(FinalHp));
                float defense = data.Get<float>(nameof(FinalDefense));
                return hp * (1 + defense / 100f);
            }
        });

    // DPS
    public static readonly DataMeta DPS = DataRegistry.Register(
        new DataMeta
        {
            Key = nameof(DPS),
            DisplayName = "DPS",
            Description = "每秒伤害（估算）",
            Category = DataCategory_Attribute.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = [nameof(FinalAttack), nameof(FinalAttackSpeed), nameof(CritRate), nameof(CritDamage)],
            Compute = (data) =>
            {
                float attack = data.Get<float>(nameof(FinalAttack));
                float speed = data.Get<float>(nameof(FinalAttackSpeed));
                float critRate = data.Get<float>(nameof(CritRate));
                float critDamage = data.Get<float>(nameof(CritDamage));
                float critMultiplier = 1f + (critRate / 100f) * (critDamage / 100f);
                return attack * (speed / 100f) * critMultiplier;
            }
        });
}
