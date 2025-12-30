using Godot;


/// <summary>
/// 移动组件 - 管理实体的物理移动，包括速度、加速度和摩擦力。
/// 通过组合方式附加到实体上，提供平滑的移动控制。
/// </summary>
public partial class VelocityComponent : Node
{
	private static readonly Log Log = new("VelocityComponent");

	// ================= Private State =================

	/// <summary>
	/// 父实体的动态数据容器。
	/// </summary>
	private Data _data = null!;

	/// <summary>
	/// 父节点引用 (CharacterBody2D)
	/// </summary>
	private CharacterBody2D _parent = null!;

	// ================= Runtime State =================

	/// <summary>
	/// 当前速度向量。
	/// </summary>
	public Vector2 Velocity { get; private set; } = Vector2.Zero;

	/// <summary>
	/// 获取速度。
	/// </summary>
	public float Speed => _data.Get<float>("Speed", 400f);

	/// <summary>
	/// 获取最大速度。
	/// </summary>
	public float MaxSpeed => _data.Get<float>("MaxSpeed", 1000f);

	/// <summary>
	/// 加速度因子
	/// <para>值越大，加速度越快。</para>
	/// <para>典型值: 10.0 (正常) ~ 20.0 (快速)。</para>
	/// </summary>
	public float Acceleration => _data.Get<float>("Acceleration", 10.0f);

	// ================= Godot Lifecycle =================

	public override void _Ready()
	{
		var parent = GetParent();
		if (parent == null)
		{
			Log.Error("VelocityComponent 错误: 必须作为实体 (Node) 的子节点存在。");
			return;
		}
		_data = parent.GetData();

		if (parent is CharacterBody2D body)
		{
			_parent = body;
			Log.Debug($"VelocityComponent attached to CharacterBody2D: {_parent.Name}");
		}
		else
		{
			Log.Error($"VelocityComponent: 父节点必须是 CharacterBody2D，当前是: {parent.GetType().Name}");
			return;
		}

		Log.Success($"Ready, Parent: {_parent?.Name}");

		// 初始化数据
	}

	public override void _ExitTree()
	{
		Log.Trace("移动组件退出场景树。");
	}

	public override void _Process(double delta)
	{
		if (_parent == null) return;

		// 获取输入
		Vector2 inputDir = InputManager.GetMoveInput();

		// 计算期望的目标速度
		Vector2 targetVelocity = inputDir.Normalized() * Speed;
		// 平滑
		Velocity = Velocity.Lerp(targetVelocity, 1.0f - Mathf.Exp(-Acceleration * (float)delta));

		// 确保不超过最大速度
		ClampVelocity();

		// 应用位移 - 使用 CharacterBody2D 的标准移动方式
		_parent.Velocity = Velocity;
		_parent.MoveAndSlide();

		// 更新组件的 Velocity (虽然 CharacterBody2D.Velocity 可能会被物理引擎修改，但这里保持同步)
		Velocity = _parent.Velocity;

	}

	// ================= 公开方法 =================

	/// <summary>
	/// 立即停止移动。
	/// </summary>
	public void Stop()
	{
		Velocity = Vector2.Zero;
		Log.Debug("移动已停止。");
	}

	/// <summary>
	/// 直接设置速度（会被限制在 Speed 内）。
	/// </summary>
	/// <param name="velocity">目标速度向量。</param>
	public void SetVelocity(Vector2 velocity)
	{
		Velocity = velocity;
		ClampVelocity();
		Log.Trace($"设置速度: {Velocity}");
	}

	/// <summary>
	/// 获取当前速度向量。
	/// </summary>
	public Vector2 GetVelocity()
	{
		return Velocity;
	}

	/// <summary>
	/// 获取当前速度大小。
	/// </summary>
	public float GetSpeed()
	{
		return Velocity.Length();
	}

	// ================= Private Methods =================

	/// <summary>
	/// 将速度限制在 Speed 内。
	/// </summary>
	private void ClampVelocity()
	{
		//TODO
		if (Velocity.Length() > MaxSpeed)
		{
			Velocity = Velocity.Normalized() * MaxSpeed;
		}
	}
}
