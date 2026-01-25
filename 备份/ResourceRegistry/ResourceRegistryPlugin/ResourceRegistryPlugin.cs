#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace BrotatoMy.Addons
{
    /// <summary>
    /// 资源注册表自动更新插件
    /// 功能：自动化扫描项目中的 .tscn 场景文件并更新 ResourceManagement.tscn
    /// </summary>
    [Tool]
    public partial class ResourceManagementPlugin : EditorPlugin
    {
        // 使用项目统一的日志工具
        private static readonly Log _log = new("ResourceManagementPlugin");

        // 菜单项名称
        private const string MENU_NAME = "Update Resource Registry";

        // 项目设置键名 (ProjectSettings)
        private const string SETTING_SCAN_PATHS = "resource_registry/config/scan_paths";
        private const string SETTING_EXCLUDE_PATHS = "resource_registry/config/exclude_paths";
        private const string SETTING_REGISTRY_PATH = "resource_registry/config/registry_file_path";
        private const string SETTING_AUTO_UPDATE = "resource_registry/config/auto_update_on_startup"; // 新增配置项

        // 默认配置值
        private readonly string[] DEFAULT_SCAN_PATHS = { "res://assets", "res://Src/UI", "res://Src/ECS/Entity", "res://Src/ECS/Component" };
        private readonly string[] DEFAULT_EXCLUDE_PATHS = { "res://addons", "res://.godot", "res://Src/Test", "res://Src/Tools" };
        private const string DEFAULT_REGISTRY_PATH = "res://Data/ResourceManagement/ResourceManagement.tscn";
        private const bool DEFAULT_AUTO_UPDATE = true;

        /// <summary>
        /// 插件启用时的初始化
        /// </summary>
        public override void _EnterTree()
        {
            InitializeProjectSettings();
            // 在编辑器“项目 -> 工具”菜单下添加按钮
            AddToolMenuItem(MENU_NAME, Callable.From(UpdateRegistry));

            // 检查自动更新配置
            if (ProjectSettings.GetSetting(SETTING_AUTO_UPDATE, DEFAULT_AUTO_UPDATE).AsBool())
            {
                _log.Info("检测到自动更新配置开启，正在执行启动时扫描...");
                // 使用 CallDeferred 确保编辑器完全加载后再执行，避免潜在的初始化竞争
                CallDeferred(nameof(UpdateRegistry));
            }
        }

        /// <summary>
        /// 插件禁用时的清理
        /// </summary>
        public override void _ExitTree()
        {
            RemoveToolMenuItem(MENU_NAME);
        }

        /// <summary>
        /// 初始化项目设置，如果不存在则创建默认值
        /// </summary>
        private void InitializeProjectSettings()
        {
            // 扫描路径配置 (PackedStringArray)
            if (!ProjectSettings.HasSetting(SETTING_SCAN_PATHS))
            {
                ProjectSettings.SetSetting(SETTING_SCAN_PATHS, DEFAULT_SCAN_PATHS);
            }
            else
            {
                // 如果已存在，检查是否由于版本更迭漏掉了新的路径
                var currentPaths = ProjectSettings.GetSetting(SETTING_SCAN_PATHS).AsStringArray().ToList();
                bool changed = false;
                foreach (var p in DEFAULT_SCAN_PATHS)
                {
                    if (!currentPaths.Contains(p))
                    {
                        currentPaths.Add(p);
                        changed = true;
                    }
                }
                if (changed) ProjectSettings.SetSetting(SETTING_SCAN_PATHS, currentPaths.ToArray());
            }

            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_SCAN_PATHS },
                { "type", (int)Variant.Type.PackedStringArray },
                { "hint", (int)PropertyHint.None }
            });
            ProjectSettings.SetInitialValue(SETTING_SCAN_PATHS, DEFAULT_SCAN_PATHS);

            // 排除路径配置 (PackedStringArray)
            if (!ProjectSettings.HasSetting(SETTING_EXCLUDE_PATHS))
            {
                ProjectSettings.SetSetting(SETTING_EXCLUDE_PATHS, DEFAULT_EXCLUDE_PATHS);
            }
            else
            {
                var currentExcludes = ProjectSettings.GetSetting(SETTING_EXCLUDE_PATHS).AsStringArray().ToList();
                bool changed = false;
                foreach (var p in DEFAULT_EXCLUDE_PATHS)
                {
                    if (!currentExcludes.Contains(p))
                    {
                        currentExcludes.Add(p);
                        changed = true;
                    }
                }
                if (changed) ProjectSettings.SetSetting(SETTING_EXCLUDE_PATHS, currentExcludes.ToArray());
            }

            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_EXCLUDE_PATHS },
                { "type", (int)Variant.Type.PackedStringArray },
                { "hint", (int)PropertyHint.None }
            });
            ProjectSettings.SetInitialValue(SETTING_EXCLUDE_PATHS, DEFAULT_EXCLUDE_PATHS);

            // 注册表文件路径配置 (String)
            if (!ProjectSettings.HasSetting(SETTING_REGISTRY_PATH))
            {
                ProjectSettings.SetSetting(SETTING_REGISTRY_PATH, DEFAULT_REGISTRY_PATH);
            }

            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_REGISTRY_PATH },
                { "type", (int)Variant.Type.String },
                { "hint", (int)PropertyHint.File },
                { "hint_string", "*.tscn" }
            });
            ProjectSettings.SetInitialValue(SETTING_REGISTRY_PATH, DEFAULT_REGISTRY_PATH);

            // 自动更新配置 (Bool)
            // 强制启用：为了响应用户需求，确保默认开启。如果之前被保存为 false，这里会将其覆盖为 true。
            // 在实际产品中可能需要更温和的迁移策略，但这里用户明确要求“默认打开”。
            ProjectSettings.SetSetting(SETTING_AUTO_UPDATE, true);

            ProjectSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_AUTO_UPDATE },
                { "type", (int)Variant.Type.Bool },
                { "hint", (int)PropertyHint.None }
            });
            ProjectSettings.SetInitialValue(SETTING_AUTO_UPDATE, DEFAULT_AUTO_UPDATE);

            ProjectSettings.Save();
        }

        /// <summary>
        /// 核心方法：执行注册表更新
        /// </summary>
        private void UpdateRegistry()
        {
            // 1. 获取注册表文件路径并验证
            string registryPath = ProjectSettings.GetSetting(SETTING_REGISTRY_PATH).AsString();

            // 修正：如果路径是 UID 格式 (uid://...)，转换为实际文件路径
            if (registryPath.StartsWith("uid://"))
            {
                long uid = ResourceUid.TextToId(registryPath);
                // 注意：ResourceUid.Singleton 是 Godot 单例的访问方式
                if (ResourceUid.Singleton.HasId(uid))
                {
                    registryPath = ResourceUid.Singleton.GetIdPath(uid);
                }
                else
                {
                    _log.Error($"无效的资源 UID: {registryPath}");
                    return;
                }
            }

            if (!FileAccess.FileExists(registryPath))
            {
                _log.Error($"注册表文件不存在: {registryPath}");
                return;
            }

            // 2. 加载 ResourceManagement 场景
            var registryScene = GD.Load<PackedScene>(registryPath);
            if (registryScene == null)
            {
                _log.Error($"无法加载注册表场景资源: {registryPath}");
                return;
            }

            // 3. 实例化场景并检查根节点类型
            var rootNode = registryScene.Instantiate();
            if (rootNode is not ResourceManagement registry)
            {
                var script = rootNode.GetScript().As<CSharpScript>();
                _log.Error($"场景 '{registryPath}' 的根节点不是 ResourceManagement 类型");
                _log.Error($"当前 C# 类型: {rootNode.GetType().FullName}");
                _log.Error($"挂载脚本: {(script != null ? script.ResourcePath : "无")}");

                rootNode.Free();
                return;
            }

            // 4. 读取扫描配置
            string[] scanPaths = ProjectSettings.GetSetting(SETTING_SCAN_PATHS).AsStringArray();
            string[] excludePaths = ProjectSettings.GetSetting(SETTING_EXCLUDE_PATHS).AsStringArray();

            var foundResources = new List<ResourceEntry>();
            var foundKeys = new HashSet<string>();
            var visitedFiles = new HashSet<string>(); // 避免不同扫描路径包含相同文件导致的重复

            // 5. 执行递归扫描
            foreach (string path in scanPaths)
            {
                if (!DirAccess.DirExistsAbsolute(path))
                {
                    _log.Warn($"配置的扫描路径不存在，已跳过: {path}");
                    continue;
                }
                ScanDirectory(path, excludePaths, foundResources, foundKeys, visitedFiles);
            }

            // 6. 更新数据：清空原列表并填充新扫描到的资源
            // 注意：这里采用完全替换策略以确保注册表与文件系统同步
            int oldCount = registry.Resources.Count;
            registry.Resources.Clear();
            foreach (var entry in foundResources)
            {
                registry.Resources.Add(entry);
            }

            // 7. 将修改后的节点重新打包并保存
            var newPackedScene = new PackedScene();
            Error packErr = newPackedScene.Pack(rootNode);
            if (packErr != Error.Ok)
            {
                _log.Error($"场景打包失败: {packErr}");
                rootNode.Free();
                return;
            }

            Error saveErr = ResourceSaver.Save(newPackedScene, registryPath);

            // 8. 处理结果与清理
            if (saveErr == Error.Ok)
            {
                _log.Success($"注册表自动更新完成! 共处理 {foundResources.Count} 个资源 (原有 {oldCount} 个)");
                // 触发文件系统扫描，确保编辑器视图同步
                EditorInterface.Singleton.GetResourceFilesystem().Scan();
            }
            else
            {
                _log.Error($"保存注册表文件失败: {saveErr}");
            }

            rootNode.Free(); // 释放实例化出的节点内存
        }

        /// <summary>
        /// 递归扫描目录下的 .tscn 文件
        /// </summary>
        private void ScanDirectory(string dirPath, string[] excludePaths, List<ResourceEntry> foundResources, HashSet<string> foundKeys, HashSet<string> visitedFiles)
        {
            if (!DirAccess.DirExistsAbsolute(dirPath)) return;

            using var dir = DirAccess.Open(dirPath);
            if (dir == null) return;

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (fileName == "." || fileName == "..")
                {
                    fileName = dir.GetNext();
                    continue;
                }

                string fullPath = dirPath.PathJoin(fileName);

                // 排除路径检查
                bool isExcluded = excludePaths.Any(ex => fullPath.StartsWith(ex));
                if (!isExcluded)
                {
                    if (dir.CurrentIsDir())
                    {
                        // 递归扫描
                        ScanDirectory(fullPath, excludePaths, foundResources, foundKeys, visitedFiles);
                    }
                    else if (fileName.EndsWith(".tscn"))
                    {
                        // 唯一性检查
                        if (!visitedFiles.Contains(fullPath))
                        {
                            visitedFiles.Add(fullPath);

                            // 防止自我注册 (排除 ResourceManagement 本身)
                            if (!fullPath.EndsWith("ResourceManagement.tscn"))
                            {
                                RegisterResource(fullPath, foundResources, foundKeys);
                            }
                        }
                    }
                }

                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }

        /// <summary>
        /// 将单个项目注册为 ResourceEntry
        /// </summary>
        private void RegisterResource(string filePath, List<ResourceEntry> resources, HashSet<string> keys)
        {
            // 资源 Key：取文件名（不含扩展名）
            string name = filePath.GetFile().GetBaseName();

            // 唯一性检查：如果出现重名 Key，打印红色错误并跳过
            if (keys.Contains(name))
            {
                _log.Error($"[自动注册失败] 发现重复的资源 Key: '{name}' (路径: {filePath})。该资源已被跳过，请检查并统一资源命名。");
                return;
            }

            // 分类识别逻辑：根据路径关键字启发式判断
            ResourceCategory category = GetCategoryFromPath(filePath);

            // 创建注册条目对象
            var entry = new ResourceEntry();
            entry.Name = name;
            entry.Category = category;

            _log.Debug($"正在加载资源: {name} ({filePath}) ..."); // 添加日志以便定位卡死位置
            try
            {
                entry.Data = GD.Load<Resource>(filePath); // 加载资源引用
            }
            catch (System.Exception e)
            {
                _log.Error($"加载资源失败: {filePath}. 错误: {e.Message}");
                return;
            }

            resources.Add(entry);
            keys.Add(name); // 记录已存在的 Key
        }

        private ResourceCategory GetCategoryFromPath(string path)
        {
            string p = path.ToLower();

            if (p.Contains("/ui/"))
                return ResourceCategory.UI;

            if (p.Contains("/component/"))
                return ResourceCategory.Component;

            if (p.Contains("/entity/"))
                return ResourceCategory.Entity;

            if (p.Contains("/assets/"))
                return ResourceCategory.Asset;

            return ResourceCategory.Other;
        }
    }
}
#endif
