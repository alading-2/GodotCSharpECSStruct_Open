# SpawnSystem 重构与实现提示词

## 1. 角色定义

你是一位资深的 Godot 4.x C# 游戏架构师，专注于 ECS (Entity-Component-System) 架构设计与高性能游戏系统开发。

## 2. 任务描述

请重构并完善 `SpawnSystem` 类，使其从单纯的“生成器”进化为核心的“敌人生成与波次管理系统”。

## 3. 核心功能需求

### 3.1 敌人生成 (Entity Spawning)

- **核心机制**: 必须继续使用 `ObjectPoolManager` 进行对象的获取与回收，严禁在运行时直接使用 `new` 或 `Instantiate`。
- **接口保持**: 保留 `SpawnEnemy(string enemyId, ...)` 作为底层执行接口。
- **位置计算**:
  - **强制要求**: 所有的位置计算逻辑（随机、屏幕外、圆周等）必须委托给 `SpawnPositionCalculator` 类处理。
  - `SpawnSystem` 自身不应包含任何坐标计算的数学逻辑。

### 3.2 波次管理 (Wave Management)

- **波次生命周期**:
  - 管理当前波次索引 (`CurrentWaveIndex`)。
  - 管理波次计时器 (`WaveTimer`)，精确控制每波的持续时间（例如 60 秒）。
  - 实现波次开始 (`StartWave`)、结束 (`EndWave`) 和过渡逻辑。
- **配置驱动**:
  - 支持从 `Resource` (如 `WaveData`) 读取波次配置。
  - 配置应包含：该波次的持续时间、包含的敌人类型、生成间隔、生成数量上限等。

### 3.3 游戏流程控制

- 提供 `StartGame()` 接口初始化第一波。
- 提供 `StopGame()` 或 `KillAllEnemies()` 接口用于游戏结束清理。

## 4. 深度重构思考 (Architectural Insights)

### 4.1 职责分离 (SRP)

- **现状**: 原有的 `SpawnSystem` 只是一个简单的生成工具函数集合。
- **目标**: 将其升级为**生成管线 (Spawning Pipeline)** 的管理者。
  - **Where (在哪里生成)**: 完全剥离给 `SpawnPositionCalculator.cs`（已独立为单独文件）。
  - **When (何时生成)**: 由内部的 `WaveController` 逻辑或独立的 `WaveSystem` 决定（本次重构要求整合在 SpawnSystem 中或由其协调）。
  - **What (生成什么)**: 由 `WaveData` 配置决定。
  - **How (如何生成)**: 委托给 `ObjectPoolManager`。

### 4.2 扩展性设计

- 考虑到未来可能会有“精英怪”或“Boss 战”，生成逻辑应支持不同的 `SpawnStrategy`（不仅仅是位置，还包括生成前的预警特效等）。
- 波次配置应设计为数组或列表，支持无限波次模式（循环或随机生成）。

## 5. 代码结构参考

```csharp
public partial class SpawnSystem : Node
{
    // ... 单例与初始化 ...

    // === 状态变量 ===
    public int CurrentWave { get; private set; }
    public float WaveTimer { get; private set; }
    public bool IsWaveActive { get; private set; }

    // === 核心依赖 ===
    // 引用 SpawnPositionCalculator (静态调用)

    // === 接口 ===
    public void StartGame();
    public void StartWave(int waveIndex);
    public void EndWave();

    // === 循环 ===
    public override void _Process(double delta)
    {
        // 处理波次计时
        // 处理生成队列/间隔
    }
}
```

## 6. 现有资源

- **位置计算工具**: Src/ECS/System/Spawn/SpawnPositionCalculator.cs (已包含 5 种位置策略)
- **对象池**: `ObjectPoolManager`
