using System.Runtime.CompilerServices;
using Godot;

public partial class EntityManager
{
    /// <summary>
    /// EntityManager 初始化注册器
    /// </summary>
    public static class Init
    {
        /// <summary>
        /// 模块初始化入口
        /// </summary>
        [ModuleInitializer]
        public static void Initialize()
        {
            // 注册 Component 缓存预热
            // 使用 InitAction 进行纯代码初始化，不需要加载场景或脚本资源
            AutoLoad.Register(new AutoLoad.AutoLoadConfig
            {
                Name = "EntityManagerPrewarm",
                Priority = AutoLoad.Priority.System, // 在 Core 之后，Game 之前
                InitAction = () => PrewarmComponentCache(),
                Path = null // 纯代码模式
            });
        }
    }
}
