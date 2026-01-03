using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Node 扩展方法工具类
/// 
/// 核心职责：
/// 提供常用的 Node 操作扩展方法
/// 
/// 重要说明：
/// Data 系统不扩展到 Node，只在 Entity 中使用。
/// Entity 类中直接包含 Data 属性：public Data Data { get; private set; } = new Data();
/// 
/// 设计理念：
/// - Data 是 Entity 的核心组成部分，不是所有 Node 都需要 Data
/// - 避免过度扩展，保持职责清晰
/// - 需要使用 Data 的组件通过 GetParent<Entity>() 获取父 Entity 的 Data
/// </summary>
public static class NodeExtensions
{
    // 未来可以添加其他 Node 扩展方法
    // 例如：查找子节点、组件管理等
}

