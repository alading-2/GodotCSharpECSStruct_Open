# ✨ with 表达式：非破坏性修改

**with** 关键字常与 **record** 或 **struct** 配合使用，用于创建一个 **修改了部分属性的副本**。
⚠️ **注意：普通 Class 不支持 with 表达式。**

## 为什么要用 with？

不是因为它是"可变"或"不可变"类型，而是因为你想做 **"非破坏性修改" (Non-destructive Mutation)** 。

### 对于 Struct (值类型)：

- `var p2 = p1;` 会发生全复制（深拷贝）。
- 然后你必须写 `p2.Y = 10;`。这需要两行代码。
- **with** 只是语法糖，把这两步合并了。

### 对于 Record (引用类型)：

- `var p2 = p1;` **只复制地址**！改 `p2` 就会改 `p1`。
- 如果你想复制并修改，普通类必须手动 `new` 一个新对象并把属性一个个抄过去。
- **with** 在这里救了命（它会在底层调用 Record 特有的克隆方法并修改指定属性）。

## 代码示例

```C#
// 必须是 record 类型
public record User(string Name, int Age);

var user1 = new User("Alice", 30);
// 创建 user1 的副本，除了 Age 变为 31，其他属性保持不变
var user2 = user1 with { Age = 31 };

// user1 保持不变 (Alice, 30)
// user2 是新对象 (Alice, 31)
```

## 技术细节与内存管理

### 值类型 (Struct / Record Struct):

```C#
public record struct Point(int X, int Y);

var p1 = new Point(1, 2);
var p2 = p1 with { X = 3 }; // 在栈上创建完整副本，性能极高
```

### 引用类型 (Record):

```C#
public record Person(string Name, int Age); // 默认为引用类型

var p1 = new Person("张三", 25);
var p2 = p1 with { Age = 26 }; // 在堆上创建新对象，原对象保持不变
```

## ⚠️ 核心陷阱：引用类型的嵌套 (Nested Reference Types)

这是 `record` 最容易翻车的地方。**`with` 表达式执行的是浅拷贝 (Shallow Copy)**。

如果你的 Record 里包含了 **可变的引用类型**（如 `List<T>`, `Dictionary<K,V>`, `StringBuilder`），`with` 生成的新对象会**共享**这些内部容器。

### 翻车示例

```C#
public record Player(string Name, List<string> Inventory);

// 1. 创建玩家 A，背包里有剑
var p1 = new Player("Hero", new List<string> { "Sword" });

// 2. 创建分身 B，只改名字
// ⚠️ 危险：p2.Inventory 和 p1.Inventory 指向堆内存里的同一个 List！
var p2 = p1 with { Name = "Clone" };

// 3. 往分身 B 的背包加盾牌
p2.Inventory.Add("Shield");

// 4. 检查玩家 A 的背包
// 结果：p1 的背包里也出现了 Shield！因为它们共享同一个 List。
Console.WriteLine(string.Join(",", p1.Inventory));
// 输出: "Sword, Shield"
```

### 解决方案

在游戏开发中（如 RPG 存档、物品数据），如果你需要真正的独立副本，有两种解法：

1. **使用不可变集合 (推荐)**： 使用 `System.Collections.Immutable` 库。修改不可变集合会自动返回新集合，从而切断引用。

   ```C#
   public record Player(string Name, ImmutableList<string> Inventory);
   ```

2. **手动深拷贝**： 在 `with` 的大括号里手动 `new` 一个新列表。

   ```C#
   var p2 = p1 with {
       Name = "Clone",
       // 手动创建新 List，把旧数据拷贝过来
       Inventory = new List<string>(p1.Inventory)
   };
   ```

## 性能考量

- **值类型**：`with` 表达式会复制整个结构体，对于大型结构体可能有性能开销
- **引用类型 (Record)**：`with` 表达式只修改指定属性，其余属性共享引用（浅拷贝行为）
- **嵌套对象**：内部的引用类型属性仍然遵循引用语义，需要特别注意

#Tag: #CSharp #WithExpression #Immutable
