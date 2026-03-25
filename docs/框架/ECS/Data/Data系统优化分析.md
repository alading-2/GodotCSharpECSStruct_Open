# Data 系统优化分析（归档）

> 状态：归档
> 当前主文档：[`DataSystem_Design.md`](./DataSystem_Design.md)

本文档原本记录 Data 系统的性能与架构优化思路。

随着 Data 系统已经完成以下关键落地，这份文档不再作为当前说明入口：

- `DataMeta` 已合并展示字段与运行时约束字段
- 主域 `DataKey` 已升级为 `static readonly DataMeta`
- Config 默认值已统一改为 `DataKey.Xxx.DefaultValue` 直读
- `Data/` 与 `Src/ECS/Data/` 的分工已重新整理

## 仍然有效的历史结论

- `Data` 应坚持“**注册 = 约束，不注册 = 自由读写**”的思路
- 修改器路径要尽量避免在高频读写中引入多余分配
- `LoadFromResource` 的反射与缓存策略值得持续关注
- Data 文档必须区分 **运行时容器** 与 **数据目录配置** 两层职责

## 后续使用方式

- 如果你要了解 **当前 Data 系统怎么用**，看 [`DataSystem_Design.md`](./DataSystem_Design.md)
- 如果你要了解 **运行时代码怎么写**，看 `Src/ECS/Data/README.md`
- 如果你要了解 **Data 目录下的配置、DataKey、EventType 怎么组织**，看：
  - `Data/README.md`
  - `Data/Data/README.md`
  - `Data/DataKey/README.md`

## 说明

本文件保留的意义仅为：

- 留存历史分析背景
- 避免旧链接失效
- 提醒后续优化时不要重新回到旧的双文档分叉状态
