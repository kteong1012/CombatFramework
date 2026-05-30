-- 测试用能力：冰弹
-- 匹配 LuaAbilityLoader 解析格式

AbilityData = {
    id = "frostbolt",
    name = "冰弹",
    cooldown = 6,
    castRange = 500,
    castPoint = 0.2,
    castAnimation = "cast_spell",
    costs = {
        Energy = 40,
    },
    parameters = {
        base_damage = 80,
        slow_pct = 30,
    },
    projectile = {
        model = "particles/frostbolt.vfx",
        speed = 1000,
        radius = 80,
        isTracking = true,
        dodgeable = true,
        visibleToEnemies = true,
    },
    OnSpellStart = function(caster, ability)
    end,
    OnProjectileHit = function(caster, ability, target)
    end,
    Modifiers = {
        frostbolt_slow = {
            duration = 3,
            isDebuff = true,
            isPurgable = true,
            isHidden = false,
            OnCreated = function(self) end,
            OnDestroy = function(self) end,
            OnIntervalThink = function(self) end,
            DeclareFunctions = {
                "MoveSpeedBonusPercentage",
                "PhysicalArmorBonus",
            },
            MoveSpeedBonusPercentage = function(self) return -20 end,
            PhysicalArmorBonus = function(self) return -5 end,
        },
        frostbolt_shield = {
            duration = -1,
            isBuff = true,
            isPurgable = false,
            isHidden = true,
            attribute = "PERMANENT",
            OnCreated = function(self) self:StartIntervalThink(2.0) end,
            OnIntervalThink = function(self) end,
            DeclareFunctions = {
                "ConstantHealthRegen",
            },
            ConstantHealthRegen = function(self) return 5 end,
        },
    },
}
