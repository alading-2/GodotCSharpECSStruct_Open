# TestSystem 源码目录说明

## 1. 目录定位

`Src/ECS/Base/System/TestSystem/` 用来承载项目的运行时测试系统源码。

这套系统面向开发调试阶段，目标是：

- 提供统一的运行时调试入口
- 支持鼠标选择实体
- 支持属性调试与技能管理
- 支持后续扩展更多测试模块

如果你要理解概念与设计边界，请先看：

- `Docs/框架/ECS/System/TestSystem运行时测试系统说明.md`

如果你要按规范扩展此目录，请看：

- `.windsurf/skills/test-system/SKILL.md`

## 2. 当前文件职责

| 文件 | 职责 |
|------|------|
| `TestSystem.cs` | 调试系统宿主，负责 AutoLoad、场景骨架绑定、实体选择、模块注册与切换 |
| `TestSystem.tscn` | 测试系统主面板骨架，导出模块场景并承载顶部工具栏、信息栏与模块宿主区 |
| `TestModuleBase.cs` | 所有测试模块的统一基类 |
| `AttributeTestModule.cs` | 属性测试模块，负责 Data 编辑与临时加成 UI |
| `AttributeTestModule.tscn` | 属性测试固定布局骨架，提供分类列表与右侧滚动编辑区 |
| `FeatureDebugService.cs` | 调试适配层，负责把调试操作转发到正式 Feature / Ability 生命周期 |
| `AbilityTestService.cs` | 技能测试服务，负责目录缓存、分组、视图模型与业务操作 |
| `AbilityTestViewModels.cs` | 技能测试共享视图模型 |
| `AbilityTestModule.cs` | 技能测试 UI，负责双 Tree、右键菜单与事件刷新 |
| `AbilityTestModule.tscn` | 技能测试固定布局骨架，提供左右双树与右键菜单节点 |

## 2.1 第一次看这个系统，推荐按这个顺序读

如果你是第一次接手 `TestSystem`，不要一上来就从某个模块细节往里钻，建议按“**宿主 -> 协议 -> 模块 -> 服务 -> 正式链路**”这个顺序看。

### 第一步：先看 `TestSystem.cs`

先看这个文件，因为它回答的是“**系统怎么启动、主界面怎么绑定、模块怎么接进来**”。

你重点看这几类内容：

- `ModuleInitializer + AutoLoad.Register(...)`：系统为什么会自动出现
- `_Ready()`：当前注册了哪些模块，默认打开哪个模块
- `TestSystem.tscn + CacheUiNodes()`：主面板骨架在哪里、代码如何拿到关键节点
- `SetSelectedEntity(...)`：选中实体后，状态如何广播给各模块
- `SwitchModule(...)`：模块切换时，生命周期是怎么流转的
- `_UnhandledInput()` / `FindEntityAtScreenPosition(...)`：鼠标点选实体的入口在哪里

如果你只想先建立全局认知，**先把这个文件看明白，其他文件就不会散**。

### 第二步：再看 `TestModuleBase.cs`

这个文件回答的是“**所有测试模块必须遵守什么协议**”。

你要重点理解这几个生命周期：

- `Initialize(TestSystem system)`
- `OnSelectedEntityChanged(IEntity? entity)`
- `OnActivated()`
- `OnDeactivated()`
- `Refresh()`

看懂它之后，你就知道所有模块都应该把逻辑挂在哪个阶段，不会在读具体模块时迷路。

### 第三步：按你的目标选模块读

#### 如果你要看属性测试

推荐顺序：

1. `AttributeTestModule.tscn`
2. `AttributeTestModule.cs`
3. `FeatureDebugService.cs`
4. 相关 `DataKey / DataMeta / DataCategory`

这样看的原因是：

- `AttributeTestModule.tscn` 先让你看懂固定布局
- `AttributeTestModule.cs` 再看右侧属性编辑行如何根据元数据动态生成
- 然后看 `FeatureDebugService.cs`，理解“临时加成”为什么不直接硬改 Data
- 最后再回到 `DataMeta`，确认哪些字段允许编辑、哪些字段支持 Modifier

你读属性测试时，重点抓住两条线：

- **直接覆写线**：`entity.Data.Set(...)`
- **临时加成线**：`FeatureDebugService.ApplyTemporaryModifier(...)`

#### 如果你要看技能测试

推荐顺序：

1. `AbilityTestModule.tscn`
2. `AbilityTestModule.cs`
3. `AbilityTestService.cs`
4. `FeatureDebugService.cs`
5. `AbilityTestViewModels.cs`

这样看的原因是：

- `AbilityTestModule.tscn` 先让你看懂左右双栏固定布局
- `AbilityTestModule.cs` 再回答树节点如何重建、交互怎么转发
- `AbilityTestService.cs` 再回答“数据从哪来、操作怎么做”
- `FeatureDebugService.cs` 再告诉你操作最后是如何转发到正式链路
- `AbilityTestViewModels.cs` 最后补齐展示数据结构

你读技能测试时，重点抓住一条主链：

- **UI 输入** -> `AbilityTestModule`
- **业务编排** -> `AbilityTestService`
- **正式系统适配** -> `FeatureDebugService`
- **运行时能力生命周期** -> `EntityManager / FeatureSystem`

### 第四步：最后再看 `FeatureDebugService.cs`

很多人第一次看会直接钻这个文件，但更好的方式是**先知道谁在调用它，再看它怎么转发**。

这个文件的核心定位不是“再做一套测试逻辑”，而是：

- 给 `TestSystem` 提供统一调试适配层
- 把调试动作转发到正式运行时 API
- 避免 UI 模块直接操作底层系统

你重点看：

