# 设计文档

## 概述

本设计通过高级功能扩展了现有的 ObjectPool 实现，用于生产级游戏开发。增强功能聚焦于三大支柱：**可观测性**（泄漏检测、指标）、**可靠性**（验证、错误恢复、线程安全）和**灵活性**（自动归还、动态调整大小、钩子）。该设计保持向后兼容性，同时通过配置添加可选的高级功能。

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      ObjectPool                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   Core Pool  │  │  Monitoring  │  │   Advanced   │     │
│  │   Manager    │  │   System     │  │   Features   │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
│         │                 │                  │              │
│         │                 │                  │              │
│  ┌──────▼─────────────────▼──────────────────▼──────┐     │
│  │          Instance Lifecycle Manager               │     │
│  └───────────────────────────────────────────────────┘     │
│         │                 │                  │              │
│  ┌──────▼──────┐   ┌──────▼──────┐   ┌──────▼──────┐     │
│  │ Validation  │   │   Hooks     │   │  Auto-Return│     │
│  │   Layer     │   │   System    │   │   Manager   │     │
│  └─────────────┘   └─────────────┘   └─────────────┘     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 组件职责

1. **核心池管理器**: 现有的获取/释放/预热逻辑
2. **监控系统**: 统计、泄漏检测、性能跟踪
3. **高级功能**: 动态调整大小、状态持久化
4. **实例生命周期管理器**: 协调所有实例状态转换
5. **验证层**: 完整性检查和错误检测
6. **钩子系统**: 带优先级的可扩展回调机制
7. **自动归还管理器**: 基于计时器的自动实例释放

## 组件和接口

### 1. 增强的 ObjectPoolConfig

使用新选项扩展现有配置类：

```gdscript
class ObjectPoolConfig extends RefCounted:
    # Existing fields...

    # Leak Detection
    var enable_leak_detection: bool = false
    var leak_threshold_seconds: float = 60.0

    # Auto-Return
    var enable_auto_return: bool = false
    var auto_return_timeout: float = 5.0

    # Validation
    var enable_validation: bool = true
    var validation_level: ValidationLevel = ValidationLevel.BASIC

    # Dynamic Resizing
    var enable_dynamic_resize: bool = false
    var resize_threshold_ratio: float = 0.8  # Trigger at 80% capacity
    var resize_growth_factor: float = 1.5
    var max_dynamic_size: int = 1000

    # Thread Safety
    var enable_thread_safety: bool = false

    # Performance Monitoring
    var enable_performance_tracking: bool = false
    var performance_warning_threshold_ms: float = 5.0

    enum ValidationLevel {
        NONE,
        BASIC,      # Check is_instance_valid
        STANDARD,   # + Check parent state
        STRICT      # + Check metadata integrity
    }
```

### 2. 实例元数据结构

每个池化实例携带扩展的元数据：

```gdscript
# Stored in instance metadata
{
    "_object_pool": ObjectPool,           # Pool reference
    "in_pool": bool,                      # Current state
    "acquire_time": int,                  # Timestamp (msec)
    "acquire_count": int,                 # Lifetime acquisitions
    "auto_return_timer": Timer,           # Optional timer node
    "validation_hash": int,               # Integrity check
    "caller_stack": String                # Debug info (if enabled)
}
```

### 3. 统计数据结构

增强的统计跟踪：

```gdscript
class PoolStatistics extends RefCounted:
    var total_created: int = 0
    var total_acquired: int = 0
    var total_released: int = 0
    var total_discarded: int = 0
    var total_auto_returned: int = 0
    var total_leaked: int = 0

    var peak_active: int = 0
    var current_active: int = 0
    var current_idle: int = 0

    # Performance metrics
    var avg_acquire_time_ms: float = 0.0
    var avg_release_time_ms: float = 0.0
    var max_acquire_time_ms: float = 0.0
    var max_release_time_ms: float = 0.0

    # Lifetime tracking
    var avg_instance_lifetime_sec: float = 0.0
    var min_instance_lifetime_sec: float = 0.0
    var max_instance_lifetime_sec: float = 0.0

    # Dynamic resizing history
    var resize_events: Array[Dictionary] = []

    func get_hit_rate() -> float:
        if total_acquired == 0: return 0.0
        var hits = total_acquired - (total_created - initial_warmup)
        return (hits / float(total_acquired)) * 100.0

    func to_dict() -> Dictionary:
        # Serialization for persistence
        pass
```

