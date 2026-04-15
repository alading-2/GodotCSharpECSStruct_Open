namespace ECS.Base.System.TestSystem.Core;

/// <summary>
/// TestSystem 共享操作结果类型
/// </summary>
internal readonly record struct TestActionResult(
    bool Success,
    string Message
);
