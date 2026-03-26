using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 8】贝塞尔曲线移动。
/// <para>沿 N 阶贝塞尔曲线前进，由 <c>elapsedTime / BezierDuration</c> 驱动参数 t。OnEnter 自动将第 0 个控制点替换为当前位置。</para>
/// <para>
/// 必须设置 <c>BezierPoints</c>（含终点在内的完整控制点数组，至少 2 点）和 <c>BezierDuration</c>。
/// 可选：<c>BezierUniformSpeed = true</c> 启用弧长参数化匀速模式，<c>DestroyOnComplete</c>。
/// 若 <c>BezierPoints</c> 为空则降级为直线（以 <c>TargetPoint</c> 作终点）。
/// </para>
/// <para>【典型用途】弧形投射物、技能抛物线、沿预设动画曲线移动的特效体。</para>
/// </summary>
public class BezierCurveStrategy : IMovementStrategy
{
    private Vector2[] _finalPoints = System.Array.Empty<Vector2>();
    private float[]? _lengthLut;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.BezierCurve, () => new BezierCurveStrategy());
    }

    public void OnEnter(IEntity entity, Data data, MovementParams @params)
    {
        if (entity is not Node2D node) return;

        if (@params.BezierPoints != null && @params.BezierPoints.Length >= 2)
        {
            // ⚠️ Clone 后修改，避免污染调用方传入的共享数组
            _finalPoints = (Vector2[])@params.BezierPoints.Clone();
            _finalPoints[0] = node.GlobalPosition;
        }
        else
        {
            _finalPoints = System.Array.Empty<Vector2>();
        }

        _lengthLut = null;
        if (@params.BezierUniformSpeed && _finalPoints.Length >= 2)
        {
            _lengthLut = BezierCurve.BuildLengthTable(_finalPoints, 64);
        }
    }

    public MovementUpdateResult Update(IEntity entity, Data data, float delta, MovementParams @params)
    {
        if (entity is not Node2D node) return MovementUpdateResult.Continue();
        if (_finalPoints.Length < 2) return MovementUpdateResult.Continue();

        float duration = @params.BezierDuration;
        if (duration <= 0f) return MovementUpdateResult.Continue();

        float t = Mathf.Clamp((@params.ElapsedTime + delta) / duration, 0f, 1f);

        Vector2 newPos = (@params.BezierUniformSpeed && _lengthLut != null)
            ? BezierCurve.EvaluateUniform(_finalPoints, t, _lengthLut)
            : BezierCurve.Evaluate(_finalPoints, t);

        Vector2 toTarget = newPos - node.GlobalPosition;
        float displacement = toTarget.Length();
        data.Set(DataKey.Velocity, displacement > 0.001f ? toTarget / Mathf.Max(delta, 0.001f) : Vector2.Zero);

        if (t >= 1f) return MovementUpdateResult.Complete();
        return MovementUpdateResult.Continue(displacement);
    }
}
