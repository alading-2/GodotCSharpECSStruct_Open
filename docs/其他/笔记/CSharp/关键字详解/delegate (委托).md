
# delegate (委托) —— 函数的“类型”

`delegate` 是 C# 中用于定义**函数签名**的关键字。它定义了一种引用类型，用于存储对某个方法的引用。

## 核心类比 (你的舒适区)

| 语言 | 概念 | 代码对比 |
| :--- | :--- | :--- |
| **C#** | **delegate** | `public delegate void MyCallback(string msg);` |
| **TypeScript** | **Function Type** | `type MyCallback = (msg: string) => void;` |
| **C** | **Function Pointer** | `typedef void (*MyCallback)(char* msg);` |

## 通俗理解
`delegate` 就像是**函数的“模具”**或**“招聘需求(JD)”**。
它不干活，它只规定干活的函数必须长什么样（参数、返回值）。
* **普通变量**：存的是数据（如 `int a = 10`）。
* **委托变量**：存的是**函数的地址**。

## 关键区别：多播 (Multicast)
与 C 的函数指针不同，C# 的 delegate 可以“**一呼百应**”。
一个 delegate 变量可以存储**多个**函数（使用 `+=` 叠加），调用时会依次执行所有函数。这在 TS 中通常需要手动维护一个 `Array<Callback>` 才能实现。

## Godot 场景 (自定义信号)
在 Godot C# 中，定义 `Signal` 本质上就是定义一个 `delegate`。

```csharp
// 1. 定义模具（Delegate）：规定信号发出时会携带两个参数
// [Signal] 特性告诉 Godot：请在编辑器里把这个 delegate 识别为信号
[Signal]
public delegate void HealthChangedEventHandler(int currentHp, string reason);

// 2. 发出信号 (Godot 4.x 写法)
// 相当于调用了这个 delegate，所有监听它的函数都会被执行
EmitSignal(SignalName.HealthChanged, 50, "Fell damage");
````

## TS 开发者备忘

如果你习惯这样写 TS：

```TypeScript
// TS: 定义一个回调类型
type OnClick = (x: number, y: number) => void;

// TS: 传递回调
function register(cb: OnClick) { ... }
```

那么在 C# 里就是这样：

```C#
// C#: 定义委托
public delegate void OnClick(int x, int y);

// C#: 传递委托
void Register(OnClick cb) { ... }
```

#Tag: #CSharp #Godot #Delegate #Signal