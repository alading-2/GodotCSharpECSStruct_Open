/// <summary>
/// Ability 子域 FeatureId 常量注册表
///
/// 层次结构同时用于 C# 代码导航和完整唯一 ID 命名。
/// 常量字符串值统一使用完整 FeatureId，如 "Ability.Movement.Dash"。
/// 分组路径单独定义在 Groups 子类中，供 FeatureHandlerRegistry.GetByGroup() 查询使用。
///
/// .tres 中填写：FeatureHandlerId = "Ability.Movement.Dash"
/// C# 代码引用：FeatureId.Ability.Movement.Dash
/// 查询某分组所有处理器：FeatureHandlerRegistry.GetByGroup(FeatureId.Ability.Groups.Movement)
/// </summary>
public static partial class FeatureId
{
    public static class Ability
    {
        // ============ 分组路径常量（仅用于 GetByGroup 查询，不作为 FeatureHandlerId）============

        public static class Groups
        {
            public const string Root = "Ability";
            public const string Active = "Ability.Active";
            public const string Movement = "Ability.Movement";
            public const string Passive = "Ability.Passive";
            public const string Projectile = "Ability.Projectile";
        }

        // ============ 主动技能（完整 FeatureHandlerId）============

        public static class Active
        {
            public const string Slam = "Ability.Active.Slam";
            public const string ChainLightning = "Ability.Active.ChainLightning";
            public const string CircleDamage = "Ability.Active.CircleDamage";
        }

        // ============ 移动技能 ============

        public static class Movement
        {
            public const string Dash = "Ability.Movement.Dash";
        }

        // ============ 被动 / 常驻技能 ============

        public static class Passive
        {
            public const string AuraShield = "Ability.Passive.AuraShield";
            public const string OrbitSkill = "Ability.Passive.OrbitSkill";
        }

        // ============ 投射物技能 ============

        public static class Projectile
        {
            public const string ParabolaShot = "Ability.Projectile.ParabolaShot";
            public const string SineWaveShot = "Ability.Projectile.SineWaveShot";
            public const string ArcShot = "Ability.Projectile.ArcShot";
            public const string BezierShot = "Ability.Projectile.BezierShot";
            public const string BoomerangThrow = "Ability.Projectile.BoomerangThrow";
        }
    }
}
