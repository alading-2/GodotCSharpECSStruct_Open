using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// 日志等级枚举，定义了日志的严重程度。
/// 数值越大，优先级越高。
/// </summary>
public enum LogLevel
{
    /// <summary> 最细粒度的追踪信息，通常用于记录方法进入/退出等 </summary>
    Trace = 0,
    /// <summary> 调试信息，用于记录关键变量的状态或逻辑分支 </summary>
    Debug = 1,
    /// <summary> 普通运行信息，如初始化成功、资源加载完成等 </summary>
    Info = 2,
    /// <summary> 成功提示，用于突出显示关键流程顺利完成 </summary>
    Success = 3,
    /// <summary> 警告信息，表示程序遇到了非预期情况但仍可继续运行 </summary>
    Warning = 4,
    /// <summary> 错误信息，表示程序遇到了严重问题或异常 </summary>
    Error = 5,
    /// <summary> 特殊等级，用于彻底关闭所有日志 </summary>
    None = 99
}

/// <summary>
/// 高级 C# 日志工具类。
/// 支持实例初始化（绑定类名）、全局分级过滤、按类名过滤、BBCode 颜色显示。
/// </summary>
public class Log
{
    // ================= 静态配置 (全局控制) =================

    /// <summary>
    /// 全局日志等级阈值。只有等级大于或等于此值的日志才会被打印。
    /// 默认为 Debug。在发布版本中建议设置为 Info 或更高。
    /// </summary>
    public static LogLevel GlobalLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// 是否在日志开头显示系统当前时间戳。
    /// 格式通常为 [HH:mm:ss]。
    /// </summary>
    public static bool ShowTimestamp { get; set; } = true;

    /// <summary>
    /// 是否显示上下文信息（类名）。
    /// 格式通常为 [ClassName]。
    /// </summary>
    public static bool ShowContext { get; set; } = true;

    /// <summary>
    /// 针对特定上下文（类名）的日志等级过滤器。
    /// Key: 类名, Value: 该类允许打印的最低等级。
    /// 用于在调试时临时开启或屏蔽特定模块的日志。
    /// </summary>
    private static readonly Dictionary<string, LogLevel> _contextFilters = new();

    // BBCode 颜色配置，用于 Godot 编辑器的 Output 面板着色
    private const string ColorTrace = "gray";    // 灰色
    private const string ColorDebug = "cyan";    // 青色
    private const string ColorInfo = "white";    // 白色
    private const string ColorSuccess = "green";  // 绿色
    private const string ColorWarn = "yellow";   // 黄色
    private const string ColorError = "red";     // 红色

    /// <summary>
    /// 全局设置特定上下文（类名）的日志等级。
    /// 这将覆盖该实例自带的 _localLevel 设置。
    /// </summary>
    /// <param name="contextName">上下文名称（通常是类名）</param>
    /// <param name="level">该上下文允许打印的最低日志等级</param>
    public static void SetLevel(string contextName, LogLevel level)
    {
        _contextFilters[contextName] = level;
    }

    // ================= 实例部分 (推荐用法) =================

    /// <summary> 当前日志实例绑定的上下文名称（类名） </summary>
    private readonly string _contextName;

    /// <summary> 当前实例默认的日志等级 </summary>
    private readonly LogLevel _localLevel;

    /// <summary>
    /// 创建一个新的日志实例。
    /// 推荐用法：在类中声明 static readonly Log Log = new Log("ClassName");
    /// </summary>
    /// <param name="contextName">上下文名称，将显示在日志中并用于过滤</param>
    /// <param name="localLevel">该实例特定的日志等级。如果不设置，将默认跟随 GlobalLevel</param>
    public Log(string contextName, LogLevel localLevel = LogLevel.None)
    {
        _contextName = contextName;
        _localLevel = localLevel;
    }

    /// <summary>
    /// 追踪日志：最细粒度的运行时流转信息。
    /// 注意：此方法标记了 [Conditional("DEBUG")]，在非 DEBUG 编译模式下会被编译器完全忽略，零性能损耗。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    [Conditional("DEBUG")]
    public void Trace(object message)
    {
        LogInternal(LogLevel.Trace, message, "TRACE", ColorTrace);
    }

