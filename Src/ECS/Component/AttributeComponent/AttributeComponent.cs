using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 属性组件 - 游戏实体的核心数据管理中心。
/// 
/// 【设计目标】
/// 1. 集中管理：统一处理实体的基础值（Base Value）和所有修改器（Modifiers）。
/// 2. 性能优化：通过“脏标记（Dirty Flag）”和“值缓存（Value Cache）”机制，避免每帧重复计算。
/// 3. 公式统一：严格执行公式 FinalValue = (BaseValue + ΣAdditive) × ΠMultiplicative。
/// 
/// 【数据存储】
/// - 基础属性存储在父实体（Entity）的 Data 容器中（key: "BaseDamage", "BaseSpeed" 等）。
/// - 本组件只负责计算逻辑和 Modifier 管理。
/// </summary>
public partial class AttributeComponent : Node
{
	private static readonly Log Log = new("AttributeComponent");

	// ================= Export Properties =================

	// ================= C# Events =================

	/// <summary>
	/// 当任何属性计算结果发生变化时触发。
	/// UI 系统或依赖属性的组件（如攻击组件）可以订阅此事件。
	/// </summary>
	public event Action? AttributeChanged;

	// ================= Private State =================

	/// <summary>
	/// 父实体的动态数据容器。
	/// </summary>
	private Data _data = null!;

	/// <summary>
	/// 存储当前挂载在实体上的所有修改器（来自 Buff、装备等）。
	/// </summary>
	private readonly List<AttributeModifier> _modifiers = [];

	/// <summary>
	/// 最终属性值的缓存字典。Key 是属性名（如 "Damage"），Value 是计算后的结果。
	/// </summary>
	private readonly Dictionary<string, float> _cachedValues = [];

	/// <summary>
	/// 脏标记。当修改器列表发生变动时设为 true，下次获取属性时将触发重新计算。
	/// </summary>
	private bool _isDirty = true;

	// ================= Computed Properties =================
	// 业务代码应始终通过 GetFinalValue 访问属性，本组件不再预设任何特定属性。

	// ================= Godot 生命周期 (Godot Lifecycle) =================

	public override void _Ready()
	{        // 获取父实体的 Data 容器（实体所有数据共享中心）
		var parent = GetParent();
		if (parent == null)
		{
			Log.Error("AttributeComponent 必须作为实体 (Node) 的子节点存在。");
			return;
		}
		_data = parent.GetData();

		// 初始化时强制清空一次缓存
		RecalculateAll();

		// 监听 Data 变化，以支持直接修改 Base 属性时自动刷新最终值
		_data.OnValueChanged += OnDataChanged;

		Log.Debug("数据组件初始化完成。");
	}

	public override void _ExitTree()
	{
		// 必须在退出树时移除监听，防止内存泄漏
		if (_data != null)
		{
			_data.OnValueChanged -= OnDataChanged;
		}

		// 清理事件绑定和集合
		AttributeChanged = null;
		_modifiers.Clear();
		_cachedValues.Clear();
		Log.Trace("属性组件退出，已清理所有修改器和事件。");
	}

	/// <summary>
	/// 当 Data 中的属性值发生变化时触发。
	/// 如果变化的是基础属性（以 "Base" 开头），则标记脏并重新计算。
	/// </summary>
	/// <param name="key"></param>
	/// <param name="oldVal"></param>
	/// <param name="newVal"></param>
	private void OnDataChanged(string key, object? oldVal, object? newVal)
	{
		// 如果变化的是基础属性（以 "Base" 开头），则标记脏并重新计算

		_isDirty = true;
		RecalculateAll();
		AttributeChanged?.Invoke();

	}

	// ================= 公共方法 (Public Methods) =================

	/// <summary>
	/// 向实体添加一个属性修改器（来自装备、技能、Buff 等）。
	/// </summary>
	/// <param name="modifier">修改器实例。</param>
	public void AddModifier(AttributeModifier modifier)
	{
		if (modifier == null)
		{
			Log.Warn("尝试添加空的修改器。");
			return;
		}

		// 检查 ID 冲突，防止重复添加同一个效果
		if (_modifiers.Any(m => m.Id == modifier.Id))
		{
			Log.Warn($"ID 为 '{modifier.Id}' 的修改器已存在，跳过。");
			return;
		}

		_modifiers.Add(modifier);
		_isDirty = true;

		Log.Debug($"添加修改器: {modifier.Id} ({modifier.Type} {modifier.Value} 到 {modifier.AttributeName})");

		RecalculateAll();
		AttributeChanged?.Invoke();
	}

