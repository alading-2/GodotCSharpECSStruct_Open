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

        // 项目设置键常量（扁平化中文路径）
        private const string SETTING_BATCH_PATHS = "sprite_frames_generator/批量扫描路径";
        private const string SETTING_DEFAULT_FPS = "sprite_frames_generator/默认帧率";
        private const string SETTING_DEFAULT_LOOP = "sprite_frames_generator/默认循环播放";
        private const string SETTING_NAME_MAP = "sprite_frames_generator/名称映射表";

        // 动画名称规范化映射表：用于统一不同美术素材的命名差异
        private static readonly Dictionary<string, string> _nameMap = new()
        {
            { "movement", "run" },
            { "deaded", "dead" },
            { "death", "dead" },
            { "die", "dead" },
        };

        private FolderContextMenuPlugin _contextMenuPlugin;

        /// <summary>
        /// 插件进入编辑器树时调用（启用插件）
        /// </summary>
        public override void _EnterTree()
        {
            // 1. 注册/初始化项目设置
            InitializeProjectSettings();

            // 2. 在编辑器顶部“工具”菜单中添加批量生成项
            AddToolMenuItem(MENU_ITEM_BATCH_NAME, Callable.From(GenerateAllFromPredefinedPaths));

            // 3. 注册文件系统面板的右键菜单插件
            _contextMenuPlugin = new FolderContextMenuPlugin(this);
            AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.Filesystem, _contextMenuPlugin);
        }

        /// <summary>
        /// 初始化和注册插件所需的项目设置
        /// 注意：Godot 的 AddPropertyInfo 目前不支持通过代码动态设置 Tooltip (描述文本)。
        /// 编辑器中的 "无可用描述" 是引擎限制，并非 Bug。请参考 README 获取详细配置说明。
        /// </summary>
        private void InitializeProjectSettings()
        {
            // --- 清理旧版本设置项 (如果存在) ---
            string[] oldKeys = {
                "sprite_frames_generator/config/batch_paths",
                "sprite_frames_generator/config/default_fps",
                "sprite_frames_generator/常规/批量扫描路径",
                "sprite_frames_generator/常规/默认帧率",
                "sprite_frames_generator/常规/默认循环播放",
                "sprite_frames_generator/高级/覆盖策略",
                "sprite_frames_generator/高级/名称映射表"
            };
            foreach (var key in oldKeys)
            {
                if (ProjectSettings.HasSetting(key))
                    ProjectSettings.SetSetting(key, new Variant());
            }

            // --- 注册：批量扫描路径 (String Array) ---
            if (!ProjectSettings.HasSetting(SETTING_BATCH_PATHS))
            {
                string[] defaultPaths = { "res://assets/Unit/ememy", "res://assets/Unit/player" };
                ProjectSettings.SetSetting(SETTING_BATCH_PATHS, defaultPaths);
            }
            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_BATCH_PATHS },
                { "type", (int)Variant.Type.PackedStringArray },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "" }
            });
            ProjectSettings.SetInitialValue(SETTING_BATCH_PATHS, ProjectSettings.GetSetting(SETTING_BATCH_PATHS));

            // --- 注册：默认帧率 (Float) ---
            if (!ProjectSettings.HasSetting(SETTING_DEFAULT_FPS))
            {
                ProjectSettings.SetSetting(SETTING_DEFAULT_FPS, 10.0f);
            }
            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_DEFAULT_FPS },
                { "type", (int)Variant.Type.Float },
                { "hint", (int)PropertyHint.Range },
                { "hint_string", "1,60,0.1" } // Range 提示串必须符合格式，不能包含描述文本
            });
            ProjectSettings.SetInitialValue(SETTING_DEFAULT_FPS, 10.0f);

            // --- 注册：默认循环播放 (Bool) ---
            if (!ProjectSettings.HasSetting(SETTING_DEFAULT_LOOP))
            {
                ProjectSettings.SetSetting(SETTING_DEFAULT_LOOP, true);
            }
            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_DEFAULT_LOOP },
                { "type", (int)Variant.Type.Bool },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "" }
            });
            ProjectSettings.SetInitialValue(SETTING_DEFAULT_LOOP, true);

            // --- 注册：名称映射表 (Dictionary) ---
            if (!ProjectSettings.HasSetting(SETTING_NAME_MAP))
            {
                var defaultMap = new Godot.Collections.Dictionary();
                foreach (var kp in _nameMap) defaultMap[kp.Key] = kp.Value;
                ProjectSettings.SetSetting(SETTING_NAME_MAP, defaultMap);
            }
            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_NAME_MAP },
                { "type", (int)Variant.Type.Dictionary },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "" }
            });
            ProjectSettings.SetInitialValue(SETTING_NAME_MAP, ProjectSettings.GetSetting(SETTING_NAME_MAP));

            // 保存更改
            ProjectSettings.Save();
        }

        /// <summary>
        /// 插件退出编辑器树时调用（禁用插件）
        /// </summary>
        public override void _ExitTree()
        {
            // 清理菜单项
            RemoveToolMenuItem(MENU_ITEM_BATCH_NAME);
            // 卸载右键菜单插件
            RemoveContextMenuPlugin(_contextMenuPlugin);
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
            // 从项目设置中获取批量路径
            string[] batchPaths = ProjectSettings.GetSetting(SETTING_BATCH_PATHS).AsStringArray();

            foreach (var basePath in batchPaths)
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
        /// 动画名称规范化：转换大小写并应用名称映射表
        /// </summary>
        private string NormalizeName(string rawName)
        {
            string cleanName = rawName.ToLower().Trim();

            // 从项目设置读取动态映射表
            if (ProjectSettings.HasSetting(SETTING_NAME_MAP))
            {
                var nameMap = ProjectSettings.GetSetting(SETTING_NAME_MAP).AsGodotDictionary();
                if (nameMap.ContainsKey(cleanName))
                {
                    return nameMap[cleanName].AsString();
                }
            }

            return cleanName;
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

            // --- 读取增强配置 ---
            float defaultFps = (float)ProjectSettings.GetSetting(SETTING_DEFAULT_FPS).AsDouble();
            bool defaultLoop = ProjectSettings.GetSetting(SETTING_DEFAULT_LOOP).AsBool();

            // 逻辑：兼容多种常见的序列帧命名格式
            // 1. Spine 导出格式示例: hero_guangfa-Attack1_00.png (解析为：动作=Attack1, frameIndex=0)
            var animGroups = new Dictionary<string, List<(int index, string path)>>();

            // 正则匹配模式
            var regexComplex = new Regex(@".*-(.*)_(\d+)\.png");
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
                GD.PushWarning("未识别到有效的序列帧命名格式");
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

                spriteFrames.AddAnimation(animName);    // 添加动画
                spriteFrames.SetAnimationSpeed(animName, defaultFps); // 使用设置中的帧率
                spriteFrames.SetAnimationLoop(animName, defaultLoop); // 使用配置中的循环播放设置

                foreach (var frameData in frames)
                {
                    // 加载纹理并添加到动画帧
                    var texture = GD.Load<Texture2D>(frameData.path);
                    if (texture != null)
                        spriteFrames.AddFrame(animName, texture);
                }
            }

            // --- 阶段 2: 保存 SpriteFrames 资源 (.tres) ---
            // 在当前目录下创建 "AnimatedSprite2D" 子文件夹存放生成结果
            string subFolder = "AnimatedSprite2D";
            string subFolderPath = folderPath.PathJoin(subFolder);
            if (!DirAccess.DirExistsAbsolute(subFolderPath))
            {
                Error makeDirErr = DirAccess.MakeDirAbsolute(subFolderPath);
                if (makeDirErr != Error.Ok)
                {
                    GD.PrintErr($"创建文件夹失败: {subFolderPath}, 错误: {makeDirErr}");
                    return;
                }
            }

            string resPath = subFolderPath.PathJoin("AnimatedSprite2D.tres");
            spriteFrames.ResourcePath = resPath; // 设置资源路径，确保序列化时引用正确
            Error resErr = ResourceSaver.Save(spriteFrames, resPath);

            if (resErr != Error.Ok)
            {
                GD.PrintErr($"资源保存失败: {resErr}");
                return;
            }

            // --- 阶段 3: 创建并保存 AnimatedSprite2D 场景 (.tscn) ---
            // 目标路径：在子文件夹中生成 AnimatedSprite2D.tscn
            string scenePath = subFolderPath.PathJoin("AnimatedSprite2D.tscn");
            AnimatedSprite2D? spriteNode = null;
            bool isNewScene = true;

            // -------------------------------------------------------------------------
            // 智能更新逻辑 (Smart Update Logic)
            // 目的：如果场景已存在，我们希望保留用户手动修改过的属性（如 Position, Scale, Script 等），
            //       仅更新其中的 SpriteFrames 引用。
            // -------------------------------------------------------------------------
            if (FileAccess.FileExists(scenePath))
            {
                try
                {
                    // 加载现有的场景资源
                    var existingScene = GD.Load<PackedScene>(scenePath);
                    if (existingScene != null)
                    {
                        // 实例化场景以检查其根节点类型
                        var instance = existingScene.Instantiate();
                        if (instance is AnimatedSprite2D existingSprite)
                        {
                            // 成功获取到现有的 AnimatedSprite2D 节点
                            spriteNode = existingSprite;
                            isNewScene = false; // 标记为非新场景
                            GD.PrintRich($"[color=cyan][Generator] 智能更新现有场景: {folderPath} (属性已保留)[/color]");
                        }
                        else
                        {
                            // 如果根节点类型不对（不是 AnimatedSprite2D），则无法保留，只能销毁
                            instance.Free();
                        }
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[Generator] 加载现有场景失败，将回退到创建新场景: {e.Message}");
                }
            }

            // -------------------------------------------------------------------------
            // 场景创建逻辑 (New Scene Creation)
            // 如果上述加载失败或文件不存在，则创建一个全新的 AnimatedSprite2D 节点
            // -------------------------------------------------------------------------
            if (spriteNode == null)
            {
                spriteNode = new AnimatedSprite2D();
                spriteNode.Name = "AnimatedSprite2D";

                // 仅对新创建的节点应用默认变换，避免覆盖现有场景的自定义变换
                spriteNode.Transform = Transform2D.Identity;
                spriteNode.Centered = true;

                GD.Print($"[Generator] 创建全新场景: {folderPath}");
            }

            // -------------------------------------------------------------------------
            // 统一更新逻辑 (Unified Update)
            // 无论新旧，强制更新 SpriteFrames 引用指向最新的 .tres 资源
            // -------------------------------------------------------------------------
            spriteNode.SpriteFrames = spriteFrames;

            // 检查当前设置的动画名是否依然有效
            // （例如原场景记录播放 "attack"，但新资源中可能只有 "idle"）
            if (!spriteFrames.HasAnimation(spriteNode.Animation))
            {
                // 如果当前动画无效，尝试智能回退到 'idle' 或列表中的第一个动画
                if (spriteFrames.HasAnimation("idle"))
                    spriteNode.Animation = "idle";
                else if (spriteFrames.GetAnimationNames().Length > 0)
                    spriteNode.Animation = spriteFrames.GetAnimationNames()[0];
            }

            // -------------------------------------------------------------------------
            // 保存场景 (Save Scene)
            // -------------------------------------------------------------------------
            // 将节点及其属性“打包”进一个新的 PackedScene 对象
            PackedScene packedScene = new PackedScene();
            Error packErr = packedScene.Pack(spriteNode);

            if (packErr == Error.Ok)
            {
                // 保存 PackedScene 到磁盘
                // 注意：如果文件已存在，ResourceSaver.Save 会直接覆盖它。
                // 这正是我们想要的：用包含最新 SpriteFrames 引用（但保留了旧属性）的 PackardScene 覆盖旧文件。
                Error sceneSaveErr = ResourceSaver.Save(packedScene, scenePath);

                if (sceneSaveErr == Error.Ok)
                {
                    GD.PrintRich($"[color=green]成功{(isNewScene ? "创建" : "更新")}场景: {scenePath}[/color]");
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

            // 释放节点内存
            // 注意：spriteNode (无论是 new 出来的还是 Instantiate 出来的) 此时都是悬空节点，需要手动释放
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
