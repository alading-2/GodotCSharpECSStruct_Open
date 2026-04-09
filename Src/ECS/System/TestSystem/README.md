# TestSystem 源码目录说明

## 1. 目录定位

`Src/ECS/System/TestSystem/` 用来承载项目的运行时测试系统源码。

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
| `TestSystem.cs` | 调试系统宿主，负责 AutoLoad、主面板、实体选择、模块注册与切换 |
| `TestModuleBase.cs` | 所有测试模块的统一基类 |
| `AttributeTestModule.cs` | 属性测试模块，负责 Data 编辑与临时加成 UI |
| `FeatureDebugService.cs` | 调试适配层，负责把调试操作转发到正式 Feature / Ability 生命周期 |
| `AbilityTestService.cs` | 技能测试服务，负责目录缓存、分组、视图模型与业务操作 |
| `AbilityTestViewModels.cs` | 技能测试共享视图模型 |
| `AbilityTestModule.cs` | 技能测试 UI，负责双 Tree、右键菜单与事件刷新 |

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
