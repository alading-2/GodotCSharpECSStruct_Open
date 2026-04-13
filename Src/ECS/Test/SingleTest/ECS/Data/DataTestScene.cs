using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Data 系统测试场景
/// 测试 Data 系统的各项功能
/// </summary>
public partial class DataTestScene : Node
{
    private static readonly Log _log = new Log("DataTestScene");

    // 测试用的 Data 容器
    private Data _testData = null!;

    // 测试结果统计
    private int _totalTests = 0;
    private int _passedTests = 0;
    private int _failedTests = 0;

    // UI 显示
    private RichTextLabel? _outputLabel;

    public override void _Ready()
    {
        _log.Info("初始化 Data 测试场景");

        // 创建测试数据容器
        _testData = new Data();

        // 设置 UI
        SetupUI();

        // 运行所有测试
        RunAllTests();

        // 显示测试结果
        ShowTestResults();
    }

    /// <summary>
    /// 设置 UI 界面
    /// </summary>
    private void SetupUI()
    {
        // 创建主容器
        var vbox = new VBoxContainer
        {
            AnchorLeft = 0.1f,
            AnchorTop = 0.1f,
            AnchorRight = 0.9f,
            AnchorBottom = 0.9f,
            OffsetLeft = 0,
            OffsetTop = 0,
            OffsetRight = 0,
            OffsetBottom = 0
        };
        AddChild(vbox);

        // 标题
        var titleLabel = new Label
        {
            Text = "Data 系统测试场景",
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        titleLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        titleLabel.AddThemeFontSizeOverride("font_size", 32);
        vbox.AddChild(titleLabel);

        // 输出区域
        _outputLabel = new RichTextLabel
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            BbcodeEnabled = true,
            ScrollFollowing = true
        };
        _outputLabel.AddThemeColorOverride("default_color", Colors.White);
        _outputLabel.AddThemeFontSizeOverride("normal_font_size", 16);
        vbox.AddChild(_outputLabel);
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    private void RunAllTests()
    {
        PrintSection("开始测试 Data 系统");

        // 测试基础类型
        Test_BasicTypes();

        // 测试数值范围
        Test_NumericRange();

        // 测试默认值
        Test_DefaultValues();

        // 测试百分比
        Test_Percentage();

        // 测试选项
        Test_Options();

        // 测试计算属性
        Test_ComputedProperties();

        // 测试修改器
        Test_Modifiers();

        // 测试事件监听
        Test_EventListening();

        PrintSection("测试完成");
    }

    /// <summary>
    /// 测试基础类型
    /// </summary>
    private void Test_BasicTypes()
    {
        PrintSection("测试基础类型");

        // 字符串
        PrintStep("设置字符串: key='TestString', value='测试字符串'");
        _testData.Set(DataKey.TestString, "测试字符串");
        PrintStep("获取字符串并验证...");
        AssertEqual("字符串类型", _testData.Get<string>(DataKey.TestString), "测试字符串");

        // 整数
        PrintStep("设置整数: key='TestInt', value=42");
        _testData.Set(DataKey.TestInt, 42);
        PrintStep("获取整数并验证...");
        AssertEqual("整数类型", _testData.Get<int>(DataKey.TestInt), 42);

        // 浮点数
        PrintStep("设置浮点数: key='TestFloat', value=3.14f");
        _testData.Set(DataKey.TestFloat, 3.14f);
        PrintStep("获取浮点数并验证...");
        AssertEqual("浮点数类型", _testData.Get<float>(DataKey.TestFloat), 3.14f);

        // 布尔值
        PrintStep("设置布尔值: key='TestBool', value=true");
        _testData.Set(DataKey.TestBool, true);
        PrintStep("获取布尔值并验证...");
        AssertEqual("布尔类型", _testData.Get<bool>(DataKey.TestBool), true);
    }

    /// <summary>
    /// 测试数值范围
    /// </summary>
    private void Test_NumericRange()
    {
        PrintSection("测试数值范围");

        // 测试最小值约束
        PrintStep("设置小于最小值(10)的数值: key='TestMinValue', value=5f");
        _testData.Set(DataKey.TestMinValue, 5f);
        PrintStep("验证是否自动限制为最小值 10f");
        AssertEqual("最小值约束", _testData.Get<float>(DataKey.TestMinValue), 10f);

        // 测试最大值约束
        PrintStep("设置大于最大值(100)的数值: key='TestMaxValue', value=150f");
        _testData.Set(DataKey.TestMaxValue, 150f);
        PrintStep("验证是否自动限制为最大值 100f");
        AssertEqual("最大值约束", _testData.Get<float>(DataKey.TestMaxValue), 100f);

        // 测试范围约束
        PrintStep("设置超出范围(0-1)的数值: key='TestRange', value=1.5f");
        _testData.Set(DataKey.TestRange, 1.5f);
        PrintStep("验证是否自动限制为最大值 1f");
        AssertEqual("范围约束(超出)", _testData.Get<float>(DataKey.TestRange), 1f);

        PrintStep("设置范围内数值: key='TestRange', value=0.7f");
        _testData.Set(DataKey.TestRange, 0.7f);
        AssertEqual("范围约束(正常)", _testData.Get<float>(DataKey.TestRange), 0.7f);
    }

    /// <summary>
    /// 测试默认值
    /// </summary>
    private void Test_DefaultValues()
    {
        PrintSection("测试默认值");

        // 未设置的数据应返回默认值
        PrintStep("尝试获取不存在的键: 'NotExistKey'");
        var defaultString = _testData.Get<string>("NotExistKey");
        AssertEqual("字符串默认值", defaultString, "");

        var defaultInt = _testData.Get<int>("NotExistKey");
        AssertEqual("整数默认值", defaultInt, 0);

        var defaultFloat = _testData.Get<float>("NotExistKey");
        AssertEqual("浮点数默认值", defaultFloat, 0f);

        // 从 DataMeta 获取默认值
        PrintStep($"检查 '{DataKey.TestString}' 在 DataMeta 中定义的注册默认值");
        var meta = DataRegistry.GetMeta(DataKey.TestString);
        if (meta != null)
        {
            AssertEqual("元数据默认值", meta.GetDefaultValue(), "默认值");
        }
    }

    /// <summary>
    /// 测试百分比
    /// </summary>
    private void Test_Percentage()
    {
        PrintSection("测试百分比");

        PrintStep("设置数值 75f 并检查元数据百分比标记");
        _testData.Set(DataKey.TestPercentage, 75f);
        var meta = DataRegistry.GetMeta(DataKey.TestPercentage);

        if (meta != null)
        {
            AssertTrue("百分比标记", meta.IsPercentage);
            PrintStep($"调用 FormatValue(75f) 检查输出格式");
            var formatted = meta.FormatValue(75f);
            AssertEqual("百分比格式化", formatted, "75.0%");
        }
    }

    /// <summary>
    /// 测试选项
    /// </summary>
    private void Test_Options()
    {
        PrintSection("测试选项");

        var meta = DataRegistry.GetMeta(DataKey.TestOptions);
        if (meta != null)
        {
            PrintStep("检查元数据选项配置");
            AssertTrue("有选项", meta.HasOptions);

            // 设置有效选项
            PrintStep("设置索引为 1 的有效选项");
            _testData.Set(DataKey.TestOptions, 1);
            AssertEqual("有效选项", _testData.Get<int>(DataKey.TestOptions), 1);

            // 获取选项名称
            PrintStep("通过元数据获取选项显示名称");
            var optionName = meta.GetOptionName(1);
            AssertEqual("选项名称", optionName, "选项2");
        }
    }

    /// <summary>
    /// 测试计算属性
    /// </summary>
    private void Test_ComputedProperties()
    {
        PrintSection("测试计算属性");

        // 设置基础值
        PrintStep("设置计算属性的基础依赖值: A=10, B=5");
        _testData.Set(DataKey.TestBaseA, 10f);
        _testData.Set(DataKey.TestBaseB, 5f);

        // 测试加法计算
        PrintStep("获取计算属性: 'TestComputedAdd' (A + B)");
        var addResult = _testData.Get<float>(DataKey.TestComputedAdd);
        AssertEqual("加法计算 (10 + 5)", addResult, 15f);

        // 测试乘法计算
        PrintStep("获取计算属性: 'TestComputedMultiply' (A * B)");
        var multiplyResult = _testData.Get<float>(DataKey.TestComputedMultiply);
        AssertEqual("乘法计算 (10 * 5)", multiplyResult, 50f);

        // 测试复杂计算
        PrintStep("获取计算属性: 'TestComputedComplex' ((A + B) * 2)");
        var complexResult = _testData.Get<float>(DataKey.TestComputedComplex);
        AssertEqual("复杂计算 ((10 + 5) * 2)", complexResult, 30f);

        // 测试依赖更新
        PrintStep("修改依赖项 A=20, 触发级联脏标记");
        _testData.Set(DataKey.TestBaseA, 20f);
        PrintStep("重新获取计算属性并验证更新后的值 (20 + 5)");
        var newAddResult = _testData.Get<float>(DataKey.TestComputedAdd);
        AssertEqual("依赖更新后的计算 (20 + 5)", newAddResult, 25f);
    }

    /// <summary>
    /// 测试修改器系统
    /// </summary>
    private void Test_Modifiers()
    {
        PrintSection("测试修改器系统");

        // 设置基础值
        PrintStep("设置基础值: key='TestModifierBase', value=100f");
        _testData.Set(DataKey.TestModifierBase, 100f);
        AssertEqual("基础值", _testData.Get<float>(DataKey.TestModifierBase), 100f);

        // 添加加法修改器
        PrintStep("添加Additive(加法)修改器: +20f");
        var addModifier = new DataModifier(ModifierType.Additive, 20f, id: "TestAdd");
        _testData.AddModifier(DataKey.TestModifierBase, addModifier);
        AssertEqual("加法修改器 (100 + 20)", _testData.Get<float>(DataKey.TestModifierBase), 120f);

        // 添加乘法修改器
        PrintStep("添加Multiplicative(乘法)修改器: *1.5f");
        var multiModifier = new DataModifier(ModifierType.Multiplicative, 1.5f, id: "TestMulti");
        _testData.AddModifier(DataKey.TestModifierBase, multiModifier);
        PrintStep("验证计算公式: (基础100 + 加法20) * 乘法1.5 = 180");
        AssertEqual("乘法修改器 ((100 + 20) * 1.5)", _testData.Get<float>(DataKey.TestModifierBase), 180f);

        // 移除修改器
        PrintStep("移除加法修改器 'TestAdd'");
        _testData.RemoveModifier(DataKey.TestModifierBase, "TestAdd");
        PrintStep("验证计算公式: 基础100 * 乘法1.5 = 150");
        AssertEqual("移除加法修改器 (100 * 1.5)", _testData.Get<float>(DataKey.TestModifierBase), 150f);

        // 清除所有修改器
        PrintStep("清除所有修改器并验证回滚到基础值");
        _testData.ClearModifiers(DataKey.TestModifierBase);
        AssertEqual("清除所有修改器", _testData.Get<float>(DataKey.TestModifierBase), 100f);
    }

    /// <summary>
    /// 测试事件监听
    /// </summary>
    private void Test_EventListening()
    {
        PrintSection("测试事件监听");

        PrintStep("记录当前值并设置新值，验证变更结果");
        var oldValue = _testData.Get<int>(DataKey.TestInt);
        _testData.Set(DataKey.TestInt, 99);
        var newValue = _testData.Get<int>(DataKey.TestInt);

        AssertTrue("值已变更", oldValue != newValue);
        AssertEqual("新值正确", newValue, 99);
    }

    /// <summary>
    /// 显示测试结果
    /// </summary>
    private void ShowTestResults()
    {
        PrintSection("测试结果统计");

        string summary = $"总测试数: {_totalTests}, 通过: {_passedTests}, 失败: {_failedTests}";
        PrintLine($"[color=white]{summary}[/color]");
        _log.Info(summary);

        if (_failedTests == 0)
        {
            PrintLine("[color=lime][b]✓ 所有测试通过![/b][/color]");
            _log.Success("所有测试通过!");
        }
        else
        {
            PrintLine($"[color=orange][b]⚠ 有 {_failedTests} 个测试失败[/b][/color]");
            _log.Warn($"有 {_failedTests} 个测试失败");
        }
    }

    // ==================== 辅助方法 ====================

    /// <summary>
    /// 打印操作步骤
    /// </summary>
    private void PrintStep(string text)
    {
        PrintLine($"  [color=gray]>> {text}[/color]");
        _log.Debug(text);
    }

    /// <summary>
    /// 断言相等
    /// </summary>
    private void AssertEqual<T>(string testName, T actual, T expected)
    {
        _totalTests++;
        if (EqualityComparer<T>.Default.Equals(actual, expected))
        {
            _passedTests++;
            PrintLine($"[color=green]✓[/color] {testName}: [color=lightgreen]{actual}[/color]");
            _log.Info($"[PASS] {testName}: {actual}");
        }
        else
        {
            _failedTests++;
            PrintLine($"[color=red]✗[/color] {testName}: 期望 [color=yellow]{expected}[/color], 实际 [color=orange]{actual}[/color]");
            _log.Error($"[FAIL] {testName} - 期望: {expected}, 实际: {actual}");
        }
    }

    /// <summary>
    /// 断言真
    /// </summary>
    private void AssertTrue(string testName, bool condition)
    {
        _totalTests++;
        if (condition)
        {
            _passedTests++;
            PrintLine($"[color=green]✓[/color] {testName}");
            _log.Info($"[PASS] {testName}");
        }
        else
        {
            _failedTests++;
            PrintLine($"[color=red]✗[/color] {testName}: 期望 true, 实际 false");
            _log.Error($"[FAIL] {testName} - 期望 true, 但为 false");
        }
    }

    /// <summary>
    /// 打印章节标题
    /// </summary>
    private void PrintSection(string title)
    {
        PrintLine("");
        PrintLine($"[color=cyan][b]=== {title} ===[/b][/color]");
        _log.Info(title);
    }

    /// <summary>
    /// 打印一行
    /// </summary>
    private void PrintLine(string text)
    {
        if (_outputLabel != null)
        {
            _outputLabel.AppendText(text + "\n");
        }
    }
}
