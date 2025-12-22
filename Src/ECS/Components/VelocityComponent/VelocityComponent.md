# VelocityComponent (移动组件)

管理实体的物理移动逻辑，包括速度计算、加速度、摩擦力以及速度限制。

## 核心功能

- **平滑移动**: 提供 `MoveToward` 方法，使实体在指定方向上根据加速度平滑加速。
- **自动减速**: 提供 `ApplyFriction` 方法，模拟地面摩擦力使实体逐渐停止。
- **速度限制**: 自动确保实体的移动速度不会超过 `MaxSpeed`。
- **动态数据驱动**: 所有的物理参数（速度、加速度、摩擦力）均从实体的 `Data` 容器中动态获取。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取以下键值：

| 键名           | 类型    | 默认值   | 说明                   |
| :------------- | :------ | :------- | :--------------------- |
| `MaxSpeed`     | `float` | `200.0`  | 实体允许的最大移动速度 |
| `Acceleration` | `float` | `1000.0` | 移动时的加速度         |
| `Friction`     | `float` | `800.0`  | 停止移动时的减速摩擦力 |

## 公开接口

### 属性

- `Velocity`: `Vector2` (只读) - 当前的物理速度向量。
- `MaxSpeed`: `float` (只读) - 当前配置的最大速度。
- `Acceleration`: `float` (只读) - 当前配置的加速度。
- `Friction`: `float` (只读) - 当前配置的摩擦力。

### 方法

- `MoveToward(Vector2 direction, float delta)`: 向指定方向加速。
- `ApplyFriction(float delta)`: 应用摩擦力使实体减速。
- `Stop()`: 立即将速度清零。
- `SetVelocity(Vector2 velocity)`: 直接设置速度向量（会自动应用速度上限限制）。
- `GetVelocity() -> Vector2`: 获取当前速度向量。
- `GetSpeed() -> float`: 获取当前速率（速度的长度）。

## 使用示例

```csharp
public override void _PhysicsProcess(double delta)
{
    Vector2 inputDir = Input.GetVector("left", "right", "up", "down");

    if (inputDir != Vector2.Zero)
    {
        _velocityComponent.MoveToward(inputDir, (float)delta);
    }
    else
    {
        _velocityComponent.ApplyFriction((float)delta);
    }

    // 将计算出的速度应用到 CharacterBody2D
    Velocity = _velocityComponent.Velocity;
    MoveAndSlide();
}
```
