AbilityData = {
    id = "eve_constellations",
    parameters = {
        c1_dmg_inc = 15,
        c6_dmg_inc = 6,
    },
    Modifiers = {
        eve_c1 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            Properties = { DamageInc = "%c1_dmg_inc" },
        },
        eve_c2 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            OnCreated = function(self)
                local u = self.Parent
                u:GetAbility("eve_atk_light01"):AddBonusLevel(2)
                u:GetAbility("eve_atk_light02"):AddBonusLevel(2)
                u:GetAbility("eve_atk_light03"):AddBonusLevel(2)
            end,
        },
        eve_c3 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
        },
        eve_c4 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            OnCreated = function(self)
                self.Parent:GetAbility("eve_ultimate"):AddBonusLevel(2)
            end,
        },
        eve_c5 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            OnCreated = function(self)
                local u = self.Parent
                u:GetAbility("eve_atk_light01"):AddBonusLevel(2)
                u:GetAbility("eve_atk_light02"):AddBonusLevel(2)
                u:GetAbility("eve_atk_light03"):AddBonusLevel(2)
                u:GetAbility("eve_atk_heavy"):AddBonusLevel(2)
                u:GetAbility("eve_ultimate"):AddBonusLevel(2)
            end,
        },
        eve_c6 = {
            duration = -1, isBuff = true, isHidden = true, attribute = "PERMANENT",
            Properties = { DamageInc = "%c6_dmg_inc" },
        },
    },
}
