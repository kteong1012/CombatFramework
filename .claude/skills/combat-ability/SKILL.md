---
name: combat-ability
description: |
  生成 CombatFramework Lua 能力文件。当用户提到创建/编写/生成 combat 技能、能力、法术、buff/debuff/modifier 时触发，包括"写一个新技能""帮我做一个技能""scaffold an ability"等表述。
  不要仅限于"skill"关键词——任何关于 CombatFramework 能力创建的请求都使用本 skill。
---

# CombatFramework Lua 能力生成器

为 CombatFramework 创建标准格式的 Lua 能力文件。

## 工作流程

1. **收集需求** — 一次性问完所有问题（见下方模板），不要分多次询问
2. **生成文件** — 按模板生成 `.lua` 文件
3. **写入磁盘** — 保存到 `examples/abilities/{id}.lua`

## 交互需求收集

### 必填
- **id** — 英文小写+下划线，如 `"fireball"` / `"frostbolt"`
- **name** — 中文显示名，如 `"火球"` / `"冰弹"`
- **一句话描述** — 能力效果概述

### 选填
- cooldown → 冷却秒数（默认 0）
- castRange → 施法距离（默认 0）
- castPoint → 前摇秒数（默认 0）
- castAnimation → 施法动画名
- costs → 消耗，如 `{ Energy = 30 }`
- behavior → 行为标志数组，如 `{"POINT", "ENEMIES"}`
- parameters → 调参表，如 `{ base_damage = 150, burn_duration = 3 }`

### 投射物（问用户是否需要）
- model, speed, radius, isTracking, dodgeable, visibleToEnemies

### Modifier 定义

对每个 modifier 收集：
- **名称** — 英文 key，如 `frostbolt_slow`
- **类型** — buff / debuff / 隐藏
- **duration** — 秒，-1 为永久
- **isPurgable** — 是否可驱散
- **attribute** — NONE / MULTIPLE / STACK_COUNT / PERMANENT
- **DeclareFunctions** — 从下方钩子参考中选择
- **是否需要 OnIntervalThink** — 如需则在 OnCreated 中 `self:StartIntervalThink(1.0)`
- **CheckState** — 状态声明如 `{ Stun = true, Invisible = false }`
- **OnCreated / OnDestroy 逻辑** — 做什么

## 输出格式规则

1. **文件路径**: `examples/abilities/{id}.lua`
2. **文件头**: `-- {name}` 注释
3. **AbilityData**: 全局表，包含 id、name 及所有选填字段
4. **事件函数**: 全局函数 `OnSpellStart(caster, ability)` 至少一个
5. **Modifiers**: 全局表，key 为 modifier 名称
6. **参数读取**: 使用 `ability:GetParameter("key")`，不硬编码数值
7. **钩子命名**: DeclareFunctions 中使用**字符串格式**（如 `"MoveSpeedBonusPercentage"`），函数名与字符串完全一致

## 完整模板

```lua
-- {name}
-- {描述}

AbilityData = {
    id = "{id}",
    name = "{name}",
    cooldown = {cooldown},
    castRange = {castRange},
    castPoint = {castPoint},
    castAnimation = "{castAnimation}",
    costs = { {resourceId} = {amount} },
    behavior = { {behavior_flags} },
    parameters = {
        {param1} = {value1},
        {param2} = {value2},
    },
    projectile = {
        model = "{particle_path}",
        speed = {speed},
        radius = {radius},
        isTracking = {true/false},
        dodgeable = {true/false},
        visibleToEnemies = {true/false},
    },
}

-- 可选的事件处理函数
-- OnAbilityPhaseStart, OnSpellStart, OnProjectileHit, OnProjectileThink, Transforms
-- 签名均为 (caster, ability, ...)

function OnSpellStart(caster, ability, targetPoint, targetUnit)
    -- 技能生效逻辑
end

Modifiers = {
    {modifier_name} = {
        duration = {duration},
        isBuff = {true/false},
        isDebuff = {true/false},
        isPurgable = {true/false},
        isHidden = {true/false},
        attribute = "{attribute}",

        OnCreated = function(self)
            -- 初始化（注意：无 kv 参数，仅传递 self）
        end,

        OnDestroy = function(self)
            -- 清理
        end,

        OnIntervalThink = function(self)
            -- 间隔回调
        end,

        DeclareFunctions = {
            "WalkSpeed",
        },

        WalkSpeed = function(self)
            return {value}
        end,

        -- 或用 Properties 快捷方式
        -- Properties = { WalkSpeed = {value} },

        CheckState = {
            stunned = {true/false},
            silenced = {true/false},
        },
    },
}
```

## 属性钩子参考（DeclareFunctions 字符串值）

Stat ID 是任意字符串，无需预定义枚举。常见 stat ID 参考 `CStatId` 常量：

