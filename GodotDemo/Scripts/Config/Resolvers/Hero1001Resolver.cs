/// <summary>
/// 角色 1001 的技能决议器 — 根据已解锁命座决定技能使用哪个变体。
/// </summary>
public static class Hero1001Resolver
{
    /// <summary>
    /// 返回最终应使用的技能 key。命名约定：[基名]_c[N]_...
    /// 比如 skill_aoe + C2 → skill_aoe_c2
    ///        skill_aoe + C2 + C4 → skill_aoe_c2_c4
    /// </summary>
    public static string Resolve(string abilityKey, bool[] unlocked /* index 0~5 */)
    {
        // ── 战技 AOE ──
        if (abilityKey == "skill_aoe")
        {
            bool c2 = unlocked.Length > 1 && unlocked[1];  // C2: 零消耗版
            bool c4 = unlocked.Length > 3 && unlocked[3];  // C4: 范围扩大版
            if (c2 && c4) return "skill_aoe_c2_c4";
            if (c2)       return "skill_aoe_c2";
            if (c4)       return "skill_aoe_c4";
        }

        // ── 充能 ──
        if (abilityKey == "skill_charge")
        {
            bool c2 = unlocked.Length > 1 && unlocked[1];
            bool c4 = unlocked.Length > 3 && unlocked[3];
            if (c2 && c4) return "skill_charge_c2_c4";
            if (c2)       return "skill_charge_c2";
            if (c4)       return "skill_charge_c4";
        }

        // ── 普攻 ──
        if (abilityKey == "normal_attack_01")
        {
            bool c2 = unlocked.Length > 1 && unlocked[1];
            bool c4 = unlocked.Length > 3 && unlocked[3];
            if (c2 && c4) return "normal_attack_01_c2_c4";
            if (c2)       return "normal_attack_01_c2";
            if (c4)       return "normal_attack_01_c4";
        }

        return abilityKey;
    }
}
