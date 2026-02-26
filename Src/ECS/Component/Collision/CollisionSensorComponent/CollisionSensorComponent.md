# CollisionSensorComponent 物理碰撞感应器组件

## 1. 组件概述

`CollisionSensorComponent` 是一个纯粹的、高度专一的物理碰撞感应器。它充当底层物理引擎 (Godot Area2D/Body) 和内部 ECS 事件驱动架构之间的翻译机。

## 2. 深度思考：解耦原理解析

过往的 `HurtComponent` 职责过度集中，既负责物理探测（自己带有 Layer/Mask 要求）又负责伤害计算（包含循环检测伤害等业务）。这导致了严重的高耦合与低扩展痛点（诸如“怪物触碰必须要扣血而不能自爆”的问题）。

### 2.1 单一职责原则：只看不想

重构后，本组件**仅做“情报收集和事件翻译转发”**：

- 继承自原生的 `Area2D` 作为感应容器。
- 自动捕获 Godot 底层进入退出视野的 `AreaEntered`/`BodyEntered` 事件。
- 抛弃所有的伤害处理运算，把它原封不动打包为 `GameEventType.Collision.CollisionEnteredEventData` 的标准数据，发送给**实体局部的 EventBus**。

### 2.2 消除“幽灵配置”与强制包含

- **解耦物理配置**：通过场景实体 (`.tscn`) 的组装特性，直接在 Godot 编辑器内为这个独立的传感器划定碰撞 Layer 与 Mask。从此每个新的实体可以自由设定需要侦察什么碰撞层。
- **可复用性极高**：因为没有任何层面的硬件逻辑绑定，任何需要感应外界进入的实体（即使是金币拾取追踪圈）都能彻底复用该感应器。

### 2.3 拥抱事件溯源拓展化

- 碰撞后该扣血？交由 `ContactDamageComponent` 监听本事件。
- 假如要求自爆？在那个实体的组件节点下加上 `KamikazeExplosionComponent` 订阅碰撞事件引爆即可。互不干扰且自由度极高。

## 3. 工作流程与运行机制

- **OnComponentRegistered()**: ECS 管理系统分配装载该实体与其组件时，本组件通过 `SetDeferred` 安全激活原生 `Monitoring` 和 `Monitorable`，挂载底层碰撞事件监听。
- **OnNodeEntered() / OnNodeExited()**: 将接收到底层的碰撞结果解包、排雷后，向 `Entity.Events.Emit()` 事件总线发布。
- **OnComponentUnregistered()**: 取消订阅引擎底层挂接与所有引用关系，并通过 `SetDeferred` 安全关闭 `Monitoring` 和 `Monitorable`，彻底从物理世界中移除感应。

> **💡 架构设计：为什么 ObjectPool 已经统一关了碰撞，这里还要再关一次？**
> 
> 这是一个基于 **“高内聚低耦合”** 原则的双重防线设计：
> 1. **对象池兜底（防空气墙）**：Godot 的 `ObjectPool` 负责兜底隐藏所有遗留的 `CollisionShape2D`（包括实体的身体和附带件），防止粗心导致进池后变成隐形障碍物。这是对业务透明的物理层脱离。
> 2. **组件内聚（状态闭环）**：`CollisionSensorComponent` 的职责是“雷达”。既然它的生命周期绑定在 `OnComponentRegistered` 和 `OnComponentUnregistered`，那么**它就必须自己管理好自己电源的开关**。即便是非池化实体，或者该组件被意外动态 `RemoveComponent`，它自己主动关闭 `Monitoring`（不听）和 `Monitorable`（不暴露），能确保无论外围架构怎么变，它自身绝不泄露哪怕一帧的脏事件。

## 4. 如何配置与应用？

1. **场景组装**：可直接将预制好的组件文件 (`CollisionSensorComponent.tscn`) 拖拽到目标实体的 `Components` 节点下作为节点组装。
2. **挂载形状**：在它的子树底下加挂用于界址的 `CollisionShape2D`。
3. **设置遮罩**：在面板属性 `CollisionObject2D` 中的 `Layer` (我是谁) 与 `Mask` (关心哪一类物体) 进行符合直觉的常规设定即可。
