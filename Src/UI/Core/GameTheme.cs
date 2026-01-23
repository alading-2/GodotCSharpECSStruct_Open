using Godot;

/// <summary>
/// 游戏主题配色工具
/// 统一管理游戏中的颜色配置，如实体颜色、文字颜色等
/// </summary>
public static class GameTheme
{
    // ========================================================
    // 基础色板 (Palette)
    // ========================================================

    // 阵营/实体相关
    public static readonly Color Player = new Color(0.2f, 0.8f, 0.2f);       // 玩家绿
    public static readonly Color EnemyNormal = new Color(0.8f, 0.2f, 0.2f);  // 敌人红
    public static readonly Color EnemyElite = new Color(1.0f, 0.5f, 0.0f);   // 敌人精英橙
    public static readonly Color EnemyBoss = new Color(0.6f, 0.0f, 0.8f);    // 敌人BOSS紫
    public static readonly Color Summon = new Color(0.2f, 0.6f, 1.0f);       // 召唤物蓝

    // UI 文字/通用相关
    public static readonly Color TextNormal = Colors.White;
    public static readonly Color TextHighlight = new Color(1f, 0.85f, 0.4f); // 高亮金黄
    public static readonly Color TextDanger = new Color(1f, 0.4f, 0.4f);     // 危险红
    public static readonly Color TextDisable = new Color(0.6f, 0.6f, 0.6f);  // 禁用灰

    /// <summary>
    /// 获取实体对应的颜色
    /// </summary>
    public static Color GetEntityColor(Team team, UnitRank rank = UnitRank.Normal)
    {
        switch (team)
        {
            case Team.Player:
                return Player;

            case Team.Enemy:
                switch (rank)
                {
                    case UnitRank.Boss: return EnemyBoss;
                    case UnitRank.Elite: return EnemyElite;
                    default: return EnemyNormal;
                }

            case Team.Neutral:
                if (rank == UnitRank.Summon) return Summon;
                return Colors.White;

            default:
                return Colors.White;
        }
    }
}
