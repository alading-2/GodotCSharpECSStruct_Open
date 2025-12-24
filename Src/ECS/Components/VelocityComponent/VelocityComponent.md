# VelocityComponent (移动组件)

管理实体的物理移动逻辑，包括速度计算、加速度、以及最大速度限制。该组件专为 `CharacterBody2D` 设计。

## 核心功能

- **自动移动控制**: 在 `_Process` 中自动获取 `InputManager` 的输入并应用到位移。
- **平滑移动**: 使用指数插值（Lerp + Exp）实现平滑的加速和减速效果。
- **速度限制**: 自动确保实体的移动速度不会超过 `MaxSpeed`。
- **动态数据驱动**: 所有的物理参数（速度、加速度等）均从实体的 `Data` 容器中动态获取。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取以下键值：

| 键名           | 类型    | 默认值   | 说明                                       |
| :------------- | :------ | :------- | :----------------------------------------- |
| `Speed`        | `float` | `400.0`  | 正常移动时的目标速度                       |
| `MaxSpeed`     | `float` | `1000.0` | 实体允许的最大速度（包括冲刺、击退等）     |
| `Acceleration` | `float` | `10.0`   | 加速度因子，值越大加速越快（典型值 10-20） |

## 公开接口

### 属性

- `Velocity`: `Vector2` (只读) - 当前的物理速度向量。
- `Speed`: `float` (只读) - 从 `Data` 中获取的当前配置速度。
- `MaxSpeed`: `float` (只读) - 从 `Data` 中获取的当前配置最大速度。
- `Acceleration`: `float` (只读) - 从 `Data` 中获取的当前配置加速度。

### 方法

- `Stop()`: 立即将速度清零。
- `SetVelocity(Vector2 velocity)`: 直接设置速度向量（会自动应用 `MaxSpeed` 限制）。
- `GetVelocity() -> Vector2`: 获取当前速度向量。
- `GetSpeed() -> float`: 获取当前速率（速度向量的长度）。

## 使用示例

### 基础用法

只需将 `VelocityComponent` 挂载到 `CharacterBody2D` 实体下，它将自动处理移动：

```csharp
// 在实体的初始化代码中
public override void _Ready()
{
    // 组件会自动寻找父节点并开始工作
}
```

### 手动干预

如果需要从外部控制速度或停止移动：

```csharp
public void OnDashEffect()
{
    var velocityComp = GetNode<VelocityComponent>("VelocityComponent");
    // 赋予一个爆发速度
    velocityComp.SetVelocity(new Vector2(2000, 0));
}

public void OnStun()
{
    var velocityComp = GetNode<VelocityComponent>("VelocityComponent");
    velocityComp.Stop(); // 立即停止
}
```
