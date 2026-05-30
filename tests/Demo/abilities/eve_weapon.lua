AbilityData = {
    id = "eve_weapon_passive",
    name = "武器被动",
    parameters = {
        crit_dmg_bonus = { 40, 47.5, 55, 62.5, 70 },
        atk_pct_per_stack = { 4, 5, 6, 7, 8 },
        max_stacks = 5,
        crit_rate_bonus = { 4, 5, 6, 7, 8 },
        buff_duration = 10,
    },
    Modifiers = {
        weapon_crit_dmg = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            Properties = { CritDMG = "%crit_dmg_bonus" },
        },
    },
}