### 4. 生命周期钩子系统

```gdscript
class PoolHook extends RefCounted:
    var callback: Callable
    var priority: int = 0
    var hook_type: HookType

    enum HookType {
        PRE_ACQUIRE,
        POST_ACQUIRE,
        PRE_RELEASE,
        POST_RELEASE
    }

# In ObjectPool
var _hooks: Dictionary = {
    PoolHook.HookType.PRE_ACQUIRE: [],
    PoolHook.HookType.POST_ACQUIRE: [],
    PoolHook.HookType.PRE_RELEASE: [],
    PoolHook.HookType.POST_RELEASE: []
}

func register_hook(hook_type: PoolHook.HookType, callback: Callable, priority: int = 0) -> void:
    var hook = PoolHook.new()
    hook.callback = callback
    hook.priority = priority
    hook.hook_type = hook_type
    _hooks[hook_type].append(hook)
    _hooks[hook_type].sort_custom(func(a, b): return a.priority < b.priority)

func _invoke_hooks(hook_type: PoolHook.HookType, instance: Node) -> void:
    for hook in _hooks[hook_type]:
        try:
            hook.callback.call(instance)
        except:
            push_error("Hook execution failed: %s" % hook)
```

### 5. 自动归还管理器

```gdscript
func _setup_auto_return(instance: Node) -> void:
    if not _config.enable_auto_return:
        return

    var timer = Timer.new()
    timer.wait_time = _config.auto_return_timeout
    timer.one_shot = true
    timer.timeout.connect(_on_auto_return_timeout.bind(instance, timer))

    # Add timer as child of pool owner or use a dedicated timer manager
    _timer_manager.add_child(timer)
    timer.start()

    instance.set_meta("auto_return_timer", timer)

func _cancel_auto_return(instance: Node) -> void:
    var timer = instance.get_meta("auto_return_timer", null)
    if timer and is_instance_valid(timer):
        timer.stop()
        timer.queue_free()
        instance.remove_meta("auto_return_timer")

func _on_auto_return_timeout(instance: Node, timer: Timer) -> void:
    if is_instance_valid(instance) and not instance.get_meta("in_pool", false):
        _stats.total_auto_returned += 1
        auto_return_triggered.emit(instance)
        release(instance)
    timer.queue_free()
```

### 6. 泄漏检测系统

```gdscript
func _check_for_leaks() -> Array[Node]:
    var leaked_instances: Array[Node] = []
    var current_time = Time.get_ticks_msec()

    # Scan all active instances (tracked separately)
    for instance in _active_instances:
        if not is_instance_valid(instance):
            continue

        var acquire_time = instance.get_meta("acquire_time", 0)
        var elapsed_sec = (current_time - acquire_time) / 1000.0

        if elapsed_sec > _config.leak_threshold_seconds:
            leaked_instances.append(instance)
            var caller = instance.get_meta("caller_stack", "unknown")
            push_warning("Potential leak detected: instance held for %.1fs by %s" % [elapsed_sec, caller])

    if leaked_instances.size() > 0:
        leak_detected.emit(leaked_instances)

    return leaked_instances

# Call periodically if leak detection enabled
func _process(delta: float) -> void:
    if _config.enable_leak_detection:
        _leak_check_timer += delta
        if _leak_check_timer >= _leak_check_interval:
            _leak_check_timer = 0.0
            _check_for_leaks()
```

### 7. 验证层

```gdscript
func _validate_instance_for_acquire(instance: Node) -> bool:
    match _config.validation_level:
        ObjectPoolConfig.ValidationLevel.NONE:
            return true

        ObjectPoolConfig.ValidationLevel.BASIC:
            return is_instance_valid(instance)

        ObjectPoolConfig.ValidationLevel.STANDARD:
            if not is_instance_valid(instance):
                return false
            if instance.get_parent() != null:
                push_warning("Instance has unexpected parent")
                return false
            return true

        ObjectPoolConfig.ValidationLevel.STRICT:
            if not is_instance_valid(instance):
                return false
            if instance.get_parent() != null:
                return false
            var expected_hash = instance.get_meta("validation_hash", 0)
            var actual_hash = _compute_validation_hash(instance)
            if expected_hash != actual_hash:
                push_warning("Instance validation hash mismatch")
                return false
            return true

    return false

func _validate_instance_for_release(instance: Node) -> bool:
    if not is_instance_valid(instance):
        return false

    var pool_ref = instance.get_meta("_object_pool", null)
    if pool_ref != self:
        push_warning("Instance does not belong to this pool")
        return false

    return true

func _compute_validation_hash(instance: Node) -> int:
    # Simple hash based on instance properties
    return hash(instance.get_instance_id())
```

