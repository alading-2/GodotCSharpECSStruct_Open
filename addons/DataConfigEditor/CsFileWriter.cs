#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Slime.Addons.DataConfigEditor
{
    /// <summary>
    /// .cs 文件保存器（v3 修复版）
    /// 支持 new() 语法、嵌套初始化器、string? null 值
    /// </summary>
    public static partial class CsFileWriter
    {
        /// <summary>
        /// 匹配静态字段初始化器，支持 new() 和 new Type() 语法
        /// 使用平衡组匹配嵌套大括号
        /// </summary>
        [GeneratedRegex(
            @"(public\s+static\s+readonly\s+\w+\s+(\w+)\s*=\s*new\s*(?:\w+)?\s*(?:<[^>]+>)?\s*\()(\s*)(\{)",
            RegexOptions.Compiled)]
        private static partial Regex StaticInitializerStartRegex();

        /// <summary>
        /// 将所有实例的所有属性变更合并写入 .cs 文件
        /// </summary>
        public static int WriteAllChanges(
            string filePath,
            List<ConfigReflectionCache.InstanceInfo> instances,
            List<PropertyMetadata> properties,
            Dictionary<string, PropertyCommentInfo> comments)
        {
            if (!File.Exists(filePath)) return 0;

            string source = File.ReadAllText(filePath);
            int saved = 0;

            foreach (var inst in instances)
            {
                if (inst.FieldInfo == null) continue;

                var range = FindStaticInitializerRange(source, inst.Name);
                if (range == null) continue;

                string body = source.Substring(range.Value.Start, range.Value.Length);
                string newBody = body;

                foreach (var prop in properties)
                {
                    object? val = prop.PropertyInfo.GetValue(inst.Instance);
                    string csValue = FormatValueForCs(val, prop.PropertyType);
                    newBody = ReplacePropertyInBody(newBody, prop.Name, csValue);
                }

                if (newBody != body)
                {
                    source = source.Remove(range.Value.Start, range.Value.Length)
                                   .Insert(range.Value.Start, newBody);
                    saved++;
                }
            }

            if (saved > 0)
                File.WriteAllText(filePath, source);

            return saved;
        }

        /// <summary>
        /// 找到静态字段初始化器的 { } 范围（支持嵌套）
        /// </summary>
        private static (int Start, int Length)? FindStaticInitializerRange(string source, string fieldName)
        {
            // 匹配开始: public static readonly Type FieldName = new(...) {
            var startMatch = StaticInitializerStartRegex().Match(source);
            while (startMatch.Success)
            {
                if (startMatch.Groups[2].Value != fieldName)
                {
                    startMatch = startMatch.NextMatch();
                    continue;
                }

                int braceStart = startMatch.Index + startMatch.Length - 1; // 最后一个 { 的位置
                int braceEnd = FindMatchingBrace(source, braceStart);
                if (braceEnd < 0) return null;

                // 包含 { 和 }
                int bodyStart = braceStart;
                int bodyLength = braceEnd - braceStart + 1;
                return (bodyStart, bodyLength);
            }
            return null;
        }

        /// <summary>
        /// 找到匹配的右大括号（支持嵌套）
        /// </summary>
        private static int FindMatchingBrace(string source, int openBraceIdx)
        {
            int depth = 0;
            for (int i = openBraceIdx; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 替换初始化器体中的属性值
        /// </summary>
        private static string ReplacePropertyInBody(string body, string propName, string newValue)
        {
            // 匹配: PropName = value, 或 PropName = value（最后一个可能无逗号）
            // value 可能是字符串（包含引号）、枚举、数字等
            var pattern = $@"({Regex.Escape(propName)}\s*=\s*)([^,\n}}]+)";
            return Regex.Replace(body, pattern, match =>
            {
                // 保留空格
                return match.Groups[1].Value + newValue;
            });
        }

        /// <summary>
        /// 将运行时值格式化为 C# 源码表达式
        /// </summary>
        private static string FormatValueForCs(object? value, Type type)
        {
            if (value == null)
            {
                if (type == typeof(string) || Nullable.GetUnderlyingType(type) != null)
                    return "null";
                return "default";
            }

            if (type == typeof(string) || Nullable.GetUnderlyingType(type) == typeof(string))
                return $"\"{value}\"";

            if (type == typeof(bool) || Nullable.GetUnderlyingType(type) == typeof(bool))
                return (bool)value ? "true" : "false";

            if (type == typeof(int))
                return value.ToString() ?? "0";

            if (type == typeof(float))
            {
                string s = ((float)value).ToString("G");
                if (!s.Contains('.') && !s.Contains('E')) s += ".0";
                return s + "f";
            }

            if (type == typeof(double))
            {
                string s = ((double)value).ToString("G");
                if (!s.Contains('.') && !s.Contains('E')) s += ".0";
                return s + "d";
            }

            if (type.IsEnum)
                return $"{type.Name}.{value}";

            return value.ToString() ?? "null";
        }
    }
}
#endif