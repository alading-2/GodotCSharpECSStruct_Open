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

## 2. HashSet `<T>` (哈希集合) —— 专治重复

如果你只关心 **“有没有”**，而不关心“第几个”，也不需要存 Key-Value 映射，那就用 `HashSet`。它的查找速度和字典一样快。

### 场景

- **成就系统**：记录玩家获得过的成就 ID (不能重复获得)。
- **Buff 系统**：当前受影响的敌人列表 (一个毒圈里的敌人，不能每帧都重复加进列表)。

### 基础用法

```CSharp
HashSet<int> unlockedAchievements = new HashSet<int>();

// 1. 添加 (Add，自动去重)
bool isNew = unlockedAchievements.Add(101); // 返回 true
bool isRepeat = unlockedAchievements.Add(101); // 返回 false (添加失败，已存在)

// 2. 极速查询 (Contains)
// 哪怕里面有 100万个数据，这也是瞬间完成
if (unlockedAchievements.Contains(101))
{
    ShowIcon("已解锁");
}

// 3. 集合运算 (IntersectWith，交集/并集) —— 进阶
// 计算两个玩家共同拥有的成就
otherPlayerSet.IntersectWith(mySet);
```

---

## 3. Queue `<T>` (队列) & Stack `<T>` (栈)

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

---

## 1.三大容器的核心区别

| **特性**          | **List`<T>` (列表)**      | **Queue`<T>` (队列)** | **Stack`<T>` (栈)** |
| ----------------- | ------------------------- | --------------------- | ------------------- |
| **逻辑模型**      | **动态数组**              | **管道 / 排队**       | **弹夹 / 叠盘子**   |
| **访问方式**      | **随机访问** (`list[5]`)  | **先进先出** (FIFO)   | **后进先出** (LIFO) |
| **核心操作**      | `Add`, `Remove`, `Insert` | `Enqueue`, `Dequeue`  | `Push`, `Pop`       |
| **谁最先出来?**   | 你指定的任意一个          | 最早进去的那个        | 最后进去的那个      |
| **遍历速度**      | ⭐⭐⭐⭐⭐ (最快)         | ⭐⭐⭐⭐              | ⭐⭐⭐⭐            |
| **插入/删除速度** | 尾部快，中间慢 (要移位)   | 极快 (只在头尾操作)   | 极快 (只在顶部操作) |

---

### 2. 详细场景与关键字对比

#### A. List `<T>` (列表) —— "万金油"

它是最常用的容器。本质上是一个**会自动扩容的数组**。

- **场景**：

  - 你需要**随机访问**（比如：我要拿第 3 个敌人）。
  - 你需要**排序**（Sort）。
  - 你需要频繁遍历（foreach）。
  - _例子：背包物品栏、当前屏幕上的所有怪物。_

- **核心关键字**：

  - `Add(item)`: 加到末尾。
  - `Insert(index, item)`: 插到中间（**慢**，因为后面的元素都要往后挪）。
  - `Remove(item)` / `RemoveAt(index)`: 删除（**慢**，后面的元素要往前挪）。
  - `[index]`: **索引器**，如 `list[0]`，这是 List 独有的，Queue 和 Stack 不能用下标访问！

#### B. Queue `<T>` (队列) —— "传送带"

严格遵守 **FIFO (First In, First Out)**。

- **场景**：

  - **缓冲处理**：比如输入缓冲（格斗游戏搓招），网络数据包处理。
  - **寻路算法**：广度优先搜索 (BFS) 必须用队列。
  - **消息日志**：聊天框，新的在下面，旧的从上面顶出去。
  - _例子： 中提到的对话系统（按顺序播放）。_

- **核心关键字**：

  - `Enqueue(item)`: **入队**（排队）。
  - `Dequeue()`: **出队**（取出并删除最前面的元素）。
  - `Peek()`: **偷看**（看一眼最前面的元素是谁，但不把它拿走）。

#### C. `Stack<T>` (栈) —— "撤销操作"

严格遵守 **LIFO (Last In, First Out)**。

- **场景**：

  - **UI 界面导航**：主菜单 -> 设置 -> 声音。按“返回”键时，是从“声音”回到“设置”。
  - **撤销功能 (Undo)**：你在编辑器里画了一笔，再画一笔。按 Ctrl+Z 时，是撤销**最后**画的那一笔。
  - **状态机**：某些 AI 行为树或状态机（Pushdown Automata）。

- **核心关键字**：

  - `Push(item)`: **压栈**（放一个盘子到顶端）。
  - `Pop()`: **弹栈**（拿走并删除顶端的盘子）。
  - `Peek()`: **偷看**（看一眼顶端的盘子，不拿走）。

---

### 3. 常见误区与“坑”

#### 误区 1：所有容器都能用 `list[i]` 访问吗？

- **不能。** 只有 `List<T>` 和数组 `T[]` 支持下标访问。
- `Queue` 和 `Stack` **不支持** `myQueue[0]`。因为它们的设计初衷就是不让你插队，只能操作头或尾。
- _如果你想遍历 Queue 的所有内容怎么办？_ 可以用 `foreach`，或者 `ToArray()` 转成数组再访问。

#### 误区 2：`Peek()` vs `Pop()`/`Dequeue()`

- 新手常犯错误：想拿出来用，结果用了 `Peek()`。
- `Peek()`：**只读**。容器里的数量 **Count 不变**。
- `Pop()` / `Dequeue()`：**读 + 删**。容器里的数量 **Count - 1**。

#### 误区 3：List 的 `Remove` 性能

- 在 `List` 中删除元素（特别是第一个元素 `RemoveAt(0)`）是非常消耗性能的，因为所有后续元素都要向前移动一位。
- 如果你需要频繁删除**第一个**元素，请改用 `Queue<T>`。
- 如果你需要频繁删除**最后一个**元素，`List` 和 `Stack` 都可以。

### 4. 总结速查表

| **我想要...**                                  | **推荐容器**        | **关键方法**          |
| ---------------------------------------------- | ------------------- | --------------------- |
| 我要存一堆东西，经常要**通过下标**找它们       | **List`<T>`**       | `list[i]`             |
| 我要处理一堆任务，**先来的先做**               | **Queue`<T>`**      | `Enqueue` / `Dequeue` |
| 我要实现**“返回上一步”**或**UI 层级管理**      | **Stack`<T>`**      | `Push` / `Pop`        |
| 我要存一堆东西，**绝对不允许重复**，且查找极快 | **HashSet`<T>`**    | `Add` / `Contains`    |
| 我要通过一个 ID 查找对应的物品                 | **Dictionary<K,V>** | `dict[key]`           |

结合你之前的笔记，你应该能发现：**List 是通用的，而 Queue 和 Stack 是为了特定的逻辑顺序而存在的专用工具。** 在游戏开发中，80% 的情况用 `List`，剩下 20% 的特殊逻辑用 `Queue` 和 `Stack` 会让代码写起来非常顺手。
