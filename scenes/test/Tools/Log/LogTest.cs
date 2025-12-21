using Godot;
using BrotatoMy;

namespace BrotatoMy.Test
{
    public partial class LogTest : Node2D
{
    // 1. 标准实例
    private static readonly Log Log = new Log("LogTest");

    // 2. 模拟另一个系统的实例 (例如模拟 战斗系统)
    private static readonly Log CombatLog = new Log("CombatSystem");

    // 3. 模拟 UI 系统的实例
    private static readonly Log UiLog = new Log("UISystem");

    public override void _Ready()
    {
        // 稍微延迟一下以确保输出顺序清晰
        CallDeferred(nameof(RunTests));
    }

    private void RunTests()
    {
        GD.PrintRich("\n[b][color=magenta]=== 开始 Log.cs 功能测试 ===[/color][/b]");

        TestBasicLogging();
        TestInstanceLevelOverride();
        TestContextFiltering();
        TestGlobalFiltering();
        TestCrossClassLogging();

        GD.PrintRich("[b][color=magenta]=== 测试结束 ===[/color][/b]\n");
    }

    private void TestBasicLogging()
    {
        Log.Info("\n[u]--- 1. 测试基础日志等级 (显示所有等级) ---[/u]");

        // 临时将全局等级设为 Trace，以便测试所有输出
        var originalLevel = Log.GlobalLevel;
        Log.GlobalLevel = LogLevel.Trace;

        Log.Trace("这是一条 Trace 日志 (最细粒度)");
        Log.Debug("这是一条 Debug 日志 (调试用)");
        Log.Info("这是一条 Info 日志 (普通信息)");
        Log.Success("这是一条 Success 日志 (操作成功)");
        Log.Warn("这是一条 Warning 日志 (警告)");
        Log.Error("这是一条 Error 日志 (错误)");

        // 恢复原始等级
        Log.GlobalLevel = originalLevel;
    }

    private void TestInstanceLevelOverride()
    {
        Log.Info("\n[u]--- 2. 测试实例等级覆盖 (Instance Level Override) ---[/u]");

        Log.Info($">> 当前全局等级: {Log.GlobalLevel} (通常屏蔽 Trace/Debug)");

        // 创建一个强制开启 Trace 的实例
        Log traceLogger = new Log("TraceSystem", LogLevel.Trace);

        traceLogger.Trace("TraceSystem: 虽然全局等级高，但我强制显示 Trace！");
        traceLogger.Info("TraceSystem: Info 正常显示");
    }

    private void TestContextFiltering()
    {
        Log.Info("\n[u]--- 3. 测试上下文过滤 (Context Filtering) ---[/u]");

        // 默认情况下所有日志都显示
        CombatLog.Info("CombatSystem: 战斗开始 (未过滤)");
        UiLog.Info("UISystem: UI 初始化 (未过滤)");

        // 过滤掉 CombatSystem 的 Info 及以下日志，只显示 Warning/Error
        Log.Info(">> [color=yellow]操作: 设置 CombatSystem 最低等级为 Warning[/color]");
        Log.SetLevel("CombatSystem", LogLevel.Warning);

        CombatLog.Info("CombatSystem: 玩家攻击 (这条不应该显示)");
        CombatLog.Warn("CombatSystem: 武器过热 (这条应该显示)");

        // 恢复
        Log.SetLevel("CombatSystem", LogLevel.Debug);
        Log.Info(">> [color=green]操作: 恢复 CombatSystem 等级为 Debug[/color]");
        CombatLog.Info("CombatSystem: 冷却恢复 (这条应该显示)");
    }

    private void TestGlobalFiltering()
    {
        Log.Info("\n[u]--- 4. 测试全局过滤 (Global Filtering) ---[/u]");

        // 保存当前全局等级
        var previousLevel = Log.GlobalLevel;

        Log.Info($">> 当前全局等级: {Log.GlobalLevel}");
        Log.Info(">> [color=yellow]操作: 将全局等级设置为 Error (屏蔽 Info/Warn/Success)[/color]");

        Log.GlobalLevel = LogLevel.Error;

        Log.Info("这条 Info 日志不应该显示");
        Log.Warn("这条 Warn 日志不应该显示");
        Log.Error("这条 Error 日志应该显示");

        // 恢复
        Log.GlobalLevel = previousLevel;
        Log.Info($">> [color=green]操作: 全局等级已恢复为: {Log.GlobalLevel}[/color]");
    }

    private void TestCrossClassLogging()
    {
        Log.Info("\n[u]--- 5. 测试跨类日志 (Cross Class Logging) ---[/u]");
        var child = new LogTestChild();
        AddChild(child);
        child.DoSomething();
    }
}
}
