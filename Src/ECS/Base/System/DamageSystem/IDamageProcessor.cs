/// <summary>
/// 伤害处理器接口（中间件）
/// </summary>
public interface IDamageProcessor
{
    /// <summary>
    /// 执行优先级（越小越先执行）
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// 处理伤害
    /// </summary>
    /// <param name="info">伤害上下文</param>
    void Process(DamageInfo info);
}
