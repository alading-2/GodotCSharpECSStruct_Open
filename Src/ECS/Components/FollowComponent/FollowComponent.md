# FollowComponent (跟随组件)

实现 AI 实体跟随目标的逻辑，提供方向计算和距离检测功能。

## 核心功能

- **目标追踪**: 管理一个 `Node2D` 类型的跟随目标。
- **方向计算**: 计算从当前实体指向目标的归一化方向向量。
- **距离检测**: 检测与目标的实时距离，并判断是否进入“停止距离”范围。
- **动态数据驱动**: 速度和停止距离从实体的 `Data` 容器中动态读取。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取以下键值：

| 键名           | 类型    | 默认值  | 说明                 |
| :------------- | :------ | :------ | :------------------- |
| `FollowSpeed`  | `float` | `100.0` | 跟随移动的速度参考值 |
| `StopDistance` | `float` | `10.0`  | 停止跟随的临界距离   |

## 公开接口

### 属性

- `Target`: `Node2D?` - 当前跟随的目标节点。
- `FollowSpeed`: `float` (只读) - 当前配置的跟随速度。
- `StopDistance`: `float` (只读) - 当前配置的停止距离。

### 方法

- `GetDirectionToTarget() -> Vector2`: 获取指向目标的归一化方向。
- `GetDistanceToTarget() -> float`: 获取到目标的像素距离。
- `IsInRange() -> bool`: 检查是否已进入停止距离。
- `ShouldFollow() -> bool`: 综合判断是否应该继续执行跟随逻辑。
- `IsTargetValid() -> bool`: 检查目标是否有效且未被销毁。
- `SetTarget(Node2D? target)`: 设置或清除跟随目标。

## 使用示例

```csharp
// 在实体的 AI 控制逻辑中
if (_followComponent.ShouldFollow())
{
    Vector2 direction = _followComponent.GetDirectionToTarget();
    _velocityComponent.Move(direction);
}
```
