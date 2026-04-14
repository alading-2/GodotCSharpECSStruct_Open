# MouseSelection 通用鼠标选择系统

## 阅读入口

从 `MouseSelectionSystem.cs` 开始看，再按 `Interaction -> Picking -> SelectionBoxUi` 的顺序阅读。当前只保留 4 个 partial 文件，避免输入、结果、几何和过滤逻辑拆得过碎：

| 文件 | 职责 |
|------|------|
| `MouseSelectionSystem.cs` | 系统入口、AutoLoad 注册、默认拾取参数、交互状态清理 |
| `MouseSelectionSystem.Interaction.cs` | `_UnhandledInput`、左键按下 / 移动 / 松开状态机、单击 / 框选结果收口、完成 / 未命中事件广播、屏幕 / 世界矩形辅助 |
| `MouseSelectionSystem.Picking.cs` | 物理点选、距离兜底、世界矩形候选收集、实体回溯、`LifecycleState` 过滤 |
| `MouseSelectionSystem.SelectionBoxUi.cs` | 内置框选预览 UI 创建、更新与隐藏 |

事件协议定义在 `Data/EventType/Global/GameEventType_Global_MouseSelection.cs`。选择层常量定义在 `Data/DataKey/Base/CollisionLayers.cs`。

## 系统定位

`MouseSelectionSystem` 是鼠标选择目标系统，用来把“鼠标点击 / 框选场景实体”从具体调用方里拆出来。

它负责：

- 在 `_UnhandledInput` 中常驻处理世界左键单击与拖拽
- 避开已经被 Godot GUI 控件处理过的输入
- 按宽松物理层与生命周期过滤候选目标
- 显示一个内置轻量框选预览 UI
- 通过全局事件回发选择完成、预览或未命中结果

它不负责：

- 维护当前选中集合
- 执行 `Replace / Add / Toggle` 等集合合并策略
- 按具体玩法做 `EntityType / Team` 业务过滤
- 绘制复杂框选 UI、单位描边或高亮表现
- 执行单位命令
- 修改实体属性
- 接入 `FeatureSystem` 生命周期

这里不用 `FeatureSystem`。鼠标选择是输入基础设施，不是被授予、激活、结束、移除的能力；普通系统应监听选择结果并自行决定是否处理。如果某个功能需要“等待玩家点选目标”的独占流程，应走更高优先级的输入占用 / 瞄准状态，而不是把 `MouseSelectionSystem` 重新收窄成某个请求方的私有服务。

## 输入仲裁

系统输入入口是 `_UnhandledInput`，不是 `_Input`，也不是 `CollisionObject2D._input_event`。

原因：

- `_UnhandledInput` 会在 GUI 控件优先处理后才收到事件，适合“UI 没吃掉点击才做世界选择”的场景
- `Control.MouseFilter` 负责 UI 是否拦截点击
- `CollisionMask` 只负责世界物理候选过滤，不能用来解决 UI 点击冲突
- `CollisionObject2D._input_event` 会把选择逻辑下沉到具体物体上，不适合做通用系统

当前系统只保证“GUI 没处理过的点击才进入选择系统”。项目中已有的高优先级输入状态（如 `TargetingManager.IsTargeting`）会让普通鼠标选择暂停；如果未来需要更复杂的“所有世界点击系统都没处理才轮到选择”，应额外设计全局点击路由 / 占用协议，不要把这层职责塞进 `MouseSelectionSystem`。

## 事件协议

核心事件：

| 事件 | 方向 | 说明 |
|------|------|------|
| `MouseSelectionPreviewUpdated` | 系统 -> 调用方 | 拖拽框选过程中的实时预览 |
| `MouseSelectionCompleted` | 系统 -> 调用方 | 选择成功，返回实体集合与主目标 |
| `MouseSelectionMissed` | 系统 -> 调用方 | 点击或框选没有命中任何目标 |

关键数据：

- `InteractionKind`：`Click / Box`，表示本次是单击还是框选
- `HitKind`：`PhysicsPoint / DistanceFallback / BoxRect / None`，表示命中来源
- `Entities`：本次命中的实体集合
- `PrimaryEntity`：集合中的默认主目标，单击时就是命中目标，框选时为排序后的第一个目标
- `ScreenPosition / WorldPosition / ScreenRect`：本次交互的屏幕坐标、世界坐标和选择矩形

## 单击与框选流程

### 单击

```text
左键按下
  -> 左键松开且未超过 DragThresholdPx
  -> PhysicsPoint 查询
  -> DistanceFallback 兜底
  -> Completed / Missed
```

### 框选

```text
左键按下
  -> 鼠标移动超过 DragThresholdPx
  -> 更新内置框选 UI
  -> PreviewUpdated
  -> 左键松开
  -> BoxRect 收集实体
  -> Completed / Missed
```

系统内置的框选 UI 只创建一次，拖拽过程中只更新位置、尺寸和显隐状态。系统不会维护“当前选中集合”，也不会自己执行 `Replace / Add / Toggle`。调用方收到 `Completed.Entities` 后自行过滤实体类型、阵营并处理集合合并。

## 过滤策略

推荐使用两级过滤：

1. **MouseSelection 通用过滤**：宽松物理拾取 + `LifecycleState` 排除死亡 / 复活中实体
2. **监听方业务过滤**：`EntityType / Team / 当前模式 / UI 状态`

示例：

```csharp
GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompletedEventData>(
    GameEventType.Global.MouseSelectionCompleted,
    OnMouseSelectionCompleted
);

private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompletedEventData evt)
{
    if (evt.InteractionKind == GameEventType.Global.MouseSelectionInteractionKind.Box)
    {
        // 正式玩法可在这里筛选 EntityType.Unit、友方阵营，并决定 Replace / Add / Toggle。
    }
}
```

调试工具如 `TestSystem` 只在自身 UI 状态允许时消费结果；正式玩法系统监听同一事件后再按阵营、类型和当前输入状态处理。

## Collision Layer 设置

`SelectionPickable` 定义在：

- `Data/DataKey/Base/CollisionLayers.cs`
- `project.godot` 的 `2d_physics/layer_9`

允许被鼠标选择的实体应在原有身份层外额外挂上 `SelectionPickable`。

示例：

```text
Player: Player + SelectionPickable = 2 + 256 = 258
Enemy:  Enemy  + SelectionPickable = 4 + 256 = 260
```

注意：

- `SelectionPickable` 只是鼠标选择拾取身份层，不承担碰撞伤害业务
- 不要用 `SelectionPickable` 替代 `Player / Enemy / Hurtbox / Projectile` 等业务层
- 不要用 `CollisionMask` 解决 UI 点击问题

## 维护规则

- 全局事件订阅必须在离树或释放时注销
- 调用方只监听 `PreviewUpdated / Completed / Missed`，不要重新引入请求方独占模式
- 内置框选 UI 只承担通用拖拽范围预览；复杂框选、描边、高亮或音效可以监听 `MouseSelectionPreviewUpdated / Completed / Missed` 扩展
- 扩展多选规则时优先在调用方维护集合，不要把具体玩法的编队规则写进通用输入系统
- 如果新增可选目标类型，优先补 `EntityType` 和对应实体初始 Data，再决定是否挂 `SelectionPickable`
