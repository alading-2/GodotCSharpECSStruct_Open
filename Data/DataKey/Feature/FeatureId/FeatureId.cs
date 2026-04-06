/// <summary>
/// Feature 处理器 ID 常量注册表
///
/// 统一存放所有 IFeatureHandler.FeatureId 的字符串常量，
/// 避免每处都写字符串字面量，与 FeatureHandlerRegistry.Register 配套使用。
///
/// 使用方式：
/// - 每个 Feature 模块在各自的 partial 文件中扩展本类
/// - 注册处理器时用 FeatureId.SpeedBoost 代替 "SpeedBoost"
/// - FeatureDefinition.FeatureHandlerId 
///
/// 示例（在各功能模块中新建 partial 文件扩展）：
/// <code>
/// // Data/DataKey/Feature/Items/SpeedBootsId.cs
/// public static partial class FeatureId
/// {
///     public const string SpeedBoots = "SpeedBoots";
/// }
///
/// // 注册处理器
/// FeatureHandlerRegistry.Register(new SpeedBootsHandler());
///
/// // FeatureDefinition.Name 填写 FeatureId.SpeedBoots 对应的字符串
/// </code>
/// </summary>
public static partial class FeatureId
{
    // ============ 各功能模块在各自的 partial 文件中扩展 ============
    // 本文件保持空白，作为 partial class 的根定义
}