### 8. 线程安全操作

```gdscript
var _mutex: Mutex = null

func _init_thread_safety() -> void:
    if _config.enable_thread_safety:
        _mutex = Mutex.new()

func acquire(parent: Node) -> Node:
    if _mutex:
        _mutex.lock()

    var instance = _acquire_internal(parent)

    if _mutex:
        _mutex.unlock()

    return instance

func release(instance: Node) -> bool:
    if _mutex:
        _mutex.lock()

    var result = _release_internal(instance)

    if _mutex:
        _mutex.unlock()

    return result
```

### 9. 动态调整大小

```gdscript
func _check_dynamic_resize() -> void:
    if not _config.enable_dynamic_resize:
        return

    var utilization = float(_active_count) / float(_config.max_size)

    # Expand if near capacity
    if utilization >= _config.resize_threshold_ratio:
        var new_size = int(_config.max_size * _config.resize_growth_factor)
        new_size = min(new_size, _config.max_dynamic_size)

        if new_size > _config.max_size:
            var old_size = _config.max_size
            _config.max_size = new_size

            _stats.resize_events.append({
                "timestamp": Time.get_ticks_msec(),
                "old_size": old_size,
                "new_size": new_size,
                "reason": "high_utilization"
            })

            pool_resized.emit(old_size, new_size)

    # Check hit rate and warmup more if needed
    var hit_rate = _stats.get_hit_rate()
    if hit_rate < 70.0 and _instances.size() < _config.max_size:
        var warmup_count = min(10, _config.max_size - _instances.size())
        warmup(warmup_count)
```

### 10. 状态持久化

```gdscript
func export_state() -> Dictionary:
    return {
        "config": {
            "max_size": _config.max_size,
            "initial_size": _config.initial_size,
            "overflow_policy": _config.overflow_policy,
            "enable_stats": _config.enable_stats,
            # ... other config fields
        },
        "statistics": _stats.to_dict(),
        "pool_name": _name,
        "scene_path": _scene.resource_path,
        "timestamp": Time.get_unix_time_from_system()
    }

func import_state(state: Dictionary) -> bool:
    if not state.has("config") or not state.has("scene_path"):
        push_error("Invalid state dictionary")
        return false

    # Validate scene matches
    if state["scene_path"] != _scene.resource_path:
        push_warning("Scene path mismatch in imported state")
        return false

    # Apply configuration
    var config_data = state["config"]
    _config.max_size = config_data.get("max_size", _config.max_size)
    # ... restore other fields

    # Optionally restore statistics
    if state.has("statistics"):
        _stats.from_dict(state["statistics"])

    state_imported.emit(state)
    return true
```

## 数据模型

### PooledInstance（概念性）

```gdscript
# Not a real class, but represents the data associated with each instance
{
    node: Node,                    # The actual Node instance
    in_pool: bool,                 # Current state
    acquire_time: int,             # Last acquire timestamp
    acquire_count: int,            # Total times acquired
    total_active_time_ms: int,     # Cumulative active time
    auto_return_timer: Timer,      # Optional timer
    validation_hash: int,          # Integrity check value
    caller_stack: String           # Debug info
}
```

### ResizeEvent（调整大小事件）

```gdscript
{
    timestamp: int,
    old_size: int,
    new_size: int,
    reason: String,  # "high_utilization", "low_hit_rate", "manual"
    utilization_at_resize: float
}
```

## 正确性属性

_属性是系统在所有有效执行中应保持为真的特征或行为——本质上是关于系统应该做什么的正式声明。属性充当人类可读规范和机器可验证正确性保证之间的桥梁。_

### 属性反思

在分析所有验收标准后，可以合并几个属性：

- 基于时间的属性（3.1、3.2）可以合并为单个"操作时间跟踪"属性
- 验证属性（4.1、4.2、4.3）可以合并为综合的"实例验证"属性
- 钩子执行属性（7.2、7.3）可以合并为单个"钩子优先级排序"属性
- 线程安全属性（9.1、9.2、9.3）可以合并为单个"并发访问安全"属性

这减少了冗余，同时保持了全面的覆盖。

