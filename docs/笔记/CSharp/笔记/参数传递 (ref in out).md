# 参数传递（ref / in / out）与标识符转义

## 一句话说明

C# 的参数传递不是只有“传值”一种：

- `ref`：把变量本身交给方法，方法里能改外面的变量
- `in`：把变量以**只读引用**传进去，避免复制大结构体
- `out`：把参数当成“输出口”，方法里必须赋值
- `@`：把关键字临时当成普通名字用

---

## 1. 为什么会有 `in`

默认情况下，C# 方法参数是**按值传递**。

这意味着：

- 如果传的是小值类型，复制成本很低
- 如果传的是大 `struct`，每次调用都复制一份就会很亏

`in` 的作用就是：

- **不复制整份结构体**，而是传递只读引用
- **禁止方法内部修改参数**，保证调用方的数据安全

这正适合像 `MovementParams` 这种“配置型、大字段、只读读取”的结构体。

### 例子

```csharp
public static void ApplyRotation(IEntity entity, in MovementParams @params, Vector2 velocity)
{
    if (!@params.RotateToVelocity) return;
    if (entity is not Node2D node) return;
    if (velocity.LengthSquared() < 0.001f) return;

    node.Rotation = velocity.Angle();
}
```

这里用 `in` 的好处是：

- 调用频率高时，少一次结构体复制
- 方法内部只能读，不能乱改 `MovementParams`
- 更符合“参数对象只负责描述本次运动”的设计

---

## 2. 为什么要写 `@params`

`params` 是 C# 关键字，通常用于可变参数：

```csharp
void Foo(params int[] values) { }
```

所以如果你想把参数名起成 `params`，就必须写成 `@params`。

### 这是什么意思

`@` 的作用是：**告诉编译器把后面的内容当普通标识符处理**。

也就是说：

- `params`：关键字
- `@params`：普通变量名

### 常见例子

```csharp
int @class = 1;
string @event = "test";
```

不推荐故意这么起名，但**如果确实撞到关键字**，这是合法写法。

---

## 3. 为什么 `MovementParams` 不直接在 `ApplyRotation` 里 `new`

这里要区分两个场景：

### 场景 A：构造参数对象

调用方在触发一次运动时，会 `new MovementParams` 来组装这次运动的数据：

```csharp
var moveParams = new MovementParams
{
    ActionSpeed = 600f,
    MaxDistance = 300f,
    RotateToVelocity = true,
};
```

这是**创建配置**，合理。

### 场景 B：策略执行过程

像 `ApplyRotation(...)` 这种方法，只是读取已有参数：

```csharp
ApplyRotation(entity, in moveParams, velocity);
```

这里就**不应该再 new 一份**，因为：

- 它只是“消费参数”，不是“创建参数”
- 频繁 `new` 会带来额外开销
- 逻辑职责也更清晰：创建和使用分离

一句话：

- **`new MovementParams`**：发生在“组装本次运动数据”的地方
- **`in MovementParams`**：发生在“只读使用数据”的地方

---

## 4. `ref` / `in` / `out` 的区别

| 关键字 | 作用 | 是否能改调用方变量 | 是否要求先初始化 | 典型用途 |
| --- | --- | ---: | ---: | --- |
| `ref` | 引用传参 | 可以 | 可以 | 需要在方法里修改外部变量 |
| `in` | 只读引用 | 不可以 | 可以 | 传大 `struct`，避免复制 |
| `out` | 输出参数 | 可以 | 不需要 | 返回多个结果 |

### 简单理解

- `ref` = “借给你，允许你改”
- `in` = “借给你看，但别动”
- `out` = “你负责写结果给我”

---

## 5. 在游戏开发里怎么选

### 推荐用法

- **小型值类型**：直接按值传递就行
- **大型 `struct` / 高频调用**：优先考虑 `in`
- **需要修改外部变量本身**：用 `ref`
- **需要返回多个结果**：用 `out`

### 对 `MovementParams` 的建议

`MovementParams` 更像“本次运动的说明书”，而不是“运行时状态容器”。

所以更适合：

- 在事件触发时 `new` 出来
- 在策略执行时用 `in` 只读访问
- 运行时状态另放到策略私有字段或组件数据里

---

## 6. 一个很容易混淆的点

`in` 和 `readonly` 不是一回事：

- `in` 是**参数传递方式**
- `readonly` 是**数据本身是否可变**

你可以把它们理解成：

- `in` 负责“怎么拿到数据”
- `readonly` 负责“数据能不能被改”

如果一个结构体本身就设计成只读，再配合 `in`，通常最适合高频读取场景。

---

## 7. 实战总结

以后看到类似下面的代码：

```csharp
public static void ApplyRotation(IEntity entity, in MovementParams @params, Vector2 velocity)
```

你可以这样理解：

- `MovementParams` 是一份配置数据
- `in` 表示这里只读、不复制
- `@params` 只是因为 `params` 是关键字
- 这不是“写法变复杂了”，而是**更适合高频、只读、性能敏感的移动系统**

Tags: #CSharp #Godot #Parameters #in #ref #out #Identifier
