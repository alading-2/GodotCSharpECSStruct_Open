using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


/// <summary>
/// 对象池名称常量定义
/// 统一管理所有对象池的名称，避免字符串硬编码带来的维护困难
/// </summary>
public struct PoolNames
{
    /// <summary> 基础敌人对象池 </summary>
    public const string EnemyPool = "EnemyPool";

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
        // 修正路径：从 Data/ObjectPool/ 改为 Src/Tools/ObjectPool/
        AutoLoad.Register("ObjectPoolInit", "res://Src/Tools/ObjectPool/ObjectPoolInit.cs", AutoLoad.Priority.System);
    }


    // ============================================================
    // 生命周期
    // ============================================================

    public override void _Ready()
    {
        // 使用 CallDeferred 代替 async/await
        // 这会将初始化推迟到当前帧末尾，此时 SceneTree.Root 已经完全就绪
        CallDeferred(nameof(DeferredInit));
    }

    private void DeferredInit()
    {
        // 1. 初始化 EnemyPool
        var scene = GD.Load<PackedScene>("res://Src/ECS/Entities/Enemy/Enemy.tscn");
        new ObjectPool<Node>(
            () => scene.Instantiate(),
            new ObjectPoolConfig
            {
                Name = PoolNames.EnemyPool,
                InitialSize = 100,
                MaxSize = 500,
                ParentPath = "ECS/Entity/Enemy"
            }
        );

        _log.Success("ObjectPoolInit (AutoLoad) 初始化完成");
    }

}
