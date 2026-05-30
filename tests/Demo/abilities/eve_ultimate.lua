AbilityData = {
    id = "eve_ultimate",
    name = "终结技",
    cooldown = 10,
    parameters = {
        coeff = { 3.80, 4.00, 4.20, 4.40, 4.60, 4.80, 5.00, 5.20, 5.40, 5.60, 5.80, 6.00, 6.20, 6.40, 6.60 },
    },
    Effects = {
        main_hit = {
            ActOnTargets = { type = "self" },
            Action = {
                Damage = { Target = "TARGET", Element = "FIRE", Damage = 5600 },
            },
        },
    },
}
