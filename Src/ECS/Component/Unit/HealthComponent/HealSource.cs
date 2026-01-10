/// <summary>
/// 治疗来源枚举
/// 用于区分不同类型的治疗，支持不同的事件处理和 UI 显示
/// </summary>
public enum HealSource
{
    /// <summary>未知来源</summary>
    Unknown = 0,

    /// <summary>自然恢复（每秒回血）</summary>
    Regen = 1,

    /// <summary>技能治疗</summary>
    Skill = 2,

    /// <summary>物品/药水治疗</summary>
    Item = 3,

    /// <summary>吸血效果</summary>
    Lifesteal = 4,

    /// <summary>复活恢复（不触发治疗事件/飘字）</summary>
    Revive = 5,
}
