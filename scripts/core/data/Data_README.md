# Data 类使用文档

`Data` 是一个轻量级、健壮且类型安全的动态数据容器，专门为 Godot C# 开发设计。它灵感来源于 ECS (实体组件系统) 的数据管理模式，旨在替代或增强传统的成员变量和 Godot 内置的 Meta 系统。

## 1. 核心特性

- **类型安全**: 支持泛型获取数据，内置自动类型转换。
- **通用数值运算**: 基于 .NET 8 `INumber<T>`，支持所有数值类型的 `Add` 和 `Multiply`。
- **响应式更新**: 提供全局和单键值的变更监听机制。
- **纯 C# 实现**: 不依赖 `GodotObject`，可在任何类中安全使用，无内存泄漏风险。
- **高性能**: 内部基于 `Dictionary<string, object>`，操作复杂度为 O(1)。

## 2. 基础操作

### 初始化

```csharp
var data = new Data();
```

### 设置数据 (`Set`)

```csharp
data.Set("HP", 100);
data.Set("Speed", 350.5f);
data.Set("PlayerName", "Knight");
```

### 获取数据 (`Get`)

```csharp
int hp = data.Get<int>("HP");
float speed = data.Get<float>("Speed", 300.0f); // 支持默认值
string name = data.Get<string>("PlayerName");
```

### 检查与移除

```csharp
if (data.Has("HP"))
{
    data.Remove("HP");
}
```

## 3. 数值运算 (通用泛型)

`Add` 和 `Multiply` 方法会自动推断类型。如果键名不存在，则从该类型的零值 (`T.Zero`) 开始计算。

```csharp
// 加法 (Add)
data.Add("Gold", 50);          // int 累加
data.Add("Mana", 10.5f);       // float 累加

// 乘法 (Multiply)
data.Multiply("Damage", 1.5f); // 伤害加深 50%
data.Multiply("CritRate", 2);  // 暴击率翻倍
```

## 4. 事件监听

### 监听特定属性变更 (`On` / `Off`)

```csharp
// 订阅
data.On("HP", (oldVal, newVal) => {
    GD.Print($"血量从 {oldVal} 变更为 {newVal}");
});

// 取消订阅
data.Off("HP", myCallback);
```

### 监听全局变更 (`OnValueChanged`)

```csharp
data.OnValueChanged += (key, oldVal, newVal) => {
    GD.Print($"[数据变更] {key}: {oldVal} -> {newVal}");
};
```

## 5. 高级用法

### 批量设置

```csharp
var config = new Dictionary<string, object> {
    { "MaxHP", 200 },
    { "Level", 1 },
    { "IsDead", false }
};
data.SetMultiple(config);
```

### 获取所有数据副本

```csharp
var allData = data.GetAll();
```

## 6. 最佳实践建议

1. **键名常量化**: 建议使用 `static readonly` 字符串或 `const` 定义键名，避免拼写错误。
2. **生命周期管理**: 在 `_ExitTree()` 或对象销毁时，记得调用 `Clear()` 或取消重要的事件订阅，以保持良好的内存习惯。
3. **配合 Resource 使用**: 将 `Resource` 用于存储静态平衡数据，而将 `Data` 类附加在运行时实例上用于管理动态状态。
