# Record (记录) —— 严谨的数据模型

`record` 是 C# 9 引入的，本质上是一个**类（Class）**（也有 `record struct`），但它专门为了**存储数据**而优化。

## 特点

- **具名**的业务模型，有明确的类型名称
- **值相等性**（最重要的特性），比较的是内容而非引用
- 支持 `with` 表达式，用于非破坏性修改
- 打印出来自带格式，方便调试
- **TS 类比**：就像一个所有的属性都是 `readonly` 的 TS `interface` 或 `type`

## Godot 场景

你在做 RPG 的背包系统，定义一个“物品”。

```C#
// 定义一个 Record
// 这一行代码相当于帮你写好了：构造函数、只读属性、ToString、Equals对比逻辑
public record Item(int Id, string Name, int Rarity);

// 使用
var sword1 = new Item(1, "Excalibur", 5);
var sword2 = new Item(1, "Excalibur", 5);

// 1. 值相等性 (Value Equality)
// 如果是普通 Class，这里会返回 false（因为内存地址不同）。
// 但因为是 Record，它对比的是内容！只要 ID、Name、Rarity 一样，它们就相等。
GD.Print(sword1 == sword2); // 输出: True 

// 2. 漂亮的 ToString
GD.Print(sword1); 
// 输出: Item { Id = 1, Name = Excalibur, Rarity = 5 } 
// (如果是普通 Class，只会输出 "YourNamespace.Item"，对调试很不友好)
```

## 适用场景

- 跨越多个文件、需要长期保存的数据
- 需要对比内容是否相同的数据（比如判断背包里是否已经有这个东西）
- 定义**游戏数据**（物品、技能、存档数据、网络消息包）
- 需要**不可变数据**（配合 `with` 修改）
- 这个数据会被**公开**给其他类使用

## 与 Tuple 的区别

|**特性**|**Tuple (int, int)**|**Record public record Pos(int x, int y)**|
|---|---|---|
|**语义**|**匿名**的数据组合|**具名**的业务模型|
|**生命周期**|局部，通常用于函数内部或返回值|全局，作为 API、配置、存档数据传输|
|**可读性**|差 (Item1, Item2) 或 弱类型名|强，明确知道它是 `Item` 还是 `Skill`|
|**相等性**|值比较|值比较 (即使是引用类型 `record class`)|  
|**扩展性**|不能加方法|可以像类一样加方法|
|**TS 对应**|`[number, string]`|`interface Item { readonly id: number }`|

## Record 的杀手锏 `with`

在 C# 中，只有 Record 支持这种优雅的非破坏性修改写法：

```C#
var oldSword = new Item(1, "Excalibur", 5);
// 生成一把新剑，名字ID不变，只有稀有度变了
var newSword = oldSword with { Rarity = 6 }; 
```

## ⚠️ 性能与 GC (进阶必读)

在游戏开发中，内存分配 (Allocation) 是性能优化的核心。默认的 `record` 是引用类型 (Class)，每次创建都会在堆 (Heap) 上分配内存，产生垃圾 (Garbage)。

| 类型 | 语法 | 内存位置 | GC 压力 | 适用场景 |
| :--- | :--- | :--- | :--- | :--- |
| **Record Class** | `public record Config(...)` | 堆 (Heap) | **有** (每次 new 都会分配) | **低频**操作：初始化配置、一次性事件、复杂的大型数据对象 |
| **Record Struct** | `public readonly record struct Config(...)` | 栈 (Stack) | **无** (零 GC) | **高频**操作：每帧调用的函数参数 (SpawnConfig)、物理计算结果、伤害包 |

### 最佳实践指南

1. **默认情况**：如果你的数据对象 **生命周期很短** 且 **被频繁创建**（例如：每帧发射子弹的配置），请务必使用 **`record struct`**。

   ```csharp
   // ✅ 推荐：零 GC，高性能
   public readonly record struct BulletSpreadConfig(float Angle, float Speed);
   ```

2. **配置/存档**：如果对象只在启动时创建，或者需要在列表/字典中长期持有引用，使用普通的 `record` (class) 更好，因为 struct 复制成本可能更高。

   ```csharp
   // ✅ 推荐：引用传递，方便管理
   public record GameSettings(float Volume, bool Fullscreen);
   ```

3. **参数对象模式 (Parameter Object)**：如果一个方法有5个以上参数，你想把它们封装起来传参：
    - 如果方法每秒调用 1000 次 → 用 `record struct`
    - 如果方法只在游戏开始时调用 1 次 → 用 `record`

#Tag: #CSharp #Record #DataStructure #Performance