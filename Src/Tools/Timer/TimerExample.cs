using Godot;

/// <summary>
/// TimerManager 使用示例
/// 演示各种常见场景的用法
/// </summary>
public partial class TimerExample : Node2D
{
    private GameTimer _loopTimer;
    private ProgressBar _progressBar;

    public override void _Ready()
    {
        // 示例 1: 基础一次性定时器
        Example1_BasicTimer();

        // 示例 2: 循环定时器
        Example2_LoopTimer();

        // 示例 3: 生命周期绑定
        Example3_LifecycleBinding();

        // 示例 4: 进度追踪
        Example4_ProgressTracking();

        // 示例 5: 标签管理
        Example5_TagManagement();

        // 示例 6: TimeScale 测试
        Example6_TimeScaleTest();
    }

    /// <summary>
    /// 示例 1: 基础一次性定时器
    /// </summary>
    private void Example1_BasicTimer()
    {
        TimerManager.Instance.CreateTimer(2.0f, () =>
        {
            GD.Print("[示例1] 2 秒后执行");
        });
    }

    /// <summary>
    /// 示例 2: 循环定时器
    /// </summary>
    private void Example2_LoopTimer()
    {
        int count = 0;
        _loopTimer = TimerManager.Instance.CreateLoopTimer(1.0f, () =>
        {
            count++;
            GD.Print($"[示例2] 循环执行第 {count} 次");

            // 5 次后停止
            if (count >= 5)
            {
                _loopTimer.Cancel();
            }
        });
    }

    /// <summary>
    /// 示例 3: 生命周期绑定
    /// </summary>
    private void Example3_LifecycleBinding()
    {
        // 创建一个临时节点
        var tempNode = new Node2D { Name = "TempNode" };
        AddChild(tempNode);

        // 定时器绑定到节点生命周期
        TimerManager.Instance.CreateTimer(tempNode, 3.0f, () =>
        {
            GD.Print("[示例3] 这条消息不会显示，因为节点已被销毁");
        });

        // 1 秒后销毁节点（定时器会自动取消）
        TimerManager.Instance.CreateTimer(this, 1.0f, () =>
        {
            tempNode.QueueFree();
            GD.Print("[示例3] 节点已销毁，定时器自动取消");
        });
    }

    /// <summary>
    /// 示例 4: 进度追踪
    /// </summary>
    private void Example4_ProgressTracking()
    {
        var timer = TimerManager.Instance.CreateTimer(5.0f, () =>
        {
            GD.Print("[示例4] 进度条完成！");
        });

        timer.OnUpdate += (progress) =>
        {
            GD.Print($"[示例4] 进度: {progress * 100:F1}%, 剩余: {timer.Remaining:F2}s");
        };
    }

    /// <summary>
    /// 示例 5: 标签管理
    /// </summary>
    private void Example5_TagManagement()
    {
        // 创建多个带标签的定时器
        for (int i = 0; i < 3; i++)
        {
            var timer = TimerManager.Instance.CreateTimer(10.0f, () =>
            {
                GD.Print("[示例5] Buff 定时器完成");
            });
            timer.Tag = "Buff";
        }

        // 2 秒后批量取消
        TimerManager.Instance.CreateTimer(this, 2.0f, () =>
        {
            TimerManager.Instance.CancelByTag("Buff");
            GD.Print("[示例5] 所有 Buff 定时器已取消");
        });
    }

    /// <summary>
    /// 示例 6: TimeScale 测试
    /// </summary>
    private void Example6_TimeScaleTest()
    {
        // 游戏时间定时器（受 TimeScale 影响）
        TimerManager.Instance.CreateTimer(3.0f, () =>
        {
            GD.Print("[示例6] 游戏时间 3 秒");
        });

        // 真实时间定时器（不受 TimeScale 影响）
        TimerManager.Instance.CreateTimer(3.0f, () =>
        {
            GD.Print("[示例6] 真实时间 3 秒");
        }, useUnscaledTime: true);

        // 1 秒后暂停游戏
        TimerManager.Instance.CreateTimer(this, 1.0f, () =>
        {
            Engine.TimeScale = 0.1; // 慢动作
            GD.Print("[示例6] 游戏减速到 0.1x");

            // 5 秒后恢复
            TimerManager.Instance.CreateTimer(this, 5.0f, () =>
            {
                Engine.TimeScale = 1.0;
                GD.Print("[示例6] 游戏速度恢复");
            }, useUnscaledTime: true);
        });
    }

    /// <summary>
    /// 实战案例: 武器冷却系统
    /// </summary>
    public class WeaponCooldownExample
    {
        private bool _canFire = true;
        private float _cooldown = 1.0f;
        private Node _owner;

        public WeaponCooldownExample(Node owner)
        {
            _owner = owner;
        }

        public void Fire()
        {
            if (!_canFire)
            {
                GD.Print("武器冷却中...");
                return;
            }

            // 发射逻辑
            GD.Print("发射！");
            _canFire = false;

            // 冷却定时器
            TimerManager.Instance.CreateTimer(_owner, _cooldown, () =>
            {
                _canFire = true;
                GD.Print("武器就绪");
            });
        }
    }

    /// <summary>
    /// 实战案例: Buff 系统
    /// </summary>
    public class BuffSystemExample
    {
        public void ApplyBuff(Node target, string buffName, float duration, System.Action onExpire)
        {
            GD.Print($"应用 Buff: {buffName}, 持续 {duration}s");

            var timer = TimerManager.Instance.CreateTimer(target, duration, () =>
            {
                GD.Print($"Buff 过期: {buffName}");
                onExpire?.Invoke();
            });
            timer.Tag = $"Buff_{buffName}";
        }

        public void RemoveBuff(string buffName)
        {
            TimerManager.Instance.CancelByTag($"Buff_{buffName}");
            GD.Print($"移除 Buff: {buffName}");
        }
    }

    public override void _ExitTree()
    {
        // 清理（可选，生命周期绑定会自动处理）
        _loopTimer?.Cancel();
    }
}
