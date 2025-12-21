using Godot;
using System;

namespace BrotatoMy.Test
{
    public partial class NodeExtensionsTest : Node
    {
        // 使用 Log 工具，上下文名称为类名
        private static readonly Log _log = new Log("NodeExtensionsTest");

        public override void _Ready()
        {
            _log.Info("========================================");
            _log.Info("开始执行 NodeExtensions 单元测试");
            _log.Info("========================================");

            RunTests();
        }

        private void RunTests()
        {
            try
            {
                Test_LazyInitialization();
                Test_DataPersistence();
                Test_NodeIsolation();
                Test_TryGetData();

                _log.Info("----------------------------------------");
                _log.Success("所有测试用例执行通过！");
            }
            catch (Exception e)
            {
                _log.Error($"测试失败: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 测试1: 验证延迟初始化机制
        /// 预期: 初始 HasData 为 false，调用 GetData 后变为 true
        /// </summary>
        private void Test_LazyInitialization()
        {
            _log.Info("测试 1: 延迟初始化 (Lazy Initialization)...");

            var node = new Node();

            // 1. 初始状态检查
            if (node.HasData())
            {
                throw new Exception("新节点不应拥有 Data，HasData() 应返回 false");
            }
            _log.Trace("Step 1: 初始状态 HasData() == false [Pass]");

            // 2. 获取数据 (触发初始化)
            var data = node.GetData();
            if (data == null)
            {
                throw new Exception("GetData() 返回了 null");
            }
            _log.Trace("Step 2: GetData() 返回非空对象 [Pass]");

            // 3. 再次检查状态
            if (!node.HasData())
            {
                throw new Exception("调用 GetData() 后，HasData() 应返回 true");
            }
            _log.Trace("Step 3: 初始化后 HasData() == true [Pass]");

            // 4. 验证单例性 (多次获取应为同一实例)
            var data2 = node.GetData();
            if (!ReferenceEquals(data, data2))
            {
                throw new Exception("多次调用 GetData() 应返回同一 Data 实例");
            }
            _log.Trace("Step 4: Data 实例一致性检查 [Pass]");

            node.QueueFree();
            _log.Debug("测试 1 通过");
        }

        /// <summary>
        /// 测试2: 验证数据存取与持久性
        /// 预期: 存入的数据可以正确取出
        /// </summary>
        private void Test_DataPersistence()
        {
            _log.Info("测试 2: 数据存取 (Data Persistence)...");

            var node = new Node();
            var data = node.GetData();

            // 1. 设置数据
            string key = "Health";
            int value = 100;
            data.Set(key, value);
            _log.Trace($"Step 1: 设置数据 {key} = {value}");

            // 2. 获取数据
            int retrieved = data.Get<int>(key);
            if (retrieved != value)
            {
                throw new Exception($"获取的数据不匹配。期望: {value}, 实际: {retrieved}");
            }
            _log.Trace("Step 2: 获取数据验证成功 [Pass]");

            // 3. 验证通过扩展方法再次获取
            var dataAgain = node.GetData();
            if (dataAgain.Get<int>(key) != value)
            {
                throw new Exception("通过 Node 重新获取 Data 后数据丢失");
            }
            _log.Trace("Step 3: 重新获取 Data 对象数据依然存在 [Pass]");

            node.QueueFree();
            _log.Debug("测试 2 通过");
        }

        /// <summary>
        /// 测试3: 验证节点间的数据隔离
        /// 预期: 不同 Node 的 Data 互不影响
        /// </summary>
        private void Test_NodeIsolation()
        {
            _log.Info("测试 3: 节点隔离性 (Node Isolation)...");

            var node1 = new Node();
            var node2 = new Node();

            // 1. 设置 Node1 数据
            node1.GetData().Set("ID", 1);

            // 2. 验证 Node2 状态
            if (node2.HasData())
            {
                throw new Exception("Node1 的操作不应影响 Node2");
            }

            // 3. 设置 Node2 数据
            node2.GetData().Set("ID", 2);

            // 4. 验证互不干扰
            int val1 = node1.GetData().Get<int>("ID");
            int val2 = node2.GetData().Get<int>("ID");

            if (val1 != 1 || val2 != 2)
            {
                throw new Exception($"数据混淆! Node1: {val1} (expect 1), Node2: {val2} (expect 2)");
            }
            _log.Trace("Step 1: 不同节点数据互不干扰 [Pass]");

            node1.QueueFree();
            node2.QueueFree();
            _log.Debug("测试 3 通过");
        }

        /// <summary>
        /// 测试4: TryGetData 方法
        /// 预期: 正确返回 bool 并输出 Data
        /// </summary>
        private void Test_TryGetData()
        {
            _log.Info("测试 4: TryGetData...");

            var node = new Node();

            // 1. 初始尝试获取 (应失败)
            if (node.TryGetData(out var d1))
            {
                throw new Exception("初始状态 TryGetData 应返回 false");
            }
            if (d1 != null)
            {
                throw new Exception("TryGetData 失败时 out 参数应为 null");
            }
            _log.Trace("Step 1: 初始 TryGetData 返回 false [Pass]");

            // 2. 初始化后尝试获取 (应成功)
            node.GetData(); // 初始化
            if (!node.TryGetData(out var d2))
            {
                throw new Exception("初始化后 TryGetData 应返回 true");
            }
            if (d2 == null)
            {
                throw new Exception("TryGetData 成功时 out 参数不应为 null");
            }
            _log.Trace("Step 2: 初始化后 TryGetData 返回 true [Pass]");

            node.QueueFree();
            _log.Debug("测试 4 通过");
        }
    }
}
