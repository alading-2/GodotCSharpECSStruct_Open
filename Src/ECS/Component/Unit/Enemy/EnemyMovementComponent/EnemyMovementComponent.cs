// using Godot;

// /// <summary>
// /// 敌人移动组件 - 将 AI 的移动决策转化为物理操作
// /// <para>
// /// 核心职责：
// /// 1. 在 _PhysicsProcess 中读取 AI 写入的 DataKey.MoveDirection 和 DataKey.MoveSpeedMultiplier
// /// 2. 设置 CharacterBody2D.Velocity 并调用 MoveAndSlide()
// /// 3. 根据移动方向自动翻转 VisualRoot 的 Scale 实现朝向控制
// /// </para>
// /// <para>
// /// 解耦原则：
// /// AI 行为树只负责"决策"（写入 DataKey），本组件负责"执行"（物理移动 + 视觉朝向）。
// /// 这样 AI 模块不直接操作 CharacterBody2D，也不直接操作 VisualRoot，
// /// 实现了 AI 决策层与物理/视觉层的完全分离。
// /// </para>
// /// </summary>
// public partial class EnemyMovementComponent : Node, IComponent
// {
//     private static readonly Log _log = new(nameof(EnemyMovementComponent));

//     // ================= 组件依赖 =================

//     private IEntity? _entity;
//     private Data? _data;
//     private CharacterBody2D? _body;
//     private AnimatedSprite2D? _visualRoot;

//     // ================= IComponent 实现 =================

//     /// <summary>组件注册：缓存物理体和视觉节点引用</summary>
//     public void OnComponentRegistered(Node entity)
//     {
//         if (entity is not IEntity iEntity) return;

//         _entity = iEntity;
//         _data = iEntity.Data;

//         // 缓存 CharacterBody2D 引用
//         if (entity is CharacterBody2D body)
//             _body = body;

//         // 缓存 VisualRoot 引用（用于朝向翻转）
//         _visualRoot = entity.GetNodeOrNull<AnimatedSprite2D>("VisualRoot");

//         _log.Debug($"[{entity.Name}] 敌人移动组件注册完成");
//     }

//     /// <summary>组件注销：释放所有缓存引用</summary>
//     public void OnComponentUnregistered()
//     {
//         _entity = null;
//         _data = null;
//         _body = null;
//         _visualRoot = null;
//     }

//     // ================= Godot 生命周期 =================

//     /// <summary>物理帧处理：读取 AI 移动意图，经分层合成后执行物理移动和朝向更新</summary>
//     public override void _PhysicsProcess(double delta)
//     {
//         if (_body == null || _data == null) return;

//         // 死亡期间停止移动
//         if (_data.Get<bool>(DataKey.IsDead))
//         {
//             _body.Velocity = Vector2.Zero;
//             _body.MoveAndSlide();
//             return;
//         }

//         // 读取 AI 写入的移动意图
//         Vector2 moveDirection = _data.Get<Vector2>(DataKey.AIMoveDirection);
//         float speedMultiplier = _data.Get<float>(DataKey.AIMoveSpeedMultiplier);
//         float moveSpeed = _data.Get<float>(DataKey.MoveSpeed);

//         // 根据移动方向更新视觉朝向（独立于实际移动，即使攻击停止时也能面向目标）
//         if (moveDirection.LengthSquared() > 0.001f)
//         {
//             UpdateFacing(moveDirection);
//         }

//         // 写入基础速度层（AI 意图速度）
//         _data.Set(DataKey.Velocity, moveDirection * moveSpeed * speedMultiplier);

//         // 通过分层合成器获取最终速度（处理击退/锁定/冲量）
//         Vector2 finalVelocity = VelocityResolver.Resolve(_data);

//         // 执行物理移动
//         _body.Velocity = finalVelocity;
//         _body.MoveAndSlide();
//     }

//     // ================= 私有方法 =================

//     /// <summary>
//     /// 根据移动方向更新 VisualRoot 的横向缩放以实现朝向翻转
//     /// </summary>
//     private void UpdateFacing(Vector2 direction)
//     {
//         if (_visualRoot == null) return;

//         if (Mathf.Abs(direction.X) < 0.1f) return;

//         _visualRoot.FlipH = direction.X < 0;
//     }
// }
