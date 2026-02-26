using Godot;
using System;

// 高级导出属性测试脚本 - 展示Godot 4.5中C#导出属性的各种功能
public partial class ExportTest : Node
{
    // 基础属性分组 - 测试基本数据类型
    [ExportGroup("基础数据类型")]

    [Export(PropertyHint.Range, "0,100,1")] // 整数范围：0-100，步长1
    public int Health { get; set; } = 50;

    [Export(PropertyHint.Range, "0.0,1.0,0.01")] // 浮点范围：0.0-1.0，步长0.01
    public float SpeedMultiplier { get; set; } = 1.0f;

    [Export(PropertyHint.MultilineText)] // 多行文本输入
    public string Description { get; set; } = "这是一个测试描述";

    [Export] // 普通字符串
    public string PlayerName { get; set; } = "玩家名称";

    // 文件和路径分组
    [ExportGroup("文件和路径")]

    [Export(PropertyHint.File, "*.png,*.jpg,*.jpeg")] // 图片文件选择器
    public string CharacterImagePath { get; set; } = "";

    [Export(PropertyHint.File, "*.tscn,*.scn")] // 场景文件选择器
    public string ScenePath { get; set; } = "";

    [Export(PropertyHint.Dir)] // 目录选择器
    public string DataDirectory { get; set; } = "res://data";

    // 资源和节点引用分组
    [ExportGroup("资源和节点引用")]

    [Export] // 纹理资源引用
    public Texture2D CharacterTexture { get; set; }

    [Export] //  PackedScene资源引用
    public PackedScene EnemyScene { get; set; }

    [Export] // 音频流资源引用
    public AudioStream BackgroundMusic { get; set; }

    [Export] // 节点路径引用
    public NodePath PlayerNodePath { get; set; }

    // 枚举和标志位分组
    [ExportGroup("枚举和标志位")]

    [Export] // 枚举导出
    public CharacterType Type { get; set; } = CharacterType.Warrior;

    [Export(PropertyHint.Flags, "跳跃,冲刺,二段跳,滑翔,爬墙")] // 标志位导出
    public Abilities PlayerAbilities { get; set; } = Abilities.Jump | Abilities.Dash;

    // 高级属性分组 - 颜色和渐变
    [ExportGroup("颜色和渐变")]

    [Export(PropertyHint.ColorNoAlpha)] // 无透明度的颜色
    public Color PrimaryColor { get; set; } = Colors.Red;

    [Export] // 完整颜色（含透明度）
    public Color SecondaryColor { get; set; } = new Color(0.0f, 1.0f, 0.0f, 0.8f);

    [Export] // 渐变资源
    public Gradient HealthGradient { get; set; }

    // 数组和集合分组
    [ExportGroup("数组和集合")]

    [Export] // 字符串数组
    public string[] ItemNames { get; set; } = new string[] { "剑", "盾", "药水" };

    [Export] // 整数数组
    public int[] LevelRequirements { get; set; } = new int[] { 1, 5, 10, 20 };

    [Export] // 纹理数组
    public Texture2D[] ItemIcons { get; set; }

    // 子分组测试
    [ExportSubgroup("高级设置")]

    [Export(PropertyHint.ExpEasing, "attenuation")] // 衰减曲线
    public float DamageFalloff { get; set; } = 1.0f;

    [Export] // 布尔值
    public bool IsBoss { get; set; } = false;

    [Export(PropertyHint.Layers2DPhysics)] // 2D物理层选择
    public uint CollisionLayer { get; set; } = 1;

    // 自定义分类测试 - 游戏平衡
    [ExportCategory("游戏平衡")]
    [ExportGroup("战斗参数")]

    [Export(PropertyHint.Range, "1,1000,1")]
    public int AttackPower { get; set; } = 100;

    [Export(PropertyHint.Range, "0.1,5.0,0.1")]
    public float AttackSpeed { get; set; } = 1.0f;

    [Export(PropertyHint.Range, "0,100,1")]
    public int Defense { get; set; } = 10;

    [ExportSubgroup("特殊能力")]

    [Export]
    public bool CanCriticalHit { get; set; } = true;

    [Export(PropertyHint.Range, "0.0,1.0,0.01")]
    public float CriticalChance { get; set; } = 0.2f;

    [Export(PropertyHint.Range, "1.5,5.0,0.1")]
    public float CriticalMultiplier { get; set; } = 2.0f;

    // 另一个分类 - 视觉效果
    [ExportCategory("视觉效果")]
    [ExportGroup("粒子效果")]

    // 使用PackedScene代替GPUParticles2D直接导出，因为粒子节点不能直接导出
    [Export] // 粒子效果场景引用
    public PackedScene HitEffectScene { get; set; }

    [Export] // 死亡效果场景引用  
    public PackedScene DeathEffectScene { get; set; }

    [ExportSubgroup("动画设置")]

    [Export(PropertyHint.Range, "0.1,2.0,0.01")]
    public float AnimationSpeed { get; set; } = 1.0f;

    [Export]
    public bool LoopAnimation { get; set; } = true;

    // 工具方法 - 用于测试导出属性的功能
    public override void _Ready()
    {
        // 在控制台输出当前属性值，用于调试
        GD.Print($"角色类型: {Type}");
        GD.Print($"生命值: {Health}");
        GD.Print($"攻击力: {AttackPower}");
        GD.Print($"玩家能力: {PlayerAbilities}");

        // 检查是否设置了关键属性
        if (CharacterTexture == null)
        {
            GD.PrintErr("未设置角色纹理！");
        }

        if (string.IsNullOrEmpty(PlayerName))
        {
            GD.PrintErr("玩家名称为空！");
        }
    }

    // 获取角色类型描述
    public string GetCharacterTypeDescription()
    {
        return Type switch
        {
            CharacterType.Warrior => "近战战斗专家，高生命值和防御力",
            CharacterType.Mage => "远程魔法攻击，高伤害但生命值较低",
            CharacterType.Archer => "远程物理攻击，平衡的属性",
            CharacterType.Healer => "支援型角色，可以治疗队友",
            _ => "未知角色类型"
        };
    }

    // 检查玩家是否拥有特定能力
    public bool HasAbility(Abilities ability)
    {
        return (PlayerAbilities & ability) == ability;
    }

    // 实例化粒子效果
    public Node2D CreateHitEffect()
    {
        if (HitEffectScene == null) return null;
        return HitEffectScene.Instantiate<Node2D>();
    }

    // 实例化死亡效果
    public Node2D CreateDeathEffect()
    {
        if (DeathEffectScene == null) return null;
        return DeathEffectScene.Instantiate<Node2D>();
    }
}

// 自定义枚举定义在类外部
public enum CharacterType
{
    Warrior,    // 战士
    Mage,       // 法师
    Archer,     // 弓箭手
    Healer      // 治疗师
}

// 标志位枚举
[Flags]
public enum Abilities
{
    None = 0,
    Jump = 1,      // 跳跃能力
    Dash = 2,      // 冲刺能力
    DoubleJump = 4, // 二段跳
    Glide = 8,     // 滑翔能力
    WallClimb = 16 // 爬墙能力
}
