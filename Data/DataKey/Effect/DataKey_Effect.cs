/// <summary>
/// 数据键定义 - 特效系统
/// </summary>
public static partial class DataKey
{
    // === 特效播放 ===

    /// <summary>播放速率倍率 (float, 默认1.0)</summary>
    public const string EffectPlayRate = "EffectPlayRate";
    /// <summary>特效缩放 (Vector2, 默认1,1)</summary>
    public const string EffectScale = "EffectScale";
    /// <summary>特效偏移 (Vector2, 默认0,0)</summary>
    public const string EffectOffset = "EffectOffset";

    // === 特效附着 ===

    /// <summary>是否附着到宿主 (bool)</summary>
    public const string EffectIsAttached = "EffectIsAttached";

    // === 特效动画 ===

    /// <summary>指定播放的动画名 (string, 空=播放默认)</summary>
    public const string EffectAnimationName = "EffectAnimationName";
    /// <summary>是否循环播放 (bool, 默认false)</summary>
    public const string EffectIsLooping = "EffectIsLooping";
}
