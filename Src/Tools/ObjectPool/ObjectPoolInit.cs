using Godot;
using System;

using System.Collections.Generic;
using System.Runtime.CompilerServices;



/// <summary>
/// 对象池名称常量定义
/// 统一管理所有对象池的名称，避免字符串硬编码带来的维护困难
/// </summary>
public struct ObjectPoolNames
{
    /// <summary> 基础敌人对象池 </summary>
    public const string EnemyPool = "EnemyPool";

    /// <summary> 定时器对象池 </summary>
    public const string TimerPool = "TimerPool";

    /// <summary> 技能实体对象池 </summary>
    public const string AbilityPool = "AbilityPool";

    /// <summary> 头顶血条对象池 </summary>
    public const string HealthBarPool = "HealthBarPool";

    /// <summary> 伤害数字对象池 </summary>
    public const string DamageNumberUIPool = "DamageNumberUIPool";

    /// <summary> 特效实体对象池 </summary>
    public const string EffectPool = "EffectPool";
}

/// <summary>
/// 全局对象池管理器 (AutoLoad)
/// 负责统一管理和预初始化游戏中的核心对象池（如 Player, Enemy, Bullet 等）。
/// 采用去中心化注册机制，通过 AutoLoad 确保全局单例存在。
/// </summary>
public partial class ObjectPoolInit
{
    private static readonly Log _log = new Log("ObjectPoolInit");

    /// <summary>
    /// 模块初始化：在程序集加载时自动向 AutoLoad 注册。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // 对象池初始化需要早一点
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(ObjectPoolInit),
            InitAction = InitPools,
            Priority = AutoLoad.Priority.Core,
        });
    }

    private static void InitPools()
    {
        // 初始化 TimerPool (纯 C# 对象池)
        new ObjectPool<GameTimer>(
            () => new GameTimer(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.TimerPool,
                InitialSize = 50,
                MaxSize = 300,
                ParentPath = "Tool/GameTimer"
            }
        );

        // 初始化 EnemyPool (Node 对象池)
        // 注意：必须使用 ObjectPool<Enemy> 而不是 ObjectPool<Node>，否则 SpawnSystem 无法通过 GetPool<Enemy> 获取
        new ObjectPool<EnemyEntity>(
            () => (EnemyEntity)ResourceManagement.Load<PackedScene>(typeof(EnemyEntity).Name, ResourceCategory.Entity).Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.EnemyPool,
                InitialSize = 100,
                MaxSize = 500,
                ParentPath = "ECS/Entity/Enemy"
            }
        );

        // 3. 初始化 AbilityPool (技能实体对象池)
        // 支持敌人技能等高频生成场景
        new ObjectPool<AbilityEntity>(
            () => (AbilityEntity)ResourceManagement.Load<PackedScene>(typeof(AbilityEntity).Name, ResourceCategory.Entity).Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.AbilityPool,
                InitialSize = 50,
                MaxSize = 300,
                ParentPath = "ECS/Entity/Ability"
            }
        );

        // 初始化 EffectPool (特效实体对象池)
        new ObjectPool<EffectEntity>(
            () => (EffectEntity)ResourceManagement.Load<PackedScene>(typeof(EffectEntity).Name, ResourceCategory.Entity).Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.EffectPool,
                InitialSize = 100,
                MaxSize = 500,
                ParentPath = "ECS/Entity/Effect"
            }
        );

        // 初始化 HealthBarPool (头顶血条对象池)
        new ObjectPool<HealthBarUI>(
            () => (HealthBarUI)ResourceManagement.Load<PackedScene>(typeof(HealthBarUI).Name, ResourceCategory.UI).Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.HealthBarPool,
                InitialSize = 50,
                MaxSize = 200,
                ParentPath = "UI/UI/HealthBarUI"
            }
        );

        // 初始化 DamageNumberUIPool (伤害数字对象池)
        new ObjectPool<DamageNumberUI>(
            () => (DamageNumberUI)ResourceManagement.Load<PackedScene>(typeof(DamageNumberUI).Name, ResourceCategory.UI).Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.DamageNumberUIPool,
                InitialSize = 100,
                MaxSize = 500,
                ParentPath = "UI/UI/DamageNumberUI"
            }
        );

        _log.Success("ObjectPoolInit (AutoLoad) 初始化完成");
    }

}
