# PickupComponent (拾取组件)

实现物品（如经验值、道具、货币）的拾取检测、磁吸效果以及与采集者的交互逻辑。

## 核心功能

- **范围检测**: 继承自 `Area2D`，用于检测玩家或其他实体的进入。
- **磁吸效果**: 支持物品向采集者平滑移动的功能，速度由 `MagnetSpeed` 控制。
- **拾取回调**: 当物品与采集者发生实际接触时，触发 `PickedUp` 事件。
- **状态管理**: 记录当前的采集者 (`Collector`) 和磁吸状态。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取/写入以下键值：

| 键名 | 类型 | 读写 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| `MagnetSpeed` | `float` | 读 | `300.0` | 磁吸模式下的移动速度 |
| `MagnetEnabled` | `bool` | 读写 | `false` | 当前是否启用了磁吸移动 |

## 公开接口

### 属性

- `Collector`: `Node2D?` (只读) - 当前正在磁吸该物品的目标节点。
- `MagnetEnabled`: `bool` (只读) - 磁吸状态开关。

### 事件 (Action)

- `PickedUp(Node2D collector)`: 物品被成功拾取时触发。

### 方法

- `EnableMagnet(Node2D collector)`: 开始向指定的采集者磁吸。
- `DisableMagnet()`: 停止磁吸移动。

## 使用说明

1. 将 `PickupComponent` 添加到掉落物（如 `ExperienceGem`）的场景中。
2. 配置 `CollisionLayer`（如掉落物层）和 `CollisionMask`（如玩家探测层）。
3. 当玩家进入外围探测圈时，调用 `EnableMagnet(player)`。
4. 当玩家与物品核心区域碰撞（触发 `BodyEntered` 或内部逻辑）时，调用 `PickedUp` 事件并销毁/回收物品。
