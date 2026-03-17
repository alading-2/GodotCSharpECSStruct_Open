# 特效系统 Entity 生成提示词

**文档类型**：AI 提示词 / 方案约束  
**目标受众**：开发者、AI 助手  
**适用范围**：`EffectEntity`、序列帧特效、附着特效、对象池特效  

---

## 1. 最终结论

这次特效系统不再做复杂分层，统一采用以下方案：

- 所有特效统一做成 `EffectEntity`
- 所有特效统一走 `EntityManager.Spawn / Destroy`
- 所有特效默认接入对象池
- 附着特效统一通过 `EntityRelationshipManager` 建立关系
- DataKey 优先复用已有字段，只补最少的新键

不要再讨论“纯表现特效要不要做 Entity”。

在这个项目里，**统一成 Entity 反而最简单、最一致、最容易维护**。

---

## 2. 为什么这次必须统一用 Entity

根据 `Src/ECS/Entity/Core` 当前实现，Entity 核心逻辑已经很完整：

### 2.1 `EntityManager.Spawn` 已经覆盖特效需要的生命周期入口

`EntityManager.Spawn<T>(EntitySpawnConfig config)` 已经负责：

- 从对象池取对象，或从场景实例化
- 写入 `DataKey.Id`
- 保留统一生命周期入口
- 对 `Node2D` 设置位置和旋转
- 注册 Entity
- 注册全部 Component
- 发送全局 `EntitySpawned` 事件

这意味着 `EffectEntity` 不需要再自造一套生成流程。
真正对外暴露的入口应当是 `EffectService`，由它接收运行时参数，再转交给 `EntityManager`。

### 2.2 `EntityManager.Destroy` 已经覆盖特效回收逻辑

`Destroy` 当前逻辑已经支持：

- 销毁前发送事件
- 注销组件
- 清空 `Data`
- 清空 `Events`
- 清理全部关系
- 如果实现 `IPoolable`，则自动回收到对象池
- 否则 `QueueFree`

这意味着特效结束时也不需要额外定义“特效专用销毁器”。

### 2.3 关系系统已经有特效关系类型

`EntityRelationshipType` 里已经存在：

- `ENTITY_TO_EFFECT`

所以附着特效不需要新造关系类型，直接复用现有定义即可。

---

## 3. 本次特效系统的统一模型

## 3.1 统一实体名

统一命名为：

- `EffectEntity`

不要拆成：

- `EffectVisualEntity`
- `PureEffectEntity`
- `AttachEffectEntity`
- `BattleEffectEntity`

第一版先统一成一个标准实体，后续如果真的出现职责爆炸，再拆也不迟。

## 3.2 统一适用范围

以下全部按 `EffectEntity` 处理：

- 命中特效
- 爆炸特效
- 武器挥砍残影
- 技能释放光效
- 挂在目标身上的燃烧/冰冻/中毒表现
- 跟随施法者或目标的持续表现

也就是说：

**表现不同，但运行时承载体统一。**

---

## 4. EffectEntity 的职责边界

`EffectEntity` 的职责只做三件事：

- 持有特效运行时 Data
- 驱动特效播放
- 在结束时正确回收

不要把以下逻辑塞进 `EffectEntity`：

- 伤害计算
- Buff 主逻辑
- 技能触发主流程
- 命中判定主流程

如果某个对象承担的是“范围伤害判定体”或“持续伤害逻辑体”，那它本质是技能实体，不是单纯特效实体。

---

## 5. EffectEntity 的生成和销毁规则

### 5.1 生成规则

对业务层必须统一使用：

- `EffectService.Spawn(...)`
- `EffectService.SpawnAttached(...)`

在服务层内部再统一使用：

- `EntityManager.Spawn<EffectEntity>(...)`

原因：

- 参数集中收口，调用端不需要创建 `EffectConfig`
- 运行时传参更适合纯表现特效
- 自动设置位置/旋转
- 自动注册组件
- 自动加入生命周期系统
- 与项目其他 Entity 保持完全一致

### 5.2 销毁规则

必须统一使用：

- `EntityManager.Destroy(effectEntity)`

禁止：

