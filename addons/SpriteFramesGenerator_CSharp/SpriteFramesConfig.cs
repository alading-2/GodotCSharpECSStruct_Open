#if TOOLS
namespace Slime.Addons
{
    /// <summary>
    /// SpriteFrames 生成器配置文件
    /// 直接修改常量和静态字段，保存后重新编译生效。
    /// </summary>
    public static class SpriteFramesConfig
    {
        // --- 批量扫描路径 ---
        // 递归查找这些路径下包含 PNG 序列帧的子文件夹
        public static readonly string[] BatchPaths = new[]
        {
            "res://assets",
        };

        // --- 默认帧率 (FPS) ---
        public const float DefaultFps = 10.0f;

        // --- 默认循环播放 ---
        // 是否默认循环。白名单中的动画会强制循环。
        public const bool DefaultLoop = true;

        // --- 名称映射表 ---
        // 将美术资源中的各种命名统一为标准名称。Key(小写) -> Value(标准名)
        public static readonly System.Collections.Generic.Dictionary<string, string> NameMap = new()
        {
            { "movement", "run" },
            { "deaded", "dead" },
            { "death", "dead" },
            { "die", "dead" },
        };

        // --- 循环播放白名单 ---
        // 强制循环播放的动画名（忽略 DefaultLoop）。"idle" 开头的动画默认自动循环。
        public static readonly System.Collections.Generic.HashSet<string> LoopAnimations = new()
        {
            "idle",
            "run",
        };

        // --- 固定 Effect 命名路径 ---
        // 这些路径下的动画将统一按字母序重命名为 Effect, Effect1, Effect2...
        public static readonly string[] UnifiedEffectPaths = new[]
        {
            "res://assets/Effect",
        };
    }
}
#endif
