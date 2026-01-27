# 技能执行器 (Ability Executors)

本目录及子目录存放技能执行器实现，每个技能对应一个执行器类。

## 目录结构

```text
AbilityData.cs             ← 技能配置数据定义
ExecutorRegistry/          ← 执行层核心框架
├── IAbilityExecutor.cs    ← 执行器接口
├── AbilityExecutorRegistry.cs ← 执行器注册表
└── README.md              ← 本文档
Ability/                   ← 具体技能执行器实现
└── DashExecutor.cs        ← 冲刺技能实现示例
```

## 创建新执行器

1. 在 `Ability/` 目录下创建 `{技能名}Executor.cs`
2. 实现 `IAbilityExecutor` 接口
3. 使用 `[ModuleInitializer]` 进行自动注册（推荐）

```csharp
using System.Runtime.CompilerServices;

public class FireballExecutor : IAbilityExecutor
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // 自动注册：名称需与 AbilityData 中的 Key 一致
        AbilityExecutorRegistry.Register("Fireball", new FireballExecutor());
    }

    public AbilityExecuteResult Execute(CastContext context)
    {
        // 实现具体技能逻辑
        return new AbilityExecuteResult { TargetsHit = 1 };
    }
}
```

## CastContext 字段

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| Caster | IEntity? | 施法者 |
| Ability | AbilityEntity? | 技能实体 |
| Targets | List\<IEntity\>? | 目标列表 |
| TargetPosition | Vector2? | 目标位置 |
| SourceEventData | object? | 触发源事件数据 |

## 相关文件

- [技能系统修改文档](../../../../Docs/框架/ECS/Ability/技能系统修改文档.md)
- [AbilitySystem.cs](../../../../Src/ECS/System/AbilitySystem/AbilitySystem.cs)
