# DataKey 静态数据类重构方案（归档）

> 状态：归档
> 当前主文档：[`DataSystem_Design.md`](./DataSystem_Design.md)

本文档原本用于记录 **DataKey 从 `const string` 迁移到 `static readonly DataMeta`** 的设计过程。

这项改造现在已经成为项目当前架构的一部分，因此本文件不再承担主说明职责。

## 已落地的核心结论

- 主域 `DataKey` 使用 `static readonly DataMeta`
- `DataRegistry.Register()` 返回 `DataMeta`
- `DataMeta` 支持隐式转换为 `string`
- Config 上的 `[DataKey]` 参数改为 `nameof(DataKey.Xxx)`
- Config 默认值直接使用 `DataKey.Xxx.DefaultValue`

## 仍然需要记住的约定

### 1. 字段名必须等于 Key

```csharp
public static readonly DataMeta BaseHp = DataRegistry.Register(
    new DataMeta { Key = nameof(BaseHp), Type = typeof(float) });
```

### 2. Config 必须优先写成 `nameof(DataKey.Xxx)`

```csharp
[DataKey(nameof(DataKey.BaseHp))]
[Export] public float BaseHp { get; set; } = (float)DataKey.BaseHp.DefaultValue!;
```

### 3. 主流键不要再新增 `const string`

只有特殊引用键、编译期常量场景才允许保留 `const string`。

## 现在应该看哪里

- **当前 Data 系统整体说明** → [`DataSystem_Design.md`](./DataSystem_Design.md)
- **Data 目录职责** → `Data/README.md`
- **Config 映射规则** → `Data/Data/README.md`
- **DataKey 定义规范** → `Data/DataKey/README.md`

## 保留本文件的原因

- 保留历史迁移背景
- 避免旧链接失效
- 让后续阅读旧提交时仍能理解当时的设计目标