- 直接 `QueueFree()`
- 绕过 EntityManager 手动回池

原因：

- `Destroy` 会先清关系
- `Destroy` 会先清组件
- `Destroy` 会清空 `Data`
- `Destroy` 会自动兼容对象池

这对附着特效尤其重要，因为附着特效一定会带关系数据。

---

## 6. 附着特效的关系设计

### 6.1 结论

附着特效必须和被附着的 Entity 建立关系。

统一使用：

- `EntityRelationshipType.ENTITY_TO_EFFECT`

关系方向：

- **宿主 Entity = Parent**
- **EffectEntity = Child**

也就是：

`HostEntity -> EffectEntity (ENTITY_TO_EFFECT)`

### 6.2 为什么不用节点父子关系代替

因为当前框架的权威关系源是 `EntityRelationshipManager`，不是 SceneTree。

节点树父子关系只能解决显示挂载，不能稳定解决：

- 查询某个单位当前有哪些附着特效
- 宿主死亡时批量销毁附着特效
- 通过特效反查宿主
- 调试当前附着关系

### 6.3 附着特效应支持的典型查询

- 通过宿主查全部附着特效
- 通过特效反查宿主
- 宿主销毁时，自动清理所有附着特效
- 特效提前销毁时，自动移除关系

当前 `EntityRelationshipManager` 已具备这些基础能力，不需要额外造轮子。

### 6.4 是否要额外使用 `PARENT`

第一版提示词中，**不要强制要求**附着特效同时建立 `PARENT`。

原因：

- 你当前需求只是“被绑定的 entity 添加关系”
- `ENTITY_TO_EFFECT` 已足够表达宿主与特效关系
- 先保持简单，避免一开始双关系维护增加复杂度

如果未来某些系统明确需要“沿父链向上查宿主”，再考虑是否补 `PARENT`。

---

## 7. 参数设计：播放速度与时间不要重复设计

你已经明确指出：

- 播放速度
- 播放时间

这两个参数在很多情况下只设置其中一个，效果是等价的。

所以第一版不要同时把两套控制都做成主入口。

### 7.1 建议规则

统一只保留一个主参数：

- 优先保留 `MaxLifeTime`

原因：

- 项目里已有 `DataKey.MaxLifeTime`
- 生命周期控制天然和 `EntityManager.Destroy` 对齐
- 对对象池回收更直观

### 7.2 播放速度怎么处理

播放速度仍然需要，但它不是主生命周期参数，而是视觉播放参数。

所以推荐：

- 生命周期主控：`MaxLifeTime`
- 视觉播放倍率：新增一个最小 DataKey 表示播放倍率

不要再设计：

- 一个 `Duration`
- 一个 `LifeTime`
- 一个 `AnimationDuration`
- 一个 `PlayTime`

这会导致语义重复。

### 7.3 第一版原则

第一版只允许一个生命周期主键，一个播放倍率键。

也就是：

- **活多久**：一个键
- **播多快**：一个键

不要搞三套时长概念。

---

## 8. DataKey 复用结论

基于当前 `DataKeyRegister`，以下字段可以直接复用。

### 8.1 可直接复用

- `DataKey.Id`
- `DataKey.VisualScenePath`
- `DataKey.MaxLifeTime`

说明：

- `Id`：`EntityManager.Spawn` 已自动写入
- `VisualScenePath`：现有体系已有视觉资源语义，可直接承载运行时传入的视觉场景
- `MaxLifeTime`：现有体系已经定义，适合直接作为特效生存时间

### 8.2 不建议复用

- `FollowSpeed`
- `StopDistance`
- `Velocity`
- `Acceleration`

原因：

- 这些字段明显偏单位移动语义
- 附着特效跟随宿主，不等于单位跟随逻辑
- 硬复用会污染语义

### 8.3 第一版建议新增的最小 DataKey

只建议新增最少的一组：

- `EffectPlayRate`
- `EffectScale`
- `EffectOffset`
- `EffectIsAttached`

其中：

- `EffectPlayRate`：视觉播放倍率
- `EffectScale`：特效缩放
- `EffectOffset`：相对宿主或生成点的偏移
- `EffectIsAttached`：是否附着模式

