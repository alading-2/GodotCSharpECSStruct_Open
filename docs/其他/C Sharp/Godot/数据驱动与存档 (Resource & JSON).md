# Godot 数据驱动与存档：Resource 与 JSON

#CSharp #Godot #JSON #Serialization

## 1. Resource (自定义资源) —— 策划的好朋友

在 Unity 里叫 `ScriptableObject`，在 Godot 里就叫 `Resource`。
这是 Godot 极其强大的特性。它允许你把 **数据** 保存为 `.tres` 文件，像贴图一样在编辑器里拖拽。

### 第一步：定义数据模板

必须继承 `Resource`，必须加 `[GlobalClass]` (Godot 4 特性)。

```csharp
using Godot;

[GlobalClass] // 加上这个，编辑器右键 "New Resource" 就能搜到它
public partial class ItemData : Resource
{
    [Export] public string ItemName { get; set; } = "Unknown";
    [Export] public int Damage { get; set; } = 10;
    [Export] public Texture2D Icon { get; set; }
    [Export] public float Weight { get; set; }
}
```

### 第二步：在编辑器创建

1. 在 Godot 文件系统面板右键 -> **Create New...** -> **Resource**。
2. 搜索 `ItemData`。
3. 保存为 `Sword.tres`。
4. 现在你可以在右侧属性面板里填数值、拖图片了！

### 第三步：在代码里使用

```C#
[Export] public ItemData MyWeaponData; // 直接把 .tres 文件拖给这个变量

public override void _Ready()
{
    GD.Print($"武器名: {MyWeaponData.ItemName}, 攻击力: {MyWeaponData.Damage}");
}
```

---

## 2. JSON 解析 —— 游戏存档/读档

`Resource` 适合存**只读**的配置数据（策划配好的）。 `JSON` 适合存**动态**的玩家存档（玩家几级了，在哪，背包有啥）。

我们需要用到 .NET 标准库 `System.Text.Json`。

### 核心类：存档数据模型

这个类**不需要**继承 Godot 的 Node 或 Resource，就是一个纯纯的 C# 类 (POCO)。

```C#
// 用来存到硬盘的数据结构
public class SaveData
{
    public string PlayerName { get; set; }
    public int Level { get; set; }
    public float[] Position { get; set; } // Vector2 不能直接序列化，建议转数组或自定义转换
    public List<string> InventoryIds { get; set; }
}
```

### 实战：保存与读取

```C#
using System.Text.Json; // 必须引用
using Godot;

public partial class SaveManager : Node
{
    private string _savePath = "user://savegame.json"; // Godot 专用用户目录

    public void SaveGame()
    {
        // 1. 准备数据
        var data = new SaveData
        {
            PlayerName = "Hero",
            Level = 5,
            InventoryIds = new List<string> { "sword_01", "potion_05" }
        };

        // 2. 序列化：Object -> String
        // WriteIndented = true 会让生成的 JSON 有换行缩进，方便人看
        string jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        // 3. 写入文件 (使用 Godot 的 FileAccess)
        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Write);
        file.StoreString(jsonString);

        GD.Print("存档成功！");
    }

    public void LoadGame()
    {
        if (!FileAccess.FileExists(_savePath)) return;

        // 1. 读取文件
        using var file = FileAccess.Open(_savePath, FileAccess.ModeFlags.Read);
        string jsonString = file.GetAsText();

        // 2. 反序列化：String -> Object
        SaveData data = JsonSerializer.Deserialize<SaveData>(jsonString);

        // 3. 恢复游戏状态
        GD.Print($"欢迎回来, {data.PlayerName}, 等级: {data.Level}");
    }
}
```
