/// <summary>
/// 战斗单位接口
/// <para>
/// 所有具有独立战斗归属的主体（玩家、敌人）必须实现此接口。
/// 用于解决“子弹造成伤害，但需要统计到玩家身上”的问题。
/// </para>
/// </summary>
public interface IUnit
{
    /// <summary>
    /// 阵营标识
    /// <para>0: 玩家, 1: 敌人, 2: 中立</para>
    /// </summary>
    int FactionId { get; }
}
