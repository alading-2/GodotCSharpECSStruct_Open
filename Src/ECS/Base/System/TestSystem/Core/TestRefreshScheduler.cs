using System;
using System.Collections.Generic;

/// <summary>
/// TestSystem 统一刷新调度器。
/// <para>
/// 负责收集模块刷新请求，并交由宿主在同一帧末统一冲刷。
/// </para>
/// </summary>
internal sealed class TestRefreshScheduler
{
    /// <summary>当收到首个刷新请求时，通知宿主安排一次统一冲刷。</summary>
    private readonly Action _requestFlush;

    /// <summary>当前帧内待刷新的模块集合。</summary>
    private readonly HashSet<TestModuleBase> _pendingModules = new();

    public TestRefreshScheduler(Action requestFlush)
    {
        _requestFlush = requestFlush;
    }

    /// <summary>
    /// 请求宿主在帧末刷新指定模块。
    /// </summary>
    public void Request(TestModuleBase module)
    {
        if (!_pendingModules.Add(module))
        {
            return;
        }

        _requestFlush(); // 由宿主统一安排一次冲刷
    }

    /// <summary>
    /// 取消某个模块的待刷新请求。
    /// </summary>
    public void Cancel(TestModuleBase module)
    {
        _pendingModules.Remove(module);
    }

    /// <summary>
    /// 取出当前帧累计的全部待刷新模块。
    /// </summary>
    public void DrainPending(List<TestModuleBase> buffer)
    {
        buffer.Clear();
        foreach (var module in _pendingModules)
        {
            buffer.Add(module);
        }

        _pendingModules.Clear();
    }
}
