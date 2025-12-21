using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;

/// <summary>
/// 健壮的动态数据 (Data) 容器类，灵感来源于 ECS (实体组件系统) 的实体数据管理。
/// 支持强类型访问、算术运算（加法、乘法）以及数据变更事件监听。
/// 适用于游戏对象属性、动态状态、配置项或任何需要灵活数据管理的场景。
/// </summary>
public class Data
{
    /// <summary>
    /// 内部存储数据的字典
    /// </summary>
    private readonly Dictionary<string, object> _data = new();

    /// <summary>
    /// 当任何数据发生变化时触发的全局事件。
    /// 参数依次为：键名 (Key), 旧值 (OldValue), 新值 (NewValue)
    /// </summary>
    public event Action<string, object?, object?>? OnValueChanged;

    /// <summary>
    /// 特定键名的监听器字典，用于支持针对单个数据项的订阅
    /// </summary>
    private readonly Dictionary<string, Action<object?, object?>> _listeners = new();

    /// <summary>
    /// 设置数据值。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="key">键名。</param>
    /// <param name="value">要设置的新值。</param>
    /// <returns>如果值发生了实际变化（或新增）则返回 true，如果新旧值相等则返回 false。</returns>
    public bool Set<T>(string key, T value)
    {
        object? oldValue = null;
        if (_data.TryGetValue(key, out var existing))
        {
            oldValue = existing;
            // 检查相等性以避免触发不必要的变更事件
            if (Equals(existing, value))
            {
                return false;
            }
        }

        _data[key] = value!;
        NotifyChanged(key, oldValue, value);
        return true;
    }

    /// <summary>
    /// 尝试获取数据值。
    /// </summary>
    /// <typeparam name="T">期望获取的类型。</typeparam>
    /// <param name="key">键名。</param>
    /// <param name="value">如果找到且类型匹配，则返回该值；否则返回默认值。</param>
    /// <returns>如果找到且类型匹配（或可转换）则返回 true。</returns>
    public bool TryGetValue<T>(string key, out T value)
    {
        if (_data.TryGetValue(key, out var rawValue))
        {
            if (rawValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            try
            {
                value = (T)Convert.ChangeType(rawValue, typeof(T));
                return true;
            }
            catch
            {
                // 转换失败
            }
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 获取数据值。
    /// </summary>
    /// <typeparam name="T">期望获取的类型。</typeparam>
    /// <param name="key">键名。</param>
    /// <param name="defaultValue">如果不存在或类型不匹配时返回的默认值。</param>
    /// <returns>返回数据值或默认值。</returns>
    public T Get<T>(string key, T defaultValue = default!)
    {
        if (_data.TryGetValue(key, out var value))
        {
            // 如果类型直接匹配，直接返回
            if (value is T typedValue)
            {
                return typedValue;
            }

            // 尝试进行类型转换（例如从 double 转换为 float）
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                // 转换失败则忽略错误，返回默认值
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 检查是否存在指定的键名。
    /// </summary>
    /// <param name="key">键名。</param>
    /// <returns>存在返回 true。</returns>
    public bool Has(string key)
    {
        return _data.ContainsKey(key);
    }

    /// <summary>
    /// 移除指定的数据项。
    /// </summary>
    /// <param name="key">键名。</param>
    /// <returns>如果成功移除则返回 true，如果不存在则返回 false。</returns>
    public bool Remove(string key)
    {
        if (_data.TryGetValue(key, out var oldValue))
        {
            _data.Remove(key);
            NotifyChanged(key, oldValue, null);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 对现有数值执行加法操作。
    /// 如果尚不存在，则将其初始值视为 0 并加上增量。
    /// </summary>
    /// <typeparam name="T">数值类型 (需实现 INumber 接口)。</typeparam>
    /// <param name="key">键名。</param>
    /// <param name="delta">要增加的数值（增量）。</param>
    public void Add<T>(string key, T delta) where T : INumber<T>
    {
        var current = Get<T>(key, T.Zero);
        Set(key, current + delta);
    }

    /// <summary>
    /// 对现有数值执行乘法操作。
    /// 如果尚不存在，则将其初始值视为 0。
    /// </summary>
    /// <typeparam name="T">数值类型 (需实现 INumber 接口)。</typeparam>
    /// <param name="key">键名。</param>
    /// <param name="factor">乘数因子。</param>
    public void Multiply<T>(string key, T factor) where T : INumber<T>
    {
        var current = Get<T>(key, T.Zero);
        Set(key, current * factor);
    }

    /// <summary>
    /// 批量设置多个数据项。
    /// </summary>
    /// <param name="properties">包含多个键值对的字典。</param>
    public void SetMultiple(Dictionary<string, object> properties)
    {
        foreach (var kvp in properties)
        {
            Set(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// 注册特定数据项的变更监听器。
    /// </summary>
    /// <param name="key">要监听的键名。</param>
    /// <param name="callback">回调函数，参数为 (旧值, 新值)。</param>
    public void On(string key, Action<object?, object?> callback)
    {
        if (!_listeners.ContainsKey(key))
        {
            _listeners[key] = callback;
        }
        else
        {
            _listeners[key] += callback;
        }
    }

    /// <summary>
    /// 注销特定数据项的变更监听器。
    /// </summary>
    /// <param name="key">键名。</param>
    /// <param name="callback">要移除的回调函数。</param>
    public void Off(string key, Action<object?, object?> callback)
    {
        if (_listeners.ContainsKey(key))
        {
            _listeners[key] -= callback;
            if (_listeners[key] == null)
            {
                _listeners.Remove(key);
            }
        }
    }

    /// <summary>
    /// 清空所有数据，并触发变更事件（新值为 null）。
    /// </summary>
    public void Clear()
    {
        var keys = new List<string>(_data.Keys);
        foreach (var key in keys)
        {
            Remove(key);
        }
    }

    /// <summary>
    /// 内部方法：触发变更通知。
    /// </summary>
    /// <param name="key">键名。</param>
    /// <param name="oldValue">变化前的值。</param>
    /// <param name="newValue">变化后的新值。</param>
    private void NotifyChanged(string key, object? oldValue, object? newValue)
    {
        // 触发全局监听器
        OnValueChanged?.Invoke(key, oldValue, newValue);

        // 触发特定键名监听器
        if (_listeners.TryGetValue(key, out var listener))
        {
            listener.Invoke(oldValue, newValue);
        }
    }

    /// <summary>
    /// 获取当前所有数据的副本。
    /// </summary>
    /// <returns>包含所有数据的字典副本。</returns>
    public Dictionary<string, object> GetAll()
    {
        return new Dictionary<string, object>(_data);
    }
}
