# C# 垃圾回收 (GC) —— 性能优化的生死线

#CSharp #Godot #Performance #Optimization

## 1. 什么是 GC (Garbage Collection)?

C# 是“托管语言”，意味着你 `new` 对象时只管申请，不用管释放。
**垃圾回收器 (GC)** 就像一个清洁工，它会定期检查内存，把不再使用的对象（垃圾）扫走。

### 🚨 游戏开发的噩梦：Stop The World

当 GC 开始打扫卫生时，为了防止你一边扔垃圾它一边扫，它会**暂停你的程序**（Stop The World）。

- 在 Web 开发中，暂停 50ms 用户无感。
- 在游戏开发中，暂停 50ms = **掉帧 / 卡顿**。

**核心目标：** **减少 GC 触发的频率，特别是在高频循环 (`_Process`) 中。**

---

## 2. 只有引用类型才会产生 GC

| 类型         | 存储位置   | GC 影响               | 例子                                                          |
| :----------- | :--------- | :-------------------- | :------------------------------------------------------------ |
| **值类型**   | 栈 (Stack) | **无** (用完即焚)     | `int`, `float`, `bool`, `Vector2`, `Color`, `Rect2`, `struct` |
| **引用类型** | 堆 (Heap)  | **有** (需要 GC 回收) | `class`, `string`, `Array`, `List`, `Dictionary`, `delegate`  |

> **好消息**：Godot 的核心数学类型 (`Vector2`, `Vector3`, `Color`) 全是 `struct`，随便用，不产生垃圾！

---

## 3. 三大禁忌：绝对不要在 `_Process` 里做的事

`_Process` 每秒运行 60 次。哪怕你每帧只制造 1KB 垃圾，1 秒就是 60KB，1 分钟就是 3.6MB，很快就会触发 GC 卡顿。

### 🚫 禁忌一：每帧 `new` 引用类型

```csharp
public override void _Process(double delta)
{
    // ❌ 错误：List 是 class，每帧都在堆上分配内存
    var nearEnemies = new List<Enemy>();

    // ... 逻辑 ...

    // 虽然函数结束了，但堆上的 List 变成了垃圾，等着 GC 来收
}
```

**✅ 修正：对象池 / 成员变量复用**

```C#
// 把 List 提到外面，作为类的成员变量
private List<Enemy> _nearEnemies = new List<Enemy>();

public override void _Process(double delta)
{
    _nearEnemies.Clear(); // ✅ 只是清空数据，不释放内存，不产生垃圾

    // ... 重新填充数据 ...
}
```

### 🚫 禁忌二：每帧拼接字符串

在 C# 中，`string` 是不可变的。`"A" + "B"` 会创建一个新的字符串对象 `"AB"`。

```C#
public override void _Process(double delta)
{
    // ❌ 错误：每帧都在创建新的 string 对象
    Label.Text = "FPS: " + Engine.GetFramesPerSecond();
}
```

**✅ 修正：**

1. **只在变化时更新**：不要每帧更，单独写一个 Timer 每 0.5 秒更一次 UI。
2. **使用插值 (C# 10+)**：`Label.Text = $"FPS: {fps}";` (现代编译器优化较好)。
3. **StringBuilder**：如果是极其复杂的拼接，用 `StringBuilder`。

### 🚫 禁忌三：LINQ 的滥用

LINQ 方便但有代价。`Where()`, `Select()` 内部会创建迭代器对象（class）。

```C#
public override void _Process(double delta)
{
    // ❌ 错误：每帧都在创建 LINQ 委托和迭代器
    var target = enemies.Where(e => e.Hp < 10).FirstOrDefault();
}
```

**✅ 修正：** 在 `_Process` 热路径中，老老实实写 `foreach` 或 `for` 循环。LINQ 留给 `_Ready` 或低频逻辑（如打开背包时）使用。

---

## 4. 终极方案：对象池 (Object Pooling)

如果你需要频繁生成和销毁子弹、特效、怪物，**千万不要**反复 `Instantiate` 和 `QueueFree`。

**原理：**

1. 游戏开始时，一口气造 100 个子弹，全部隐藏 (`Visible = false`)。
2. 开枪时，找一个隐藏的子弹，重置位置，设为显示。
3. 子弹击中墙壁时，不要 `QueueFree`，而是设为隐藏。

这样游戏运行过程中，**没有任何内存分配和销毁**，GC 永远不会来打扰你。

---

## 总结 checklist

1. **结构体 (struct) 是好朋友**：Godot 的 `Vector2` 随便造。
2. **热路径 (`_Process`) 严查**：这里面不能有 `new Class()`, 不能有 `string + string`。
3. **复用**：List 用 `Clear()` 复用，子弹用对象池复用。
4. **时机**：在加载关卡时随便 `new`，但在战斗进行时要“抠门”。
