# using (多面手)

`using` 是 C# 里的瑞士军刀，它有 **4 种完全不同的身份**。

## 身份 A：导包 (Directive) —— 最常用

### 作用
相当于 TS 的 `import`。

### Godot 场景
引入 `Godot` 命名空间，这样你才能直接写 `Node` 而不是 `Godot.Node`。

### 代码
```csharp
using Godot; // 必须写在文件最顶端
using System;
```

## 身份 B：语法糖 (Static Import) —— 偷懒神器

### 作用
把工具箱里的工具全倒在桌子上，不用每次都翻箱子。

### Godot 场景
让你像写 GDScript 一样直接调用 `Print()`。

### 代码
```csharp
using static Godot.GD;

public void Test()
{
    Print("爽！"); // 省略了 GD.Print
}
```

## 身份 C：起别名 (Alias) —— 解决冲突

### 作用
相当于 TS 的 `import ... as ...`。

### Godot 场景
当 Godot 的类和 C# 系统类重名时（比如 `Timer`）。

### 代码
```csharp
using GTimer = Godot.Timer; // 给 Godot 的 Timer 起个外号
using SysTimer = System.Timers.Timer;
```

## 身份 D：自动清理 (Statement) —— 借完即还

### 作用
借用资源（文件/网络），用完自动归还（销毁）。

### 注意事项
**Godot 的节点（Node）不能用这个！** 节点要用 `QueueFree()`。这个主要用于纯 C# 的文件读写。

### 代码
```csharp
// 只要代码跳出这个作用域，file 就会自动被 Close/Dispose
using var file = System.IO.File.OpenText("data.txt");
```

#Tag: #CSharp #Godot #using