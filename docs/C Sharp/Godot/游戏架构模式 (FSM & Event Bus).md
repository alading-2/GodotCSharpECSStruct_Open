# Godot 游戏架构：有限状态机与事件总线

#CSharp #Godot #FSM #DesignPattern

## 1. 有限状态机 (FSM) —— 让角色的逻辑“活”过来

### 场景痛点：为什么不能用 `if-else`？

想象你在写马里奥。

- **普通逻辑**：如果按了“下”，就蹲下。
- **Bug 出现**：我在“跳跃”在空中的时候，按“下”，马里奥居然在空中蹲下了？或者在空中发起了攻击？
- **屎山代码**：你开始写 `if (isJumping == false && isAttacking == false)`... 越写越乱。

**FSM 的核心思想**：
**任何时刻，马里奥只能处于一种状态。** 在“跳跃状态”下，他根本就没有“蹲下”这个逻辑代码，所以绝对不会出 Bug。

---

### 第一步：定义“状态的模具” (Interface)

所有状态（站立、跑、跳）都必须具备这三个功能。

```csharp
public interface IState
{
    // 1. 进门：刚切换到这个状态时做的事 (比如：播放跳跃动画)
    void Enter();

    // 2. 只有在这个状态下，每一帧才会执行的逻辑 (比如：检测是否落地)
    void Update(double delta);

    // 3. 出门：离开这个状态时做的事 (比如：重置重力参数)
    void Exit();
}
```

### 第二步：写具体的状态类 (纯 C# 类)

注意：这些类不需要继承 `Node`，它们只是逻辑块。

#### 1. 站立状态 (IdleState)

```C#
public class IdleState : IState
{
    private Player _player; // 持有玩家引用，方便控制他

    public IdleState(Player player)
    {
        _player = player;
    }

    public void Enter()
    {
        GD.Print("进入站立状态");
        _player.PlayAnim("Idle"); // 播放呼吸动画
    }

    public void Update(double delta)
    {
        // 只有在站立时，按空格才能跳
        if (Input.IsActionJustPressed("jump"))
        {
            // 关键：请求切换到跳跃状态
            _player.ChangeState(new JumpState(_player));
        }

        // 如果有水平速度，切换到跑步状态
        if (_player.Velocity.X != 0)
        {
            _player.ChangeState(new RunState(_player));
        }
    }

    public void Exit() { } // 没什么要清理的
}
```

#### 2. 跳跃状态 (JumpState)

```C#
public class JumpState : IState
{
    private Player _player;

    public JumpState(Player player)
    {
        _player = player;
    }

    public void Enter()
    {
        GD.Print("起跳！");
        _player.Velocity += new Vector2(0, -500); // 施加向上冲量
        _player.PlayAnim("Jump");
    }

    public void Update(double delta)
    {
        // 在空中无法再次起跳，所以这里根本不写 Input 检测代码！
        // 彻底杜绝了“二段跳”Bug（除非你专门写逻辑）。

        // 检测落地
        if (_player.IsOnFloor())
        {
            _player.ChangeState(new IdleState(_player));
        }
    }

    public void Exit()
    {
        GD.Print("落地了，产生一点灰尘特效");
    }
}
```

### 第三步：在 Player 里组装状态机

这是最关键的一步。`Player` 变成了“空壳”，它把逻辑全权外包给了 `_currentState`。

```C#
public partial class Player : CharacterBody2D
{
    // 核心变量：当前是哪个状态？
    private IState _currentState;

    public override void _Ready()
    {
        // 游戏开始，默认是站立
        ChangeState(new IdleState(this));
    }

    public override void _PhysicsProcess(double delta)
    {
        // ★★★ 每一帧，只运行当前状态的逻辑 ★★★
        // 如果现在是 Idle，就只跑 Idle.Update
        // 如果现在是 Jump，就只跑 Jump.Update
        _currentState?.Update(delta);

        MoveAndSlide(); // Godot 的物理移动
    }

    // 公共方法：切换状态
    public void ChangeState(IState newState)
    {
        // 1. 让旧状态做清理工作
        _currentState?.Exit();

        // 2. 换新状态
        _currentState = newState;

        // 3. 让新状态做初始化
        _currentState.Enter();
    }

    // 辅助方法：给状态类调用的
    public void PlayAnim(string name)
    {
        // 播放动画的代码...
    }
}
```

### 总结 FSM 的威力

1. **逻辑隔离**：`JumpState` 里完全看不到“蹲下”的代码，代码极其干净。
2. **扩展性无敌**：如果你想加个“滑翔”状态，直接新建 `GlideState.cs`，在跳跃状态里加个判断 `if (Input.IsActionPressed("glide")) ChangeState(new GlideState(...))` 即可。**完全不需要改动 Player.cs 的核心代码。**

---

## 2. 事件总线 (Event Bus) —— 解耦神器

游戏里 UI 和 逻辑 经常打架。 **痛点**：血条 UI 脚本里如果不写 `player = GetNode<Player>(...)` 就拿不到血量。 但是，如果有多个主角？如果主角死了重生成了新对象？引用就断了。

**解决方案**：使用 `static` 事件作为中转站。

### 第一步：创建总线

```C#
// 一个纯静态类，不需要挂载到任何节点
public static class EventBus
{
    // 定义一个事件：玩家血量改变
    // Action<int, int> 代表：当前血量，最大血量
    public static event Action<int, int> OnPlayerHealthChanged;

    // 提供一个触发方法（可选，也可以直接 Invoke）
    public static void PlayerHit(int current, int max)
    {
        OnPlayerHealthChanged?.Invoke(current, max);
    }
}
```

### 第二步：玩家 (发送者)

玩家不需要知道 UI 存在不存在。

```C#
public void TakeDamage(int dmg)
{
    Hp -= dmg;
    // 广播：我被打啦！谁关心谁就去听，我不care。
    EventBus.PlayerHit(Hp, MaxHp);
}
```

### 第三步：UI (监听者)

UI 不需要知道 Player 在哪，叫什么名字。

```C#
public partial class HealthBar : ProgressBar
{
    public override void _Ready()
    {
        // 订阅事件
        EventBus.OnPlayerHealthChanged += UpdateBar;
    }

    private void UpdateBar(int current, int max)
    {
        Value = (float)current / max * 100;
    }

    // ★★★ 静态事件必须手动解绑，否则内存泄漏 ★★★
    public override void _ExitTree()
    {
        EventBus.OnPlayerHealthChanged -= UpdateBar;
    }
}
```
