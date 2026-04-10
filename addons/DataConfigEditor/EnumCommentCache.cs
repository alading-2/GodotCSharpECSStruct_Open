#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Slime.Addons.DataConfigEditor
{
/// <summary>
/// 枚举中文注释缓存 - 扫描项目所有枚举定义文件，解析成员注释
/// </summary>
    public static class EnumCommentCache
    {
        private static readonly Dictionary<Type, EnumMemberInfo[]> _cache = new();
        private static bool _initialized;

        /// <summary>
        /// 枚举成员信息：名称 + 中文注释
        /// </summary>
        public struct EnumMemberInfo
        {
            public string Name;
            public string Comment;
            public bool IsFlags;
            public int Value;
        }

        public static void EnsureLoaded()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                string projectRoot = ProjectSettings.GlobalizePath("res://");
                GD.Print($"[DataConfigEditor] EnumCommentCache projectRoot={projectRoot}");
                var enumTypes = CollectAllEnumTypes();
                GD.Print($"[DataConfigEditor] 找到 {enumTypes.Count} 个枚举类型");
                foreach (var type in enumTypes)
                {
                    var members = ParseEnumMembers(type, projectRoot);
                    if (members != null && members.Length > 0)
                        _cache[type] = members;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[DataConfigEditor] EnumCommentCache 加载失败: {e.Message}");
            }
        }

        public static EnumMemberInfo[] GetMembers(Type enumType)
        {
            EnsureLoaded();
            return _cache.TryGetValue(enumType, out var info) ? info : Array.Empty<EnumMemberInfo>();
        }

        public static string GetComment(Type enumType, string memberName)
        {
            var members = GetMembers(enumType);
            foreach (var m in members)
            {
                if (m.Name == memberName) return m.Comment;
            }
            return "";
        }

        private static List<Type> CollectAllEnumTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.IsEnum && t.IsPublic)
                .ToList();
        }

        private static EnumMemberInfo[] ParseEnumMembers(Type enumType, string projectRoot)
        {
            var sourceFile = FindEnumSourceFile(enumType, projectRoot);
            if (sourceFile == null) return BuildFromReflection(enumType);

            var rawNames = Enum.GetNames(enumType);
            var rawValues = Enum.GetValues(enumType);
            var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;
            var comments = CsCommentParser.ParseEnumComments(sourceFile);

            var result = new EnumMemberInfo[rawNames.Length];
            for (int i = 0; i < rawNames.Length; i++)
            {
                string comment = comments.TryGetValue(rawNames[i], out var c) ? c : "";
                result[i] = new EnumMemberInfo
                {
                    Name = rawNames[i],
                    Comment = comment,
                    IsFlags = isFlags,
                    Value = Convert.ToInt32(rawValues.GetValue(i)),
                };
            }
            return result;
        }

        private static EnumMemberInfo[] BuildFromReflection(Type enumType)
        {
            var rawNames = Enum.GetNames(enumType);
            var rawValues = Enum.GetValues(enumType);
            var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() != null;
            var result = new EnumMemberInfo[rawNames.Length];
            for (int i = 0; i < rawNames.Length; i++)
            {
                result[i] = new EnumMemberInfo
                {
                    Name = rawNames[i],
                    Comment = "",
                    IsFlags = isFlags,
                    Value = Convert.ToInt32(rawValues.GetValue(i)),
                };
            }
            return result;
        }

        private static string? FindEnumSourceFile(Type enumType, string projectRoot)
        {
            string typeName = enumType.Name;
            var files = Directory.GetFiles(projectRoot, $"{typeName}.cs", SearchOption.AllDirectories);
            return files.FirstOrDefault(f =>
                !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}.godot{Path.DirectorySeparatorChar}"));
        }
    }
}
#endif
