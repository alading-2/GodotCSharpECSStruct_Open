/// <summary>
/// 武器接口
/// <para>所有武器实体必须实现此接口，用于伤害统计归属。</para>
/// <para>实现此接口的实体会在伤害统计时累加数据到自身的 Data 中。</para>
/// </summary>
public interface IWeapon : IEntity { }
