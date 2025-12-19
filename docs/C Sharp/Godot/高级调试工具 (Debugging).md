
# Godot C# 进阶：调试与可视化

#CSharp #Godot #Debug

## 1. 远程场景树 (Remote Tree) —— 此时此刻的真相

你按下 F5 运行游戏后，编辑器场景面板里显示的还是**原本的**场景结构。
如果代码里动态生成了怪物，或者怪物移动了，你在编辑器里是看不到的。

**操作方法：**
1. 运行游戏。
2. 切换回 Godot 编辑器。
3. 在左侧“场景”面板上方，点击 **"Remote" (远程)** 按钮。



**你会看到什么？**
* **实时的节点树**：所有你 `AddChild` 进去的怪物都在这。
* **实时的属性**：点击一个怪物，在右侧属性面板里，你可以看到它当前的 `Position`、`Hp` 都在变。
* **实时修改**：你可以手动修改 `Hp`，测试怪物死没死；或者手动拖动它的位置，测试物理碰撞。

---

## 2. 断点调试 (Breakpoints) —— 让时间停止

别再用 `GD.Print` 猜 Bug 了。

**操作方法 (VS Code / Rider)：**
1.  在代码行号左边点一下，出现一个🔴红点。
2.  按 F5 启动调试模式运行游戏。
3.  当游戏运行到这一行时，会**完全暂停**。

**你能做什么？**
* **查看变量**：鼠标悬停在变量名上，看它此刻的值是多少（是不是 null？）。
* **单步执行**：按 F10 一行行往下走，看逻辑流程是不是按你想的走的。
* **调用堆栈**：看是谁调用了这个函数。

---

## 3. 可视化调试 (_Draw) —— 画出看不见的东西

对于射线检测 (RayCast)、视野范围 (FOV)、寻路路径，肉眼是看不见的。我们需要把它们画出来。

Godot 提供了 `_Draw` 虚函数，专门用于在 2D 节点上绘图。

### 实战：画出怪物的警戒范围

```csharp
public partial class Enemy : Node2D
{
    [Export] public float ViewRadius = 200.0f; // 警戒半径
    [Export] public bool IsAggro = false;      // 是否发现玩家

    public override void _Process(double delta)
    {
        // 强制重绘 (否则 _Draw 只会在初始化时跑一次)
        QueueRedraw();
    }

    // 每一帧 Godot 都会调用这个来画画
    public override void _Draw()
    {
        // 1. 画一个半透明的圆圈表示视野
        var color = IsAggro ? Colors.Red : Colors.Green;
        color.A = 0.3f; // 透明度
        
        // 在自身位置画圆 (0,0 是相对于自己的原点)
        DrawCircle(Vector2.Zero, ViewRadius, color);

        // 2. 画一条线指向玩家 (如果知道玩家在哪)
        // DrawLine(Vector2.Zero, ToLocal(PlayerPos), Colors.White, 2.0f);
    }
}
````

**效果**： 你在游戏里能清楚地看到每个怪物周围有个绿圈，一旦你走进去，圈变成红色。调试 AI 极其好用！