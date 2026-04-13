using Godot;

/// <summary>
/// 连线特效接口
/// 实现此接口的节点（如 Line2D）可以在技能（如闪电链、连续射线）中作为连线表现
/// </summary>
public interface ILineEffect
{
    /// <summary>
    /// 在两点之间播放连线动画
    /// </summary>
    /// <param name="fromPos">起点世界坐标</param>
    /// <param name="toPos">终点世界坐标</param>
    void PlayChain(Vector2 fromPos, Vector2 toPos);
}
