
using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 测试数据注册器
/// </summary>
public partial class TestDataRegister : Node
{
    private static readonly Log _log = new Log("TestDataRegister");

    [ModuleInitializer]
    public static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = "TestDataRegister",
            Path = "res://Src/Test/ECS/Data/TestDataRegister.cs",
            Priority = AutoLoad.Priority.Game,
            ParentPath = "AutoLoad/DataRegistry"
        });
    }

    public override void _Ready()
    {
        _log.Info("注册测试数据...");

        // === 基础类型测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestString,
            DisplayName = "测试字符串",
            Description = "用于测试字符串类型",
            Category = TestCategory.Basic,
            Type = typeof(string),
            DefaultValue = "默认值"
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestInt,
            DisplayName = "测试整数",
            Description = "用于测试整数类型",
            Category = TestCategory.Basic,
            Type = typeof(int),
            DefaultValue = 0
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestFloat,
            DisplayName = "测试浮点数",
            Description = "用于测试浮点数类型",
            Category = TestCategory.Basic,
            Type = typeof(float),
            DefaultValue = 0f
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestBool,
            DisplayName = "测试布尔值",
            Description = "用于测试布尔类型",
            Category = TestCategory.Basic,
            Type = typeof(bool),
            DefaultValue = false
        });

        // === 数值范围测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestMinValue,
            DisplayName = "测试最小值",
            Description = "测试最小值约束 (>= 10)",
            Category = TestCategory.Numeric,
            Type = typeof(float),
            DefaultValue = 10f,
            MinValue = 10f
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestMaxValue,
            DisplayName = "测试最大值",
            Description = "测试最大值约束 (<= 100)",
            Category = TestCategory.Numeric,
            Type = typeof(float),
            DefaultValue = 50f,
            MaxValue = 100f
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestRange,
            DisplayName = "测试范围",
            Description = "测试范围约束 (0-1)",
            Category = TestCategory.Numeric,
            Type = typeof(float),
            DefaultValue = 0.5f,
            MinValue = 0f,
            MaxValue = 1f
        });

        // === 百分比测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestPercentage,
            DisplayName = "测试百分比",
            Description = "测试百分比显示",
            Category = TestCategory.Numeric,
            Type = typeof(float),
            DefaultValue = 50f,
            MinValue = 0f,
            MaxValue = 100f,
            IsPercentage = true
        });

        // === 选项测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestOptions,
            DisplayName = "测试选项",
            Description = "测试固定选项约束",
            Category = TestCategory.Options,
            Type = typeof(int),
            DefaultValue = 0,
            Options = new System.Collections.Generic.List<string> { "选项1", "选项2", "选项3" }
        });

        // === 计算属性测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestBaseA,
            DisplayName = "基础数值A",
            Description = "计算属性的依赖项A",
            Category = TestCategory.Computed,
            Type = typeof(float),
            DefaultValue = 10f
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestBaseB,
            DisplayName = "基础数值B",
            Description = "计算属性的依赖项B",
            Category = TestCategory.Computed,
            Type = typeof(float),
            DefaultValue = 5f
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestComputedAdd,
            DisplayName = "计算属性(加法)",
            Description = "测试加法计算: A + B",
            Category = TestCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = new[] { DataKey.TestBaseA, DataKey.TestBaseB },
            Compute = (data) =>
            {
                float a = data.Get<float>(DataKey.TestBaseA);
                float b = data.Get<float>(DataKey.TestBaseB);
                return a + b;
            }
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestComputedMultiply,
            DisplayName = "计算属性(乘法)",
            Description = "测试乘法计算: A * B",
            Category = TestCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = new[] { DataKey.TestBaseA, DataKey.TestBaseB },
            Compute = (data) =>
            {
                float a = data.Get<float>(DataKey.TestBaseA);
                float b = data.Get<float>(DataKey.TestBaseB);
                return a * b;
            }
        });

        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestComputedComplex,
            DisplayName = "计算属性(复杂)",
            Description = "测试复杂计算: (A + B) * 2",
            Category = TestCategory.Computed,
            Type = typeof(float),
            DefaultValue = 0f,
            SupportModifiers = false,
            Dependencies = new[] { DataKey.TestBaseA, DataKey.TestBaseB },
            Compute = (data) =>
            {
                float a = data.Get<float>(DataKey.TestBaseA);
                float b = data.Get<float>(DataKey.TestBaseB);
                return (a + b) * 2;
            }
        });

        // === 修改器测试 ===
        DataRegistry.Register(new DataMeta
        {
            Key = DataKey.TestModifierBase,
            DisplayName = "修改器基础值",
            Description = "用于测试修改器系统",
            Category = TestCategory.Numeric,
            Type = typeof(float),
            DefaultValue = 100f,
            SupportModifiers = true
        });

        _log.Info("测试数据注册完成");
    }
}