### Stat 钩子（轮询求和）
| 字符串 | 类型 | 说明 |
|--------|------|------|
| `"WalkSpeed"` | float | 移速百分比加成 |
| `"CritRate"` | float | 暴击率（0~1） |
| `"CritDMG"` | float | 暴击伤害百分比 |
| `"HealBonus"` | float | 治疗加成百分比 |
| `"HealRecv"` | float | 受疗加成百分比 |
| `"DamageInc"` | float | 伤害增加百分比 |
| `"DamageRed"` | float | 伤害减免百分比 |
| `"DefIgnore"` | float | 防御忽略固定值 |
| `"DefIgnoreRate"` | float | 防御穿透百分比 |
| `"HpMin"` | float | 薄葬式生命下限（取最大值非求和） |

### 复合属性分量（用于 modifier 加 Attack/HP/Defense）
| Stat 名 | 说明 |
|---------|------|
| `"AtkBase"` | 攻击力基础值 |
| `"AtkMult"` | 攻击力百分比加成（如 0.2 = +20%） |
| `"AtkAdd"` | 攻击力固定值 |
| `"HpBase"` | 生命基础值 |
| `"HpMult"` | 生命百分比加成 |
| `"HpAdd"` | 生命固定值 |
| `"DefBase"` | 防御基础值 |
| `"DefMult"` | 防御百分比加成 |
| `"DefAdd"` | 防御固定值 |

### 事件钩子（通知分发）
| 字符串 | 触发时机 |
|--------|----------|
| `"OnTakeDamage"` | 单位受到伤害时（接收 `DamageEventData`） |
| `"OnDealDamage"` | 单位造成伤害时（接收 `DamageEventData`） |
| `"OnHealReceived"` | 单位受到治疗时（接收 `HealEventData`） |
| `"OnHealDealt"` | 单位造成治疗时（接收 `HealEventData`） |
| `"OnKill"` | 单位击杀目标时 |
| `"OnDeath"` | 单位死亡时 |
| `"OnAttackStart"` | 攻击前摇开始时 |
| `"OnAttackHit"` | 攻击命中时 |
| `"OnAbilityPhaseStart"` | 技能前摇开始时（可阻止施法，接收 `PreCastEventData`） |
| `"OnSpellStartCast"` | 施法开始时 |
| `"OnSpellEndCast"` | 施法完成时 |
| `"OnStackCountChanged"` | 叠加层数变化时 |

## CheckState 可用状态（小写字符串）
`"stunned"` / `"invincible"` / `"dead"` / `"silenced"`

## 堆叠模式
| attribute | 行为 |
|-----------|------|
| `"NONE"` | 同名覆盖旧 modifier，触发 OnRefresh |
| `"MULTIPLE"` | 允许多个同名共存 |
| `"STACK_COUNT"` | 同名叠加层数 |
| `"PERMANENT"` | 永不过期 |

## Lua API 参考

### 框架注册的全局 API
```lua
-- 伤害（positional args，不是 table）
ApplyDamage(victim, attacker, damage, damageType, ability?)

-- 治疗
ApplyHeal(target, source, amount)

-- VFX
PlayVfxOnUnit(path, unit, lifeTime?)

-- 伤害类型常量
DAMAGE_TYPE_NONE
DAMAGE_TYPE_FIRE
DAMAGE_TYPE_WATER
DAMAGE_TYPE_LIGHTNING
DAMAGE_TYPE_WIND
DAMAGE_TYPE_EARTH
DAMAGE_TYPE_LIGHT
DAMAGE_TYPE_DARK
```

### C# 类型暴露给 Lua（通过 UserData）
```lua
-- ModifierInstance （self 上下文）
self:GetParent()                  -- → UnitEntity（modifier 所属单位）
self:GetCaster()                  -- → UnitEntity（modifier 来源单位）
self:GetAbility()                 -- → AbilityInstance（所属能力）
self:StartIntervalThink(seconds)  -- 启动间隔定时器
self:SetDuration(seconds)         -- 设置持续时间
self:SetStackCount(n)             -- 设置层数
self:GetRemainingTime()           -- 剩余时间

-- AbilityInstance
ability:GetParameter("key")       -- → float（单个参数）

-- UnitEntity
unit:HasTag("stunned")
unit:GetResource("HP")

-- ResourceSystem
unit.Resources:GetCurrent("HP")
unit.Resources:GetMax("HP")
```

### 注意
- `ApplyDamage` 使用 **positional args**（5 个参数），不是 Dota 2 的 table 格式
- 框架**不提供** `FindUnitsInRadius`、`caster:GetPosition()`、`caster:PlayEffect()` 等方法
- 单位查找、位置查询、特效播放由 Unity 侧实现并通过 `IVfxEffectService` 桥接
- 效果调度由 Timeline clip 通过 `Effects` 表驱动，不在 Lua 函数中手动创建投射物
