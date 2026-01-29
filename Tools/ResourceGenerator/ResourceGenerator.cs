using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;


class ResourceGenerator
{
    // 配置部分
    private static readonly string[] ScanPaths = {
        "assets",
        "Src/UI",
        "Src/ECS/Entity",
        "Src/ECS/Component",
        "Data/Data",  // 新增：扫描 Resource 配置目录
        "Src/ECS/System",
        "Src/Tools",
        "Src/Test" // 新增：扫描测试资源
    };

    private static readonly string[] ExcludePaths = {
        "addons",
        ".godot",
    };

    private const string OutputFile = "Data/ResourceManagement/ResourcePaths.cs";

    private static ResourceCategory GetCategoryFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return ResourceCategory.Other;

        // 1. Resource 配置优先匹配（.tres 文件，必须在 Data/Data/ 路径下）
        if (path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
        {
            // 只有 Data/Data/ 路径下的 .tres 才是真正的配置文件
            if (path.Contains("Data/Data/", StringComparison.OrdinalIgnoreCase))
            {
                if (path.Contains("/Enemy/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.EnemyConfig;
                if (path.Contains("/Player/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.PlayerConfig;
                if (path.Contains("/Ability/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AbilityConfig;
                if (path.Contains("/Item/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.ItemConfig;
            }
            // assets 下的 .tres 文件（如 SpriteFrames）归类为 Asset
            else if (path.StartsWith("res://assets/", StringComparison.OrdinalIgnoreCase))
            {
                return ResourceCategory.Asset;
            }
        }

        // 2. 场景文件匹配（.tscn 文件）
        if (path.StartsWith("res://Src/UI/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.UI;
        if (path.StartsWith("res://Src/ECS/Component/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Component;
        if (path.StartsWith("res://Src/ECS/Entity/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Entity;
        if (path.StartsWith("res://assets/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Asset;
        if (path.StartsWith("res://Src/ECS/System/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.System;
        if (path.StartsWith("res://Src/Tools/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Tools;
        if (path.StartsWith("res://Src/Test/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Test;

        // 3. 启发式包含匹配 (后备方案)
        if (path.Contains("/UI/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.UI;
        if (path.Contains("/Component/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Component;
        if (path.Contains("/Entity/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Entity;
        if (path.Contains("/assets/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Asset;
        if (path.Contains("/System/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.System;
        if (path.Contains("/Tools/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Tools;
        if (path.Contains("/Test/", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Test;

        return ResourceCategory.Other;
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

        // 修改：使用嵌套字典存储资源 [Category -> [Name -> Path]]
        var resourcesByCategory = new Dictionary<ResourceCategory, Dictionary<string, string>>();
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

            ScanDirectory(fullPath, projectRoot, resourcesByCategory, duplicates);
        }

        // 3. 生成代码
        GenerateCode(projectRoot, resourcesByCategory);

        // 统计总数
        int totalResources = resourcesByCategory.Sum(x => x.Value.Count);
        Console.WriteLine($"处理完成! 共找到 {totalResources} 个资源。");

        if (duplicates.Count > 0)
        {
            Console.WriteLine($"[警告] 发现 {duplicates.Count} 个分类内重名资源 (已跳过):");
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

    private static void ScanDirectory(string dirPath, string projectRoot, Dictionary<ResourceCategory, Dictionary<string, string>> resourcesByCategory, List<string> duplicates)
    {
        var files = Directory.GetFiles(dirPath);
        foreach (var file in files)
        {
            // 处理 .tscn, .tres 和 .cs 文件 (System/Manager 可能是 .cs)
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
                if (relativePath.StartsWith(exclude))
                {
                    isExcluded = true;
                    break;
                }
            }

            if (isExcluded) continue;

            // 获取分类
            ResourceCategory category = GetCategoryFromPath(resPath);

            // 确保分类字典存在
            if (!resourcesByCategory.ContainsKey(category))
            {
                resourcesByCategory[category] = new Dictionary<string, string>();
            }

            var categoryResources = resourcesByCategory[category];

            if (categoryResources.TryGetValue(name, out var existingPath))
            {
                // 冲突解决策略：优先保留 .tscn 文件 (通常是场景)，覆盖 .tres 文件 (可能是资源或 SpriteFrames)
                bool existingIsScene = existingPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);
                bool newIsScene = resPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);

                if (existingIsScene && !newIsScene)
                {
                    // 已有的是 .tscn，新的是 .tres -> 忽略新的
                    duplicates.Add($"[{category}] {name} ({resPath}) [Skipped: Prefer .tscn]");
                }
                else if (!existingIsScene && newIsScene)
                {
                    // 已有的是 .tres，新的是 .tscn -> 覆盖旧的
                    categoryResources[name] = resPath;
                }
                else
                {
                    // 同样扩展名或无法判断优先级，视为真正冲突
                    duplicates.Add($"[{category}] {name} ({resPath})");
                }
            }
            else
            {
                categoryResources[name] = resPath;
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

            ScanDirectory(subDir, projectRoot, resourcesByCategory, duplicates);
        }
    }

    private static void GenerateCode(string projectRoot, Dictionary<ResourceCategory, Dictionary<string, string>> resourcesByCategory)
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

        // 生成统一的 Resources 字典
        sb.AppendLine("    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()");
        sb.AppendLine("    {");

        // 确保所有枚举值都在字典中，即使为空
        var allCategories = Enum.GetNames(typeof(ResourceCategory));

        foreach (var categoryName in allCategories)
        {
            sb.AppendLine($"        {{ ResourceCategory.{categoryName}, new Dictionary<string, ResourceData>");
            sb.AppendLine("            {");

            if (resourcesByCategory.TryGetValue(Enum.Parse<ResourceCategory>(categoryName), out var items))
            {
                foreach (var kvp in items.OrderBy(x => x.Key))
                {
                    sb.AppendLine($"                {{ \"{kvp.Key}\", new ResourceData(ResourceCategory.{categoryName}, \"{kvp.Value}\") }},");
                }
            }

            sb.AppendLine("            }");
            sb.AppendLine("        },");
        }

        sb.AppendLine("    };");
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
