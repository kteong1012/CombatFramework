namespace CombatFramework.Core.Enums;

/// <summary>
/// 技能槽位类型枚举。底层以 (int) 作为固定数组索引使用。
/// </summary>
public enum SlotType
{
    // ── 主动技能槽（Visible = true，显示在技能栏 UI）──────────────────────
    NormalAtk = 0,   // 普攻
    Skill     = 1,   // 战技
    Burst     = 2,   // 终结技

    // ── 被动槽（Visible = false，不进技能栏）──────────────────────────────
    Passive0  = 3,
    Passive1  = 4,

    // ── 命座槽（Visible = false，不进技能栏，装备后永久生效）──────────────
    Const0    = 5,
    Const1    = 6,
    Const2    = 7,
    Const3    = 8,
    Const4    = 9,
    Const5    = 10,
}