### 8.4 暂时不要新增的键

第一版不要急着加：

- `EffectDuration`
- `EffectLifeTime`
- `EffectFollowTarget`
- `EffectLoop`
- `EffectFps`
- `EffectAttachEntityId`
- `EffectZIndex`

原因：

- `MaxLifeTime` 已能承担生命周期
- 附着关系应用 `EntityRelationshipManager` 表达，而不是把宿主 ID 再冗余存进 Data
- 循环、层级、帧率等可先由视觉节点或运行时参数承载，不必第一版就塞进 DataKey

---

## 9. EffectEntity 第一版最小参数集

如果只做你现在要的功能，第一版最小参数集就是：

- 视觉资源：`VisualScenePath`
- 生命周期：`MaxLifeTime`
- 播放倍率：`EffectPlayRate`
- 缩放：`EffectScale`
- 偏移：`EffectOffset`
- 是否附着：`EffectIsAttached`

就这些。

不要一开始扩成十几个字段。

---

## 10. 对象池约束

既然这次已经定了“统一对象池”，那提示词里应强制要求：

- `EffectEntity` 默认实现可池化
- 通过 `EntityManager.Destroy` 回收
- 回收前必须清理附着关系
- 回收后必须是干净状态

### 10.1 必须重置的内容

- 可见性
- 位置
- 旋转
- 缩放
- 偏移
- 播放状态
- 颜色/透明度等临时视觉状态
- 当前附着关系
- 运行中的计时器/回调

因为当前 `Destroy` 会清空 `Data` 与关系，所以提示词重点应放在：

- 不要绕过 `Destroy`
- 不要保留视觉节点残留状态

---

## 11. 给 AI 的重构版提示词

> "请基于本项目的 Godot 4.6 C# 伪 ECS 架构，设计并生成一个 `EffectEntity` 方案，但当前阶段不要写代码，先给出实现设计。请严格遵守以下要求：
>
> 1. 所有特效统一使用 `EffectEntity`，不要再做复杂分层，不要拆成多个特效实体类型。
> 2. 业务层统一通过 `EffectService.Spawn / SpawnAttached / Destroy` 调用特效；`EffectService` 内部再统一走 `EntityManager.Spawn / Destroy`。禁止直接 `QueueFree()`，禁止绕过 `EntityManager` 处理生命周期。
> 3. 所有特效统一接入对象池，默认按可池化 Entity 设计。
> 4. 附着特效必须通过 `EntityRelationshipManager` 与宿主建立关系，直接复用现有 `EntityRelationshipType.ENTITY_TO_EFFECT`，关系方向为 `HostEntity -> EffectEntity`。
> 5. 不要设计 `EffectConfig` 资源。特效需要的视觉场景、生命周期、播放倍率、缩放、偏移、动画名、是否循环等参数，都由 `EffectService` 在运行时直接传入并写入 Data。
> 6. 第一版只允许补最小的新 DataKey，建议仅考虑：`EffectPlayRate`、`EffectScale`、`EffectOffset`、`EffectIsAttached`。如果已有字段足够，就不要新增。
> 7. 播放速度和播放时间不要做成两套重复主参数。生命周期统一优先使用 `MaxLifeTime`，播放速度只作为视觉倍率参数处理。
> 8. `EffectEntity` 的职责只包括：承载 Data、驱动播放、处理附着、生命周期结束后回收。不要把伤害、Buff 主逻辑、技能主流程塞进去。
> 9. 请先输出：EffectService 参数设计、Entity 结构建议、附着关系方案、最小 DataKey 清单、对象池重置清单、生命周期流程，而不是直接写实现代码。"

---

## 12. 本次重构后的唯一目标

这份提示词只服务一个目标：

**用最少概念，把特效系统统一进现有 ECS。**

所以最终答案就是：

- 一个 `EffectEntity`
- 一套对象池
- 一个附着关系类型 `ENTITY_TO_EFFECT`
- 一组最小 DataKey
- 一个统一生命周期入口 `Spawn / Destroy`

不要再把问题想复杂。
