# 实现计划

- [ ] 1. 使用新配置选项扩展 ObjectPoolConfig

  - 添加泄漏检测配置字段（enable_leak_detection、leak_threshold_seconds）
  - 添加自动归还配置字段（enable_auto_return、auto_return_timeout）
  - 添加验证配置字段（enable_validation、validation_level 枚举）
  - 添加动态调整大小配置字段（enable_dynamic_resize、resize_threshold_ratio、resize_growth_factor、max_dynamic_size）
  - 添加线程安全配置字段（enable_thread_safety）
  - 添加性能跟踪配置字段（enable_performance_tracking、performance_warning_threshold_ms）
  - 更新 \_parse_dict_config() 以处理所有新配置键
  - _需求：1.1、1.2、2.1、2.4、3.1、4.5、5.1、5.5、9.4、3.5_

- [ ]\* 1.1 编写配置解析的属性测试

  - **属性 1：配置字典解析**
  - **验证：需求 1.1、2.1、3.1、4.5、5.1、9.4**

- [ ] 2. 实现 PoolStatistics 类

  - 创建包含所有跟踪字段的 PoolStatistics RefCounted 类
  - 实现 get_hit_rate() 计算方法
  - 实现 to_dict() 序列化方法
  - 实现 from_dict() 反序列化方法
  - 添加性能指标字段（avg_acquire_time_ms、avg_release_time_ms、最大时间）
  - 添加生命周期跟踪字段（平均、最小、最大实例生命周期）
  - 添加用于动态调整大小历史的 resize_events 数组
  - _需求：3.1、3.2、3.3、3.4、5.4_

- [ ]\* 2.1 编写统计计算的属性测试

  - **属性 9：统计结构完整性**
  - **验证：需求 3.4**

- [ ] 3. 实现增强的实例元数据系统

  - 更新 \_create_instance() 以添加扩展元数据（acquire_time、acquire_count、validation_hash）
  - 更新 acquire() 以记录获取时间戳并递增 acquire_count
  - 添加 \_compute_validation_hash() 辅助方法
  - 更新 release() 以跟踪实例生命周期以用于统计
  - _需求：1.1、3.3、4.5_

- [ ]\* 3.1 编写元数据记录的属性测试

  - **属性 1：获取元数据记录**
  - **验证：需求 1.1**

- [ ] 4. 实现泄漏检测系统

  - 添加 \_active_instances 跟踪数组以监控获取的实例
  - 实现 \_check_for_leaks() 方法以扫描超过阈值的实例
  - 在 \_process() 方法中添加定期泄漏检查（每 5 秒）
  - 添加 leak_check_timer 和 leak_check_interval 私有变量
  - 更新 acquire() 以将实例添加到 \_active_instances
  - 更新 release() 以从 \_active_instances 中删除实例
  - 添加包含泄漏实例详情的 leak_detected 信号发射
  - 更新 clear() 以在启用泄漏检测时记录未释放的实例
  - _需求：1.2、1.3、1.4、1.5_

- [ ]\* 4.1 编写泄漏检测阈值的属性测试

  - **属性 2：泄漏检测阈值**
  - **验证：需求 1.2、1.3**

- [ ]\* 4.2 编写泄漏统计准确性的属性测试

  - **属性 3：泄漏统计准确性**
  - **验证：需求 1.4**

- [ ] 5. 实现自动归还功能

  - 创建 \_timer_manager Node 以保存自动归还计时器
  - 实现 \_setup_auto_return() 以为实例创建和配置计时器
  - 实现 \_cancel_auto_return() 以停止和清理计时器
  - 实现 \_on_auto_return_timeout() 回调以处理自动释放
  - 更新 acquire() 以在启用时调用 \_setup_auto_return()
  - 更新 release() 以在释放前调用 \_cancel_auto_return()
  - 添加 auto_return_triggered 信号发射
  - 更新统计以跟踪 total_auto_returned 计数
  - _需求：2.1、2.2、2.3、2.4、2.5_

