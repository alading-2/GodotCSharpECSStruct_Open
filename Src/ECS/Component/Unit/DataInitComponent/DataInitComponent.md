# DataInitComponent 数据初始化组件

## 简介
`DataInitComponent` 是一个通用的功能组件，致力于解决 **实体生成时的状态初始化** 问题。

在 ECS 架构中，数据通常分为两类：
1. **静态配置数据** (Config Data)：如 `BaseHp`, `FinalHp`，通常来自 `Resource` 配置。
2. **运行时动态数据** (Runtime Data)：如 `CurrentHp`，通常在游戏运行中变化。

`DataInitComponent` 的作用就是作为这两者之间的桥梁，在实体生成的那一刻，根据静态配置自动填充运行时数据的初始值。

## 核心功能

### 1. 自动同步 HP
- **检测**：检查 `Data` 容器中是否存在 `CurrentHp`。
- **行为**：如果不存在（且存在 `FinalHp`），则自动执行 `CurrentHp = FinalHp`。
- **结果**：确保单位生成时是满血状态。

### 2. (可扩展) 其他属性
未来可在此组件中轻松添加其他初始化规则，例如：
- `CurrentMana = MaxMana`
- `CurrentSpeed = BaseSpeed`

## 使用方法

### 方式一：EntityManager 自动挂载 (推荐)
在 `EntityManager.Spawn` 流程中识别 `IUnit` 接口，如果缺失此组件则动态添加。
*(注：这部分集成逻辑由项目组其他成员处理)*

### 方式二：场景预设
直接在 `.tscn` 场景文件中（如 `Player.tscn` 或 `Enemy.tscn`），添加一个 `Node` 并挂载脚本 `Src/ECS/Component/Data/DataInitComponent.cs`。

## 代码示例

```csharp
// 典型逻辑 (DataInitComponent.cs)
private void InitializeData()
{
    // 如果没有当前血量，就用最大血量填满
    if (!_data.Has(DataKey.CurrentHp))
    {
        float maxHp = _data.Get<float>(DataKey.FinalHp);
        _data.Set(DataKey.CurrentHp, maxHp);
    }
}
```
