# Component 数据驱动设计理念

## 概述

本文档深入解析 Component 规范中"纯数据驱动"原则的设计动机和最佳实践。

---

## 为什么要纯数据驱动？

### 1. 对象池复用

传统 OOP 方式存储状态的问题：
```csharp
// ❌ 对象池复用时，需要手动重置每个字段
public class HealthComponent 
{
    private float _hp = 100f;
    private float _maxHp = 100f;
    private bool _isDead = false;
    
    public void Reset() 
    {
        _hp = 100f;       // 容易遗漏
        _maxHp = 100f;    // 必须手动维护
        _isDead = false;  // 字段越多越容易出错
    }
}
```

数据驱动方式：
```csharp
// ✅ Entity.Data.Clear() 自动清理所有状态
public class HealthComponent
{
    public float CurrentHp => _data.Get<float>(DataKey.CurrentHp);
    
    // OnComponentUnregistered 无需手动重置属性
}
```

### 2. 调试与可视化

所有状态集中在 Data 容器中：
- 编辑器中可直接查看/修改
- 运行时可通过调试面板监控
- 便于录制/回放游戏状态

### 3. 网络同步

Data 容器天然支持序列化，网络同步只需同步 Data 即可。

### 4. 解耦系统间通信

Component 之间通过 Data 读写状态，无需直接引用：
```csharp
// LifecycleComponent 读取 HP 判断死亡
float hp = _data.Get<float>(DataKey.CurrentHp);
if (hp <= 0) Kill();

// HealthComponent 不需要知道 LifecycleComponent 的存在
```

---

## DataKey 设计动机

### 为什么用常量而非枚举？

```csharp
// ❌ 枚举：无法扩展
public enum DataKey { Hp, MaxHp }  // Mod 无法添加新键

// ✅ partial DataKey：支持按域扩展
public static partial class DataKey 
{
    public static readonly DataMeta Hp = DataRegistry.Register(
        new DataMeta { Key = nameof(Hp), Type = typeof(float), DefaultValue = 0f });
}
// Mod 可以扩展
public static partial class DataKey 
{
    public static readonly DataMeta CustomStat = DataRegistry.Register(
        new DataMeta { Key = nameof(CustomStat), Type = typeof(float), DefaultValue = 0f });
}
```

### 为什么禁止字符串字面量？

```csharp
// ❌ 字符串字面量：容易拼写错误、难以重构
_data.Get<float>("CurrentHp");
_data.Get<float>("currentHp");  // 拼错了！

// ✅ DataKey / DataMeta：编译期检查
_data.Get<float>(DataKey.CurrentHp);  // 安全
```

---

## DataMeta / DataRegistry 的作用

### 运行时类型验证

```csharp
DataRegistry.Register(new DataMeta 
{
    Key = nameof(DataKey.CurrentHp),
    Type = typeof(float),  // 类型验证
    DefaultValue = 100f    // 默认值
});
```

### 编辑器支持

注册后的 Key 可在编辑器中显示友好名称和分类。

### 默认值管理

未显式设置的 Key 返回注册的默认值。

---

## 数据存储决策树

```
需要存储到 Data 吗？
    │
    ├── 是运行时状态？（HP、State、计时器）
    │   └── ✅ 必须存 Data
    │
    ├── 对象池复用时需要重置？
    │   └── ✅ 必须存 Data
    │
    ├── 需要被其他 Component/System 访问？
    │   └── ✅ 必须存 Data
    │
    ├── 是固定配置？（ReviveDuration、Acceleration）
    │   └── ❌ 不需要存 Data，用属性即可
    │
    └── 是临时引用？（Target、Collector）
        └── ❌ 不需要存 Data，用 private 字段
```

---

## 为何组件通常无需手动重置

Entity 销毁流程：
```
EntityManager.Destroy()
    └── UnregisterEntity()
        ├── Events.Clear()     // 清理事件
        ├── Data.Clear()       // 🎯 自动重置所有状态
        └── UnregisterComponents()
            └── OnComponentUnregistered() // 🎯 兼顾清理引用与重置计时器等
```

**结论**：只要状态都在 Data 里，组件在 `OnComponentUnregistered()` 时就无需手动重置属性。

---

## 总结

| 原则 | 传统 OOP | 数据驱动 |
|------|----------|----------|
| 状态存储 | private 字段 | Data 容器 |
| 对象池重置 | 手动重置每个字段 | Data.Clear() 自动清理 |
| 调试 | 难以查看 | 编辑器可视化 |
| 扩展性 | 需要修改源码 | DataKey partial 扩展 |
| 类型安全 | 低（字符串） | 高（DataKey / DataMeta） |
