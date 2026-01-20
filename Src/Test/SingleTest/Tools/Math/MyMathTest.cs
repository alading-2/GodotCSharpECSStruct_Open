using Godot;
using System;

public partial class MyMathTest : Node
{
    private static readonly Log _log = new Log("MyMathTest");

    public override void _Ready()
    {
        Run();
        GetTree().Quit();
    }

    public void Run()
    {
        _log.Info("开始测试 MyMath...");

        TestCheckProbability();

        _log.Info("MyMath 测试完成");
    }

    private void TestCheckProbability()
    {
        // 1. 0% 概率
        bool result0 = MyMath.CheckProbability(0);
        AssertFalse(result0, "0% 概率不应触发");

        // 2. 100% 概率
        bool result100 = MyMath.CheckProbability(100);
        AssertTrue(result100, "100% 概率应触发");

        // 3. 超过 100% 概率
        bool result150 = MyMath.CheckProbability(150);
        AssertTrue(result150, "超过 100% 概率应触发");

        // 4. 负概率
        bool resultNeg = MyMath.CheckProbability(-10);
        AssertFalse(resultNeg, "负概率不应触发");

        // 5. 50% 概率 (统计测试)
        int successCount = 0;
        int totalTests = 10000;
        for (int i = 0; i < totalTests; i++)
        {
            if (MyMath.CheckProbability(50))
            {
                successCount++;
            }
        }
        float rate = (float)successCount / totalTests;
        // 允许 5% 的误差
        bool isReasonable = rate > 0.45f && rate < 0.55f;
        AssertTrue(isReasonable, $"50% 概率统计测试结果: {rate:P2} (应接近 50%)");
    }

    private void AssertTrue(bool condition, string message)
    {
        if (condition)
        {
            _log.Info($"[通过] {message}");
        }
        else
        {
            _log.Error($"[失败] {message}");
        }
    }

    private void AssertFalse(bool condition, string message)
    {
        AssertTrue(!condition, message);
    }
}
