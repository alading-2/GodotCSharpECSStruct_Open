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
        private const string MENU_ITEM_NAME = "Generate SpriteFrames (Auto)";

        public override void _EnterTree()
        {
            // 添加工具菜单项
            // 在 Godot 4 中，直接使用 EditorInterface.Singleton.GetSelectedPaths() 获取选中项更可靠
            AddToolMenuItem(MENU_ITEM_NAME, Callable.From(GenerateFromSelection));
        }

        public override void _ExitTree()
        {
            RemoveToolMenuItem(MENU_ITEM_NAME);
        }

        private void GenerateFromSelection()
        {
            // 获取当前选中的路径
            var selectedPaths = EditorInterface.Singleton.GetSelectedPaths();
            if (selectedPaths.Length == 0)
            {
                GD.PrintErr("请先在文件系统面板中选中一个文件夹！");
                return;
            }

            string path = selectedPaths[0];

            // 确保是文件夹
            var dir = DirAccess.Open(path);
            if (dir == null)
            {
                // 可能是选中了文件，尝试获取其父目录
                if (FileAccess.FileExists(path))
                {
                    path = path.GetBaseDir();
                    dir = DirAccess.Open(path);
                }
            }

            if (dir != null)
            {
                GenerateSpriteFrames(path);
            }
            else
            {
                GD.PrintErr($"无法访问路径: {path}");
            }
        }

        private void GenerateSpriteFrames(string folderPath)
        {
            GD.Print($"正在扫描文件夹: {folderPath}");
            var dir = DirAccess.Open(folderPath);
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
                    animName = match.Groups[1].Value.ToLower();
                    frameIndex = int.Parse(match.Groups[2].Value);
                    matched = true;
                }
                else
                {
                    match = regexSimple.Match(fileName);
                    if (match.Success)
                    {
                        animName = match.Groups[1].Value.ToLower();
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

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
    }
}
#endif