    /// <summary>
    /// 调试日志：关键变量或逻辑点调试信息。
    /// 注意：此方法标记了 [Conditional("DEBUG")]，在非 DEBUG 编译模式下会被编译器完全忽略，零性能损耗。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    [Conditional("DEBUG")]
    public void Debug(object message)
    {
        LogInternal(LogLevel.Debug, message, "DEBUG", ColorDebug);
    }

    /// <summary>
    /// 信息日志：通用的运行状态或关键步骤说明。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    public void Info(object message)
    {
        LogInternal(LogLevel.Info, message, "INFO", ColorInfo);
    }

    /// <summary>
    /// 成功日志：突出显示某个关键操作已成功完成。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    public void Success(object message)
    {
        LogInternal(LogLevel.Success, message, "SUCCESS", ColorSuccess);
    }

    /// <summary>
    /// 警告日志：表示程序遇到了非预期情况但仍可继续运行。
    /// 除了在 Output 打印，还会调用 GD.PushWarning 以便在 Godot 的 Debugger 面板显示。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    public void Warn(object message)
    {
        if (ShouldLog(LogLevel.Warning))
        {
            // 推送到 Godot 编辑器的错误/警告列表
            GD.PushWarning(FormatRawMessage(message, "WARNING"));
            // 打印带颜色的 Rich Text
            LogInternal(LogLevel.Warning, message, "WARNING", ColorWarn, checkFilter: false);
        }
    }

    /// <summary>
    /// 错误日志：表示程序遇到了严重问题或异常。
    /// 除了在 Output 打印，还会调用 GD.PushError 以便在 Godot 的 Debugger 面板中高亮红色显示。
    /// </summary>
    /// <param name="message">要打印的内容</param>
    public void Error(object message)
    {
        if (ShouldLog(LogLevel.Error))
        {
            // 推送到 Godot 编辑器的错误列表
            GD.PushError(FormatRawMessage(message, "ERROR"));
            // 打印带颜色的 Rich Text
            LogInternal(LogLevel.Error, message, "ERROR", ColorError, checkFilter: false);
        }
    }

    // ================= 内部逻辑 =================

    /// <summary>
    /// 判断当前日志等级是否允许打印。
    /// </summary>
    /// <param name="level">当前要打印的日志等级</param>
    /// <returns>True 表示允许打印</returns>
    private bool ShouldLog(LogLevel level)
    {
        // 1. 检查特定上下文过滤器 (最高优先级，由 Log.SetLevel 设置)
        if (_contextFilters.TryGetValue(_contextName, out LogLevel filterLevel))
        {
            return level >= filterLevel;
        }

        // 2. 如果实例设置了特定等级 (不是 None)，则以实例等级为准
        if (_localLevel != LogLevel.None)
        {
            return level >= _localLevel;
        }

        // 3. 否则跟随全局等级
        return level >= GlobalLevel;
    }

    /// <summary>
    /// 核心打印方法，负责格式化字符串并调用 Godot 的 PrintRich。
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="message">消息内容</param>
    /// <param name="tag">标签字符串 (如 DEBUG, INFO)</param>
    /// <param name="color">BBCode 颜色值</param>
    /// <param name="checkFilter">是否需要进行过滤检查</param>
    private void LogInternal(LogLevel level, object message, string tag, string color, bool checkFilter = true)
    {
        if (checkFilter && !ShouldLog(level)) return;

        // 构建时间戳字符串
        string timestampStr = ShowTimestamp ? $"[{Time.GetTimeStringFromSystem()}]" : "";

        // 构建上下文信息字符串 [类名]
        string contextInfoStr = ShowContext ? $"[{_contextName}]" : "";

        // 使用 GD.PrintRich 输出
        GD.PrintRich($"[color={color}]{timestampStr}[{tag}]{contextInfoStr} {message}[/color]");
    }

    /// <summary>
    /// 格式化原始消息字符串，不包含 BBCode 标签。
    /// </summary>
    private string FormatRawMessage(object message, string tag)
    {
        return $"[{tag}][{_contextName}] {message}";
    }
}