- [ ]\* 5.1 编写自动归还计时器关联的属性测试

  - **属性 4：自动归还计时器关联**
  - **验证：需求 2.1、2.4**

- [ ]\* 5.2 编写自动归还超时行为的属性测试

  - **属性 5：自动归还超时行为**
  - **验证：需求 2.2、2.5**

- [ ]\* 5.3 编写自动归还取消的属性测试

  - **属性 6：自动归还取消**
  - **验证：需求 2.3**

- [ ] 6. 实现性能跟踪和指标

  - 使用 Time.get_ticks_usec() 向 acquire() 添加时间测量
  - 使用 Time.get_ticks_usec() 向 release() 添加时间测量
  - 使用操作持续时间更新统计（增量平均计算）
  - 实现超过阈值时的 performance_warning 信号发射
  - 跟踪从获取到释放的实例生命周期
  - 使用生命周期分布更新统计（最小、最大、平均）
  - _需求：3.1、3.2、3.3、3.5_

- [ ]\* 6.1 编写操作时间跟踪的属性测试

  - **属性 7：操作时间跟踪**
  - **验证：需求 3.1、3.2**

- [ ]\* 6.2 编写生命周期分布跟踪的属性测试

  - **属性 8：生命周期分布跟踪**
  - **验证：需求 3.3**

- [ ]\* 6.3 编写性能警告发射的属性测试

  - **属性 10：性能警告发射**
  - **验证：需求 3.5**

- [ ] 7. 实现验证层

  - 在 ObjectPoolConfig 中创建 ValidationLevel 枚举
  - 实现具有三个验证级别的 \_validate_instance_for_acquire()
  - 实现 \_validate_instance_for_release() 以检查池所有权
  - 实现 \_compute_validation_hash() 以进行完整性检查
  - 更新 acquire() 以在使用实例前调用验证
  - 更新 release() 以在接受实例前调用验证
  - 添加包含原因的 validation_failed 信号发射
  - _需求：4.1、4.2、4.3、4.4、4.5_

- [ ]\* 7.1 编写实例验证完整性的属性测试

  - **属性 11：实例验证完整性**
  - **验证：需求 4.1、4.2、4.3**

- [ ] 8. 检查点 - 确保所有测试通过

  - 确保所有测试通过，如有疑问请询问用户。

- [ ] 9. 实现动态调整大小系统

  - 实现 \_check_dynamic_resize() 方法以评估调整大小条件
  - 添加高利用率的调整大小逻辑（扩展池）
  - 添加低命中率的调整大小逻辑（增加预热）
  - 添加低利用率的调整大小逻辑（清理多余实例）
  - 更新 acquire() 或 \_process() 以定期调用 \_check_dynamic_resize()
  - 添加包含旧大小和新大小的 pool_resized 信号发射
  - 在 statistics.resize_events 数组中跟踪调整大小事件
  - 对动态调整大小强制执行最小/最大边界
  - _需求：5.1、5.2、5.3、5.4、5.5_

- [ ]\* 9.1 编写动态调整大小触发器的属性测试

  - **属性 12：动态调整大小触发器**
  - **验证：需求 5.1、5.5**

- [ ]\* 9.2 编写自适应预热的属性测试

  - **属性 13：自适应预热**
  - **验证：需求 5.2**

- [ ]\* 9.3 编写低利用率清理的属性测试

  - **属性 14：低利用率清理**
  - **验证：需求 5.3**

- [ ]\* 9.4 编写调整大小信号发射的属性测试

  - **属性 15：调整大小信号发射**
  - **验证：需求 5.4**

- [ ] 10. 实现增强的批量操作

  - 更新 acquire_batch() 以接受可选的回调参数
  - 更新 release_batch() 以接受可选的回调参数
  - 实现批量操作中回调失败的错误处理
  - 跟踪批量操作的成功/失败计数
  - 添加包含操作结果的 batch_completed 信号发射
  - 确保事务一致性（全有或全无或部分成功处理）
  - _需求：6.1、6.2、6.3、6.4、6.5_

