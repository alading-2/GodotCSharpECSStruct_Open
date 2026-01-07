


public static class MyMath
{
    /// <summary>
    /// 属性加成计算 finalValue = baseVal * (1 + rate / 100)
    /// </summary>
    /// <param name="baseVal">基础值</param>
    /// <param name="rate">加成比例</param>
    /// <returns>计算结果</returns>
    public static float AttributeBonusCalculation(float baseVal, float rate)
    {
        return baseVal * (1 + rate / 100);
    }
}