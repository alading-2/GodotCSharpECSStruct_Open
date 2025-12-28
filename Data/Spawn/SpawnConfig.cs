using Godot;
using System.Collections.Generic;

/// <summary>
/// 全局生成配置 - 用于替代分散的 WaveData
/// </summary>
[GlobalClass]
public partial class SpawnConfig : Resource
{
	/// <summary> 每一波的默认持续时间（秒） </summary>
	[Export] public float WaveDuration { get; set; } = 60.0f;

	/// <summary> 最大波次数量（达到后可能循环或结束） </summary>
	[Export] public int MaxWaves { get; set; } = 20;

	/// <summary> 波次间隔时间（休息时间） </summary>
	[Export] public float WaveBreakTime { get; set; } = 5.0f;

	/// <summary> 所有敌人的生成规则列表 </summary>
	[Export] public Godot.Collections.Array<EnemySpawnRule> SpawnRules { get; set; } = new();

	// 提供默认构造函数，方便代码中直接 new
	public SpawnConfig() { }

	// 可以添加更多全局曲线，例如：
	// [Export] public Curve GlobalDifficultyCurve { get; set; }
}
