# ObjectPool 测试体系

## 测试场景说明

### 1. `object_pool_test.tscn` - 可视化交互测试

**用途**：人工测试、演示、调试

**功能**：

- ✅ 可视化显示对象的生命周期
- ✅ 实时统计信息
- ✅ 交互式控制面板
- ✅ 复用计数显示

**适合场景**：

- 演示对象池的工作原理
- 调试视觉效果和状态重置
- 压力测试（自动生成模式）

**局限性**：

- 只能人工观察，无自动化验证
- 只测试了基本的获取/归还流程
- 没有边界条件测试

---

### 2. `object_pool_unit_test.tscn` - 自动化单元测试

**用途**：回归测试、功能验证、CI/CD

**测试覆盖**：

#### 基础功能

- ✅ 基本获取/归还
- ✅ 手动预热（`warmup`）
- ✅ 批量操作（`acquire_batch`/`release_batch`）
- ✅ 清空池（`clear`）
- ✅ 清理保留（`cleanup`）

#### 边界条件

- ✅ 池满溢出（销毁多余对象）
- ✅ 重复归还检测
- ✅ 归还无效对象

#### 配置测试

- ✅ 字典配置
- ✅ 配置对象
- ✅ 默认配置

#### 性能测试

- ✅ 压力测试（100 次获取/归还）
- ✅ 复用率验证

**使用方法**：

```bash
# 在 Godot 编辑器中运行 object_pool_unit_test.tscn
# 或通过命令行运行：
godot --headless --script scenes/test/object_pool/object_pool_unit_test.gd
```

**输出示例**：

```
============================================================
开始对象池单元测试
============================================================

[测试] 基本获取/归还
  ✓ 预热数量正确 (期望:5, 实际:5)
  ✓ 成功获取对象
  ✓ 活跃数量+1 (期望:1, 实际:1)
  ✓ 空闲数量-1 (期望:4, 实际:4)
  ✓ 成功归还对象
  ✓ 活跃数量归零 (期望:0, 实际:0)
  ✓ 空闲数量恢复 (期望:5, 实际:5)

[测试] 手动预热
  ✓ 初始预热5个 (期望:5, 实际:5)
  ✓ 补充到10个 (期望:10, 实际:10)
  ✓ 继续增加到15个 (期望:15, 实际:15)

... (省略其他测试)

============================================================
测试完成
============================================================
通过: 10
失败: 0
总计: 10

✓ 所有测试通过！
============================================================
```

---

## 测试文件结构

```
scenes/test/object_pool/
├── object_pool_test.tscn           # 可视化交互测试
├── object_pool_test.gd
├── test_pooled_object.tscn         # 测试用的可池化对象
├── test_pooled_object.gd
├── object_pool_unit_test.tscn      # 自动化单元测试 ⭐ 新增
├── object_pool_unit_test.gd        # ⭐ 新增
└── TEST_README.md                  # 本文档 ⭐ 新增
```

---

## 何时使用哪个测试

| 场景                   | 使用测试场景                 |
| ---------------------- | ---------------------------- |
| 验证功能正确性         | `object_pool_unit_test.tscn` |
| 代码修改后的回归测试   | `object_pool_unit_test.tscn` |
| CI/CD 自动化测试       | `object_pool_unit_test.tscn` |
| 演示对象池的工作原理   | `object_pool_test.tscn`      |
| 调试视觉效果和状态重置 | `object_pool_test.tscn`      |
| 手动压力测试和性能观察 | `object_pool_test.tscn`      |

---

## 扩展测试

如果需要添加新的测试用例，在 `object_pool_unit_test.gd` 中：

1. 创建新的测试方法：

```gdscript
func test_my_feature() -> void:
    var test_name = "我的新功能"
    var pool = ObjectPool.new(test_scene, {...})

    # 测试逻辑
    _assert_eq(pool.some_property, expected_value, "验证描述")

    _test_done(test_name)
```

2. 在 `_run_all_tests()` 中调用：

```gdscript
func _run_all_tests() -> void:
    # ... 其他测试
    test_my_feature()  # 添加这里
```

---

## 最佳实践

1. **开发新功能时**：先写单元测试，验证功能正确性
2. **修改代码后**：运行单元测试，确保没有破坏现有功能
3. **向他人演示时**：使用可视化测试，直观展示效果
4. **调试问题时**：结合两个测试，定位问题根源

---

_路径：`res://scenes/test/object_pool/TEST_README.md`_
