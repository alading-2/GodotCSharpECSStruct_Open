/// <summary>
/// 测试用数据键定义
/// </summary>
public static partial class DataKey
{
    // === 基础类型测试 ===
    /// <summary>测试用的字符串数据</summary>
    public const string TestString = "Test_String";

    /// <summary>测试用的整数数据</summary>
    public const string TestInt = "Test_Int";

    /// <summary>测试用的浮点数数据</summary>
    public const string TestFloat = "Test_Float";

    /// <summary>测试用的布尔数据</summary>
    public const string TestBool = "Test_Bool";

    // === 数值范围测试 ===
    /// <summary>测试最小值约束</summary>
    public const string TestMinValue = "Test_MinValue";

    /// <summary>测试最大值约束</summary>
    public const string TestMaxValue = "Test_MaxValue";

    /// <summary>测试范围约束</summary>
    public const string TestRange = "Test_Range";

    // === 百分比测试 ===
    /// <summary>测试百分比显示</summary>
    public const string TestPercentage = "Test_Percentage";

    // === 选项测试 ===
    /// <summary>测试选项约束</summary>
    public const string TestOptions = "Test_Options";

    // === 计算属性测试 ===
    /// <summary>测试基础数值A</summary>
    public const string TestBaseA = "Test_BaseA";

    /// <summary>测试基础数值B</summary>
    public const string TestBaseB = "Test_BaseB";

    /// <summary>测试加法计算 (A + B)</summary>
    public const string TestComputedAdd = "Test_Computed_Add";

    /// <summary>测试乘法计算 (A * B)</summary>
    public const string TestComputedMultiply = "Test_Computed_Multiply";

    /// <summary>测试复杂计算 (A + B) * 2</summary>
    public const string TestComputedComplex = "Test_Computed_Complex";

    // === 修改器测试 ===
    /// <summary>测试修改器基础值</summary>
    public const string TestModifierBase = "Test_Modifier_Base";
}
