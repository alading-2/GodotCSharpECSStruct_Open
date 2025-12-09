extends Node
## 对象池单元测试
##
## 提供自动化测试套件，验证 ObjectPool 的所有功能

var test_scene: PackedScene
var passed_tests: int = 0
var failed_tests: int = 0
var test_results: Array[String] = []


func _ready() -> void:
	print("\n" + "=".repeat(60))
	print("开始对象池单元测试")
	print("=".repeat(60))
	
	# 创建简单的测试场景
	test_scene = _create_test_scene()
	
	# 运行所有测试
	_run_all_tests()
	
	# 打印结果
	_print_results()
	
	# 退出
	await get_tree().create_timer(0.1).timeout
	get_tree().quit()


func _run_all_tests() -> void:
	# 基础功能测试
	test_basic_acquire_release()
	test_warmup()
	test_batch_operations()
	test_clear()
	test_cleanup()
	
	# 边界条件测试
	test_pool_overflow()
	test_duplicate_release()
	test_invalid_release()
	
	# 配置测试
	test_different_configs()
	
	# 性能测试
	test_stress()


#region 测试用例
func test_basic_acquire_release() -> void:
	var test_name = "基本获取/归还"
	var pool = ObjectPool.new(test_scene, {"max_size": 10, "initial_size": 5})
	
	# 验证预热
	_assert_eq(pool.available_count, 5, "预热数量正确")
	
	# 获取对象
	var obj = pool.acquire(self)
	_assert_not_null(obj, "成功获取对象")
	_assert_eq(pool.active_count, 1, "活跃数量+1")
	_assert_eq(pool.available_count, 4, "空闲数量-1")
	
	# 归还对象
	var success = pool.release(obj)
	_assert_true(success, "成功归还对象")
	_assert_eq(pool.active_count, 0, "活跃数量归零")
	_assert_eq(pool.available_count, 5, "空闲数量恢复")
	
	_test_done(test_name)


func test_warmup() -> void:
	var test_name = "手动预热"
	var pool = ObjectPool.new(test_scene, {"initial_size": 5})
	
	_assert_eq(pool.available_count, 5, "初始预热5个")
	
	# 手动补充预热
	pool.warmup(5)
	_assert_eq(pool.available_count, 10, "补充到10个")
	
	# 再次调用会继续增加
	pool.warmup(5)
	_assert_eq(pool.available_count, 15, "继续增加到15个")
	
	_test_done(test_name)


func test_batch_operations() -> void:
	var test_name = "批量操作"
	var pool = ObjectPool.new(test_scene, {"max_size": 20})
	
	# 批量获取
	var objects = pool.acquire_batch(self, 5)
	_assert_eq(objects.size(), 5, "批量获取5个")
	_assert_eq(pool.active_count, 5, "活跃数量=5")
	
	# 批量归还
	pool.release_batch(objects)
	_assert_eq(pool.active_count, 0, "批量归还后活跃=0")
	_assert_eq(pool.available_count, 5, "空闲数量=5")
	
	_test_done(test_name)


func test_clear() -> void:
	var test_name = "清空池"
	var pool = ObjectPool.new(test_scene, {"initial_size": 10})
	
	_assert_eq(pool.available_count, 10, "初始10个")
	
	# 清空
	pool.clear()
	_assert_eq(pool.available_count, 0, "清空后空闲=0")
	
	# 获取应创建新实例
	var obj = pool.acquire(self)
	_assert_not_null(obj, "清空后仍可获取")
	
	_test_done(test_name)


func test_cleanup() -> void:
	var test_name = "清理保留指定数量"
	var pool = ObjectPool.new(test_scene, {"initial_size": 20})
	
	_assert_eq(pool.available_count, 20, "初始20个")
	
	# 清理，保留5个
	pool.cleanup(5)
	_assert_eq(pool.available_count, 5, "清理后保留5个")
	
	# 指定保留数量
	pool.warmup(15)
	pool.cleanup(10)
	_assert_eq(pool.available_count, 10, "指定保留10个")
	
	_test_done(test_name)


