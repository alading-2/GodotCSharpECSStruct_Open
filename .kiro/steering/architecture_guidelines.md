# 架构指南 - Brotato 复刻项目 (Pseudo-ECS)

## 1. 核心理念

- **伪 ECS 混合架构**：利用 Godot Node 系统实现 Entity，通过组合 Node 实现 Component。
- **三层设计**：
  - **逻辑层**：组件逻辑（Node Composition）。
  - **数据层**：静态 Resource + 动态 Data 容器。
  - **优化层**：对象池 (ObjectPool) + 高性能服务器接口 (RenderingServer/PhysicsServer)。

## 2. 关键系统实现规则

### 2.1 组件系统 (Component System)

- **职责分离**：每个组件只负责一个原子功能（如 Health, Velocity）。
- **通信机制**：**强制使用 C# 原生 Event** (`Action<T>`)。
  - 严禁在逻辑层频繁使用 Godot Signal。
  - 必须在 `_ExitTree` 中手动清理事件订阅。
- **数据存储**：运行时状态必须挂载到 Node 的 `Data` 容器中。

### 2.2 属性与修改器 (Stats & Modifiers)

- **公式**：`最终值 = (基础值 + Σ加法) × Π乘法`。
- **性能优化**：使用脏标记 (`_isDirty`) 缓存计算结果。
- **优先级**：修改器支持优先级排序。

### 2.3 战斗与交互

- **Hitbox/Hurtbox**：基于 `Area2D`。
- **伤害转发**：Hurtbox 接收 Hitbox 数据并调用 HealthComponent。
- **无敌帧**：Hurtbox 负责计时和过滤伤害。

## 3. 目录规范

- `Src/ECS/Components/`：核心功能组件。
- `Src/Autoload/`：全局单例。
- `Src/Tools/`：底层工具（Log, Data, ObjectPool）。
- `Resources/Data/`：静态配置文件 (.tres)。
- `Assets/`：美术与音效。

## 4. 性能基准

- **同屏实体**：目标支持 500+ 动态物体。
- **GC 零容忍**：在 `_Process` 或热路径中严禁 `new` 引用类型、字符串拼接或 LINQ。