- `GrantAbility(...)`
- `RemoveAbility(...)`
- `SetFeatureEnabled(...)`
- `ApplyTemporaryModifier(...)`
- `ClearTemporaryModifier(...)`

### 第五步：带着代码回头看正式说明

当你把上面几个文件看过一遍后，再去看：

- `Docs/框架/ECS/System/TestSystem运行时测试系统说明.md`

这时你会更容易把“代码实现”和“设计边界”对上。

建议你重点确认三件事：

- `TestSystem` 只是测试宿主，不是正式玩法 UI
- 技能测试只负责管理，不负责执行
- Feature / Ability 生命周期必须继续复用正式系统，而不是在这里另起炉灶

### 一个最实用的阅读口诀

如果你只记一句话，就记这个：

- **先看宿主 `TestSystem.cs + TestSystem.tscn`，再看协议 `TestModuleBase`，然后按模块读 `*.tscn` 骨架、`*.cs` 动态逻辑和服务层，最后看 `FeatureDebugService` 怎么接正式链路。**

## 3. 使用方式

### 3.1 运行时打开面板

`TestSystem` 通过 `ModuleInitializer + AutoLoad.Register(...)` 自动挂到 Debug 层。

正常启动游戏后：

1. 点击左上角“测试”按钮
2. 打开或隐藏测试面板
3. 通过下拉框切换“属性测试”与“技能测试”模块

### 3.2 选择实体

有两种常用方式：

#### 方式 A：鼠标点选

- 打开“选择实体”开关
- 鼠标点击场景中的目标实体
- `TestSystem` 会优先用物理查询拾取，再尝试回溯所属 `IEntity`

#### 方式 B：代码主动指定

如果你在测试场景中创建了一个固定玩家 / 敌人，推荐在生成后直接调用：

```csharp
TestSystem.Instance?.SetSelectedEntity(entity);
```

适合：

- 单元测试场景
- 技能测试场景
- 属性回归测试场景

## 4. 属性测试怎么用

属性测试模块当前采用双轨模式：

### 4.1 直接改 Data

适合：

- 临时改当前值
- 快速验证状态字段
- 回归检查 `DataMeta` 上下限与类型约束

底层行为：

- 直接 `selectedEntity.Data.Set(...)`

### 4.2 临时 Feature 加成

当 `DataMeta` 满足：

- `IsNumeric == true`
- `SupportModifiers == true`
- `IsComputed == false`

面板会显示“临时加成”行。

底层行为：

- 通过 `FeatureDebugService` 构造运行时 `FeatureDefinition`
- 用 `EntityManager.AddAbility(owner, definition)` 挂到目标实体
- 用 `FeatureModifierEntry` 验证正式 Modifier 链路

## 5. 技能测试怎么用

技能测试模块只负责**管理技能**，不负责执行技能。

### 当前支持

- 添加技能
- 移除技能
- 启用技能
- 禁用技能

### 数据来源

左侧技能库来自：

- `ResourcePaths.Resources[ResourceCategory.DataAbility]`
- `AbilityConfig`

分组优先看：

- `FeatureGroupId`

### 交互方式

- 左侧树：点击叶子添加技能
- 右侧树：点击叶子移除技能
- 右键右侧叶子：启用 / 禁用 / 移除

底层不会绕开正式系统，而是通过：

- `AbilityTestService`
- `FeatureDebugService`
- `EntityManager` / `FeatureSystem`

完成转发。

## 6. 新增测试模块的推荐步骤

### 第一步：创建模块类

新增一个继承 `TestModuleBase` 的模块：

```csharp
public partial class MyTestModule : TestModuleBase
{
    internal override string DisplayName => "我的测试";

    internal override void Initialize(TestSystem system)
    {
        base.Initialize(system);
    }

    internal override void Refresh()
    {
    }
}
```

### 第二步：注册模块

在 `TestSystem._Ready()` 中调用：

```csharp
RegisterModule(new MyTestModule());
```

### 第三步：处理订阅生命周期

如果模块订阅了实体事件或全局事件：

- 在 `OnSelectedEntityChanged(...)` 切换旧订阅
- 在 `OnActivated()` 恢复订阅
- 在 `OnDeactivated()` 解除订阅

### 第四步：不要把系统调用直接塞进 UI

如果模块会操作：

- Feature 生命周期
- Ability 生命周期
- 其它正式子系统

推荐先写一层 Service / Adapter，再由 UI 转发调用。

## 7. 开发约束

维护此目录时请遵守以下边界：

- `TestSystem` 只做宿主与模块切换
- `TestModuleBase` 只做统一生命周期协议
- UI 模块只做展示和输入转发
- Feature / Ability 生命周期优先复用正式链路
- 不要在这里新增一套测试版技能执行系统
- 不要直接编辑计算属性

## 8. 你通常会改哪些地方

### 新增调试模块

通常要改：

- 新模块源码文件
- `TestSystem.cs` 注册入口
- `Docs/框架/ECS/System/TestSystem运行时测试系统说明.md`
- `.windsurf/skills/test-system/SKILL.md`
- `Docs/框架/项目索引.md`

### 扩展属性测试

通常要改：

- `AttributeTestModule.cs`
- 必要时 `FeatureDebugService.cs`
- 相关 `DataMeta / DataKey / DataCategory`
- 正式说明文档与 skill

### 扩展技能管理

通常要改：

- `AbilityTestModule.cs`
- `AbilityTestService.cs`
- 必要时 `FeatureDebugService.cs`
- 正式说明文档与 skill

## 9. 快速自检清单

提交前建议检查：

- 模块切换后是否正确解除旧订阅
- 未选中实体时是否有明确提示
- 是否绕开了正式 `EntityManager / FeatureSystem`
- 是否错误地把技能测试做成了执行入口
- 文档、项目索引、skill 是否同步更新
