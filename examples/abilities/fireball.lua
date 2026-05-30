-- 火球
-- 展示单文件数据驱动格式的示例能力。
-- 注意：本文件是文档风格示例，展示完整的能力结构。
-- 框架接续测试使用 examples/abilities/test_frostbolt.lua。
--
-- 每个能力文件包含三个段：
--   1. AbilityData 表（必填）
--   2. 事件处理函数（可选，签名匹配框架 LuaAbilityLoader 的缓存逻辑）
--   3. Modifiers 表（可选）

AbilityData = {
    id = "fireball",
    name = "火球",

    -- 冷却
    cooldown = 8,

    -- 资源消耗（引用已注册的资源 ID）
    costs = {
        Energy = 30,
    },

    -- 施法参数
    castRange = 600,
    castPoint = 0.3,          -- 秒
    castAnimation = "cast_ability",

    -- 行为标志
    behavior = {
        "POINT",               -- 需要目标点
        "ENEMIES",             -- 可对敌
    },

    -- 投射物
    projectile = {
        model = "particles/fireball_projectile.vfx",
        speed = 1200,
        radius = 100,
        isTracking = false,    -- 线性弹
        dodgeable = true,
        visibleToEnemies = true,
    },

    -- 能力参数（Lua 中通过 ability:GetParameter("name") 引用）
    parameters = {
        base_damage = 150,
        burn_duration = 3,
        burn_dps = 30,
        splash_radius = 200,
    },
}

-- 开始施法（前摇阶段）
-- 签名匹配 LuaAbilityLoader 的缓存逻辑
function OnAbilityPhaseStart(caster, ability)
    PlayVfxOnUnit("particles/fireball_cast.vfx", caster, 1.0)
    return true   -- 返回 false 取消施法
end

-- 施法完成（前摇结束后）
function OnSpellStart(caster, ability, targetPoint, targetUnit)
    -- 投射物由 ProjectileManager 创建（框架侧 C# 驱动）
    -- 这里通过 Effects 声明式系统触发效果
end

-- 投射物命中单位或到达终点
function OnProjectileHit(caster, ability, hitTarget, hitPosition)
    local params = ability:GetParameter

    if hitTarget ~= nil then
        -- 直击：伤害 + 燃烧
        ApplyDamage(hitTarget, caster, params("base_damage"), DAMAGE_TYPE_FIRE, ability)
    end

    -- 溅射等区域效果由 Effects 表或游戏层实现
    -- 框架不提供 FindUnitsInRadius，由 Unity 侧实现
end

-- ========================================
-- Effects 声明式效果数据
-- 由 Timeline clip 调度执行，数据与执行解耦
-- ========================================

Effects = {
    main_hit = {
        picker = {
            type = "Area",
            filter = "Enemy",
            shape = {
                type = "Box",
                radius = 5,
                offset = { x = 0, y = 0, z = 0 },
                rotate = { x = 0, y = 0, z = 0 },
                scale = { x = 1, y = 1, z = 1 },
            },
        },
        ops = {
            {
                type = "Damage",
                target = "TARGET",
                baseStat = "Attack",
                coeff = 0.9,
                raw = 150,
                damageType = "FIRE",
            },
            {
                type = "Modifier",
                target = "TARGET",
                modifier = "fireball_burn",
                duration = 3,
            },
        },
    },
}

-- ========================================
-- Modifier 定义
-- ========================================

Modifiers = {
    fireball_burn = {
        duration = 3,
        isDebuff = true,
        isPurgable = true,
        isHidden = false,

        OnCreated = function(self)
            self:StartIntervalThink(1.0)
        end,

        OnIntervalThink = function(self)
            local parent = self:GetParent()
            local caster = self:GetCaster()
            local ability = self:GetAbility()
            -- Lua 端用全局 ApplyDamage（positional args）
            ApplyDamage(parent, caster, 30, DAMAGE_TYPE_FIRE, ability)
        end,

        OnDestroy = function(self)
            -- 自动停 VFX（ModifierManager 处理）
        end,

        DeclareFunctions = {
            "DamageRed",
        },

        -- 燃烧期间降低火抗
        Properties = {
            DamageRed = -15,
        },
    },
}
