# Godot CSharp 实战：超越 List 的数据结构

#CSharp #Godot #DataStructures #Optimization

## 1. Dictionary<TKey, TValue> (字典) —— 查找速度快 100 倍

你一定写过这种代码：用 `for` 循环遍历列表去找一个 ID 为 1001 的物品。当物品有几千个时，游戏就会卡顿。
**字典** 就像一本查字典，通过 **Key (索引)** 瞬间找到 **Value (内容)**，不需要遍历。

### 场景

- 背包系统 (ItemID -> ItemData)
- 技能池 (SkillName -> SkillObj)
- 装备栏 (SlotType -> Equipment)

### 基础用法

```csharp
// 定义：Key是字符串(ID)，Value是物品类
Dictionary<string, Item> inventory = new Dictionary<string, Item>();

// 1. 添加 (Add)
inventory.Add("sword_01", new Sword());
// inventory.Add("sword_01", ...); // ❌ 报错！Key 不能重复

// 2. 查找 (索引器)
// 如果确定 Key 存在，可以直接用 [ ]
var mySword = inventory["sword_01"];

// 3. 安全查找 (TryGetValue) —— ★★★ 推荐
// 如果不确定 Key 是否存在，用这个防止报错
if (inventory.TryGetValue("shield_99", out Item result))
{
    result.Use();
}
else
{
    GD.Print("查无此物");
}

// 4. 遍历
foreach (var kvp in inventory)
{
    GD.Print($"ID: {kvp.Key}, 物品: {kvp.Value.Name}");
}
```

---

## 2. HashSet<T> (哈希集合) —— 专治重复

如果你只关心 **“有没有”**，而不关心“第几个”，也不需要存 Key-Value 映射，那就用 `HashSet`。它的查找速度和字典一样快。

### 场景

- **成就系统**：记录玩家获得过的成就 ID (不能重复获得)。
- **Buff 系统**：当前受影响的敌人列表 (一个毒圈里的敌人，不能每帧都重复加进列表)。

### 基础用法

```CSharp
HashSet<int> unlockedAchievements = new HashSet<int>();

// 1. 添加 (自动去重)
bool isNew = unlockedAchievements.Add(101); // 返回 true
bool isRepeat = unlockedAchievements.Add(101); // 返回 false (添加失败，已存在)

// 2. 极速查询 (Contains)
// 哪怕里面有 100万个数据，这也是瞬间完成
if (unlockedAchievements.Contains(101))
{
    ShowIcon("已解锁");
}

// 3. 集合运算 (交集/并集) —— 进阶
// 计算两个玩家共同拥有的成就
otherPlayerSet.IntersectWith(mySet);
```

---

## 3. Queue<T> (队列) & Stack<T> (栈)

这两个是 **“有顺序”** 的容器。

### A. Queue (队列) —— 先进先出 (FIFO)

像排队买票。先加进去的，先拿出来。

- **场景**：
  - **对话系统**：把 NPC 要说的 5 句话塞进去，按空格弹出一句。
  - **输入缓冲**：格斗游戏中，玩家极快按下的连招指令。

```CSharp
Queue<string> dialogs = new Queue<string>();
dialogs.Enqueue("你好");
dialogs.Enqueue("吃饭了吗？");
dialogs.Enqueue("再见");

// 拿出第一个
string nextLine = dialogs.Dequeue(); // "你好"
// 现在的第一个是 "吃饭了吗？"
```

### B. Stack (栈) —— 后进先出 (LIFO)

像叠盘子。最后放上去的盘子，最先被拿走。

- **场景**：
  - **UI 窗口管理**：主界面 -> 打开设置 -> 打开音频设置。点“返回”时，应该回到“设置”，再点回到“主界面”。

```CSharp
Stack<Control> uiStack = new Stack<Control>();

// 打开新窗口
uiStack.Push(settingsPanel);
uiStack.Push(audioPanel);

//以此类推，返回上级
Control currentPanel = uiStack.Pop(); // 关闭 audioPanel，取出它
currentPanel.Hide();
```
