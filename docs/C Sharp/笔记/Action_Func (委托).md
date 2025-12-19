# C# 现代委托 (Action & Func) —— 告别繁琐的 delegate

#CSharp #Godot #Delegate #Callback

## 1. 核心概念：为什么要换？

在早期的 C# 中，如果你想把一个函数当作参数传给另一个函数（比如回调），你需要先定义一个“模具” (`delegate`)。

**❌ 老派写法 (繁琐)**

```csharp
// 1. 先定义格式：必须是无返回值，且接收一个 int
public delegate void OnDamageHandler(int damage);

// 2. 再声明变量
public OnDamageHandler damageCallback;

// 3. 极其啰嗦，而且每个不同的函数签名都要定义一个新的 delegate
```

✅ 现代写法 (Action & Func)

C# 系统库 (System) 已经为你预制好了万能模具。你不再需要自己定义 delegate 关键字，直接用 Action 或 Func 即可。

---

## 2. Action (无返回值)

只要是 **“执行某个动作”** 且 **“不需要结果”** 的函数（返回 `void`），统统用 `Action`。

### 语法规则

- `Action`: 无参数，无返回值。
- `Action<T>`: 接收 1 个参数，无返回值。
- `Action<T1, T2>`: 接收 2 个参数，无返回值。

### Godot 实战：UI 回调

假设你写了一个通用的确认弹窗。

```C#
public partial class ConfirmDialog : Control
{
    // 定义一个回调：当玩家点击"确定"时执行的操作
    public Action OnConfirmed;

    public void _on_ok_button_pressed()
    {
        // ?.Invoke() 是标准写法：如果有人绑定了就执行，没人绑定就不崩
        OnConfirmed?.Invoke();
        QueueFree();
    }
}

// --- 调用者 ---
public void ShowDialog()
{
    var dialog = GD.Load<PackedScene>("res://Dialog.tscn").Instantiate<ConfirmDialog>();
    AddChild(dialog);

    // 绑定回调 (Lambda 写法)
    dialog.OnConfirmed = () =>
    {
        GD.Print("玩家点了确定，开始删号...");
        DeleteSaveFile();
    };
}
```

---

## 3. Func (有返回值)

只要是 **“计算”、“查询”** 或 **“获取数据”** 的函数（有具体返回值），统统用 `Func`。

### 语法规则

**⚠️ 重点：尖括号里，最后一个类型是返回值！前面的全是参数。**

- `Func<int>`: 无参数，返回 `int`。
- `Func<string, bool>`: 接收 `string` 参数，返回 `bool`。
- `Func<float, float, Vector2>`: 接收两个 `float`，返回 `Vector2`。

### Godot 实战：AI 索敌逻辑

假设你写了一个通用的 AI 脚本，但不同的怪物索敌逻辑不一样。

```C#
public partial class EnemyAI : Node
{
    // 定义一个逻辑插槽：给我一个 Player，我告诉你是否应该攻击他 (bool)
    public Func<Player, bool> ShouldAttackPlayer;

    public void UpdateAI(Player target)
    {
        // 如果满足攻击条件...
        if (ShouldAttackPlayer != null && ShouldAttackPlayer(target))
        {
            Fire();
        }
    }
}

// --- 在初始化怪物时配置逻辑 ---
public void SpawnZombies()
{
    var zombie = new EnemyAI();

    // 僵尸的逻辑：距离小于 10 米就攻击
    zombie.ShouldAttackPlayer = (p) => p.GlobalPosition.DistanceTo(zombie.GlobalPosition) < 10.0f;
}

public void SpawnSniper()
{
    var sniper = new EnemyAI();

    // 狙击手的逻辑：距离大于 50 米才攻击
    sniper.ShouldAttackPlayer = (p) => p.GlobalPosition.DistanceTo(sniper.GlobalPosition) > 50.0f;
}
```

---

## 4. Lambda 表达式 (配合委托的神器)

在使用 `Action` 和 `Func` 时，我们通常懒得专门写一个函数名，而是直接写匿名函数，也就是 **Lambda** (`=>`)。

| **写法**     | **代码示例**                          | **说明**                                 |
| ------------ | ------------------------------------- | ---------------------------------------- |
| **单行逻辑** | `() => GD.Print("Hi")`                | 自动省略大括号，代码极简。               |
| **带参数**   | `(dmg) => Hp -= dmg`                  | 编译器自动推断 `dmg` 是 int 还是 float。 |
| **多行逻辑** | `() => { PlayAnim(); Sound.Play(); }` | 如果逻辑复杂，加上 `{}` 即可。           |

---

## 总结

1. **`Action`**：只干活，不说话（返回 void）。
2. **`Func`**：干完活，必须给个交代（有返回值，最后一个泛型是返回类型）。
3. **场景**：回调函数、事件监听、动态逻辑配置。
