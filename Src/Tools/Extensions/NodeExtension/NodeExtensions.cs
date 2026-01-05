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
/// Data 系统已重构，不再扩展到所有 Node。
/// Entity 通过 IEntity 接口直接拥有 Data 属性：entity.Data
/// Component 通过 EntityManager.GetEntityData(component) 或保存 IEntity 引用访问 Entity Data
/// 
/// 设计理念：
/// - Data 是 Entity 的核心组成部分，不是所有 Node 都需要 Data
/// - 避免过度扩展，保持职责清晰
/// - 需要使用 Data 的组件通过 IEntity 接口访问父 Entity 的 Data
/// </summary>
public static class NodeExtensions
{
    // 未来可以添加其他 Node 扩展方法
    // 例如：查找子节点、组件管理等
}