- [ ]\* 10.1 编写批量回调调用的属性测试

  - **属性 16：批量回调调用**
  - **验证：需求 6.1、6.2**

- [ ]\* 10.2 编写批量操作一致性的属性测试

  - **属性 17：批量操作一致性**
  - **验证：需求 6.3**

- [ ]\* 10.3 编写批量错误处理的属性测试

  - **属性 18：批量错误处理**
  - **验证：需求 6.4**

- [ ]\* 10.4 编写批量完成信号的属性测试

  - **属性 19：批量完成信号**
  - **验证：需求 6.5**

- [ ] 11. 实现生命周期钩子系统

  - 创建包含 callback、priority 和 hook_type 字段的 PoolHook RefCounted 类
  - 创建 HookType 枚举（PRE_ACQUIRE、POST_ACQUIRE、PRE_RELEASE、POST_RELEASE）
  - 添加 \_hooks Dictionary 以按类型存储钩子
  - 实现具有基于优先级插入的 register_hook() 方法
  - 实现保持优先级顺序的 unregister_hook() 方法
  - 实现具有错误处理的 \_invoke_hooks() 方法
  - 更新 acquire() 以调用 PRE_ACQUIRE 和 POST_ACQUIRE 钩子
  - 更新 release() 以调用 PRE_RELEASE 和 POST_RELEASE 钩子
  - _需求：7.1、7.2、7.3、7.4、7.5_

- [ ]\* 11.1 编写钩子注册排序的属性测试

  - **属性 20：钩子注册排序**
  - **验证：需求 7.1、7.5**

- [ ]\* 11.2 编写钩子执行顺序的属性测试

  - **属性 21：钩子执行顺序**
  - **验证：需求 7.2、7.3**

- [ ]\* 11.3 编写钩子错误隔离的属性测试

  - **属性 22：钩子错误隔离**
  - **验证：需求 7.4**

- [ ] 12. 实现状态持久化

  - 实现 export_state() 方法以序列化配置和统计
  - 实现 import_state() 方法以从字典恢复
  - 在 import_state() 中添加验证以检查数据结构和场景路径
  - 添加 state_exported 和 state_imported 信号发射
  - 确保包含所有相关配置和统计字段
  - 优雅地处理导入失败而不损坏当前状态
  - _需求：8.1、8.2、8.3、8.4、8.5_

- [ ]\* 12.1 编写状态导出完整性的属性测试

  - **属性 23：状态导出完整性**
  - **验证：需求 8.1、8.3**

- [ ]\* 12.2 编写状态往返一致性的属性测试

  - **属性 24：状态往返一致性**
  - **验证：需求 8.2**

- [ ]\* 12.3 编写状态导入验证的属性测试

  - **属性 25：状态导入验证**
  - **验证：需求 8.4**

- [ ]\* 12.4 编写状态操作信号的属性测试

  - **属性 26：状态操作信号**
  - **验证：需求 8.5**

- [ ] 13. 实现线程安全机制

  - 添加 \_mutex 私有变量（Mutex 类型）
  - 实现 \_init_thread_safety() 以在启用时创建互斥锁
  - 将 acquire() 重构为 \_acquire_internal() 并添加互斥锁包装器
  - 将 release() 重构为 \_release_internal() 并添加互斥锁包装器
  - 向统计更新添加互斥锁保护
  - 向 \_instances 数组访问添加互斥锁保护
  - 确保所有关键部分都得到适当保护
  - _需求：9.1、9.2、9.3、9.4、9.5_

- [ ]\* 13.1 编写并发访问安全的属性测试

  - **属性 27：并发访问安全**
  - **验证：需求 9.1、9.2、9.3**

- [ ]\* 13.2 编写死锁自由的属性测试

  - **属性 28：死锁自由**
  - **验证：需求 9.5**

- [ ] 14. 检查点 - 确保所有测试通过

  - 确保所有测试通过，如有疑问请询问用户。

