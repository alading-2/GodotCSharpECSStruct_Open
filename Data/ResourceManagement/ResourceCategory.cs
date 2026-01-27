namespace Brotato.Data.ResourceManagement;
using System;
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

    // Resource 配置分类
    /// <summary>敌人配置 (.tres)</summary>
    EnemyConfig,
    /// <summary>玩家配置 (.tres)</summary>
    PlayerConfig,
    /// <summary>技能配置 (.tres)</summary>
    AbilityConfig,
    /// <summary>道具配置 (预留)</summary>
    ItemConfig,

    /// <summary>Other 其他</summary>
    Other,
}

