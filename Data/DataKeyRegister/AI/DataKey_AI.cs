/// <summary>
/// 数据键定义 - AI 相关
/// </summary>
public static partial class DataKey
{
    // === AI ===
    public const string AIState = "AIState"; // AI状态（Idle/Chasing/Attacking/Fleeing）
    public const string Threat = "Threat"; // 威胁值（仇恨值）
    public const string Target = "Target"; // 当前目标（EntityId）
    public const string DetectionRange = "DetectionRange"; // 索敌范围
    public const string AttackRange = "AttackRange"; // 攻击范围
}