- [ ] 15. 实现全面的错误恢复

  - 在 \_create_instance() 中添加 try-catch 块以处理实例化失败
  - 在回调调用中添加 try-catch 块（钩子、生命周期方法）
  - 实现 \_recover_from_corruption() 方法以重建有效状态
  - 添加错误信号发射（creation_failed、callback_error、corruption_detected）
  - 确保所有错误路径将池返回到有效的操作状态
  - 在成功恢复后添加 corruption_recovered 信号发射
  - 更新 acquire() 以在创建失败时优雅地返回 null
  - _需求：10.1、10.2、10.3、10.4、10.5_

- [ ]\* 15.1 编写创建失败处理的属性测试

  - **属性 29：创建失败处理**
  - **验证：需求 10.1**

- [ ]\* 15.2 编写回调错误恢复能力的属性测试

  - **属性 30：回调错误恢复能力**
  - **验证：需求 10.2**

- [ ]\* 15.3 编写错误信号发射的属性测试

  - **属性 31：错误信号发射**
  - **验证：需求 10.4**

- [ ]\* 15.4 编写恢复后操作的属性测试

  - **属性 32：恢复后操作**
  - **验证：需求 10.5**

- [ ] 16. 向 ObjectPool 添加所有新信号

  - 添加 leak_detected 信号（leaked_instances: Array[Node]）
  - 添加 auto_return_triggered 信号（instance: Node）
  - 添加 performance_warning 信号（operation: String, duration_ms: float）
  - 添加 validation_failed 信号（instance: Node, reason: String）
  - 添加 callback_error 信号（callback: Callable, error: Variant）
  - 添加 creation_failed 信号（scene: PackedScene, error: Variant）
  - 添加 corruption_detected 信号（details: Dictionary）
  - 添加 corruption_recovered 信号
  - 添加 pool_resized 信号（old_size: int, new_size: int）
  - 添加 batch_completed 信号（success_count: int, failure_count: int）
  - 添加 state_exported 信号（state: Dictionary）
  - 添加 state_imported 信号（state: Dictionary）
  - _需求：1.3、2.5、3.5、4.4、5.4、6.5、8.5、10.4_

- [ ]\* 16.1 编写信号发射的单元测试

  - 测试每个信号在正确的时间以正确的参数发出
  - _需求：所有与信号相关的需求_

- [ ] 17. 更新 get_stats_string() 以增强统计

  - 在统计字符串中包含泄漏计数
  - 在统计字符串中包含自动归还计数
  - 包含性能指标（平均获取/释放时间）
  - 包含调整大小事件计数
  - 格式化字符串以使其可读且全面
  - _需求：1.4、3.4_

- [ ]\* 17.1 编写统计字符串格式化的单元测试

  - 测试统计字符串包含所有预期信息
  - _需求：3.4_

- [ ] 18. 更新 README.md 文档

  - 记录所有新配置选项及示例
  - 记录新信号及其发出时机
  - 记录生命周期钩子系统及使用示例
  - 记录状态持久化及导出/导入示例
  - 记录线程安全考虑和使用
  - 添加新功能的故障排除部分
  - 添加新功能的性能调优指南
  - 更新 API 参考，包含所有新方法和属性
  - _需求：所有需求（文档）_

- [ ] 19. 创建迁移指南和示例配置

  - 创建开发配置配置文件示例
  - 创建生产配置配置文件示例
  - 创建测试配置配置文件示例
  - 记录向后兼容性保证
  - 提供常见场景的前后代码示例
  - _需求：所有需求（迁移支持）_

- [ ] 20. 最终检查点 - 全面测试
  - 运行所有单元测试并验证 100% 通过率
  - 运行所有基于属性的测试，每个测试 100+ 次迭代
  - 验证所有 32 个正确性属性已测试
  - 测试与现有 ObjectPool 使用的向后兼容性
  - 对所有新功能执行手动测试
  - 验证性能基准满足要求
  - 确保所有测试通过，如有疑问请询问用户。
