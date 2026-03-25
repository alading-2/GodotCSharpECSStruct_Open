using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 8】贝塞尔曲线运动（支持任意阶）
/// <para>
/// 沿 N 阶贝塞尔曲线运动，通过 t∈[0,1] 参数化。
/// 核心求值委托给通用工具 BezierCurve（De Casteljau 算法）。
/// </para>
/// <para>
/// 控制点来源（优先级从高到低）：
/// 1. BezierControlPoints (Vector2[]) — 任意阶完整控制点数组
/// 2. BezierStartPoint + BezierControlPoint1/2 + MoveTargetPoint — 三阶向后兼容
/// </para>
/// <para>
/// 可选功能：
/// - BezierUniformSpeed = true 时启用弧长参数化（匀速运动）
/// - t 由 MoveElapsedTime / BezierDuration 驱动，t>=1 时完成
/// </para>
/// </summary>
public class BezierCurveStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.BezierCurve, new BezierCurveStrategy());
    }

    public void OnEnter(IEntity entity, Data data)
    {
        if (entity is not Node2D node) return;

        // 记录起始点
        data.Set(DataKey.BezierStartPoint, node.GlobalPosition);

        // 如果用户没有预设 BezierControlPoints，从旧版 ControlPoint1/2 构建三阶兼容数组
        var existingPoints = data.Get<Vector2[]>(DataKey.BezierControlPoints);
        if (existingPoints == null || existingPoints.Length < 2)
        {
            Vector2 p0 = node.GlobalPosition;
            Vector2 p1 = data.Get<Vector2>(DataKey.BezierControlPoint1);
            Vector2 p2 = data.Get<Vector2>(DataKey.BezierControlPoint2);
            Vector2 p3 = data.Get<Vector2>(DataKey.MoveTargetPoint);
            data.Set(DataKey.BezierControlPoints, new[] { p0, p1, p2, p3 });
        }
        else
        {
            // 用户提供了完整数组，将第一个点替换为当前位置（起始点）
            existingPoints[0] = node.GlobalPosition;
            data.Set(DataKey.BezierControlPoints, existingPoints);
        }

        // 如果启用匀速模式，预计算弧长查找表
        if (data.Get<bool>(DataKey.BezierUniformSpeed))
        {
            var points = data.Get<Vector2[]>(DataKey.BezierControlPoints);
            var lut = BezierCurve.BuildLengthTable(points, 64);
            data.Set(DataKey.BezierLengthLut, lut);
        }
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        if (entity is not Node2D node) return 0f;

        float duration = data.Get<float>(DataKey.BezierDuration);
        if (duration <= 0f) return 0f;

        float elapsed = data.Get<float>(DataKey.MoveElapsedTime);
        float t = Mathf.Clamp((elapsed + delta) / duration, 0f, 1f);

        var points = data.Get<Vector2[]>(DataKey.BezierControlPoints);
        if (points == null || points.Length < 2) return 0f;

        // 求值：根据是否匀速选择不同路径
        Vector2 newPos;
        bool uniform = data.Get<bool>(DataKey.BezierUniformSpeed);
        if (uniform)
        {
            var lut = data.Get<float[]>(DataKey.BezierLengthLut);
            newPos = lut != null
                ? BezierCurve.EvaluateUniform(points, t, lut)
                : BezierCurve.Evaluate(points, t);
        }
        else
        {
            newPos = BezierCurve.Evaluate(points, t);
        }

        // 将绝对位置意图转换为速度，由调度器统一执行位移
        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();
        Vector2 velocity = displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero;
        data.Set(DataKey.Velocity, velocity);

        // t>=1 时完成
        if (t >= 1f) return -1f;

        return displacement;
    }
}