### 属性 1：获取元数据记录

*对于任何*从池中获取的实例，实例元数据应包含有效的获取时间戳，并且获取计数应递增。

**验证：需求 1.1**

### Property 2: Leak Detection Threshold

_For any_ instance that remains active beyond the configured leak threshold duration, the leak detection system SHALL identify it as a potential leak when leak detection is enabled.

**Validates: Requirements 1.2, 1.3**

### Property 3: Leak Statistics Accuracy

_For any_ pool state, when statistics are requested, the reported leak count SHALL equal the number of instances active beyond the leak threshold.

**Validates: Requirements 1.4**

### Property 4: Auto-Return Timer Association

_For any_ instance acquired with auto-return enabled, the instance SHALL have an associated timer with the configured timeout duration.

**Validates: Requirements 2.1, 2.4**

### Property 5: Auto-Return Timeout Behavior

_For any_ instance with auto-return enabled, if the timeout expires without manual release, the instance SHALL be automatically released and the auto-return signal SHALL be emitted.

**Validates: Requirements 2.2, 2.5**

### Property 6: Auto-Return Cancellation

_For any_ instance with auto-return enabled, if manually released before timeout, the auto-return timer SHALL be cancelled and SHALL not fire.

**Validates: Requirements 2.3**

### Property 7: Operation Timing Tracking

_For any_ acquire or release operation when statistics are enabled, the operation duration SHALL be recorded and SHALL contribute to the average timing metrics.

**Validates: Requirements 3.1, 3.2**

### Property 8: Lifetime Distribution Tracking

_For any_ instance that is acquired and released, when statistics are enabled, its lifetime SHALL be recorded and SHALL contribute to the lifetime distribution metrics.

**Validates: Requirements 3.3**

### Property 9: Statistics Structure Completeness

_For any_ statistics request, the returned data structure SHALL contain all required fields (total counts, averages, peak values, hit rate) with valid numeric values.

**Validates: Requirements 3.4**

### Property 10: Performance Warning Emission

_For any_ operation that exceeds the configured performance threshold, a performance warning signal SHALL be emitted with the operation details.

**Validates: Requirements 3.5**

### Property 11: Instance Validation Integrity

_For any_ instance being acquired or released, when validation is enabled, the pool SHALL verify the instance is valid, has correct parent state, and belongs to this pool (for release).

**Validates: Requirements 4.1, 4.2, 4.3**

### Property 12: Dynamic Resize Trigger

_For any_ pool state where active count exceeds the resize threshold ratio and dynamic resizing is enabled, the pool SHALL increase max_size by the growth factor (respecting maximum bounds).

**Validates: Requirements 5.1, 5.5**

### Property 13: Adaptive Warmup

_For any_ pool state where hit rate falls below 70% and dynamic resizing is enabled, the pool SHALL increase the warmup count to improve hit rate.

**Validates: Requirements 5.2**

### Property 14: Underutilization Cleanup

_For any_ pool state where utilization is low and dynamic resizing is enabled, the pool SHALL reduce retained instances through cleanup.

**Validates: Requirements 5.3**

### Property 15: Resize Signal Emission

_For any_ dynamic resize operation, a pool_resized signal SHALL be emitted containing the old size and new size.

**Validates: Requirements 5.4**

### Property 16: Batch Callback Invocation

_For any_ batch acquire or release operation with a callback, the callback SHALL be invoked exactly once for each instance in the batch.

**Validates: Requirements 6.1, 6.2**

### Property 17: Batch Operation Consistency

_For any_ batch operation, if any instance operation fails, the pool state SHALL remain consistent and other instances SHALL be processed correctly.

**Validates: Requirements 6.3**

### Property 18: Batch Error Handling

_For any_ batch operation where a callback fails for one instance, the remaining instances SHALL still be processed and the operation SHALL complete.

**Validates: Requirements 6.4**

### Property 19: Batch Completion Signal

_For any_ batch operation, upon completion, a signal SHALL be emitted containing the operation results (success count, failure count).

**Validates: Requirements 6.5**

### Property 20: Hook Registration Ordering

_For any_ set of registered hooks, when hooks are added or removed, the hooks SHALL be maintained in priority order (lowest priority value first).

**Validates: Requirements 7.1, 7.5**

### Property 21: Hook Execution Order

_For any_ acquire or release operation, registered hooks SHALL be invoked in priority order (lowest to highest priority value).

