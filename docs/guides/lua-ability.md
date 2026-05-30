# 编写 Lua 能力

每个能力一个 `.lua` 文件，包含三个主要段：`AbilityData` 表、事件处理函数、`Modifiers` 表，以及可选的 `Effects` 表。

## 最小示例

```lua
AbilityData = {
    id = "my_skill",
    name = "我的技能",
    cooldown = 5,
}

function OnSpellStart(caster, ability)
    -- 技能生效逻辑
end
```

## AbilityData 字段

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | 是 | 唯一标识，用于缓存键 |
| `name` | string | 是 | 显示名称 |
| `cooldown` | number | 否 | 冷却秒数，默认 0 |
| `castRange` | number | 否 | 施法距离，默认 0 |
| `castPoint` | number | 否 | 前摇秒数，默认 0 |
| `castAnimation` | string | 否 | 施法动画名 |
| `costs` | table | 否 | `{ Energy = 30 }`，引用已注册资源 ID |
| `parameters` | table | 否 | `{ base_damage = 100 }`，Lua 中通过 `ability:GetParameter("base_damage")` 读取 |
| `projectile` | table | 否 | 投射物配置（见下方） |

### 投射物配置

```lua
projectile = {
    model = "particles/fireball.vfx",
    speed = 1200,
    radius = 100,
    isTracking = false,    -- false=直线弹，true=追踪弹
    dodgeable = true,
    visibleToEnemies = true,
}
```

## 事件处理函数

框架通过 `LuaAbilityLoader` 从 Lua 全局作用域查找并缓存以下函数（在 `AbilityData.EventHandlers` 字典中）：

| 函数 | 触发时机 |
|------|----------|
| `OnSpellStart(caster, ability, targetPoint, targetUnit)` | 施法完成（前摇结束后） |
| `OnAbilityPhaseStart(caster, ability)` | 前摇开始，返回 `false` 取消施法 |
| `OnProjectileHit(caster, ability, target)` | 投射物命中目标 |
| `OnProjectileThink(caster, ability, target)` | 投射物飞行中每帧 |
| `Transforms(caster, ability)` | 变形/转换（预留） |

注意：`OnSpellStart` 的签名是 `(caster, ability, targetPoint?, targetUnit?)`——如果能力不需要目标点或目标单位，这两个参数可以省略。

## Modifier 定义

在 `Modifiers` 表中定义，key 为 modifier 名称：

```lua
Modifiers = {
    my_buff = {
        duration = 5,           -- 秒，-1 为永久
        isBuff = true,
        isDebuff = false,
        isPurgable = true,      -- 是否可被驱散
        isHidden = false,
        attribute = "NONE",     -- NONE | MULTIPLE | STACK_COUNT | PERMANENT

        -- 生命周期
        OnCreated = function(self) end,
        OnRefresh = function(self) end,
        OnDestroy = function(self) end,
        OnIntervalThink = function(self) end,

        -- 声明式钩子
        DeclareFunctions = {
            "MoveSpeedBonusPercentage",
            "PhysicalArmorBonus",
        },

        -- 属性钩子实现（函数名对应 stat 字符串 ID）
        MoveSpeedBonusPercentage = function(self)
            return 20   -- +20% 移速
        end,

        PhysicalArmorBonus = function(self)
            return -5   -- -5 护甲
        end,

        -- 状态声明
        CheckState = {
            stunned = false,       -- 不处于眩晕
            invincible = true,     -- 处于无敌
        },
    },
}
```

### Modifier 字段

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `duration` | 0 | 持续时间秒数，`-1` 配合 `PERMANENT` 属性 |
| `isBuff` | false | 正面效果 |
| `isDebuff` | false | 负面效果 |
| `isPurgable` | true | 可被驱散 |
| `isHidden` | false | 是否在 UI 隐藏 |
| `attribute` | "NONE" | 堆叠行为 |
| `EffectName` | nil | 特效资源路径，OnCreated 自动播放，OnDestroy 自动停止 |
| `EffectAttachType` | nil | 特效附着方式（Dota 风格） |
| `DeclareTags` | nil | 标签列表，自动同步到 UnitEntity.TagSystem |

### 堆叠模式

| 模式 | 行为 |
|------|------|
| `NONE` | 同名覆盖旧 modifier，触发 OnRefresh |
| `MULTIPLE` | 允许多个同名 modifier 共存 |
| `STACK_COUNT` | 同名叠加层数，触发 OnStackCountChanged |
| `PERMANENT` | 永不过期，不受 duration 影响 |

### 属性钩子

