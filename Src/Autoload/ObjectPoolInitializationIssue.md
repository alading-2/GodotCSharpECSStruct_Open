# ObjectPool 初始化层级缺失问题总结

## 1. 问题现象
在游戏启动时，通过 `ObjectPoolInit` (AutoLoad) 创建的 `ECS/Entity/Enemy` 节点层级在 Godot 远程场景树中不可见，且日志显示父节点路径为空。

## 2. 核心原因：AutoLoad 初始化时机 (Timing Issue)
这是 Godot 引擎的一个底层特性导致的：

- **AutoLoad 优先级**：`ObjectPoolInit` 设置了 `AutoLoad.Priority.System`，这使得它在引擎启动的极早期阶段就会运行 `_Ready`。
- **场景树未就绪**：在那个时刻，虽然 `SceneTree.Root` (Window) 对象已经创建，但它尚未完全“激活”并挂载到主引擎循环中。
- **结果**：
    - 对尚未激活的节点调用 `GetPath()` 会返回空字符串。
    - 此时调用 `AddChild` 虽然在内存中建立了父子关系，但由于父节点还没进入主场景树，子节点也无法获得正确的层级路径，导致在编辑器远程树中不可见。

## 3. 解决方案对比

### 方案 A：异步等待 (已验证)
使用 `await ToSignal(GetTree(), "process_frame")` 强制等待一帧。
- **优点**：简单直接，逻辑线性。
- **缺点**：引入了 `async void`，在某些情况下可能导致调用栈难以追踪。

### 方案 B：延迟调用 (CallDeferred) - 推荐方案
使用 `CallDeferred(nameof(InitMethod))`。
- **原理**：将初始化逻辑推迟到当前帧的末尾，此时 Godot 已经完成了所有底层节点的挂载。
- **优点**：无需 `async/await`，代码更符合 Godot 原生习惯，且执行时机恰好在场景树就绪后。

## 4. 结论
对于需要在启动时创建全局节点层级的 AutoLoad 模块，**必须避开第一帧的初始时刻**。使用 `CallDeferred` 是最稳健且简洁的处理方式。
