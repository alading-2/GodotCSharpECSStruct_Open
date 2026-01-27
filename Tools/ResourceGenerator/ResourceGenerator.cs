using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ResourceGenerator;

class ResourceGenerator
{
    // 配置部分
    private static readonly string[] ScanPaths = {
        "assets",
        "Src/UI",
        "Src/ECS/Entity",
        "Src/ECS/Component",
        "Data/Data/Resources"  // 新增：扫描 Resource 配置目录
    };

    private static readonly string[] ExcludePaths = {
        "addons",
        ".godot",
        "Src/Test",
        "Src/Tools"
    };

    private const string OutputFile = "Data/ResourceManagement/ResourcePaths.cs";

    private static string GetCategoryFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Other";

        // 1. Resource 配置优先匹配（.tres 文件）
        if (path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
        {
            if (path.Contains("/Enemies/", StringComparison.OrdinalIgnoreCase)) return "EnemyConfig";
            if (path.Contains("/Players/", StringComparison.OrdinalIgnoreCase)) return "PlayerConfig";
            if (path.Contains("/Abilities/", StringComparison.OrdinalIgnoreCase)) return "AbilityConfig";
            if (path.Contains("/Items/", StringComparison.OrdinalIgnoreCase)) return "ItemConfig";
        }

        // 2. 场景文件匹配（.tscn 文件）
        if (path.StartsWith("res://Src/UI/", StringComparison.OrdinalIgnoreCase)) return "UI";
        if (path.StartsWith("res://Src/ECS/Component/", StringComparison.OrdinalIgnoreCase)) return "Component";
        if (path.StartsWith("res://Src/ECS/Entity/", StringComparison.OrdinalIgnoreCase)) return "Entity";
        if (path.StartsWith("res://assets/", StringComparison.OrdinalIgnoreCase)) return "Asset";

        // 3. 启发式包含匹配 (后备方案)
        if (path.Contains("/UI/", StringComparison.OrdinalIgnoreCase)) return "UI";
        if (path.Contains("/Component/", StringComparison.OrdinalIgnoreCase)) return "Component";
        if (path.Contains("/Entity/", StringComparison.OrdinalIgnoreCase)) return "Entity";
        if (path.Contains("/assets/", StringComparison.OrdinalIgnoreCase)) return "Asset";

        return "Other";
    }

    static void Main(string[] args)
    {
        Console.WriteLine("开始扫描资源文件...");

        // 1. 确定项目根目录 (假设当前运行目录在 Tools/ResourceGenerator 或项目根目录下)
        // 我们需要找到包含 project.godot 的目录
        var projectRoot = FindProjectRoot(Directory.GetCurrentDirectory());
        if (projectRoot == null)
        {
            Console.Error.WriteLine("API Error: 无法找到项目根目录 (未发现 project.godot)");
            return;
        }

        Console.WriteLine($"项目根目录: {projectRoot}");

        var resources = new Dictionary<string, string>();
        var duplicates = new List<string>();

        // 2. 扫描目录
        foreach (var relativePath in ScanPaths)
        {
            var fullPath = Path.Combine(projectRoot, relativePath);
            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"[警告] 路径不存在: {fullPath}");
                continue;
            }