	/// <summary>
	/// 根据唯一 ID 移除修改器（如 Buff 到期、装备脱下）。
	/// </summary>
	/// <param name="modifierId">修改器唯一标识符。</param>
	public void RemoveModifier(string modifierId)
	{
		var modifier = _modifiers.FirstOrDefault(m => m.Id == modifierId);
		if (modifier == null) return;

		_modifiers.Remove(modifier);
		_isDirty = true;

		Log.Debug($"移除修改器: {modifierId}");

		RecalculateAll();
		AttributeChanged?.Invoke();
	}

	/// <summary>
	/// 检查当前是否拥有特定的修改器。
	/// </summary>
	public bool HasModifier(string modifierId)
	{
		return _modifiers.Any(m => m.Id == modifierId);
	}

	/// <summary>
	/// 获取当前所有修改器的只读列表。
	/// </summary>
	public IReadOnlyList<AttributeModifier> GetModifiers()
	{
		return _modifiers.AsReadOnly();
	}

	/// <summary>
	/// 清除所有修改器。通常用于实体销毁或状态重置。
	/// </summary>
	public void ClearModifiers()
	{
		if (_modifiers.Count == 0) return;

		_modifiers.Clear();
		_isDirty = true;

		Log.Debug("所有修改器已清除。");

		RecalculateAll();
		AttributeChanged?.Invoke();
	}

	/// <summary>
	/// 重置组件状态（用于对象池复用）。
	/// </summary>
	public void Reset()
	{
		_modifiers.Clear();
		_cachedValues.Clear();
		_isDirty = true;

		// 强制重新读取 Data 中的 Base 值并计算
		RecalculateAll();
		AttributeChanged?.Invoke();

		Log.Debug("属性组件已重置。");
	}


	/// <summary>
	/// 获取指定属性的最终计算值（自动推断 BaseKey）。
	/// BaseKey 默认为 "Base" + attrName。
	/// </summary>
	public float Get(string attrName, float defaultBaseValue = 0f)
	{
		return Get(attrName, "Base" + attrName, defaultBaseValue);
	}

	/// <summary>
	/// 获取指定属性的最终计算值。
	/// </summary>
	/// <param name="attrName">目标属性名（如 "Damage"）。</param>
	/// <param name="baseAttrKey">在 Data 中存储基础值 Key（如 \"BaseDamage\"）。</param>
	/// <param name="defaultBaseValue">如果 Data 中没有找到该 Key，使用的默认值。</param>
	/// <returns>经过所有修改器运算后的最终结果。</returns>
	public float Get(string attrName, string baseAttrKey, float defaultBaseValue = 0f)
	{
		// 1. 尝试从缓存中获取（脏标记为 false 时有效）
		if (!_isDirty && _cachedValues.TryGetValue(attrName, out float cached))
		{
			return cached;
		}

		// 2. 从 Data 获取基础值
		float baseValue = _data?.Get<float>(baseAttrKey, defaultBaseValue) ?? defaultBaseValue;

		// 3. 计算、存入缓存并返回
		float finalValue = CalculateFinalValue(attrName, baseValue);
		_cachedValues[attrName] = finalValue;

		return finalValue;
	}

	// ================= 私有方法 (Private Methods) =================

	/// <summary>
	/// 核心算法：计算单个属性的最终值。
	/// 逻辑：(基础值 + Σ加法) * Π乘法
	/// </summary>
	private float CalculateFinalValue(string attrName, float baseValue)
	{
		var attrModifiers = _modifiers
			.Where(m => m.AttributeName == attrName)
			.OrderBy(m => m.Priority)
			.ToList();

		if (attrModifiers.Count == 0) return baseValue;

		// 1. 计算加法修正
		float additiveSum = attrModifiers
			.Where(m => m.Type == ModifierType.Additive)
			.Sum(m => m.Value);

		// 2. 计算乘法修正
		float multiplicativeProduct = attrModifiers
			.Where(m => m.Type == ModifierType.Multiplicative)
			.Aggregate(1f, (acc, m) => acc * m.Value);

		return (baseValue + additiveSum) * multiplicativeProduct;
	}

	/// <summary>
	/// 强制清空缓存并重置脏标记。
	/// 下次访问属性时将重新计算。
	/// </summary>
	private void RecalculateAll()
	{
		_cachedValues.Clear();
		_isDirty = false;
	}
}
