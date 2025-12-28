#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BrotatoMy.Addons
{
    /// <summary>
    /// SpriteFrames 自动生成插件
    /// 功能：将指定目录下的序列帧图片（PNG）自动转换为 Godot 的 SpriteFrames 资源，并生成配套的 AnimatedSprite2D 场景。
    /// </summary>
    [Tool]
    public partial class SpriteFramesGeneratorPlugin : EditorPlugin
    {
        // 菜单项名称常量
        private const string MENU_ITEM_NAME = "Generate SpriteFrames (Single/Selection)";
        private const string MENU_ITEM_BATCH_NAME = "Generate All SpriteFrames (Batch)";

        // 预设的自动扫描路径：批量生成功能会遍历这些路径下的子文件夹
        private static readonly string[] BATCH_PATHS = { "res://assets/character/ememy", "res://assets/character/player" };

        // 动画名称规范化映射表：用于统一不同美术素材的命名差异
        // 例如：将 "movement" 统一映射为 "run"
        private static readonly Dictionary<string, string> _nameMap = new()
        {
            { "movement", "run" },
            { "deaded", "dead" },
            { "death", "dead" },
            { "die", "dead" },
        };

        /// <summary>
        /// 规范化动画名称
        /// </summary>
        /// <param name="rawName">原始文件名中的动画部分</param>
        /// <returns>映射后的标准名称</returns>
        private string NormalizeName(string rawName)
        {
            rawName = rawName.ToLower();
            return _nameMap.GetValueOrDefault(rawName, rawName);
        }

        private FolderContextMenuPlugin _contextMenuPlugin;

        /// <summary>
        /// 插件进入编辑器树时调用（启用插件）
        /// </summary>
        public override void _EnterTree()
        {
            // 1. 在编辑器顶部“工具”菜单中添加批量生成项
            AddToolMenuItem(MENU_ITEM_BATCH_NAME, Callable.From(GenerateAllFromPredefinedPaths));

            // 2. 注册文件系统面板的右键菜单插件
            _contextMenuPlugin = new FolderContextMenuPlugin(this);
            AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.Filesystem, _contextMenuPlugin);
        }

        /// <summary>
        /// 插件退出编辑器树时调用（禁用插件）
        /// </summary>
        public override void _ExitTree()
        {
            // 移除菜单项和插件，避免内存泄漏或菜单残留
            RemoveToolMenuItem(MENU_ITEM_BATCH_NAME);
            if (_contextMenuPlugin != null)
            {
                RemoveContextMenuPlugin(_contextMenuPlugin);
                _contextMenuPlugin = null;
            }
        }

        /// <summary>
        /// 处理选中的路径并生成资源
        /// </summary>
        /// <param name="paths">选中的文件或文件夹路径列表</param>
        public void GenerateFromPaths(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            string path = paths[0];

            // 如果用户选中了文件，则自动获取该文件所在的目录
            if (FileAccess.FileExists(path))
            {
                path = path.GetBaseDir();
            }

            // 检查文件夹是否存在
            if (DirAccess.DirExistsAbsolute(path))
            {
                GenerateSpriteFrames(path, true);
            }
            else
            {
                GD.PrintErr($"路径不存在或无法访问: {path}");
            }
        }

        /// <summary>
        /// 内部类：用于扩展文件系统面板的右键菜单
        /// </summary>
        private partial class FolderContextMenuPlugin : EditorContextMenuPlugin
        {
            private SpriteFramesGeneratorPlugin _plugin;

            public FolderContextMenuPlugin() { }
            public FolderContextMenuPlugin(SpriteFramesGeneratorPlugin plugin)
            {
                _plugin = plugin;
            }

            /// <summary>
            /// 当用户在文件系统右键点击时触发
            /// </summary>
            public override void _PopupMenu(string[] paths)
            {
                // 遍历选中项，如果包含目录，则显示生成菜单
                foreach (var path in paths)
                {
                    if (DirAccess.DirExistsAbsolute(path))
                    {
                        AddContextMenuItem(MENU_ITEM_NAME, Callable.From(() => _plugin.GenerateFromPaths(paths)));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 批量处理预设路径下的所有角色文件夹
        /// </summary>
        private void GenerateAllFromPredefinedPaths()
        {
            int totalGenerated = 0;
            foreach (var basePath in BATCH_PATHS)
            {
                if (!DirAccess.DirExistsAbsolute(basePath))
                {
                    GD.Print($"[SpriteFramesGenerator] 路径不存在: {basePath}");
                    continue;
                }

                using var dir = DirAccess.Open(basePath);
                if (dir == null) continue;

                dir.ListDirBegin();
                string subDirName = dir.GetNext();
                while (subDirName != "")
                {
                    // 排除隐藏文件夹（以.开头）
                    if (dir.CurrentIsDir() && !subDirName.StartsWith("."))
                    {
                        string fullSubPath = basePath.PathJoin(subDirName);
                        // 仅当目录下存在 PNG 文件时才进行处理
                        if (HasPngFiles(fullSubPath))
                        {
                            GenerateSpriteFrames(fullSubPath, false); // 批量模式下暂不执行文件系统扫描
                            totalGenerated++;
                        }
                    }
                    subDirName = dir.GetNext();
                }
                dir.ListDirEnd();
            }

            if (totalGenerated > 0)
            {
                // 批量完成后统一执行一次文件系统扫描，更新编辑器视图
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
                GD.PrintRich($"[color=cyan]批量生成完成！共处理 {totalGenerated} 个角色目录。[/color]");
            }
            else
            {
                GD.PrintRich("[color=yellow]未在预设路径下发现可处理的资源。[/color]");
            }
        }

        /// <summary>
        /// 检查目录下是否存在 PNG 文件
        /// </summary>
        private bool HasPngFiles(string path)
        {
            using var dir = DirAccess.Open(path);
            if (dir == null) return false;

            foreach (var file in dir.GetFiles())
            {
                // 过滤掉 .import 文件，只查找原始图片
                if (file.EndsWith(".png") && !file.EndsWith(".import")) return true;
            }
            return false;
        }

        /// <summary>
        /// 核心逻辑：扫描文件夹并生成 SpriteFrames 资源和场景
        /// </summary>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <param name="triggerScan">完成后是否立即触发编辑器资源扫描</param>
        private void GenerateSpriteFrames(string folderPath, bool triggerScan = true)
        {
            GD.Print($"正在扫描文件夹: {folderPath}");
            using var dir = DirAccess.Open(folderPath);
            string[] files = dir.GetFiles();

            // 获取所有 PNG 文件
            var pngFiles = files.Where(f => f.EndsWith(".png") && !f.EndsWith(".import")).ToList();

            if (pngFiles.Count == 0)
            {
                GD.PushWarning("该文件夹下没有 PNG 文件。");
                return;
            }

            // 逻辑：兼容多种常见的序列帧命名格式
            // 1. Spine 导出格式示例: hero_guangfa-Attack1_00.png (解析为：动作=Attack1, 帧号=00)
            // 2. 简单格式示例: Idle_00.png (解析为：动作=Idle, 帧号=00)
            var animGroups = new Dictionary<string, List<(int index, string path)>>();

            // 正则匹配模式
            // Complex: 匹配带横杠的复杂格式，取横杠后和下划线前的部分作为动画名
            var regexComplex = new Regex(@".*-(.*)_(\d+)\.png");
            // Simple: 匹配直接以下划线分隔的格式
            var regexSimple = new Regex(@"(.*)_(\d+)\.png");

            foreach (var fileName in pngFiles)
            {
                string animName = "";
                int frameIndex = 0;
                bool matched = false;

                // 优先尝试复杂匹配
                var match = regexComplex.Match(fileName);
                if (match.Success)
                {
                    animName = NormalizeName(match.Groups[1].Value);
                    frameIndex = int.Parse(match.Groups[2].Value);
                    matched = true;
                }
                else
                {
                    // 备选简单匹配
                    match = regexSimple.Match(fileName);
                    if (match.Success)
                    {
                        animName = NormalizeName(match.Groups[1].Value);
                        frameIndex = int.Parse(match.Groups[2].Value);
                        matched = true;
                    }
                }

                if (matched)
                {
                    string fullPath = folderPath.PathJoin(fileName);
                    if (!animGroups.ContainsKey(animName))
                        animGroups[animName] = new List<(int, string)>();
                    animGroups[animName].Add((frameIndex, fullPath));
                }
            }

            if (animGroups.Count == 0)
            {
                GD.PushWarning("未识别到有效的序列帧命名格式（需满足 *-Anim_Num.png 或 Anim_Num.png）");
                return;
            }

            // --- 阶段 1: 构建 SpriteFrames 资源 ---
            SpriteFrames spriteFrames = new SpriteFrames();
            if (spriteFrames.HasAnimation("default"))
                spriteFrames.RemoveAnimation("default");

            foreach (var anim in animGroups)
            {
                string animName = anim.Key;
                // 按帧序号升序排列
                var frames = anim.Value.OrderBy(f => f.index).ToList();

                spriteFrames.AddAnimation(animName);
                spriteFrames.SetAnimationSpeed(animName, 10.0f); // 默认 10 FPS
                spriteFrames.SetAnimationLoop(animName, true);

                foreach (var frameData in frames)
                {
                    // 加载纹理并添加到动画帧
                    Texture2D texture = GD.Load<Texture2D>(frameData.path);
                    spriteFrames.AddFrame(animName, texture);
                }
            }

            // --- 阶段 2: 保存资源和创建子目录 ---
            // 在当前目录下创建 "AnimatedSprite2D" 子文件夹存放生成结果
            string outputDir = folderPath.PathJoin("AnimatedSprite2D");
            if (!DirAccess.DirExistsAbsolute(outputDir))
            {
                Error makeDirErr = DirAccess.MakeDirAbsolute(outputDir);
                if (makeDirErr != Error.Ok)
                {
                    GD.PrintErr($"创建文件夹失败: {outputDir}, 错误: {makeDirErr}");
                    return;
                }
            }

            // 保存 .tres 资源文件
            string resPath = outputDir.PathJoin("AnimatedSprite2D.tres");
            spriteFrames.ResourcePath = resPath; // 设置资源路径，确保序列化时引用正确
            Error resErr = ResourceSaver.Save(spriteFrames, resPath);

            if (resErr != Error.Ok)
            {
                GD.PrintErr($"资源保存失败: {resErr}");
                return;
            }

            // --- 阶段 3: 创建并保存 AnimatedSprite2D 场景 (.tscn) ---
            var spriteNode = new AnimatedSprite2D();
            spriteNode.Name = "AnimatedSprite2D";
            spriteNode.SpriteFrames = spriteFrames;

            // 关键修复：重置变换属性，确保作为根节点时不会产生位移/缩放警告
            spriteNode.Transform = Transform2D.Identity;
            spriteNode.Position = Vector2.Zero;
            spriteNode.Rotation = 0;
            spriteNode.Scale = Vector2.One;

            spriteNode.Centered = true;           // 图片居中显示
            spriteNode.Offset = Vector2.Zero;     // 重置偏移量

            GD.Print($"[Generator] 正在生成场景: {folderPath} | Position: {spriteNode.Position} | Centered: {spriteNode.Centered}");

            // 设置初始默认播放的动画（优先查找 "idle"）
            if (spriteFrames.HasAnimation("idle"))
                spriteNode.Animation = "idle";
            else if (spriteFrames.GetAnimationNames().Length > 0)
                spriteNode.Animation = spriteFrames.GetAnimationNames()[0];

            // 将节点打包为场景资源
            PackedScene packedScene = new PackedScene();
            Error packErr = packedScene.Pack(spriteNode);

            if (packErr == Error.Ok)
            {
                string scenePath = outputDir.PathJoin("AnimatedSprite2D.tscn");

                // 如果已存在同名场景，先删除，防止合并冲突
                if (FileAccess.FileExists(scenePath))
                {
                    DirAccess.RemoveAbsolute(scenePath);
                }

                Error sceneSaveErr = ResourceSaver.Save(packedScene, scenePath);

                if (sceneSaveErr == Error.Ok)
                {
                    GD.PrintRich($"[color=green]成功生成完整场景: {scenePath}[/color]");
                    GD.PrintRich($"[color=green]成功生成资源文件: {resPath}[/color]");
                }
                else
                {
                    GD.PrintErr($"场景保存失败: {sceneSaveErr}");
                }
            }
            else
            {
                GD.PrintErr($"场景打包失败: {packErr}");
            }

            // 释放临时创建的节点内存
            spriteNode.QueueFree();

            // 如果需要，触发资源系统扫描以便编辑器立即显示新文件
            if (triggerScan)
            {
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }
        }
    }
}
#endif