            ScanDirectory(fullPath, projectRoot, resources, duplicates);
        }

        // 3. 生成代码
        GenerateCode(projectRoot, resources);

        Console.WriteLine($"处理完成! 共找到 {resources.Count} 个资源。");
        if (duplicates.Count > 0)
        {
            Console.WriteLine($"[警告] 发现 {duplicates.Count} 个重名资源 (已跳过):");
            foreach (var dup in duplicates)
            {
                Console.WriteLine($"  - {dup}");
            }
        }
    }

    private static string? FindProjectRoot(string currentPath)
    {
        var dir = new DirectoryInfo(currentPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static void ScanDirectory(string dirPath, string projectRoot, Dictionary<string, string> resources, List<string> duplicates)
    {
        var files = Directory.GetFiles(dirPath);
        foreach (var file in files)
        {
            // 处理 .tscn 和 .tres 文件
            if (!file.EndsWith(".tscn") && !file.EndsWith(".tres")) continue;

            // 排除 ResourceManagement 本身
            if (file.EndsWith("ResourceManagement.tscn")) continue;

            var name = Path.GetFileNameWithoutExtension(file);

            // 转换为 res:// 路径
            var relativePath = Path.GetRelativePath(projectRoot, file).Replace("\\", "/");
            var resPath = "res://" + relativePath;

            // 检查排除路径
            bool isExcluded = false;
            foreach (var exclude in ExcludePaths)
            {
                // 简单的字符串匹配，如果 resPath 以 exclude 开头则排除
                // 这里的逻辑比较简单：如果 relativePath 以排除项开头，则忽略
                if (relativePath.StartsWith(exclude))
                {
                    isExcluded = true;
                    break;
                }
            }

            if (isExcluded) continue;

            if (resources.TryGetValue(name, out var existingPath))
            {
                // 冲突解决策略：优先保留 .tscn 文件 (通常是场景)，覆盖 .tres 文件 (可能是资源或 SpriteFrames)
                bool existingIsScene = existingPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);
                bool newIsScene = resPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);

                if (existingIsScene && !newIsScene)
                {
                    // 已有的是 .tscn，新的是 .tres -> 忽略新的
                    duplicates.Add($"{name} ({resPath}) [Skipped: Prefer .tscn]");
                }
                else if (!existingIsScene && newIsScene)
                {
                    // 已有的是 .tres，新的是 .tscn -> 覆盖旧的
                    resources[name] = resPath;
                    // (可选) 可以记录一下被覆盖的文件，但这里简单处理
                }
                else
                {
                    // 同样扩展名或无法判断优先级，视为真正冲突
                    duplicates.Add($"{name} ({resPath})");
                }
            }
            else
            {
                resources[name] = resPath;
            }
        }

        var subDirs = Directory.GetDirectories(dirPath);
        foreach (var subDir in subDirs)
        {
            // 检查目录是否被排除
            var dirName = Path.GetFileName(subDir);
            if (dirName.StartsWith(".")) continue; // 忽略 .godot 等隐藏目录

            var relativePath = Path.GetRelativePath(projectRoot, subDir).Replace("\\", "/");
            bool isExcluded = false;
            foreach (var exclude in ExcludePaths)
            {
                if (relativePath.StartsWith(exclude) || relativePath == exclude)
                {
                    isExcluded = true;
                    break;
                }
            }
            if (isExcluded) continue;

            ScanDirectory(subDir, projectRoot, resources, duplicates);
        }
    }

    private static void GenerateCode(string projectRoot, Dictionary<string, string> resources)
    {
        var sb = new StringBuilder();
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine("// <ResourceGenerator>");
        sb.AppendLine("//     ResourceGenerator 资源路径生成器工具");
        sb.AppendLine("//");
        sb.AppendLine("//     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。");
        sb.AppendLine("// </ResourceGenerator>");
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Brotato.Data.ResourceManagement;");
        sb.AppendLine();
        sb.AppendLine("public struct ResourceData");
        sb.AppendLine("{");
        sb.AppendLine("    public string Path;");
        sb.AppendLine("    public ResourceCategory Category;");
        sb.AppendLine("    public ResourceData(ResourceCategory category, string path)");
        sb.AppendLine("    {");
        sb.AppendLine("        Category = category;");
        sb.AppendLine("        Path = path;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public static class ResourcePaths");
        sb.AppendLine("{");

        // 按类别分组资源
        var categorized = resources
            .Select(kvp => new { Name = kvp.Key, Path = kvp.Value, Category = GetCategoryFromPath(kvp.Value) })
            .GroupBy(x => x.Category)
            .OrderBy(g => g.Key)
            .ToList();

        // 为每个类别生成字典
        var categoryDicts = new Dictionary<string, string>
        {
            { "Entity", "Entities" },
            { "Component", "Components" },
            { "UI", "UI" },
            { "Asset", "Assets" },
            { "EnemyConfig", "EnemyConfigs" },
            { "PlayerConfig", "PlayerConfigs" },
            { "AbilityConfig", "AbilityConfigs" },
            { "ItemConfig", "ItemConfigs" },
            { "Other", "Other" }
        };

        foreach (var dictPair in categoryDicts.OrderBy(x => x.Key))
        {
            var category = dictPair.Key;
            var dictName = dictPair.Value;
            var items = categorized.FirstOrDefault(g => g.Key == category);

            sb.AppendLine($"    public static readonly Dictionary<string, ResourceData> {dictName} = new()");
            sb.AppendLine("    {");

            if (items != null)
            {
                foreach (var item in items.OrderBy(x => x.Name))
                {
                    sb.AppendLine($"        {{ \"{item.Name}\", new ResourceData(ResourceCategory.{category}, \"{item.Path}\") }},");
                }
            }

            sb.AppendLine("    };");
            sb.AppendLine();
        }

        // 生成合并的 All 字典（兼容性）
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// 所有资源的合并字典（兼容性保留，建议使用分类字典）");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly Dictionary<string, ResourceData> All = new();");
        sb.AppendLine();
        sb.AppendLine("    static ResourcePaths()");
        sb.AppendLine("    {");
        sb.AppendLine("        // 合并所有分类到 All 字典");
        sb.AppendLine("        foreach (var dict in new[] { Entities, Components, UI, Assets, EnemyConfigs, PlayerConfigs, AbilityConfigs, ItemConfigs, Other })");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var kvp in dict)");
        sb.AppendLine("            {");
        sb.AppendLine("                if (!All.ContainsKey(kvp.Key))");
        sb.AppendLine("                    All[kvp.Key] = kvp.Value;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var outputPath = Path.Combine(projectRoot, OutputFile);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir!);
        }

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"已生成文件: {OutputFile}");
    }


}
