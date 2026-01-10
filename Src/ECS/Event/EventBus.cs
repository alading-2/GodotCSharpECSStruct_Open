using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 通用游戏事件总线
/// 支持泛型数据传递、优先级排序、一次性订阅
/// </summary>
public class EventBus
{
    // 使用 Log 系统
    private static readonly Log _log = new Log("EventBus");

    /// <summary>
    /// 事件订阅封装
    /// </summary>
    private class Subscription
    {
        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 事件处理器委托
        /// </summary>
        public Delegate Handler { get; set; }

        /// <summary>
        /// 优先级，数值越大越优先
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否一次性订阅
        /// </summary>
        public bool Once { get; set; }

        /// <summary>
        /// 参数类型缓存（用于类型检查和调试）
        /// </summary>
        public Type? ParameterType { get; set; }

        // 标记是否已被移除（处理迭代期间的安全删除）
        public bool IsPendingRemoval { get; set; } = false;

        public Subscription(string eventName, Delegate handler, int priority, bool once)
        {
            EventName = eventName;
            Handler = handler;
            Priority = priority;
            Once = once;
        }
    }

    // 事件存储字典：事件名 -> 订阅列表
    private readonly Dictionary<string, List<Subscription>> _subscriptions = new();

    // 防止在 Emit 过程中修改列表导致的集合修改异常
    // 如果正在触发事件，所有的移除操作将延迟进行
    private int _emittingCount = 0;
    private readonly List<Subscription> _pendingRemovals = new();

    // ✅ 重入保护：追踪正在执行的事件类型，防止同类型事件递归触发
    private readonly HashSet<string> _emittingEvents = new();

    // ==================== 订阅 (Subscribe) ====================

    /// <summary>
    /// 订阅带参数的事件
    /// </summary>
    public void On<T>(string eventName, Action<T> handler, int priority = 0)
    {
        Subscribe(eventName, handler, priority, false);
    }

    /// <summary>
    /// 订阅无参数的事件
    /// </summary>
    public void On(string eventName, Action handler, int priority = 0)
    {
        Subscribe(eventName, handler, priority, false);
    }

    /// <summary>
    /// 订阅一次性事件（带参数）
    /// </summary>
    public void Once<T>(string eventName, Action<T> handler, int priority = 0)
    {
        Subscribe(eventName, handler, priority, true);
    }

    /// <summary>
    /// 订阅一次性事件（无参数）
    /// </summary>
    public void Once(string eventName, Action handler, int priority = 0)
    {
        Subscribe(eventName, handler, priority, true);
    }