**Validates: Requirements 7.2, 7.3**

### Property 22: Hook Error Isolation

_For any_ hook that throws an error during execution, the error SHALL be caught, logged, and remaining hooks SHALL continue to execute.

**Validates: Requirements 7.4**

### Property 23: State Export Completeness

_For any_ pool state, when export_state() is called, the returned Dictionary SHALL contain all configuration fields, current statistics, and metadata.

**Validates: Requirements 8.1, 8.3**

### Property 24: State Round-Trip Consistency

_For any_ pool state, exporting then importing the state SHALL restore the configuration and statistics to equivalent values.

**Validates: Requirements 8.2**

### Property 25: State Import Validation

_For any_ import_state() call with invalid or mismatched data, the operation SHALL fail gracefully, return false, and leave the pool in its previous valid state.

**Validates: Requirements 8.4**

### Property 26: State Operation Signals

_For any_ state export or import operation, upon completion, a signal SHALL be emitted indicating success or failure.

**Validates: Requirements 8.5**

### Property 27: Concurrent Access Safety

_For any_ set of concurrent acquire and release operations when thread-safety is enabled, all operations SHALL complete without data corruption, and final pool state SHALL be consistent.

**Validates: Requirements 9.1, 9.2, 9.3**

### Property 28: Deadlock Freedom

_For any_ high-contention scenario with many concurrent operations, when thread-safety is enabled, all operations SHALL eventually complete without deadlock.

**Validates: Requirements 9.5**

### Property 29: Creation Failure Handling

_For any_ acquire operation where instance creation fails, the pool SHALL log the error, return null, and remain in a valid operational state.

**Validates: Requirements 10.1**

### Property 30: Callback Error Resilience

_For any_ instance callback (on_pool_acquire, on_pool_release) that throws an error, the pool SHALL catch the error, log it, and continue normal operation.

**Validates: Requirements 10.2**

### Property 31: Error Signal Emission

_For any_ critical error that occurs during pool operations, an error signal SHALL be emitted containing diagnostic information about the error.

**Validates: Requirements 10.4**

### Property 32: Post-Recovery Operation

_For any_ pool that undergoes error recovery, subsequent acquire and release operations SHALL function normally if recovery was successful.

**Validates: Requirements 10.5**

## 错误处理

### 错误类别

1. **验证错误**：无效实例、错误的池所有权、损坏的元数据

   - 策略：记录警告、拒绝操作、返回错误代码
   - 恢复：继续处理有效实例

2. **回调错误**：用户提供的回调中的异常（钩子、批量回调、生命周期方法）

   - 策略：捕获异常、记录错误、继续执行
   - 恢复：跳过失败的回调、处理剩余回调

3. **资源错误**：场景实例化失败、计时器创建失败

   - 策略：记录错误、返回 null/false、维护池完整性
   - 恢复：池对其他操作保持可操作

4. **并发错误**：死锁、竞态条件（当启用线程安全时）

   - 策略：使用基于超时的互斥锁获取、检测死锁
   - 恢复：释放锁、记录错误、重试或优雅失败

5. **状态损坏**：操作期间检测到无效的池状态
   - 策略：尝试重建有效状态、记录关键错误
   - 恢复：清除损坏的数据、如果可能则重新初始化

### 错误处理模式

```gdscript
# Pattern 1: Validation with early return
func acquire(parent: Node) -> Node:
    if not _validate_parent(parent):
        push_error("Invalid parent node")
        return null
    # ... continue

# Pattern 2: Try-catch for callbacks
func _invoke_callback(callback: Callable, instance: Node) -> bool:
    try:
        callback.call(instance)
        return true
    except error:
        push_error("Callback failed: %s" % error)
        callback_error.emit(callback, error)
        return false

# Pattern 3: Graceful degradation
func _create_instance() -> Node:
    var instance = null
    try:
        instance = _scene.instantiate()
    except error:
        push_error("Failed to instantiate scene: %s" % error)
        creation_failed.emit(_scene, error)
        return null

    if not is_instance_valid(instance):
        push_error("Instantiated invalid instance")
        return null

    return instance

# Pattern 4: State recovery
func _recover_from_corruption() -> bool:
    push_warning("Attempting pool state recovery")

    # Remove invalid instances
    var valid_instances: Array[Node] = []
    for instance in _instances:
        if is_instance_valid(instance):
            valid_instances.append(instance)

    _instances = valid_instances

    # Recalculate statistics
    _recalculate_stats()

    corruption_recovered.emit()
    return true
```

