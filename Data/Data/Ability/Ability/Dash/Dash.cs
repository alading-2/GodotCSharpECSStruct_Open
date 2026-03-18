
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 冲刺技能执行器
/// 
/// 触发方式：Manual（手动，玩家按键），充能系统（最多 2 层，每层 5 秒回充）
/// 目标选择：None（直接作用于施法者自身）
/// 特效：Effect_004龙卷风（附着到施法者，跟随移动，播完自动销毁）
/// 效果：向施法者当前移动方向（或面朝方向）瞬间位移 AbilityEffectRadius 距离
/// </summary>
public class DashExecutor : IAbilityExecutor
{
    private static readonly Log _log = new(nameof(DashExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        AbilityExecutorRegistry.Register("Dash", new DashExecutor());
    }

    public AbilityExecutedResult Execute(CastContext context)
    {
        var caster = context.Caster;
        var ability = context.Ability;

        // 验证施法者和技能数据的合法性
        if (caster == null || ability == null)
        {
            _log.Error("冲刺失败：施法者或技能为空");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 冲刺需要 Node2D 来直接操作 GlobalPosition
        if (caster is not Node2D casterNode)
        {
            _log.Error("冲刺失败：施法者不是 Node2D");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        // 1. 获取冲刺距离
        // 从技能的 Data 容器中读取 AbilityEffectRadius 键值，若未配置则使用默认值 300
        var range = ability.Data.Get<float>(DataKey.AbilityEffectRadius);
        if (range <= 0) range = 300f;

        // 2. 确定冲刺方向：优先使用当前移动速度方向，否则用 FlipH 判断面朝方向
        // 读取施法者当前的 Velocity 数据
        var moveDir = caster.Data.Get<Vector2>(DataKey.Velocity);
        Vector2 dashDir;

        // 如果当前有明显的移动趋势，则沿着移动方向冲刺
        if (moveDir.LengthSquared() > 0.01f)
        {
            dashDir = moveDir.Normalized();
        }
        else
        {
            // 如果处于静止状态，则根据 Sprite 的翻转状态确定左右朝向作为冲刺方向
            var sprite = casterNode.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
            bool facingLeft = sprite?.FlipH ?? false;
            dashDir = facingLeft ? Vector2.Left : Vector2.Right;
        }

        // 3. 执行物理位移
        // 计算目标位置并在当前帧瞬间移动 GlobalPosition
        var targetPos = casterNode.GlobalPosition + dashDir * range;
        casterNode.GlobalPosition = targetPos;

        // 4. 生成附加特效（跟随施法者移动，播放完毕后由 EffectComponent 自动销毁）
        // 使用 ResourceManagement.Load 安全加载资源，避免硬编码路径
        var effectScene = ResourceManagement.Load<PackedScene>(ResourcePaths.Asset_Effect_lrsc3, ResourceCategory.Asset);
        if (effectScene != null)
        {
            // 在施法者当前位置生成特效，并通过 Host 参数将其绑定到施法者上
            EffectTool.Spawn(casterNode.GlobalPosition, new EffectSpawnOptions(
                VisualScene: effectScene,
                Host: casterNode,
                Name: "冲刺特效"
            ));
        }

        _log.Info($"冲刺执行: 方向 {dashDir}, 距离 {range}, 落点 {targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }
}
