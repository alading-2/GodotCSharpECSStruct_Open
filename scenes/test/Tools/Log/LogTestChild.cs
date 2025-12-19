using Godot;
using BrotatoMy;

namespace BrotatoMy.Test
{
    public partial class LogTestChild : Node
    {
        private static readonly Log Log = new Log("ChildSystem");

        public void DoSomething()
        {
            Log.Info("我是 ChildSystem，正在执行任务...");
            Log.Debug("ChildSystem 正在计算复杂数据...");
            Log.Success("ChildSystem 任务完成！");
        }
    }
}
