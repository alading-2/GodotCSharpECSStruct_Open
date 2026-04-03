# C# 可空类型 (Nullable Types) 核心指南

## 1. 核心概念

在 C# 中，类型名后面加 `?` (如 `string?`, `int?`)，意味着该变量被发放了**“空值通行证”**，允许存储 `null`。

| **符号** | **含义**           | **默认值**             | **编译器行为**    |
| -------- | ------------------ | ---------------------- | ----------------- |
| **`T`**  | **不可空**(Strict) | 取决于类型 (0 或 报错) | 赋值 `null`会报错 |
| **`T?`** | **可空**(Nullable) | `null`                 | 允许赋值 `null`   |

---

## 2. 引用类型 vs 值类型 (最关键的区别)

这是 TypeScript 开发者最容易混淆的地方，C# 对待这两者完全不同。

### A. 引用类型 (Reference Types)

**对象：** `string`, `class`, `Node2D`, `数组`, `List<T>`

- **本质：** 这里加 `?` 只是给编译器的**“注解”** (Annotation)。
- **运行时：** 无论是 `string` 还是 `string?`，运行时都是一样的对象指针。
- **对比 TS：**
  - `string` **$\approx$** TS `string`
  - `string?` **$\approx$** TS `string | null`

**C#**

```
string  name = "Godot"; // ✅ 必须有值，不能为 null
string? desc = null;    // ✅ 允许为空
```

### B. 值类型 (Value Types)

**对象：** `int`, `float`, `bool`, `Vector2` (struct), `Enum`

- **本质：** **“硬汉”变“盒子”** 。`int` 绝不为空，`int?` 是一个特殊的包装结构体 (`Nullable<T>`)。
- **默认值陷阱：**
  - `int a;` **$\rightarrow$** 默认是 **0** 。
  - `int? a;` **$\rightarrow$** 默认是 **null** (不是 0)。
- **底层原理：** `int?` 实际上是在值外面套了一层皮：
  - 内部包含：`Value` (数值) + `HasValue` (是否有值的标记)。
  - **内存变化：** 比普通 `int` 略大（多存一个 bool 标记）。

---

## 3. 常用操作符 (工具箱)

### 🛡️ 安全访问 `?.`

如果对象是 null，直接返回 null，不报错（不崩溃）。

**C#**

```
// 如果 player 为空，Name 也返回空，游戏继续运行
string? name = player?.Name;
```

### 🎁 空合并 `??` (Godot 常用)

如果是 null，就给个备胎（默认值）。 **拆包神器** 。

**C#**

```
// 如果 config.Volume 是 null，就默认用 100
int volume = config.Volume ?? 100;
```

### 🤐 闭嘴符 `!` (Null-Forgiving)

告诉编译器：“我知道它看起来像空的，但我保证它现在有值，别报错。”

**C#**

```
// GetNodeOrNull 返回 Node?，但我确定场景里肯定有它
var hero = GetNodeOrNull<Node2D>("Hero")!;
```

### 📦 拆包 `.Value` (仅限值类型)

强行取出 `int?` 里的 `int`。 **危险操作** ，如果是 null 会崩。

**C#**

```
int? score = 100;
int final = score.Value; // ✅ 安全，因为有值
// int final = ((int?)null).Value; // ❌ 崩溃：InvalidOperationException
```

---

## 4. Godot 开发实战建议

| **场景**           | **推荐类型** | **理由**                                                                               |
| ------------------ | ------------ | -------------------------------------------------------------------------------------- |
| **必须存在的组件** | `Sprite2D`   | 不要加 `?`，配合 `GetNode`使用。如果找不到节点，让它在启动时直接崩掉报错，方便修 Bug。 |
| **可能没有的目标** | `Node2D?`    | 比如 AI 的 `Target`。`null`代表“当前没敌人”，逻辑清晰。                                |
| **怪物血量**       | `int`        | 血量最少是 0，不应该存在 `null`状态。                                                  |
| **关卡最高分**     | `int?`       | `null`代表“没玩过”，`0`代表“玩得很烂”。UI 显示不一样 (`--`vs `0`)。                    |
| **配置读取**       | `float?`     | 用于区分“用户没填配置”和“用户填了 0.0”。                                               |

## 5. 总结：TS 开发者速记表

- **`string?`** **$\rightarrow$** 完全等于 TS 的 `string | null`。
- **`int`** **$\rightarrow$** 这是一个必须有值的数字 (TS 没有直接对应的强制非空原始类型)。
- **`int?`** **$\rightarrow$** 这是一个 **盒子** ，默认是空的。想用里面的数，得先确认盒子里有东西 (`??` 或 `.Value`)。
- **`null` 传染性** **$\rightarrow$** `int? a = null; int b = 5;` 此时 `a + b` 的结果是 `null`。
  #Tag: #CSharp #Nullable
