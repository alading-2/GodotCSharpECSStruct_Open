# SpawnSystem (敌人生成与波次管理系统)

`SpawnSystem` 是游戏的核心控制模块，负责管理敌人生成、波次流程以及游戏生命周期逻辑。该系统已升级为**程序化生成 (Procedural Generation)** 架构，通过规则配置自动组装每一波的怪物刷新逻辑。

## 核心设计理念

- **规则驱动 (Rule-Based)**: 不再手动配置每一波的敌人,而是定义"规则"。
  - 例如:"史莱姆在第 1-5 波出现,间隔 3 秒"。
  - 系统会根据当前波次自动筛选并激活符合条件的规则。
- **全局配置 (Global Config)**: 使用 `SpawnConfig` 资源文件统一管理游戏节奏。
- **TimerManager 驱动**: 使用项目统一的 `TimerManager` 管理所有计时器,实现对象池复用,零 GC 压力。
- **生成管线化 (Pipeline)**:
  - **What (生成什么)**: 由 `SpawnConfig` 中的 `EnemySpawnRule` 决定。
  - **Where (在哪里生成)**: 委托给 `SpawnPositionCalculator` 处理（支持屏幕外、随机等策略）。
  - **How (如何生成)**: 系统在初始化时自动为所有配置的敌人创建对象池,并强制使用 `ObjectPoolManager` 进行复用。

## 数据结构

### SpawnConfig (Resource)

全局游戏节奏配置。

- `WaveDuration`: 每一波的标准持续时间（秒）。
- `MaxWaves`: 最大波次数量。
- `SpawnRules`: 所有敌人的生成规则列表 (`Array<EnemySpawnRule>`)。

### EnemySpawnRule (Resource)

定义特定敌人的出现逻辑。

- `EnemyData`: 关联的敌人属性资源（包含 `PackedScene` 和 `EnemyName`）。
- `MinWave` / `MaxWave`: 出现的波次范围（闭区间，-1 表示无限）。
- `SpawnInterval`: 生成间隔（秒）。
- `MaxCountPerWave`: 单波次最大生成上限（-1 表示不限，仅受间隔和时间限制）。
- `StartDelay`: 波次开始后的首次生成延迟。

## 核心逻辑流

1. **系统初始化 (`_Ready`)**:

   - 系统启动时,会自动读取 `SpawnConfig`。
   - 遍历所有 `SpawnRules`,检查是否已存在对应的对象池。
   - 如果不存在,则根据 `EnemyData.EnemyScene` 自动创建并注册 `ObjectPool<Node>`。
   - 确保后续生成时 `ObjectPoolManager.GetPool(name)` 始终能获取到有效的池实例。

2. **启动波次 (`StartWave`)**:

   - 使用 `TimerManager.CreateTimer()` 创建波次主计时器 (`_waveTimer`)。
   - 筛选当前波次 (`CurrentWaveIndex`) 激活的所有规则。
   - 初始化运行时状态 (`RuleRuntimeState`),重置累积时间。
   - 使用 `TimerManager.CreateLoopTimer()` 创建核心轮询计时器 (`_checkTimer`)。

3. **生成的驱动 (TimerManager Architecture)**:

   - 采用 **TimerManager 统一管理** 架构,所有计时器由对象池复用,零 GC 压力。
   - `_checkTimer` 每 0.2 秒触发一次 `OnCheckTimerTimeout` 回调。
   - 在回调中遍历所有激活的规则,累加 `delta` 时间。
   - 当 `AccumulatedTime >= SpawnInterval` 时,触发生成逻辑并扣除时间(支持"追赶"机制以应对卡顿)。

4. **动态适应**:

   - 如果某一波没有任何规则匹配(空波次),系统会发出警告,但游戏流程不会中断。
   - 支持无限波次模式。

## 公开接口

### 游戏流程控制

- `StartGame()`: 从第一波开始启动游戏。
- `StopGame()`: 停止逻辑并清理全场敌人。
- `StartWave(int waveIndex)`: 开启指定波次。
- `EndWave()`: 强制结束当前波次。

### 清理与回收

- `KillAllEnemies()`: 智能识别当前配置中涉及的所有敌人类型并批量回收。
- `SpawnBatch(...)`: 手动批量生成（用于测试或特殊事件）。
- `SpawnEnemy(string enemyName, Vector2 position)`: 从对象池获取一个敌人并放置在指定位置。

## 使用示例

### 1. 配置生成规则

在 Godot 编辑器中创建一个 `SpawnConfig` 资源文件（例如 `Level1_Config.tres`）。

- 设置 `WaveDuration = 60`。
- 添加 `SpawnRules`:
  - **规则 A (史莱姆)**:
    - `EnemyData`: SlimeResource (EnemyName="Slime")
    - `MinWave`: 0, `MaxWave`: 5
    - `SpawnInterval`: 2.0
  - **规则 B (哥布林)**:
    - `EnemyData`: GoblinResource (EnemyName="Goblin")
    - `MinWave`: 3, `MaxWave`: -1
    - `SpawnInterval`: 1.5

### 2. 注入配置

将创建好的 `SpawnConfig` 拖入场景中 `SpawnSystem` 节点的 `Config` 属性中。

### 3. 启动游戏

```csharp
SpawnSystem.Instance.StartGame();
```
