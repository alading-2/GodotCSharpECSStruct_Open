using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式 6】螺旋运动
/// <para>环绕运动 + 半径平滑渐变。常用于螺旋弹道、龙卷风特效或吸附效果。</para>
/// </summary>
public class SpiralStrategy : IMovementStrategy
{
    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.Spiral, new SpiralStrategy());
    }

    public float Update(IEntity entity, Data data, float delta)
    {
        float radius = data.Get<float>(DataKey.OrbitRadius);
        float targetRadius = data.Get<float>(DataKey.OrbitTargetRadius);

        // 线性插值改变半径
        if (!Mathf.IsEqualApprox(radius, targetRadius))
        {
            float radialSpeed = data.Get<float>(DataKey.OrbitRadialSpeed);
            if (radialSpeed <= 0f) radialSpeed = 50f;

            float dr = radialSpeed * delta;
            radius = targetRadius > radius
                ? Mathf.Min(radius + dr, targetRadius)
                : Mathf.Max(radius - dr, targetRadius);

            data.Set(DataKey.OrbitRadius, radius);
        }

        // 复用 OrbitPoint 策略
        var orbitStrategy = MovementStrategyRegistry.Get(MoveMode.OrbitPoint);
        return orbitStrategy?.Update(entity, data, delta) ?? 0f;
    }
}
