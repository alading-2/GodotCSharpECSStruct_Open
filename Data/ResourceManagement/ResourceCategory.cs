
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
    /// <summary>Asset 资源（未匹配子分类时的兜底）</summary>
    Asset,
    /// <summary>AssetEffect 特效动画资源（assets/Effect/）</summary>
    AssetEffect,
    /// <summary>AssetUnit 单位动画资源（assets/Unit/）</summary>
    AssetUnit,
    /// <summary>AssetUnitEnemy 敌人动画资源（assets/Unit/Enemy/）</summary>
    AssetUnitEnemy,
    /// <summary>AssetUnitPlayer 玩家动画资源（assets/Unit/Player/）</summary>
    AssetUnitPlayer,


    /// <summary>System 系统 (如 SpawnSystem)</summary>
    System,
    /// <summary>Manager 管理器 (如 TimerManager)</summary>
    Tools,

    /// <summary>数据配置兜底（Data/Data/ 下未匹配子分类的 .tres）</summary>
    Data,
    /// <summary>技能数据配置（Data/Data/Ability/）</summary>
    DataAbility,
    /// <summary>单位数据配置（Data/Data/Unit/）</summary>
    DataUnit,
    /// <summary>碰撞数据配置（Data/Data/Collision/）</summary>
    DataCollision,
    /// <summary>Test 测试资源</summary>
    Test,

    /// <summary>Other 其他</summary>
    Other,
}

