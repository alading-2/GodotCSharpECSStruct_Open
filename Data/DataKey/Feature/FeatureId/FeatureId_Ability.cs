/// <summary>
/// Ability 子域 FeatureId 常量注册表
///
/// 层次结构同时用于 C# 代码导航和完整唯一 ID 命名。
/// 常量字符串值统一使用完整 FeatureId，如 "技能.位移.冲刺"。
/// 分组路径单独定义在 Groups 子类中，供 FeatureHandlerRegistry.GetByGroup() 查询使用。
///
/// .tres 中填写：FeatureHandlerId = "技能.位移.冲刺"
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
            public const string Root = "技能";
            public const string Active = "技能.主动";
            public const string Movement = "技能.位移";
            public const string Passive = "技能.被动";
            public const string Projectile = "技能.投射物";
        }

        // ============ 主动技能（完整 FeatureHandlerId）============

        public static class Active
        {
            public const string Slam = "技能.主动.猛击";
            public const string ChainLightning = "技能.主动.连锁闪电";
        }

        // ============ 移动技能 ============

        public static class Movement
        {
            public const string Dash = "技能.位移.冲刺";
        }

        // ============ 被动 / 常驻技能 ============

        public static class Passive
        {
            public const string AuraShield = "技能.被动.光环护盾";
            public const string OrbitSkill = "技能.被动.环绕技能";
            public const string CircleDamage = "技能.被动.圆环伤害";

        }

        // ============ 投射物技能 ============

        public static class Projectile
        {
            public const string ParabolaShot = "技能.投射物.抛物线射击";
            public const string SineWaveShot = "技能.投射物.正弦波射击";
            public const string ArcShot = "技能.投射物.圆弧射击";
            public const string BezierShot = "技能.投射物.贝塞尔射击";
            public const string BoomerangThrow = "技能.投射物.回旋镖投掷";
        }
    }
}
