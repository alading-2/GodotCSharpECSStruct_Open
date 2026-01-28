using Godot;
using System;

namespace Brotato.Test.Data
{
    /// <summary>
    /// 用于测试 DataKey 映射机制的虚拟资源类
    /// </summary>
    public partial class TestMappingResource : Resource
    {
        /// <summary>基础生命值</summary>
        // 测试正常场景：属性名和 DataKey 一致，但带了标签
        [DataKey(DataKey.BaseHp)]
        [Export] public float BaseHp { get; set; }

        /// <summary>自定义攻击力名称</summary>
        // 测试映射场景：属性名和 DataKey 不一致，通过标签强制映射
        [DataKey(DataKey.BaseAttack)]
        [Export] public float MyCustomAttackName { get; set; }

        /// <summary>移动速度</summary>
        // 测试兼容场景：不带标签，应当回退到按名映射
        [Export] public float MoveSpeed { get; set; }
    }

    /// <summary>
    /// 验证 DataKey 映射有效性的测试脚本
    /// </summary>
    public partial class TestDataKeyMapping : Node
    {
        /// <summary>初始化测试</summary>
        public override void _Ready()
        {
            GD.Print("\n--- 开始验证 DataKey 映射机制 ---");

            // 1. 准备测试数据
            var resource = new TestMappingResource
            {
                BaseHp = 100f,
                MyCustomAttackName = 50f,
                MoveSpeed = 300f
            };

            var data = new global::Data();

            // 2. 执行加载
            GD.Print("执行 Data.LoadFromResource...");
            data.LoadFromResource(resource);

            // 3. 验证结果
            bool success = true;

            // 验证点 1: 标签映射（同名）
            float hp = data.Get<float>(DataKey.BaseHp);
            if (Math.Abs(hp - 100f) < 0.01f)
                GD.Print("[通过] 同名标签映射成功: BaseHp = 100");
            else
            {
                GD.Print($"[失败] 同名标签映射异常: 期望 100, 实际 {hp}");
                success = false;
            }

            // 验证点 2: 标签映射（异名强制重新映射）
            float attack = data.Get<float>(DataKey.BaseAttack);
            if (Math.Abs(attack - 50f) < 0.01f)
                GD.Print("[通过] 异名标签映射成功: BaseAttack = 50 (映射自 MyCustomAttackName)");
            else
            {
                GD.Print($"[失败] 异名标签映射异常: 期望 50, 实际 {attack}");
                success = false;
            }

            // 验证点 3: 兼容性验证（无标签回退到按名映射）
            float speed = data.Get<float>(DataKey.MoveSpeed);
            if (Math.Abs(speed - 300f) < 0.01f)
                GD.Print("[通过] 无标签按名映射成功: MoveSpeed = 300");
            else
            {
                GD.Print($"[失败] 无标签按名映射异常: 期望 300, 实际 {speed}");
                success = false;
            }

            if (success)
                GD.Print("--- 所有映射验证全部通过！ ---\n");
            else
                GD.Print("--- 验证过程发现错误，请检查 Data.cs 逻辑 ---\n");
        }
    }
}
