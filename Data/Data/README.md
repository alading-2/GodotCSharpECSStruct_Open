# Godot Resource 属性映射规则 (DataKey 机制)

本文档说明了如何将 Godot Resource (Config 类) 中的属性正确映射到游戏的 `Data` 容器系统。

## 1. 核心机制：[DataKey] 特性

为了保证类型安全并防止拼写错误，我们使用 `[DataKey]` 特性来显式指定属性对应的键名。

### 推荐写法 (类型安全)

使用 `DataKey` 常量类中的定义。这样如果 `DataKey` 发生变化，编译器会帮你检查错误。

```csharp
[DataKey(DataKey.BaseHp)]
[Export] public float BaseHp { get; set; }
```

### 兼容写法 (按名映射)

如果不加 `[DataKey]` 特性，系统会 fallback 到使用 **属性名** 作为键名。

```csharp
// 系统会尝试寻找名为 "MoveSpeed" 的 DataKey
[Export] public float MoveSpeed { get; set; }
```

> [!WARNING]
> **风险提示**：按名映射没有任何编译检查。如果你把 `MoveSpeed` 改名为 `Speed`，但 `Data` 系统期望的是 `"MoveSpeed"`，那么数据加载将会静默失败。**强烈建议所有核心战斗属性都使用 `[DataKey]` 特性。**

## 2. 映射原理

系统在调用 `Data.LoadFromResource(resource)` 时，会执行以下逻辑：

1. 扫描属性：遍历 Resource 对象的所有公共属性。
2. 提取特性：检查属性上是否存在 `DataKeyAttribute`。
3. 确定键值：
   * 有特性：使用特性中指定的 `Key`。
   * 没有特性：使用该属性的 `Name`。
4. 注入数据：将该属性的值存入 `Data` 容器中对应的键位。

## 3. 常见问答

**Q: 为什么我要费力写标签，而不直接起个一样的名字？**
A: 标签提供了**编译时保护**。属性名是字符串，写错了编译器不知道；标签参数是常量，写错了代码就编译不过。此外，标签允许你的 C# 属性名和 Data 系统里的键名不一样（例如属性叫 `HP`，但映射到 `DataKey.BaseHp`）。

**Q: 我新增了一个属性，需要做什么？**

A:

1. 在 `DataKey` 类中定义一个新的常量。
2. 在 Config 类中添加属性，并加上 `[DataKey(DataKey.YourNewKey)]`。
3. 确保在 `DataRegistry` 中注册了该 Key 的元数据（如果有的话）。
