using Godot;

/// <summary>
/// <para>
/// 【用法】
/// <list type="bullet">
/// <item><c>MovementUpdateResult.Continue(displacement)</c>：本帧正常运动，传入估算位移量（&gt;=0）</item>
/// <item><c>MovementUpdateResult.Continue(displacement, facingDirection)</c>：本帧正常运动，并显式指定朝向意图（不再从 Velocity 猜测）</item>
/// <item><c>MovementUpdateResult.Complete()</c>：运动已完成，调度器将触发 OnMoveComplete</item>
/// </list>
/// </para>
/// <para>
/// 【位移量说明】
/// 仅在 Continue 时有意义，用于 AccumulateTravel 统计 MoveTraveledDistance。
/// 停顿/目标丢失/条件未满足时传 0f 即可。
/// </para>
/// </summary>
public readonly struct MovementUpdateResult
{
    /// <summary>运动是否已完成</summary>
    public bool IsCompleted { get; }

    /// <summary>本帧移动距离，IsCompleted 为 true 时无意义</summary>
    public float Distance { get; }

    /// <summary>是否显式提供了朝向意图</summary>
    public bool HasFacingDirection { get; }

    /// <summary>本帧朝向意图（仅在 HasFacingDirection=true 时有效）</summary>
    public Vector2 FacingDirection { get; }

    private MovementUpdateResult(bool isCompleted, float displacement, bool hasFacingDirection, Vector2 facingDirection)
    {
        IsCompleted = isCompleted;
        Distance = displacement;
        HasFacingDirection = hasFacingDirection;
        FacingDirection = facingDirection;
    }

    /// <summary>本帧继续运动</summary>
    /// <param name="displacement">估算位移量（像素），停顿时传 0f</param>
    public static MovementUpdateResult Continue(float displacement = 0f)
        => new MovementUpdateResult(false, displacement, false, Vector2.Zero);

    /// <summary>本帧继续运动，并显式提供朝向意图</summary>
    /// <param name="displacement">估算位移量（像素），停顿时传 0f</param>
    /// <param name="facingDirection">本帧面向方向（无需预归一化，零向量将按原样透传）</param>
    public static MovementUpdateResult Continue(float displacement, Vector2 facingDirection)
        => new MovementUpdateResult(false, displacement, true, facingDirection);

    /// <summary>运动完成，调度器将触发 OnMoveComplete</summary>
    public static MovementUpdateResult Complete()
        => new MovementUpdateResult(true, 0f, false, Vector2.Zero);
}