在 `DeclareFunctions` 中声明，同名函数实现。引擎在计算属性时遍历所有活跃 modifier 的声明钩子并求和。钩子名可以是任意字符串——Stat ID 无需预定义枚举。注意：DeclareFunctions 中的事件钩子名（如 `"OnTakeDamage"`）会匹配 `ModifierHookType` 枚举，用于事件分发；非事件钩子名则归入 stat 路由。

### Properties 快捷方式

无需写函数，直接返回固定值：

```lua
my_modifier = {
    DeclareFunctions = { "DamageInc" },
    Properties = { DamageInc = 15 },
}
```

`Properties` 的优先级低于同名函数——如果同时定义函数，函数优先。

### StatModifiers 结构化写法

支持 Add/Override 操作：

```lua
my_modifier = {
    StatModifiers = {
        { stat = "AtkMult", op = "Add", value = 0.2 },      -- +20% 攻击力百分比
        { stat = "CritRate", op = "Override", value = 50 },  -- 覆盖暴击率为 50%
    },
}
```

未指定 op 时默认 Add。不可识别的 op 名会安全退化为 Add。

### 间隔回调

`OnCreated` 中调用 `self:StartIntervalThink(1.0)` 启动间隔定时器，`OnIntervalThink` 每秒被调用一次。

## Effects 声明式效果数据

`Effects` 表将技能的判定逻辑和效果执行解耦。格式：

```lua
Effects = {
    main_hit = {
        picker = {
            type = "Area",          -- 目标选择方式
            filter = "Enemy",       -- 过滤器
            shape = {
                type = "Box",
                offset = { x = 0, y = 0, z = 0 },
                rotate = { x = 0, y = 0, z = 0 },
                scale = { x = 1, y = 1, z = 1 },
                radius = 5,
            },
        },
        ops = {
            {
                type = "Damage",
                target = "TARGET",      -- "TARGET"|"CASTER"
                baseStat = "Attack",    -- 公式: unit.GetStat(baseStat) * coeff + raw
                coeff = 0.9,            -- 技能系数
                raw = 0,                -- 固定值
                damageType = "FIRE",
            },
            {
                type = "Modifier",
                target = "TARGET",
                modifier = "my_burn",   -- Modifiers 表中定义的 modifier 名
                duration = 3,
            },
            {
                type = "Projectile",
                projectile = {
                    model = "particles/fireball.vfx",
                    speed = 1200,
                    radius = 80,
                    distance = 600,
                    deleteOnHit = true,
                },
            },
        },
    },
}
```

### Op 类型

| Op 类型 | 说明 |
|---------|------|
| `Damage` | 伤害：`unit.GetStat(baseStat) * coeff + raw`，支持 target=CASTER |
| `Heal` | 治疗：同前公式 |
| `Modifier` | 施加 modifier，按 name 引用 |
| `Projectile` | 发射投射物 |

`target` 字段支持 `"TARGET"`（目标）/ `"CASTER"`（施法者自身），用于操作目标为施法者自身的场景（如自 buff）。

### Shape 形状字段

| 字段 | 说明 |
|------|------|
| `offset` | `{ x, y, z }` 偏移 |
| `rotate` | `{ x, y, z }` 旋转 |
| `scale` | `{ x, y, z }` 缩放 |
| `radius` | 半径 |
| `height` | 高度 |
| `angle` | 角度 |

## Lua 端全局 API

框架注册到 Lua 环境的全局函数和常量：

| 名称 | 说明 |
|------|------|
| `ApplyDamage(victim, attacker, damage, damageType, ability?)` | 造成伤害 |
| `ApplyHeal(target, source, amount)` | 施加治疗 |
| `PlayVfxOnUnit(path, unit, lifeTime?)` | 播放 VFX，返回句柄 |
| `DAMAGE_TYPE_NONE` / `DAMAGE_TYPE_FIRE` / `DAMAGE_TYPE_WATER` / `DAMAGE_TYPE_LIGHTNING` / `DAMAGE_TYPE_WIND` / `DAMAGE_TYPE_EARTH` / `DAMAGE_TYPE_LIGHT` / `DAMAGE_TYPE_DARK` | 伤害类型常量 |

注意：不同于 Dota 2 的 Lua API，CF 不提供 `FindUnitsInRadius`、`caster:PlayEffect`、`ProjectileManager:CreateLinearProjectile` 等方法。这些能力由游戏层（Unity）实现并注入。

## 完整示例

见 `examples/abilities/test_frostbolt.lua`（匹配实际桥接签名的可测试示例）。`examples/abilities/fireball.lua` 为文档风格示例，不完全匹配当前桥接签名。
