#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BrotatoMy.Addons
{
    [Tool]
    public partial class SpriteFramesGeneratorPlugin : EditorPlugin
    {
        private const string MENU_ITEM_NAME = "Generate SpriteFrames (Single/Selection)";
        private const string MENU_ITEM_BATCH_NAME = "Generate All SpriteFrames (Batch)";

        // 预设的自动扫描路径
        private static readonly string[] BATCH_PATHS = { "res://assets/character/ememy", "res://assets/character/player" };

        // 规范化映射表：[原始名称] -> [标准名称]
        private static readonly Dictionary<string, string> _nameMap = new()
        {
            { "movement", "run" },
            { "deaded", "dead" },
            { "death", "dead" },
            { "die", "dead" },
        };

        private string NormalizeName(string rawName)
        {
            rawName = rawName.ToLower();
            return _nameMap.GetValueOrDefault(rawName, rawName);
        }

        private FolderContextMenuPlugin _contextMenuPlugin;

        public override void _EnterTree()
        {
            // 1. 顶部工具菜单仅保留批量生成
            AddToolMenuItem(MENU_ITEM_BATCH_NAME, Callable.From(GenerateAllFromPredefinedPaths));

            // 2. 注册文件系统右键菜单插件
            _contextMenuPlugin = new FolderContextMenuPlugin(this);
            AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.Filesystem, _contextMenuPlugin);
        }

        public override void _ExitTree()
        {
            RemoveToolMenuItem(MENU_ITEM_BATCH_NAME);
            if (_contextMenuPlugin != null)
            {
                RemoveContextMenuPlugin(_contextMenuPlugin);
                _contextMenuPlugin = null;
            }
        }

        public void GenerateFromPaths(string[] paths)
        {
            if (paths == null || paths.Length == 0) return;

            string path = paths[0];

            // 如果选中了文件，则获取其所在的父文件夹
            if (FileAccess.FileExists(path))
            {
                path = path.GetBaseDir();
            }

            // 只要文件夹存在就尝试生成
            if (DirAccess.DirExistsAbsolute(path))
            {
                GenerateSpriteFrames(path, true);
            }
            else
            {
                GD.PrintErr($"路径不存在或无法访问: {path}");
            }
        }

        private partial class FolderContextMenuPlugin : EditorContextMenuPlugin
        {
            private SpriteFramesGeneratorPlugin _plugin;

            public FolderContextMenuPlugin() { }
            public FolderContextMenuPlugin(SpriteFramesGeneratorPlugin plugin)
            {
                _plugin = plugin;
            }

            public override void _PopupMenu(string[] paths)
            {
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
                    if (dir.CurrentIsDir() && !subDirName.StartsWith("."))
                    {
                        string fullSubPath = basePath.PathJoin(subDirName);
                        // 检查该目录下是否有 PNG
                        if (HasPngFiles(fullSubPath))
                        {
                            GenerateSpriteFrames(fullSubPath, false); // 不在每一步都扫描文件系统
                            totalGenerated++;
                        }
                    }
                    subDirName = dir.GetNext();
                }
                dir.ListDirEnd();
            }

            if (totalGenerated > 0)
            {
                EditorInterface.Singleton.GetResourceFilesystem().Scan(); // 批量完成后统一扫描
                GD.PrintRich($"[color=cyan]批量生成完成！共处理 {totalGenerated} 个角色目录。[/color]");
            }
            else
            {
                GD.PrintRich("[color=yellow]未在预设路径下发现可处理的资源。[/color]");
            }
        }

        private bool HasPngFiles(string path)
        {
            using var dir = DirAccess.Open(path);
            if (dir == null) return false;

            foreach (var file in dir.GetFiles())
            {
                if (file.EndsWith(".png") && !file.EndsWith(".import")) return true;
            }
            return false;
        }


        private void GenerateSpriteFrames(string folderPath, bool triggerScan = true)
        {
            GD.Print($"正在扫描文件夹: {folderPath}");
            using var dir = DirAccess.Open(folderPath);
            string[] files = dir.GetFiles();
            var pngFiles = files.Where(f => f.EndsWith(".png") && !f.EndsWith(".import")).ToList();

            if (pngFiles.Count == 0)
            {
                GD.PushWarning("该文件夹下没有 PNG 文件。");
                return;
            }

            // 逻辑优化：兼容更多命名格式
            // 1. Spine格式: hero_guangfa-Attack1_00.png (取 - 后面, _ 前面)
            // 2. 简单格式: Idle_00.png (取 _ 前面)
            var animGroups = new Dictionary<string, List<(int index, string path)>>();

            // 优先尝试复杂匹配: Prefix-AnimName_Index.png
            var regexComplex = new Regex(@".*-(.*)_(\d+)\.png");
            // 备用匹配: AnimName_Index.png
            var regexSimple = new Regex(@"(.*)_(\d+)\.png");

            foreach (var fileName in pngFiles)
            {
                string animName = "";
                int frameIndex = 0;
                bool matched = false;

                var match = regexComplex.Match(fileName);
                if (match.Success)
                {
                    animName = NormalizeName(match.Groups[1].Value);
                    frameIndex = int.Parse(match.Groups[2].Value);
                    matched = true;
                }
                else
                {
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

            // 构建 SpriteFrames
            SpriteFrames spriteFrames = new SpriteFrames();
            if (spriteFrames.HasAnimation("default"))
                spriteFrames.RemoveAnimation("default");

            foreach (var anim in animGroups)
            {
                string animName = anim.Key;
                var frames = anim.Value.OrderBy(f => f.index).ToList();

                spriteFrames.AddAnimation(animName);
                spriteFrames.SetAnimationSpeed(animName, 10.0f); // 默认 10 FPS
                spriteFrames.SetAnimationLoop(animName, true);

                foreach (var frameData in frames)
                {
                    Texture2D texture = GD.Load<Texture2D>(frameData.path);
                    spriteFrames.AddFrame(animName, texture);
                }
            }

            // 1. 创建子文件夹 AnimatedSprite2D
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

            // 2. 保存 SpriteFrames 资源 (.tres)
            string resPath = outputDir.PathJoin("AnimatedSprite2D.tres");
            spriteFrames.ResourcePath = resPath; // 设置路径以便打包场景时能正确引用
            Error resErr = ResourceSaver.Save(spriteFrames, resPath);

            if (resErr != Error.Ok)
            {
                GD.PrintErr($"资源保存失败: {resErr}");
                return;
            }

            // 3. 创建并保存完整的 AnimatedSprite2D 场景 (.tscn)
            var spriteNode = new AnimatedSprite2D();
            spriteNode.Name = "AnimatedSprite2D";
            spriteNode.SpriteFrames = spriteFrames;

            // 核心修复：强制使用 Identity 变换，彻底消除根节点警告
            spriteNode.Transform = Transform2D.Identity;
            spriteNode.Position = Vector2.Zero;   // 双重保险
            spriteNode.Rotation = 0;
            spriteNode.Scale = Vector2.One;

            spriteNode.Centered = true;           // 确保图片居中
            spriteNode.Offset = Vector2.Zero;     // 清空手动偏移

            GD.Print($"[Generator] 正在生成场景: {folderPath} | Position: {spriteNode.Position} | Centered: {spriteNode.Centered}");

            // 设置默认动画（优先选择 idle）
            if (spriteFrames.HasAnimation("idle"))
                spriteNode.Animation = "idle";
            else if (spriteFrames.GetAnimationNames().Length > 0)
                spriteNode.Animation = spriteFrames.GetAnimationNames()[0];

            PackedScene packedScene = new PackedScene();
            Error packErr = packedScene.Pack(spriteNode);

            if (packErr == Error.Ok)
            {
                string scenePath = outputDir.PathJoin("AnimatedSprite2D.tscn");

                // 删除旧文件，确保没有残留数据
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

            // 释放临时节点
            spriteNode.QueueFree();

            if (triggerScan)
            {
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }
        }
    }
}
#endif
