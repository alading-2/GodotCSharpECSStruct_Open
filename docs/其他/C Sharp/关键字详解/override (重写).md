# override (重写)

## 一句话解释

**显式覆盖**父类允许修改的行为。

## ⚠️ 核心规则 (最重要的点)

在 C# 中，**并不是**父类的所有方法都能被子类修改。
必须遵守“**双向契约**”：

1.  **父类点头 (`virtual`)**：父类必须在方法前加 `virtual`，表示“我允许这个方法被修改”。
2.  **子类动手 (`override`)**：子类必须在方法前加 `override`，表示“我确实要修改这个方法”。

> **如果没有 `virtual`**：父类的方法就是“封死”的，子类无法重写（会报错）。

## Godot 场景

所有的 Godot 脚本都继承自父类（比如 `Player : Node2D : Node`）。
Godot 引擎的底层代码里，已经把生命周期方法定义成了虚方法：

```csharp
// Godot 源代码里的定义（伪代码）
public class Node
{
    public virtual void _Ready() { } // 👈 因为是 virtual，你才能重写
    public virtual void _Process(double delta) { }
}
```

当你写脚本时：

```C#
public partial class Player : Node2D
{
    // ✅ 合法：因为父类 Node 标记了 _Ready 为 virtual
    public override void _Ready()
    {
        GD.Print("玩家加载完毕");
    }
}
```

## 代码对比：C# vs TS

### TypeScript (默认允许)

TS 不需要父类同意，子类直接写同名方法就覆盖了。

```TypeScript
class Monster {
    attack() { console.log("通用攻击"); }
}

class Slime extends Monster {
    // 直接覆盖，没有任何关键字限制
    override attack() { console.log("史莱姆攻击"); }
}
```

### C# (默认禁止)

C# 必须显式授权。

```C#
// ❌ 错误示范
public class Monster
{
    public void Attack() { } // 没加 virtual
}

public class Slime : Monster
{
    // 💥 编译报错：无法重写，因为父类 Attack 不是 virtual
    public override void Attack() { }
}

// ✅ 正确示范
public class Monster
{
    public virtual void Attack() { } // 👈 加上 virtual
}

public class Slime : Monster
{
    public override void Attack() { } // 👈 加上 override
}
```

## 注意事项

- **为什么这么麻烦？**：为了安全。防止你“不小心”重写了父类的核心逻辑。
- **隐藏警告**：如果你不写 `override` 直接写 `void _Ready()`，编译器会警告你“隐藏了父类成员”。虽然游戏能跑，但极不推荐，容易产生歧义。

#Tag: #CSharp #Godot #Override #Virtual
