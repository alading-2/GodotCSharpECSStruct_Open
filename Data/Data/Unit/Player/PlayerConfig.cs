using Godot;

namespace Slime.Config.Units
{
    [GlobalClass]
    public partial class PlayerConfig : UnitConfig
    {
        /// <summary>
        /// 基础最大法力值
        /// </summary>
        [ExportGroup("玩家专有")]
        [DataKey(DataKey.BaseMana)]
        [Export] public float BaseMana { get; set; } = 50f;
        /// <summary>
        /// 当前法力值
        /// </summary>
        [DataKey(DataKey.CurrentMana)]
        [Export] public float CurrentMana { get; set; } = 50f;
        /// <summary>
        /// 基础法力回复 (每秒)
        /// </summary>
        [DataKey(DataKey.BaseManaRegen)]
        [Export] public float BaseManaRegen { get; set; } = 2f;

        /// <summary>
        /// 拾取范围
        /// </summary>
        [DataKey(DataKey.PickupRange)]
        [Export] public float PickupRange { get; set; } = 100f;
        /// <summary>
        /// 基础技能伤害
        /// </summary>
        [DataKey(DataKey.BaseSkillDamage)]
        [Export] public float BaseSkillDamage { get; set; } = 100f;
        /// <summary>
        /// 冷却缩减 (%)
        /// </summary>
        [DataKey(DataKey.CooldownReduction)]
        [Export] public float CooldownReduction { get; set; } = 0f;
    }
}
