#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Slime.Addons
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

        // 需要循环播放的动画名称集合（其他动画默认不循环）
        private static readonly HashSet<string> _loopAnimations = new()
        {
            "idle",
            "run",
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
                string[] defaultPaths = { "res://assets/Unit/Enemy", "res://assets/Unit/Player" }; // Fixed typo: ememy -> Enemy
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
        /// 批量处理预设路径下的所有角色文件夹（递归扫描）
        /// </summary>
        private void GenerateAllFromPredefinedPaths()
        {
            int totalGenerated = 0;
            string[] batchPaths = ProjectSettings.GetSetting(SETTING_BATCH_PATHS).AsStringArray();
            var validFolders = new List<string>();

            foreach (var basePath in batchPaths)
            {
                if (!DirAccess.DirExistsAbsolute(basePath))
                {
                    GD.Print($"[SpriteFramesGenerator] 路径不存在: {basePath}");
                    continue;
                }

                // 递归扫描所有子文件夹
                ScanFolderRecursively(basePath, validFolders);
            }

            if (validFolders.Count > 0)
            {
                foreach (var folder in validFolders)
                {
                    // 批量模式下暂不每次都触发资源扫描，最后统一触发
                    GenerateSpriteFrames(folder, false);
                    totalGenerated++;
                }

                EditorInterface.Singleton.GetResourceFilesystem().Scan();
                GD.PrintRich($"[color=cyan]批量生成完成！共处理 {totalGenerated} 个角色目录。[/color]");
            }
            else
            {
                GD.PrintRich("[color=yellow]未在预设路径下发现可处理的序列帧资源。[/color]");
            }
        }

        /// <summary>
        /// 递归查找包含有效序列帧的文件夹
        /// </summary>
        private void ScanFolderRecursively(string dirPath, List<string> results)
        {
            // 1. 检查当前文件夹是否包含有效序列帧
            if (HasPngFiles(dirPath))
            {
                // 预检：如果能提取出动画分组，则认为是一个有效的角色文件夹
                var groups = FindSpriteSequences(dirPath);
                if (groups != null && groups.Count > 0)
                {
                    results.Add(dirPath);
                    // 如果当前文件夹已经是角色文件夹（包含序列帧），
                    // 通常不需要再深入扫描它的子文件夹（除非是分层结构），
                    // 但为了保险起见，这里选择继续深入，但要避免生成的 AnimatedSprite2D 文件夹。
                }
            }

            // 2. 遍历子文件夹
            using var dir = DirAccess.Open(dirPath);
            if (dir == null) return;

            dir.ListDirBegin();
            string subDirName = dir.GetNext();
            while (subDirName != "")
            {
                if (dir.CurrentIsDir() && !subDirName.StartsWith("."))
                {
                    // 跳过插件生成的输出目录，避免递归死循环或误判
                    if (subDirName != "AnimatedSprite2D")
                    {
                        ScanFolderRecursively(dirPath.PathJoin(subDirName), results);
                    }
                }
                subDirName = dir.GetNext();
            }
            dir.ListDirEnd();
        }

        /// <summary>
        /// 解析文件夹中的序列帧并分组
        /// </summary>
        private Dictionary<string, List<(int index, string path)>> FindSpriteSequences(string folderPath)
        {
            var animGroups = new Dictionary<string, List<(int index, string path)>>();

            using var dir = DirAccess.Open(folderPath);
            if (dir == null) return animGroups;

            var regexComplex = new Regex(@".*-(.*)_(\d+)\.png");
            var regexSimple = new Regex(@"(.*)_(\d+)\.png");

            foreach (var fileName in dir.GetFiles())
            {
                if (!fileName.EndsWith(".png") || fileName.EndsWith(".import")) continue;

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
            return animGroups;
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

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && fileName.EndsWith(".png") && !fileName.EndsWith(".import"))
                {
                    dir.ListDirEnd();
                    return true;
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
            return false;
        }

        /// <summary>
        /// 核心逻辑：扫描文件夹并生成 SpriteFrames 资源和场景
        /// </summary>
        /// <param name="folderPath">目标文件夹路径</param>
        /// <param name="triggerScan">完成后是否立即触发编辑器资源扫描</param>
        private void GenerateSpriteFrames(string folderPath, bool triggerScan = true)
        {
            // 复用解析逻辑
            var animGroups = FindSpriteSequences(folderPath);

            if (animGroups.Count == 0)
            {
                // 如果是手动选择单个文件夹触发，才显示警告。批量模式下这只是“未命中”的文件夹。
                // 由于 GenerateSpriteFrames 现在既被单选调用也被批量调用，
                // 我们通过 triggerScan 参数（单选为 true，批量为此 false）简单区分一下日志级别，或者干脆在这里如果不匹配就静默退出。
                // 考虑到用户单选时需要反馈，保留警告。但在批量扫描中，我们在 ScanFolderRecursively 已经做过预检，
                // 所以理论上批量调用时 animGroups 不会为空。
                if (triggerScan)
                    GD.PushWarning($"[{folderPath}] 未识别到有效的序列帧命名格式 (示例: attack_0.png)");
                return;
            }

            GD.Print($"正在处理文件夹: {folderPath}");

            // 获取当前文件夹名称，用作资源名和节点名
            // 例如 .../Unit/Enemy/豺狼人 -> 豺狼人
            string folderName = folderPath.GetFile();
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = "AnimatedSprite2D"; // 兜底
            }

            // --- 读取增强配置 ---
            float defaultFps = (float)ProjectSettings.GetSetting(SETTING_DEFAULT_FPS).AsDouble();
            bool defaultLoop = ProjectSettings.GetSetting(SETTING_DEFAULT_LOOP).AsBool();

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
                // 只有白名单中的动画循环播放（idle/run/castingidle），其他动画播放一次
                bool shouldLoop = _loopAnimations.Contains(animName);
                spriteFrames.SetAnimationLoop(animName, shouldLoop);

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

            // 使用文件夹名作为资源文件名: "豺狼人.tres"
            string resPath = subFolderPath.PathJoin($"{folderName}.tres");
            spriteFrames.ResourcePath = resPath; // 设置资源路径，确保序列化时引用正确
            Error resErr = ResourceSaver.Save(spriteFrames, resPath);

            if (resErr != Error.Ok)
            {
                GD.PrintErr($"资源保存失败: {resErr}");
                return;
            }

            // --- 阶段 3: 创建并保存 AnimatedSprite2D 场景 (.tscn) ---
            // 目标路径：在子文件夹中生成 "豺狼人.tscn"
            string scenePath = subFolderPath.PathJoin($"{folderName}.tscn");
            AnimatedSprite2D? spriteNode = null;
            bool isNewScene = true;

            // -------------------------------------------------------------------------
            // 智能更新逻辑 (Smart Update Logic)
            // -------------------------------------------------------------------------
            if (FileAccess.FileExists(scenePath))
            {
                try
                {
                    var existingScene = GD.Load<PackedScene>(scenePath);
                    if (existingScene != null)
                    {
                        var instance = existingScene.Instantiate();
                        if (instance is AnimatedSprite2D existingSprite)
                        {
                            spriteNode = existingSprite;
                            isNewScene = false;
                            GD.PrintRich($"[color=cyan][Generator] 智能更新现有场景: {folderPath} (属性已保留)[/color]");
                        }
                        else
                        {
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
            // -------------------------------------------------------------------------
            if (spriteNode == null)
            {
                spriteNode = new AnimatedSprite2D();
                // 设置节点名称为文件夹名，方便调试 (如 "豺狼人")
                spriteNode.Name = folderName;
                spriteNode.Transform = Transform2D.Identity;
                spriteNode.Centered = true;

                GD.Print($"[Generator] 创建全新场景: {folderPath}");
            }
            else
            {
                // 即使是旧场景，也强制更新节点名称，确保一致性
                if (spriteNode.Name != folderName)
                {
                    spriteNode.Name = folderName;
                }
            }

            // -------------------------------------------------------------------------
            // 统一更新逻辑 (Unified Update)
            // -------------------------------------------------------------------------
            spriteNode.SpriteFrames = spriteFrames;

            // 检查当前设置的动画名是否依然有效
            if (!spriteFrames.HasAnimation(spriteNode.Animation))
            {
                if (spriteFrames.HasAnimation("idle"))
                    spriteNode.Animation = "idle";
                else if (spriteFrames.GetAnimationNames().Length > 0)
                    spriteNode.Animation = spriteFrames.GetAnimationNames()[0];
            }

            // -------------------------------------------------------------------------
            // 保存场景 (Save Scene)
            // -------------------------------------------------------------------------
            PackedScene packedScene = new PackedScene();
            Error packErr = packedScene.Pack(spriteNode);

            if (packErr == Error.Ok)
            {
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
            spriteNode.QueueFree();

            // 触发资源扫描
            if (triggerScan)
            {
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }
        }
    }
}
#endif
