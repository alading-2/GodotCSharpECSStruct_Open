using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;


class ResourceGenerator
{
    // ============================================================
    // 配置部分：key=扫描路径（相对项目根，正斜杠），value=资源分类
    // 分类按最长前缀匹配，因此子路径可覆盖父路径
    // ============================================================
    private static readonly Dictionary<string, ResourceCategory> ScanConfig = new(StringComparer.OrdinalIgnoreCase)
    {
        { "assets/Effect", ResourceCategory.AssetEffect },
        { "assets/Unit", ResourceCategory.AssetUnit },       // 长度 10 — 兜底
        { "assets/Unit/Player", ResourceCategory.AssetUnitPlayer }, // 长度 18 — 自动更优先
        { "assets/Unit/Enemy", ResourceCategory.AssetUnitEnemy },  // 长度 16 — 自动更优先
        { "Src/UI", ResourceCategory.UI },
        { "Src/ECS/Entity", ResourceCategory.Entity },
        { "Src/ECS/Component", ResourceCategory.Component },
        { "Data/Data", ResourceCategory.Data },         // 兜底
        { "Data/Data/Collision", ResourceCategory.DataCollision },
        { "Data/Data/Ability", ResourceCategory.DataAbility },  // 自动更优先
        { "Data/Data/Unit", ResourceCategory.DataUnit },      // 自动更优先
        { "Src/ECS/System", ResourceCategory.System },
        { "Src/Tools", ResourceCategory.Tools },
        { "Src/Test", ResourceCategory.Test },
    };

    private static readonly string[] ExcludePaths = {
        "addons",
        ".godot",
    };

    private const string CollisionDataPath = "Data/Data/Collision";
    private const string CollisionRegistryOutputFile = "Data/Data/Collision/CollisionTypeRegistry.cs";

    private const string OutputFile = "Data/ResourceManagement/ResourcePaths.cs";

    // 通过最长前缀匹配 ScanConfig 确定资源分类
    private static ResourceCategory GetCategoryFromPath(string resPath)
    {
        if (string.IsNullOrEmpty(resPath)) return ResourceCategory.Other;
        var relPath = resPath.StartsWith("res://") ? resPath.Substring(6) : resPath;

        ResourceCategory best = ResourceCategory.Other;
        int bestLen = -1;
        foreach (var kv in ScanConfig)
        {
            string prefix = kv.Key;
            if ((relPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                 relPath.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                && prefix.Length > bestLen)
            {
                bestLen = prefix.Length;
                best = kv.Value;
            }
        }
        return best;
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

        // 2. 扫描目录（路径来源于 ScanConfig 配置字典）
        foreach (var relativePath in ScanConfig.Keys)
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

        // 4. 生成碰撞类型注册表
        GenerateCollisionTypeRegistry(projectRoot);

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

            // 转换为 res:// 路径
            var relativePath = Path.GetRelativePath(projectRoot, file).Replace("\\", "/");
            var resPath = "res://" + relativePath;

            var name = GetNameFromResPath(resPath);

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

            // 若该子目录已在 ScanConfig 中单独配置，跳过（由独立扫描处理，避免重复）
            if (ScanConfig.ContainsKey(relativePath)) continue;

            ScanDirectory(subDir, projectRoot, resourcesByCategory, duplicates);
        }
    }

    private static string GetNameFromResPath(string resPath)
    {
        string relPath = resPath.StartsWith("res://") ? resPath.Substring(6) : resPath;

        // 通过 ScanConfig 找最长匹配前缀并剥离，与分类逻辑保持一致
        int bestLen = -1;
        foreach (var key in ScanConfig.Keys)
        {
            if (relPath.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase) && key.Length > bestLen)
                bestLen = key.Length;
        }
        if (bestLen > 0)
            relPath = relPath.Substring(bestLen + 1); // +1 跳过分隔符 '/'

