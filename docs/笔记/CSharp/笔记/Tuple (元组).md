# Tuple (元组) —— 此时此刻的“临时工”

在 C# 中，`ValueTuple`（即 `(Type, Type)` 语法）是为了解决“我想函数返回多个值，但懒得专门去写一个 Class/Struct”而生的。

## 特点
- **匿名**的数据组合，没有名字
- 临时使用，语法极其轻量
- **TS 类比**：就像 TypeScript 的 `[number, string]` 元组

## Godot 场景

你需要写一个射线检测函数，不仅要告诉我有没撞到，还要告诉我撞到了哪。

```C#
// 函数返回一个 Tuple：(是否击中, 击中点坐标)
public (bool IsHit, Vector2 HitPos) RaycastCheck()
{
    // ... 模拟逻辑
    if (raycast.IsColliding())
        return (true, raycast.GetCollisionPoint());
    
    return (false, Vector2.Zero);
}

// 使用时，直接解构（Destructuring）
var (hit, pos) = RaycastCheck();
if (hit) {
    GD.Print($"Hit at {pos}");
}
```

## 适用场景
- 这种数据只在这一瞬间有用，出了这个函数就没意义了
- 没必要为此专门定义一个 `RaycastResult` 类
- 从函数里**返回多个值**（比如 `TryParse` 模式）
- 这个数据结构**不需要名字**（比如 `dict.Add((x, y), value)` 作为字典的 Key）
- 数据**只在私有方法内部**流转

#Tag: #CSharp #Tuple #DataStructure