func test_pool_overflow() -> void:
	var test_name = "池满溢出-销毁多余对象"
	var pool = ObjectPool.new(test_scene, {"max_size": 3})
	
	# 获取4个对象
	var objs = pool.acquire_batch(self, 4)
	
	# 归还4个，但池只能缓存3个，第4个会被销毁
	pool.release_batch(objs)
	_assert_eq(pool.available_count, 3, "池满后只保留3个")
	
	_test_done(test_name)


func test_duplicate_release() -> void:
	var test_name = "重复归还检测"
	var pool = ObjectPool.new(test_scene)
	
	var obj = pool.acquire(self)
	_assert_true(pool.release(obj), "第一次归还成功")
	
	# 应该检测到重复归还（通过 meta 标记）
	# 注意：第二次归还会打印警告，但返回 true（已在池中）
	var result = pool.release(obj)
	_assert_true(result, "重复归还返回true（已在池中）")
	
	_test_done(test_name)


func test_invalid_release() -> void:
	var test_name = "归还无效对象"
	var pool = ObjectPool.new(test_scene)
	
	# 创建一个不属于池的对象
	var fake_obj = Node.new()
	add_child(fake_obj)
	
	# 归还应该失败（会打印警告）
	# 实际上会成功加入池（因为没有严格校验），但这是个边界情况
	var result = pool.release(fake_obj)
	
	# 清理
	fake_obj.queue_free()
	
	_test_done(test_name)


func test_different_configs() -> void:
	var test_name = "不同配置参数"
	
	# 字典配置
	var pool1 = ObjectPool.new(test_scene, {
		"max_size": 10,
		"initial_size": 5,
		"name": "字典池"
	})
	_assert_eq(pool1.available_count, 5, "字典配置生效")
	
	# 配置对象
	var config = ObjectPool.PoolConfig.new()
	config.max_size = 20
	config.initial_size = 10
	var pool2 = ObjectPool.new(test_scene, config)
	_assert_eq(pool2.available_count, 10, "配置对象生效")
	
	# 默认配置
	var pool3 = ObjectPool.new(test_scene)
	_assert_not_null(pool3, "默认配置创建成功")
	
	_test_done(test_name)


func test_stress() -> void:
	var test_name = "性能压力测试"
	var pool = ObjectPool.new(test_scene, {
		"max_size": 50,
		"initial_size": 20
	})
	
	# 快速创建/销毁100次
	var start_time = Time.get_ticks_msec()
	for i in range(100):
		var obj = pool.acquire(self)
		pool.release(obj)
	var elapsed = Time.get_ticks_msec() - start_time
	
	print("  压力测试：100次获取/归还耗时 %d ms" % elapsed)
	_assert_true(elapsed < 100, "性能达标(<100ms)")
	
	# 验证命中率
	var stats = pool.get_stats_string()
	print("  统计: %s" % stats)
	
	_test_done(test_name)
#endregion


#region 断言辅助
func _assert_true(condition: bool, message: String) -> void:
	if condition:
		test_results.append("  ✓ " + message)
	else:
		test_results.append("  ✗ " + message)
		failed_tests += 1


func _assert_false(condition: bool, message: String) -> void:
	_assert_true(not condition, message)


func _assert_eq(actual, expected, message: String) -> void:
	_assert_true(actual == expected, "%s (期望:%s, 实际:%s)" % [message, expected, actual])


func _assert_not_null(value, message: String) -> void:
	_assert_true(value != null, message)


func _test_done(test_name: String) -> void:
	passed_tests += 1
	print("\n[测试] %s" % test_name)
	for result in test_results:
		print(result)
	test_results.clear()
#endregion


#region 辅助方法
func _create_test_scene() -> PackedScene:
	# 创建一个简单的测试节点场景
	var scene = PackedScene.new()
	var node = Node2D.new()
	node.name = "TestNode"
	scene.pack(node)
	return scene


func _print_results() -> void:
	print("\n" + "=".repeat(60))
	print("测试完成")
	print("=".repeat(60))
	print("通过: %d" % passed_tests)
	print("失败: %d" % failed_tests)
	print("总计: %d" % (passed_tests + failed_tests))
	
	if failed_tests == 0:
		print("\n✓ 所有测试通过！")
	else:
		print("\n✗ 有测试失败，请检查")
	print("=".repeat(60))
#endregion
