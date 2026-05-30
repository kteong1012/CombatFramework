AbilityData = {
    id = "eve_atk_light03",
    name = "轻攻击三段",
    parameters = {
        coeff = { 1.20, 1.30, 1.40, 1.50, 1.60, 1.70, 1.80, 1.90, 2.00, 2.10, 2.20, 2.30, 2.40, 2.50, 2.60 },
    },
    Effects = {
        main_hit = {
            ActOnTargets = { type = "self" },
            Action = {
                Damage = { Target = "TARGET", Element = "FIRE", Damage = { RunFunctionInAbility = "Light03Damage" } },
            },
        },
    },
    Light03Damage = function(caster, ability, target)
        local atk = caster:GetStat("Attack")
        local coe = ability:GetParameter("coeff")
        return atk * coe
    end,
}
