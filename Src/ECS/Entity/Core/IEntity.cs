/// <summary>
/// Entity标记接口 - 定义所有游戏实体的核心能力
/// 
/// 设计理念：
/// - Scene 即 Entity：每个Entity都是独立的.tscn文件
/// - 零继承污染：通过接口而非继承实现Data能力
/// - 精准范围控制：只有Entity拥有Data，Component和普通Node不需要
/// 
/// 使用场景：
/// - Enemy、Player、Bullet、Item等游戏实体
/// - 任何需要拥有动态数据容器的游戏对象
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 动态数据容器
    /// 用于存储和管理实体的运行时数据（属性、状态、标记等）
    /// </summary>
    Data Data { get; }

    /// <summary>
    /// Entity的唯一标识符
    /// 通常使用 GetInstanceId().ToString()
    /// </summary>
    string EntityId { get; }
}
