using Godot;
using System;

namespace ECS.Base.System.TestSystem.Core;

/// <summary>
/// TestSystem 共享场景工具 - 提供节点解析和场景实例化的扩展方法
/// </summary>
internal static class TestSceneHelper
{
    /// <summary>
    /// 解析一个必需的子节点。优先使用 Godot unique-name (%Path)，
    /// 然后是回退路径。如果都未找到则抛出。
    /// </summary>
    internal static T ResolveRequiredNode<T>(
        this Node self,
        string uniquePath,
        string fallbackPath,
        string contextName) where T : Node
    {
        var node = self.GetNodeOrNull<T>(uniquePath)
                ?? self.GetNodeOrNull<T>(fallbackPath);

        if (node != null) return node;

        throw new InvalidOperationException(
            $"{contextName} 节点缺失: node={self.Name}, unique={uniquePath}, fallback={fallbackPath}");
    }

    /// <summary>
    /// 实例化一个 PackedScene，如果场景为空或类型不匹配则抛出。
    /// </summary>
    internal static T InstantiateScene<T>(
        PackedScene? scene,
        string sceneName) where T : Node
    {
        if (scene == null)
            throw new InvalidOperationException($"场景未配置: {sceneName}");

        var instance = scene.Instantiate<T>();
        if (instance == null)
            throw new InvalidOperationException($"场景实例化失败: {sceneName}");

        return instance;
    }
}
