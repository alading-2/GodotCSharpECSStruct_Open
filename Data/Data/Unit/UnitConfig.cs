using Godot;

namespace Slime.Config.Units
{
    /// <summary>
    /// 单位配置基类
    /// 包含 Player 和 Enemy 共有的属性
    /// 属性名必须与 DataKey 中的字符串常量一致，以便自动加载
    /// </summary>
    public abstract partial class UnitConfig : Resource
    {
        /// <summary>
        /// 单位名称
        /// </summary>
        [ExportGroup("基础信息")]
        [DataKey(DataKey.Name)]
        [Export] public string Name { get; set; } = "未命名单位";
        /// <summary>
        /// 所属队伍
        /// </summary>
        [DataKey(DataKey.Team)]
        [Export] public Team Team { get; set; }

        /// <summary>
        /// 死亡类型
        /// </summary>
        [DataKey(DataKey.DeathType)]
        [Export] public DeathType DeathType { get; set; }

        /// <summary>
        /// 视觉场景路径 (PackedScene)
        /// Data.LoadFromResource 会自动读取此属性
        /// </summary>
        [ExportGroup("视觉")]
        [DataKey(DataKey.VisualScenePath)]
        [Export] public PackedScene? VisualScenePath { get; set; }

        /// <summary>
        /// 血条显示高度
        /// </summary>
        [DataKey(DataKey.HealthBarHeight)]
        [Export] public float HealthBarHeight { get; set; } = 100f;

        /// <summary>
        /// 基础最大生命值
        /// </summary>
        [ExportGroup("生命属性")]
        [DataKey(DataKey.BaseHp)]
        [Export] public float BaseHp { get; set; }
        /// <summary>
        /// 基础生命回复 (每秒)
        /// </summary>
        [DataKey(DataKey.BaseHpRegen)]
        [Export] public float BaseHpRegen { get; set; } = 0f;
        /// <summary>
        /// 吸血比例 (%)
        /// </summary>
        [DataKey(DataKey.LifeSteal)]
        [Export] public float LifeSteal { get; set; } = 0f;

        /// <summary>
        /// 基础攻击力
        /// </summary>
        [ExportGroup("攻击属性")]
        [DataKey(DataKey.BaseAttack)]
        [Export] public float BaseAttack { get; set; }
        /// <summary>
        /// 基础攻击速度
        /// </summary>
        [DataKey(DataKey.BaseAttackSpeed)]
        [Export] public float BaseAttackSpeed { get; set; } = 100f;
        /// <summary>
        /// 攻击距离/范围
        /// </summary>
        [DataKey(DataKey.AttackRange)]
        [Export] public float AttackRange { get; set; }
        /// <summary>
        /// 暴击率 (%)
        /// </summary>
        [DataKey(DataKey.CritRate)]
        [Export] public float CritRate { get; set; }
        /// <summary>
        /// 暴击伤害倍率 (%)
        /// </summary>
        [DataKey(DataKey.CritDamage)]
        [Export] public float CritDamage { get; set; }
        /// <summary>
        /// 护甲穿透
        /// </summary>
        [DataKey(DataKey.Penetration)]
        [Export] public float Penetration { get; set; }

        /// <summary>
        /// 基础防御力/护甲
        /// </summary>
        [ExportGroup("防御属性")]
        [DataKey(DataKey.BaseDefense)]
        [Export] public float BaseDefense { get; set; }
        /// <summary>
        /// 伤害减免 (%)
        /// </summary>
        [DataKey(DataKey.DamageReduction)]
        [Export] public float DamageReduction { get; set; }

        /// <summary>
        /// 移动速度
        /// </summary>
        [ExportGroup("移动属性")]
        [DataKey(DataKey.MoveSpeed)]
        [Export] public float MoveSpeed { get; set; } = 100f;

        /// <summary>
        /// 闪避率 (%)
        /// </summary>
        [DataKey(DataKey.DodgeChance)]
        [Export] public float DodgeChance { get; set; }
    }
}
