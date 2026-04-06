/// <summary>
/// Ability 子域 FeatureId 常量注册表
///
/// 层次结构仅用于 C# 代码导航；常量的字符串值是短名，与 .tres FeatureHandlerId 保持一致。
/// 分组路径单独定义在 Groups 子类中，供 FeatureHandlerRegistry.GetByGroup() 查询使用。
///
/// .tres 中填写：FeatureHandlerId = "Dash"（即常量的 string 值）
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

        // ============ 主动技能（FeatureHandlerId 短名）============

        public static class Active
        {
            public const string Slam = "Slam";
            public const string ChainLightning = "ChainLightning";
            public const string CircleDamage = "CircleDamage";
        }

        // ============ 移动技能 ============

        public static class Movement
        {
            public const string Dash = "Dash";
        }

        // ============ 被动 / 常驻技能 ============

        public static class Passive
        {
            public const string AuraShield = "AuraShield";
            public const string OrbitSkill = "OrbitSkill";
        }

        // ============ 投射物技能 ============

        public static class Projectile
        {
            public const string ParabolaShot = "ParabolaShot";
            public const string SineWaveShot = "SineWaveShot";
            public const string ArcShot = "ArcShot";
            public const string BezierShot = "BezierShot";
            public const string BoomerangThrow = "BoomerangThrow";
        }
    }
}
