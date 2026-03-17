
/// <summary>
/// 资源分类枚举 - 用于对资源进行分组管理
/// </summary>
public enum ResourceCategory
{
    /// <summary>Entity</summary>
    Entity,
    /// <summary>Component</summary>
    Component,
    /// <summary>UI</summary>
    UI,
    /// <summary>Asset 资源</summary>
    Asset,

    /// <summary>System 系统 (如 SpawnSystem)</summary>
    System,
    /// <summary>Manager 管理器 (如 TimerManager)</summary>
    Tools,

    /// <summary>数据配置 (.tres) (合并原先的 *Config)</summary>
    Data,

    /// <summary>Test 测试资源</summary>
    Test,

    /// <summary>Other 其他</summary>
    Other,
}