### 错误信号

```gdscript
# New signals for error reporting
signal validation_failed(instance: Node, reason: String)
signal callback_error(callback: Callable, error: Variant)
signal creation_failed(scene: PackedScene, error: Variant)
signal corruption_detected(details: Dictionary)
signal corruption_recovered()
signal performance_warning(operation: String, duration_ms: float)
signal leak_detected(leaked_instances: Array[Node])
signal auto_return_triggered(instance: Node)
signal pool_resized(old_size: int, new_size: int)
signal state_exported(state: Dictionary)
signal state_imported(state: Dictionary)
```

## 测试策略

### 单元测试方法

单元测试将专注于单个组件和特定场景：

1. **配置测试**

   - 测试默认配置值
   - 测试基于字典的配置解析
   - 测试配置验证

2. **元数据测试**

   - 测试获取时元数据正确设置
   - 测试释放时元数据正确清除
   - 测试元数据在获取/释放周期中的持久性

3. **验证测试**

   - 测试不同级别的验证（NONE、BASIC、STANDARD、STRICT）
   - 测试拒绝无效实例
   - 测试验证哈希计算

4. **钩子系统测试**

   - 测试钩子注册和优先级排序
   - 测试钩子执行顺序
   - 测试钩子错误处理

5. **状态持久化测试**
   - 测试导出生成有效字典
   - 测试使用有效数据导入
   - 测试拒绝无效数据导入

### 基于属性的测试方法

基于属性的测试将使用 **GdUnit4**（Godot 的基于属性的测试框架）来验证跨多个随机输入的通用属性：

1. **Leak Detection Properties**

   - Generate random acquire/release patterns
   - Verify leak detection identifies instances held too long
   - Verify leak statistics are accurate

2. **Auto-Return Properties**

   - Generate random timeout values
   - Verify timers are created and fire correctly
   - Verify manual release cancels timers

3. **Statistics Properties**

   - Generate random operation sequences
   - Verify statistics accurately reflect operations
   - Verify hit rate calculation is correct

4. **Dynamic Resizing Properties**

   - Generate random usage patterns
   - Verify resize triggers at correct thresholds
   - Verify bounds are respected

5. **Batch Operation Properties**

   - Generate random batch sizes
   - Verify all instances are processed
   - Verify callbacks are invoked correctly

6. **Hook Execution Properties**

   - Generate random hook priorities
   - Verify execution order matches priorities
   - Verify error in one hook doesn't affect others

7. **Thread Safety Properties**

   - Generate concurrent operation sequences
   - Verify no data corruption
   - Verify statistics remain consistent

8. **State Persistence Properties**

   - Generate random pool states
   - Verify export/import round-trip preserves state
   - Verify invalid imports are rejected

9. **Error Recovery Properties**

   - Generate random error scenarios
   - Verify pool remains operational after errors
   - Verify error signals are emitted

10. **Validation Properties**
    - Generate instances in various states
    - Verify validation correctly accepts/rejects
    - Verify validation level affects behavior

### 测试配置

```gdscript
# Property-based tests will run with:
# - Minimum 100 iterations per property
# - Random seed for reproducibility
# - Timeout of 30 seconds per property
# - Shrinking enabled to find minimal failing cases

# Example property test structure:
func test_leak_detection_identifies_old_instances():
    # Property: Any instance held longer than threshold is detected as leak
    for i in range(100):
        var pool = create_test_pool({"enable_leak_detection": true, "leak_threshold_seconds": 1.0})
        var parent = create_test_parent()

        # Acquire random number of instances
        var instance_count = randi() % 10 + 1
        var instances = []
        for j in range(instance_count):
            instances.append(pool.acquire(parent))

        # Wait past threshold
        await get_tree().create_timer(1.5).timeout

        # Check for leaks
        var leaks = pool._check_for_leaks()

        # Property: All acquired instances should be detected as leaks
        assert_eq(leaks.size(), instance_count, "All instances should be detected as leaks")

        # Cleanup
        for instance in instances:
            pool.release(instance)
        pool.clear()
```

### 集成测试

集成测试将验证组件之间的交互：

1. **泄漏检测 + 自动归还**：验证自动归还防止泄漏
2. **动态调整大小 + 统计**：验证调整大小决策使用准确的统计信息
3. **钩子 + 验证**：验证钩子可以访问验证结果
4. **线程安全 + 所有功能**：验证所有功能在启用线程安全时正常工作

