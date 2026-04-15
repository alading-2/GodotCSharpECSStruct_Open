/// <summary>
/// FeatureContext 通用读取工具。
/// 子系统把自己的运行上下文放入 ActivationData，Handler 通过泛型安全取回。
/// 这里使用扩展方法写法，所以虽然方法本体是 static，调用时仍然可以写成
/// featureContext.GetActivationData<T>()，编译器会自动翻译成 FeatureContextExtensions.GetActivationData<T>(featureContext)。
/// </summary>
public static class FeatureContextExtensions
{
    /// <summary>
    /// 读取指定类型的 ActivationData。
    /// 调用方应在子系统入口保证类型正确；类型错误说明接入链路本身有问题。
    /// 这是一个扩展方法：第一个参数前的 this 让它可以像实例方法一样被 FeatureContext 直接调用。
    /// </summary>
    /// <param name="context">Feature 运行上下文</param>
    /// <typeparam name="T">期望的上下文类型，如 CastContext</typeparam>
    /// <returns>指定类型的子系统上下文</returns>
    public static T GetActivationData<T>(this FeatureContext context)
    {
        if (context.ActivationData is T typedData)
        {
            return typedData;
        }

        var actualType = context.ActivationData?.GetType().Name ?? "null";
        throw new System.InvalidOperationException(
            $"FeatureContext.ActivationData 类型错误，期望 {typeof(T).Name}，实际 {actualType}");
    }

    /// <summary>
    /// 尝试把 ActivationData 读取为指定子系统上下文类型。
    /// </summary>
    /// <param name="context">Feature 运行上下文</param>
    /// <param name="data">读取到的子系统上下文</param>
    /// <typeparam name="T">期望的上下文类型，如 CastContext</typeparam>
    /// <returns>类型匹配时返回 true，否则返回 false</returns>
    public static bool TryGetActivationData<T>(this FeatureContext context, out T data)
    {
        if (context.ActivationData is T typedData)
        {
            data = typedData;
            return true;
        }

        data = default!;
        return false;
    }
}
