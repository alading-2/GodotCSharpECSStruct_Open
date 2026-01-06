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
}

/// <summary>
/// 全局对象池管理器 (AutoLoad)
/// 负责统一管理和预初始化游戏中的核心对象池（如 Player, Enemy, Bullet 等）。
/// 采用去中心化注册机制，通过 AutoLoad 确保全局单例存在。
/// </summary>
public partial class ObjectPoolInit : Node
{
    private static readonly Log _log = new Log("ObjectPoolInit");

    /// <summary>
    /// 模块初始化：在程序集加载时自动向 AutoLoad 注册。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        // 对象池初始化需要早一点
        AutoLoad.Register("ObjectPoolInit", "res://Src/Tools/ObjectPool/ObjectPoolInit.cs", AutoLoad.Priority.Core, "ParentManager");
    }


    // ============================================================
    // 生命周期
    // ============================================================

    public override void _EnterTree()
    {
        // 关键：必须在 _EnterTree 中初始化，因为其他系统可能在它们的 _EnterTree 中就需要获取对象池
        // 如果放在 _Ready 中，会导致时序问题（_EnterTree 先于 _Ready 执行）
        InitPools();
    }

    private void InitPools()
    {
        // 1. 初始化 TimerPool (纯 C# 对象池)
        new ObjectPool<GameTimer>(
            () => new GameTimer(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.TimerPool,
                InitialSize = 50,
                MaxSize = 200,
                ParentPath = "Tool/GameTimer"
            }
        );

        // 2. 初始化 EnemyPool (Node 对象池)
        // 注意：必须使用 ObjectPool<Enemy> 而不是 ObjectPool<Node>，否则 SpawnSystem 无法通过 GetPool<Enemy> 获取
        var scene = GD.Load<PackedScene>("res://Src/ECS/Entity/Enemy/Enemy.tscn");
        new ObjectPool<Enemy>(
            () => (Enemy)scene.Instantiate(),
            new ObjectPoolConfig
            {
                Name = ObjectPoolNames.EnemyPool,
                InitialSize = 100,
                MaxSize = 500,
                ParentPath = "ECS/Entity/Enemy"
            }
        );

        _log.Success("ObjectPoolInit (AutoLoad) 初始化完成");
    }

}