        if (relPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)) relPath = relPath.Substring(0, relPath.Length - 5);
        if (relPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)) relPath = relPath.Substring(0, relPath.Length - 5);

        // 过滤掉 AnimatedSprite2D 中间目录，仅保留有效路径段
        var parts = relPath.Replace("\\", "/").Split('/');
        var filteredParts = System.Array.FindAll(parts,
            p => !string.Equals(p, "AnimatedSprite2D", StringComparison.OrdinalIgnoreCase));

        // 只取文件名（最后一段）；若同分类内出现重名，ResourceGenerator 日志会提示
        return filteredParts.Last().Replace("-", "_");
    }

    // ==================== 碰撞类型注册表生成 ====================

    private struct CollisionEntry
    {
        public string NodeName;
        public uint Layer;
        public uint Mask;
        public int EnumValue;
    }

    /// <summary>
    /// 扫描 Data/Data/Collision 目录下所有 .tscn 文件，提取节点名称和 layer/mask，
    /// 生成 CollisionType 枚举 + CollisionTypeRegistry 查找表到 CollisionTypeRegistry.cs
    /// </summary>
    private static void GenerateCollisionTypeRegistry(string projectRoot)
    {
        var collisionDir = Path.Combine(projectRoot, CollisionDataPath);
        if (!Directory.Exists(collisionDir))
        {
            Console.WriteLine($"[警告] 碰撞目录不存在，跳过 CollisionTypeRegistry 生成: {collisionDir}");
            return;
        }

        var tscnFiles = Directory.GetFiles(collisionDir, "*.tscn", SearchOption.AllDirectories);
        var entries = new List<CollisionEntry>();

        foreach (var file in tscnFiles)
        {
            var entry = ParseCollisionTscn(file);
            if (!string.IsNullOrEmpty(entry.NodeName))
                entries.Add(entry);
        }

        if (entries.Count == 0)
        {
            Console.WriteLine("[警告] Data/Data/Collision 未找到有效 .tscn 文件，跳过 CollisionTypeRegistry 生成。");
            return;
        }

        // 按节点名字母序排列（保证枚举值稳定）
        entries.Sort((a, b) => string.Compare(a.NodeName, b.NodeName, StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            e.EnumValue = i + 1; // 0 保留给 Custom
            entries[i] = e;
        }

        var sb = new StringBuilder();
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine("//* <ResourceGenerator>");
        sb.AppendLine("//*     此文件由 ResourceGenerator 自动生成，请勿手动修改。");
        sb.AppendLine("//*     来源：Data/Data/Collision/ 目录下所有 .tscn 文件（按节点名称字母序排列）。");
        sb.AppendLine("//*     重新运行 ResourceGenerator 会覆盖本文件。");
        sb.AppendLine("//* </ResourceGenerator>");
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 碰撞类型枚举");
        sb.AppendLine("/// 值来源：Data/Data/Collision/ 目录下 .tscn 场景的根节点名称（字母序）");
        sb.AppendLine("/// Custom = 0 永远保留；添加新场景请勿改变已有场景文件名，否则枚举值会错位");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public enum CollisionType");
        sb.AppendLine("{");
        sb.AppendLine("    Custom = 0,");
        foreach (var e in entries)
            sb.AppendLine($"    {e.NodeName} = {e.EnumValue},      // layer={e.Layer}, mask={e.Mask}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 碰撞类型注册表 - 纯数据，仅存储各类型的 Layer/Mask 映射");
        sb.AppendLine("/// 查询方法请使用 CollisionTypeQuery（Data/Data/Collision/CollisionTypeQuery.cs）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class CollisionTypeRegistry");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>CollisionType → (Layer, Mask) 正向字典</summary>");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<CollisionType, (uint Layer, uint Mask)> LayerMaskByType =\n        new Dictionary<CollisionType, (uint Layer, uint Mask)>");
        sb.AppendLine("    {");
        foreach (var e in entries)
            sb.AppendLine($"        {{ CollisionType.{e.NodeName}, ({e.Layer}u, {e.Mask}u) }},");
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>(Layer, Mask) 元组 → CollisionType 反向字典（包含 layer=0 的类型）</summary>");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<(uint Layer, uint Mask), CollisionType> TypeByLayerMask =\n        new Dictionary<(uint Layer, uint Mask), CollisionType>");
        sb.AppendLine("    {");
        foreach (var e in entries)
            sb.AppendLine($"        {{ ({e.Layer}u, {e.Mask}u), CollisionType.{e.NodeName} }},");
        sb.AppendLine("    };");
        sb.AppendLine("}");

        var outputPath = Path.Combine(projectRoot, CollisionRegistryOutputFile);
        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"[碰撞注册表] 已生成 {entries.Count} 个 CollisionType 枚举值 → {CollisionRegistryOutputFile}");
    }

    private static CollisionEntry ParseCollisionTscn(string filePath)
    {
        var entry = new CollisionEntry();
        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("[node name=", StringComparison.OrdinalIgnoreCase))
                {
                    var nameStart = trimmed.IndexOf('"') + 1;
                    var nameEnd = trimmed.IndexOf('"', nameStart);
                    if (nameStart > 0 && nameEnd > nameStart)
                        entry.NodeName = trimmed.Substring(nameStart, nameEnd - nameStart);
                }
                else if (trimmed.StartsWith("collision_layer =", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split('=')[1].Trim();
                    if (uint.TryParse(val, out var l)) entry.Layer = l;
                }
                else if (trimmed.StartsWith("collision_mask =", StringComparison.OrdinalIgnoreCase))
                {
                    var val = trimmed.Split('=')[1].Trim();
                    if (uint.TryParse(val, out var m)) entry.Mask = m;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[警告] 解析碰撞场景失败: {filePath}: {ex.Message}");
        }
        return entry;
    }

    private static void GenerateCode(string projectRoot, Dictionary<ResourceCategory, Dictionary<string, string>> resourcesByCategory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine("//* <ResourceGenerator>");
        sb.AppendLine("//*     ResourceGenerator 资源路径生成器工具");
        sb.AppendLine("//*");
        sb.AppendLine("//*     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。");
        sb.AppendLine("//* </ResourceGenerator>");
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

        // 1. 生成静态常量 (强类型访问)
        // 格式: ResourcePaths.EnemyConfig_Name
        var allCategories = Enum.GetNames(typeof(ResourceCategory));
        foreach (var categoryName in allCategories)
        {
            sb.AppendLine($"    // --- {categoryName} ---");
            if (resourcesByCategory.TryGetValue(Enum.Parse<ResourceCategory>(categoryName), out var items))
            {
                foreach (var kvp in items.OrderBy(x => x.Key))
                {
                    // 使用 Category_Name 风格的常量
                    sb.AppendLine($"    public const string {categoryName}_{kvp.Key} = \"{kvp.Key}\";");
                }
            }
            sb.AppendLine();
        }

        // 2. 生成统一的 Resources 字典 (运行时查找)
        sb.AppendLine("    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()");
        sb.AppendLine("    {");

        foreach (var categoryName in allCategories)
        {
            sb.AppendLine($"        {{ ResourceCategory.{categoryName}, new Dictionary<string, ResourceData>");
            sb.AppendLine("            {");

            if (resourcesByCategory.TryGetValue(Enum.Parse<ResourceCategory>(categoryName), out var items))
            {
                foreach (var kvp in items.OrderBy(x => x.Key))
                {
                    // 使用扁平化的静态常量作为 Key，确保一致性
                    sb.AppendLine($"                {{ {categoryName}_{kvp.Key}, new ResourceData(ResourceCategory.{categoryName}, \"{kvp.Value}\") }},");
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