    private void Subscribe(string eventName, Delegate handler, int priority, bool once)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            _log.Error("尝试订阅空事件名");
            return;
        }

        if (handler == null)
        {
            _log.Error($"尝试订阅空处理器: {eventName}");
            return;
        }

        if (!_subscriptions.TryGetValue(eventName, out var list))
        {
            list = new List<Subscription>();
            _subscriptions[eventName] = list;
        }

        // 提取参数类型（用于类型检查和调试）
        var paramType = handler.Method.GetParameters().FirstOrDefault()?.ParameterType;

        var sub = new Subscription(eventName, handler, priority, once)
        {
            ParameterType = paramType
        };

        // 插入并保持优先级排序 (降序：优先级高的在前)
        // 简单实现：直接添加后排序，或者插入到正确位置
        // 考虑到读多写少，直接 Add 然后 Sort 是可以接受的，但为了极致性能，做插入排序更好
        // 这里为了代码清晰，且订阅操作频率通常远低于触发，使用 Sort
        list.Add(sub);
        list.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // _log.Debug($"订阅事件: {eventName} (Pri:{priority}, Once:{once})");
    }

    // ==================== 取消订阅 (Unsubscribe) ====================

    /// <summary>
    /// 取消订阅带参数的事件
    /// </summary>
    public void Off<T>(string eventName, Action<T> handler)
    {
        Unsubscribe(eventName, handler);
    }

    /// <summary>
    /// 取消订阅无参数的事件
    /// </summary>
    public void Off(string eventName, Action handler)
    {
        Unsubscribe(eventName, handler);
    }

    private void Unsubscribe(string eventName, Delegate handler)
    {
        if (!_subscriptions.TryGetValue(eventName, out var list)) return;

        // 查找匹配的订阅
        var target = list.FirstOrDefault(s => s.Handler == handler);
        if (target != null)
        {
            RemoveSubscription(target, list);
        }
    }

    private void RemoveSubscription(Subscription sub, List<Subscription> list)
    {
        // 如果当前正在分发事件，不能直接修改列表
        if (_emittingCount > 0)
        {
            sub.IsPendingRemoval = true;
            _pendingRemovals.Add(sub);
        }
        else
        {
            list.Remove(sub);
            // 如果列表为空，可以选择清理 Key，但为了缓存复用通常保留 Key
            if (list.Count == 0)
            {
                _subscriptions.Remove(sub.EventName);
            }
        }
    }

    // ==================== 触发 (Emit) ====================

    /// <summary>
    /// 触发带参数的事件
    /// </summary>
    public void Emit<T>(string eventName, T data)
    {
        Trigger(eventName, data);
    }

    /// <summary>
    /// 触发无参数的事件
    /// </summary>
    public void Emit(string eventName)
    {
        Trigger<object?>(eventName, null);
    }

    private void Trigger<T>(string eventName, T data)
    {
        // ✅ 重入检测：阻止同类型事件递归触发（防止死循环）
        if (_emittingEvents.Contains(eventName))
        {
            _log.Warn($"检测到事件重入，已阻止: [{eventName}]");
            return;
        }

        if (!_subscriptions.TryGetValue(eventName, out var list) || list.Count == 0)
        {
            return;
        }

        _emittingEvents.Add(eventName);  // 标记事件开始执行
        _emittingCount++;
        try
        {
            // 使用 for 循环避免 foreach 的枚举器开销，同时配合 pendingRemoval 检查
            // 注意：因为列表已经按优先级排序，所以直接遍历即可
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var sub = list[i];

                // 跳过已被标记移除的订阅
                if (sub.IsPendingRemoval) continue;

                try
                {
                    // 参数匹配检查与调用
                    if (sub.Handler is Action action)
                    {
                        // 无参处理器：总是可以被调用，忽略数据
                        action.Invoke();
                    }
                    else
                    {
                        // 带参处理器：需匹配参数类型
                        // 优化：尝试直接强转为 Action<T> 以避免 DynamicInvoke 的反射和装箱开销
                        // 这是最常见的路径（Emit<T> 和 On<T> 类型一致）
                        if (sub.Handler is Action<T> typedHandler)
                        {
                            typedHandler(data);
                        }
                        else
                        {
                            // 类型不匹配：记录警告并跳过（不再使用 DynamicInvoke 反射调用）
                            _log.Warn($"事件 [{eventName}] 类型不匹配: " +
                                     $"订阅者需要 {sub.Handler.GetType()}, " +
                                     $"但事件数据是 Action<{typeof(T)}>");
                        }
                    }

                    // 处理 Once 逻辑
                    if (sub.Once)
                    {
                        RemoveSubscription(sub, list);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"事件处理异常 [{eventName}]: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        finally
        {
            _emittingCount--;
            _emittingEvents.Remove(eventName);  // 标记事件执行结束
            // 如果所有嵌套的 Emit 都结束了，处理延迟移除
            if (_emittingCount <= 0)
            {
                ProcessPendingRemovals();
            }
        }
    }

    /// <summary>
    /// 处理延迟移除的订阅
    /// </summary>
    private void ProcessPendingRemovals()
    {
        if (_pendingRemovals.Count == 0) return;

        foreach (var sub in _pendingRemovals)
        {
            if (_subscriptions.TryGetValue(sub.EventName, out var list))
            {
                list.Remove(sub);
                if (list.Count == 0)
                {
                    _subscriptions.Remove(sub.EventName);
                }
            }
        }
        _pendingRemovals.Clear();
    }

    // ==================== 管理 ====================

    /// <summary>
    /// 清除指定事件的所有订阅
    /// </summary>
    public void ClearEvent(string eventName)
    {
        if (_subscriptions.ContainsKey(eventName))
        {
            _subscriptions.Remove(eventName);
        }
    }

    /// <summary>
    /// 清空所有订阅
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
        _pendingRemovals.Clear();
        _emittingCount = 0;
        _log.Debug("EventBus 已清空");
    }
}
