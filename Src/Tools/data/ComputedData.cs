using System;
using System.Linq;

/// <summary>
/// 计算数据定义 - 由其他数据派生的只读数据
/// 例如：攻击间隔 = 1.0 / (攻击速度 / 100)
/// </summary>
public class ComputedData
{
    /// <summary>
    /// 计算数据的键名
    /// </summary>
    public string Key { get; init; } = "";

    /// <summary>
    /// 依赖的数据键列表
    /// </summary>
    public string[] Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 计算函数
    /// </summary>
    public Func<Data, object> Compute { get; init; } = _ => 0f;

    /// <summary>
    /// 检查是否依赖指定的数据键
    /// </summary>
    /// <param name="dataKey">数据键</param>
    /// <returns>是否依赖</returns>
    public bool DependsOn(string dataKey)
    {
        return Dependencies.Contains(dataKey);
    }
}
