# Static 陷阱与生命周期管理

在 Godot C# 开发中，`static` 关键字是一把双刃剑。如果使用不当，会导致极其隐蔽的内存泄漏、`ObjectDisposedException` 甚至游戏崩溃。理解 C# 对象生命周期与 Godot 节点生命周期的差异至关重要。

## 1. 核心冲突：生命周期不匹配

- **C# Static**: 生命周期伴随整个 **应用程序域 (AppDomain)**。除非游戏彻底关闭，否则静态变量持有的引用永远不会被释放。
- **Godot Node**: 生命周期伴随 **场景树 (SceneTree)**。当你切换场景或调用 `QueueFree()` 时，旧场景的节点会在 **C++ 层** 被彻底销毁。

**后果**：
如果你用 `static` 持有了一个 Godot 节点的引用，当场景切换后，C# 端的引用依然存在（它不会自动变为 `null`），但它指向的底层 C++ 对象已经没了。此时访问该对象的任何属性（如 `Position`）都会抛出 `ObjectDisposedException`。

---

## 2. 深度拆解：为什么 ObjectPool 的 static 是“剧毒”？

### ❌ 反面教材：静态对象池持有 Node

```csharp
public partial class Bullet : Area2D
{
    // 危险！持有了 List<Node> (Bullet 实例)
    private static readonly ObjectPool<Bullet> _pool = new(...);
}
```

- **持有物**：`Bullet` 是 Godot 场景节点。
- **崩溃过程**：
  1. 场景 A 加载，子弹进入池子。
  2. 切换到场景 B，场景 A 的所有节点被销毁。
  3. `_pool` 依然存活（因为是 `static`），里面抓着一堆“死掉”的 `Bullet` 引用。
  4. 场景 B 尝试 `_pool.Acquire()`，拿到一个死掉的子弹并访问它 -> **崩溃**。

### ✅ 正确做法：随场景生灭

Node 类型的对象池必须放在 **非静态** 的管理器中（如 `LevelManager` 节点或 `GlobalPoolManager` 自动加载单例），确保场景切换时池子随之销毁或清空。

---

## 3. 为什么 Logger 的 static 是“良药”？

### ✅ 安全教材：静态日志助手

```csharp
private static readonly Log Log = new Log("Weapon");
```

- **持有物**：`Log` 实例内部通常只持有一个 `string` 类型的名字（"Weapon"）和一些配置（`LogLevel`）。
- **安全性分析**：
  - **纯数据**：`string` 是纯 C# 数据，不依赖 Godot 引擎的生命周期。
  - **全局 API**：它调用的 `GD.Print` 或 `GD.PushError` 是 Godot 的静态全局 API，不需要依附于任何节点树。
- **结论**：即使 `Weapon` 节点被销毁，静态的 `Log` 实例依然活着，但它没有引用任何已经销毁的引擎资源，所以它是安全的。

---

## 4. 判定准则：什么时候可以用 static？

| 持有物类型                              | 是否推荐 static | 原因                                                                     |
| :-------------------------------------- | :-------------- | :----------------------------------------------------------------------- |
| **纯 C# 数据** (string, int, struct)    | **推荐**        | 不依赖引擎生命周期，无副作用。                                           |
| **纯 C# 类** (POCO, 数据模型)           | **安全**        | 只要不间接持有 Node 引用即可。                                           |
| **Godot Node** (Bullet, Player, Sprite) | **严禁**        | 节点会随场景销毁，static 会导致悬空引用。                                |
| **Godot Resource** (Texture, Mesh)      | **慎用**        | 虽然 Resource 是引用计数的，但 static 会阻止资源卸载，导致内存占用过高。 |

## 5. 唯一的“危险禁区”：如何把 Logger 搞崩

即使是 Logger，如果实现时犯了以下错误，也会变毒：

```csharp
// ❌ 危险写法：让 Log 持有 Node 引用
public class Log {
    private Node _target;
    public Log(Node target) { _target = target; } // 持有了节点！
}

// 在 Weapon 中使用
private static readonly Log Log = new Log(this); // 即使绕过编译错误，也会导致 ObjectDisposedException
```

**金律**：**永远不要在 `static` 变量中存储任何直接或间接继承自 `GodotObject` 的对象（特别是 Node），除非你能 100% 保证在它们销毁前手动将静态引用置为 `null`。**
