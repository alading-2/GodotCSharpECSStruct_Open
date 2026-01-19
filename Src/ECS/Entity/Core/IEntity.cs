/// <summary>
/// Entity 标记接口 - 纯数据容器
/// 
/// 核心原则：
/// 1. **纯容器**：Entity 仅负责持有 Data 和 Events，不应包含任何业务逻辑。
/// 2. **零继承**：通过接口实现，避免基类继承污染。
/// 3. **Scene 即 Entity**：每个 Entity 对应一个 .tscn 场景文件。
/// 
/// 职责：
/// - 提供统一的 Data 访问入口
/// - 提供局部 Events 总线
/// - 提供唯一标识符 (EntityId)
/// 
/// 注意：
/// - 所有业务逻辑（移动、攻击、AI等）必须放入 Component 或 System。
/// - Entity 类本身应保持极简，通常只有几行代码。
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 动态数据容器
    /// 用于存储和管理实体的运行时数据（属性、状态、标记等）
    /// </summary>
    Data Data { get; }

    /// <summary>
    /// 实体局部事件总线
    /// 用于组件间通信 (Component <-> Component) 或 (Component <-> Entity)
    /// </summary>
    EventBus Events { get; }
}
