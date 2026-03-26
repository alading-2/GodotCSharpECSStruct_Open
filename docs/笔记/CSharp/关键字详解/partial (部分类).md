
# partial (部分类/分部类)

## 一句话解释

允许你把“同一个类”的代码，拆分写在“多个不同文件”里，编译时会自动合体。

---

## 核心概念
**“化整为零，编译归一。”**

`partial` 关键字允许将一个类（struct 或 interface）的源代码拆分到多个 `.cs` 文件中。
在**编译阶段**（Compile Time），C# 编译器会自动把这些碎片像拉链一样“拉”在一起，合并成一个完整的类。

> ⚠️ **注意**：这只是代码组织方式（源代码层面的拆分），编译后的 DLL 中它依然是一个完整的类，没有任何性能损耗。

## 判决指南：我到底要不要加？ | 你的类继承了什么？

| 你的类继承了什么？                  | 需要 partial 吗？ | 例子            |
| :------------------------- | :------------ | :------------ |
| **Node / Node2D / Node3D** | ✅ **必须加**     | 挂在场景物体上的脚本    |
| **Resource**               | ✅ **必须加**     | 自定义物品、技能数据    |
| **Control / Button**       | ✅ **必须加**     | UI 界面脚本       |
| **什么都不继承 (纯C#)**           | ❌ **不用加**     | 纯逻辑算法、JSON数据类 |
### 1. 必须加的情况 (99% 的场景)

只要你的类**继承自 Godot 的内置类型，用来挂在节点上的脚本**，就**必须**加 `partial`。 这包括：

- `Node`, `Node2D`, `Node3D`, `Control` (所有挂在场景里的脚本)
- `Resource` (自定义资源)
- `RefCounted`
- `GodotObject`

**理由**：Godot 4.x 只要发现你继承了它的类，就会在后台尝试生成优化代码。如果你不加 `partial`，它生成的代码就没法合并进来，编译器会直接报错。

### 2. 不需要加的情况

如果你的类是**纯 C# 类**（不继承 Godot 的任何东西），用来写纯逻辑或数据结构，就不需要加。 比如：

- 纯数据模型：`public class InventoryItem { ... }`
- 工具类：`public static class MathUtils { ... }`
- JSON 解析类

## 为什么 Godot 4.x 必须用它？(深度解析)

在 Godot 3.x 时代，C# 与 Godot 的交互大量依赖 **反射 (Reflection)**，这很慢且容易出错。
Godot 4.x 引入了 **源生成器 (Source Generators)** 技术来提升性能，而 `partial` 是这项技术的基石。

当你写下 `public partial class Player : Node` 时，发生了以下过程：

1.  **你写的代码 (`Player.cs`)**：
    包含游戏逻辑（移动、跳跃）。
2.  **Godot 生成的代码 (`Player.GodotGenerated.cs`)**：
    Godot 分析你的类，自动在内存中生成另一个 `partial class Player`。
    这个文件里包含了：
    - 重写 `_GetGodotSignalList`（注册信号）
    - 重写 `_GetGodotPropertyList`（注册 Export 变量）
    - 底层 C++ 指针的绑定方法
3.  **合并**：
    C# 编译器看到两个 `partial class Player`，将它们合并。

**如果没有 `partial`**：
编译器会认为你定义的 `Player` 类已经结束了，Godot 无法把那些高性能的“胶水代码”注入到你的类中，导致游戏无法正确运行或性能退化回反射模式。

## TS/Lua 开发者视角的对比

| 语言 | 类似概念 | 区别 |
| :--- | :--- | :--- |
| **TypeScript** | **Interface Merging** (接口合并) | TS 的 `interface` 可以重复定义并自动合并，但 **Class 不行**。TS 的类一旦定义就封闭了。C# 的 `partial` 相当于打破了类的物理封闭性。 |
| **Lua** | **Table 动态扩展** | Lua 是动态语言，随时可以 `Player.NewFunc = function...`。C# 是静态语言，`partial` 是让静态语言也能像动态语言一样“分多次”定义结构，但必须在**编译前**完成。 |
| **C/C++** | **Header/Source** (.h/.cpp) | 有点像，但不完全一样。C# 的 `partial` 两个文件都可以包含实现细节，不分声明和实现。 |

## 语法规则与限制 (避坑指南)

虽然它像拼图，但拼图的边缘必须吻合：

1.  **关键字统一**：所有部分都必须加上 `partial` 关键字。
2.  **类型一致**：要么都是 `class`，要么都是 `struct`。
3.  **命名空间一致**：必须在同一个 `namespace` 下。
4.  **访问修饰符一致**：如果一个是 `public`，另一个是 `internal`，编译报错。
5.  **不可跨程序集**：所有的 partial 文件必须在同一个 DLL 项目中（不能一部分在 Core.dll，一部分在 Game.dll）。

## 最佳实践：什么时候主动使用？

除了配合 Godot，你在以下场景也可以主动使用：

1.  **隔离复杂的嵌套类**：
    如果你的类里定义了很多 `private class` 或 `struct` 仅仅给内部使用，可以把这些定义挪到 `Player.Types.cs` 里，让主文件保持清爽。
2.  **UI 开发**：
    虽然你现在用 Godot，但如果你以后接触 WinForms/WPF/Avalonia，它们的 UI 代码和逻辑代码也是通过 `partial` 分离的。

## 代码全景图

```csharp
// 文件 1: Player.GamePlay.cs (你手写的)
namespace MyGame;

public partial class Player : Node3D // ⬅️ 必须有 partial
{
    [Export] 
    public int Hp { get; set; }

    public override void _Ready()
    {
        GD.Print("Player Ready");
    }
}
````

```C#
// 文件 2: (Godot 在内存中生成的，你看不到但真实存在)
namespace MyGame;

public partial class Player : Node3D // ⬅️ 对应上面的 partial
{
    // Godot 自动生成的底层代码，用于和 C++ 引擎通信
    public override void _Notification(int what) { ... }
    public static void BindMethods() { ... }
}
```

#Tag: #CSharp #Godot #SourceGenerator #Partial #Architecture