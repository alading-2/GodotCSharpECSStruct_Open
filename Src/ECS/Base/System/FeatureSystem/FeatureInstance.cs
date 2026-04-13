/// <summary>
/// Feature 运行时实例 - 表示某个 Feature 挂载到特定宿主后的运行时状态快照
///
/// 职责：
/// - 持有 Owner 与 FeatureEntity 引用
/// - 提供对 Data 层常用状态的语义化快捷访问
/// - 作为生命周期回调与 Action 执行的统一上下文载体
///
/// 生命周期：
/// - 在 FeatureSystem.OnFeatureGranted 时创建
/// - 存储于 FeatureSystem 的实例表（如调用方需要）
/// - 在 FeatureSystem.OnFeatureRemoved 时移除
/// </summary>
public class FeatureInstance
{
    /// <summary>拥有该 Feature 的实体</summary>
    public IEntity? Owner { get; }

    /// <summary>承载 Feature 数据与事件的实体（任意 IEntity）</summary>
    public IEntity? FeatureEntity { get; }

    /// <summary>运行时唯一 ID（每次授予重新生成）</summary>
    public string RuntimeId { get; }

    /// <summary>授予时的游戏时间（秒）</summary>
    public double GrantedTime { get; }

    /// <summary>Feature 名称（快捷访问 Data["Name"]）</summary>
    public string FeatureName => FeatureEntity?.Data.Get<string>(DataKey.Name) ?? string.Empty;

    /// <summary>当前是否启用</summary>
    public bool IsEnabled => FeatureEntity?.Data.Get<bool>(DataKey.FeatureEnabled) ?? false;

    /// <summary>当前是否处于激活执行中</summary>
    public bool IsActive => FeatureEntity?.Data.Get<bool>(DataKey.FeatureIsActive) ?? false;

    /// <summary>累计激活次数</summary>
    public int ActivationCount => FeatureEntity?.Data.Get<int>(DataKey.FeatureActivationCount) ?? 0;

    public FeatureInstance(IEntity owner, IEntity featureEntity, double grantedTime)
    {
        Owner = owner;
        FeatureEntity = featureEntity;
        RuntimeId = System.Guid.NewGuid().ToString("N")[..8];
        GrantedTime = grantedTime;
    }

    public override string ToString() => $"FeatureInstance({FeatureName}@{Owner}, id={RuntimeId})";
}
