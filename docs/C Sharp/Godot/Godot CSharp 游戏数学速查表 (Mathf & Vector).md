# Godot C# 游戏数学速查表 (Mathf & Vector)

#CSharp #Godot #Mathf #GameDev

## 1. 核心类说明

Godot 的 C# 数学库和 Unity 不同，也和 System.Math 不同。

- **`Godot.Mathf`**: 包含了所有通用数学函数 (Lerp, Clamp, Sign 等)。
- **`Vector2` / `Vector3`**: 结构体，包含了向量运算 (Dot, DistanceTo, Normalized)。

---

## 2. 向量 (Vector) —— 所有的位移与方向

假设：`Player` (玩家位置), `Target` (怪物位置)。

### A. 获取方向 (Normalized)

**场景**：我想让子弹飞向敌人，或者让玩家朝向敌人移动。
**原理**：`目标 - 自身` 得到一个矢量，然后 `Normalized()` 把它变成长度为 1 的标准方向向量。

```csharp
// 1. 计算向量
Vector2 direction = (Target.GlobalPosition - Player.GlobalPosition).Normalized();

// 2. 移动
Velocity = direction * Speed;
```

### B. 获取距离 (Distance)

**场景**：怪物离我多近？是不是进入攻击范围了？

```C#
// 方式 1: 直接算距离 (开销略大，因为要开平方)
float dist = Player.GlobalPosition.DistanceTo(Target.GlobalPosition);
if (dist < 5.0f) Attack();

// 方式 2: 算距离的平方 (性能极高，适合高频检测)
// 比如 5米 的平方是 25
float distSq = Player.GlobalPosition.DistanceSquaredTo(Target.GlobalPosition);
if (distSq < 25.0f) Attack();
```

### C. 向量插值 (Lerp) —— 丝滑的移动

**场景**：摄像机跟随。不要瞬间“闪现”到主角头顶，而是慢悠悠地飘过去。

```C#
// "当前位置" 慢慢变成 "目标位置"，weight 是速度系数 (0 ~ 1)
// delta * 5.0f 决定了平滑程度，数字越大越快
Camera.GlobalPosition = Camera.GlobalPosition.Lerp(Player.GlobalPosition, (float)delta * 5.0f);
```

---

## 3. 点积 (Dot Product) —— 判定方位

**公式**：`A.Dot(B)` **结果**：一个浮点数。

### 实战：我在敌人的前面还是后面？(背刺判定)

![Vector Dot Product visualization的图片](https://encrypted-tbn3.gstatic.com/licensed-image?q=tbn:ANd9GcRtZrDjXPWSzseu_5e8LS3yqYWHkmE3SP3t36_dfGtG5UqLc8RM3k0L3NK0Oe2-5CTiQkBNm-FQba2Qr2RYlK3421N3L5DtznnOBby73OAAdRB7Zwc)

Shutterstock

利用 **“玩家的前方向量”** 和 **“敌人的方位向量”** 做点积。

```C#
Vector2 toEnemy = (Enemy.GlobalPosition - Player.GlobalPosition).Normalized();
Vector2 playerFacing = Player.Transform.X; // 假设 X 轴是正脸朝向

float dotResult = playerFacing.Dot(toEnemy);

if (dotResult > 0.5f)
{
    GD.Print("敌人在我正前方 (视野内)");
}
else if (dotResult < -0.5f)
{
    GD.Print("敌人在我正后方 (视野盲区)");
}
// -0.5 ~ 0.5 之间就是在侧面
```

---

## 4. Mathf 通用工具库

这些函数全在 `Godot.Mathf` 静态类里。

### A. 钳制 (Clamp) —— 限制数值范围

**场景**：血量不能超过 100，也不能低于 0。

```C#
// ❌ 笨写法
if (hp > maxHp) hp = maxHp;
if (hp < 0) hp = 0;

// ✅ Mathf 写法
hp = Mathf.Clamp(hp, 0, 100);
```

### B. 移向 (MoveToward) —— 匀速变化

**场景**：很多教程教你用 Lerp 做移动，其实不对。Lerp 是“先快后慢”，`MoveToward` 才是“匀速直线”。

```C#
// 让 CurrentHp 匀速涨到 MaxHp，每帧涨 10 点
CurrentHp = Mathf.MoveToward(CurrentHp, MaxHp, 10 * (float)delta);
```

### C. 符号 (Sign) —— 获取方向

**场景**：根据移动方向翻转 Sprite 图片。

```C#
// 如果 velocity.X 是正数返回 1，负数返回 -1，0 返回 0
int direction = Mathf.Sign(Velocity.X);

if (direction != 0)
{
    // 利用 Scale.X = -1 来翻转图片
    Sprite.Scale = new Vector2(direction, 1);
}
```

---

## 5. 高级技巧：角度与旋转

### A. LookAt (盯着看)

**场景**：炮台永远指向玩家。 这不是数学函数，是 Node2D/Node3D 的内置方法，但底层全是数学。

```C#
// 瞬间转过去
LookAt(Player.GlobalPosition);
```

### B. 平滑旋转 (RotateToward)

**场景**：坦克炮塔转动，不能瞬间转过去，要有旋转速度。

```C#
// 1. 算出目标角度
float targetAngle = GetAngleTo(Player.GlobalPosition);

// 2. 慢慢转过去 (Rotation 是当前弧度)
// delta * 2.0f 是旋转速度 (弧度/秒)
Rotation = Mathf.RotateToward(Rotation, Rotation + targetAngle, 2.0f * (float)delta);
```

---

## 总结：程序员的数学直觉

1. 看到 **“平滑”、“缓冲”** -> 马上想到 **`Lerp`**。
2. 看到 **“范围限制”** -> 马上想到 **`Clamp`**。
3. 看到 **“视野”、“背刺”** -> 马上想到 **`Dot` (点积)**。
4. 看到 **“距离判定”** -> 优先用 **`DistanceSquaredTo`** (平方距离) 省性能。