### 性能测试

性能测试将测量：

1. **获取时间**：测量获取实例的时间（有/无验证、钩子）
2. **释放时间**：测量释放实例的时间
3. **内存使用**：监控不同池大小的内存消耗
4. **线程争用**：测量高并发下的吞吐量
5. **泄漏检测开销**：测量泄漏检测的性能影响

### 测试覆盖率目标

- **行覆盖率**：最低 90%
- **分支覆盖率**：最低 85%
- **属性覆盖率**：100% 的正确性属性已测试
- **错误路径覆盖率**：所有错误处理路径已测试

## 实现说明

### 向后兼容性

所有新功能都通过配置选择加入：

- 使用 ObjectPool 的现有代码将继续不变地工作
- 新功能默认禁用（基本验证除外）
- 配置可以逐步采用

### 性能考虑

1. **泄漏检测**：定期运行（默认：每 5 秒）以最小化开销
2. **统计**：使用增量更新而不是重新计算
3. **验证**：可配置级别允许在安全性和性能之间权衡
4. **线程安全**：仅在明确需要时启用（增加互斥锁开销）
5. **钩子**：存储在预排序数组中以实现 O(n) 执行

### 内存管理

1. **计时器管理**：自动归还计时器被正确清理
2. **钩子存储**：考虑使用弱引用以避免保留循环
3. **统计**：调整大小事件的有界历史记录（最多 100 个条目）
4. **元数据**：每个实例存储最少的元数据

### Godot 特定考虑

1. **信号连接**：在适当的地方使用 CONNECT_ONE_SHOT
2. **计时器节点**：通过专用计时器管理器节点管理
3. **线程安全**：使用 Godot 的 Mutex 和 Semaphore 类
4. **序列化**：使用 Godot 的 Dictionary/Array 进行状态持久化

## 迁移指南

### 对于现有用户

```gdscript
# Before (still works):
var pool = ObjectPool.new(scene, {"max_size": 50})

# After (with new features):
var pool = ObjectPool.new(scene, {
    "max_size": 50,
    "enable_leak_detection": true,
    "leak_threshold_seconds": 30.0,
    "enable_auto_return": true,
    "auto_return_timeout": 10.0,
    "enable_performance_tracking": true
})

# Register hooks for custom behavior:
pool.register_hook(PoolHook.HookType.POST_ACQUIRE, func(instance):
    print("Instance acquired: ", instance)
, 10)

# Monitor for issues:
pool.leak_detected.connect(func(leaks):
    push_warning("Detected %d leaked instances" % leaks.size())
)

pool.performance_warning.connect(func(operation, duration):
    push_warning("%s took %.2fms" % [operation, duration])
)
```

### 推荐的配置配置文件

```gdscript
# Development Profile (maximum debugging)
const DEV_CONFIG = {
    "enable_leak_detection": true,
    "leak_threshold_seconds": 10.0,
    "enable_validation": true,
    "validation_level": ObjectPoolConfig.ValidationLevel.STRICT,
    "enable_performance_tracking": true,
    "performance_warning_threshold_ms": 2.0
}

# Production Profile (optimized performance)
const PROD_CONFIG = {
    "enable_leak_detection": false,
    "enable_validation": true,
    "validation_level": ObjectPoolConfig.ValidationLevel.BASIC,
    "enable_performance_tracking": false,
    "enable_dynamic_resize": true
}

# Testing Profile (for unit tests)
const TEST_CONFIG = {
    "enable_stats": true,
    "enable_validation": true,
    "validation_level": ObjectPoolConfig.ValidationLevel.STANDARD,
    "enable_leak_detection": true,
    "leak_threshold_seconds": 1.0
}
```

## 未来增强

潜在的未来添加（超出本规范范围）：

1. **池层次结构**：相关对象类型的父子池关系
2. **分布式池化**：跨多个场景/关卡的池实例
3. **预测性预热**：基于机器学习的所需池大小预测
4. **可视化调试器**：编辑器内池状态和统计信息的可视化
5. **网络同步**：跨网络同步池状态以支持多人游戏
6. **自定义分配器**：可插拔的实例创建策略
7. **池模板**：常见用例的预定义配置
8. **指标导出**：将统计信息导出到外部监控系统

---

_设计文档版本：1.0_
_最后更新：2024-12-03_
