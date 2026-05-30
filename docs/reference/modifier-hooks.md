# Modifier 钩子参考

Modifier 通过函数或 `Properties` 表声明钩子，引擎在事件触发或 stat 收集时调用。

## 事件钩子（通知分发）

这些钩子在特定事件发生时被调用，所有在 `DeclareFunctions` 中声明了该钩子的 modifier 收到通知。事件钩子名被解析为 `ModifierHookType` 枚举。

| 枚举值 | 事件数据 | 触发时机 |
|--------|----------|----------|
| `OnTakeDamage` | `DamageEventData` | 单位受到伤害时 |
| `OnDealDamage` | `DamageEventData` | 单位造成伤害时 |
| `OnHealReceived` | `HealEventData` | 单位受到治疗时 |
| `OnHealDealt` | `HealEventData` | 单位造成治疗时 |
| `OnKill` | `KillEventData` | 单位击杀目标时 |
| `OnDeath` | `DeathEventData` | 单位死亡时 |
| `OnAttackStart` | `AttackEventData` | 攻击前摇开始时 |
| `OnAttackHit` | `AttackEventData` | 攻击命中时 |
| `OnAbilityPhaseStart` | `PreCastEventData` | 技能前摇开始时（可阻止施法） |
| `OnSpellStartCast` | `SpellEventData` | 施法开始时 |
| `OnSpellEndCast` | `SpellEventData` | 施法完成时 |
| `OnStackCountChanged` | `StackEventData` | 叠加层数变化时 |

事件钩子在 Lua 中通过 `DeclareFunctions` 声明：

```lua
my_modifier = {
    DeclareFunctions = {
        "OnTakeDamage",
    },
    OnTakeDamage = function(self, eventData)
        -- eventData.RawDamage, eventData.FinalDamage, eventData.IsCritical
    end,
}
```

注意：与 `DeclareFunctions` 自动发现机制配合——如果 modifier 表上有以对应枚举名命名的函数，会自动注册到 `DeclaredHooks` 中，无需显式声明。

## Stat 钩子（轮询求和）

引擎计算单位属性时，遍历所有活跃 modifier，对每个声明的 stat ID 执行函数或取 Properties 表的值，**求和**得到总贡献。

Stat ID 是字符串，无需预先定义枚举。常见的 stat ID 可通过 `StatId` 常量类引用。

引擎通过 `ModifierManager.AggregateStat(statId, baseValue)` 轮询求和。聚合规则：
1. PropertyHooks（Lua 函数闭包）→ 等价 Add
2. Properties（KV 表）→ 等价 Add，与 PropertyHooks 互斥
3. StatModifiers（结构化条目）→ 支持 Add / Override
4. Override 存在时直接返回 override 值

Stat hook 在 Lua 中通过 `DeclareFunctions` 声明：

```lua
my_modifier = {
    DeclareFunctions = {
        "WalkSpeed",
        "CritRate",
    },
    WalkSpeed = function(self)
        return 25  -- +25% 移速
    end,
    CritRate = function(self)
        return 10  -- +10% 暴击率
    end,
}
```

与事件钩子的区别：stat 钩子名不会被解析为 `ModifierHookType` 枚举，而是归入 stat 路由（`DeclaredStats` 列表）。

## Properties 快捷方式

无需写函数，直接返回固定值：

```lua
my_modifier = {
    DeclareFunctions = {
        "DamageInc",
    },
    Properties = {
        DamageInc = 15,  -- 等效于函数返回 15
    },
}
```

`Properties` 的优先级低于同名函数——如果同时定义函数，函数优先。

## StatModifiers 结构化写法

```lua
my_modifier = {
    StatModifiers = {
        { stat = "AtkMult", op = "Add", value = 0.2 },
        { stat = "CritRate", op = "Override", value = 50 },
    },
}
```

## 自动发现机制

LuaAbilityLoader 在解析 Modifier 表时，会自动扫描所有非生命周期的函数值：
- 如果函数名匹配 `ModifierHookType` 枚举值 → 自动加入 `DeclaredHooks`
- 否则 → 自动加入 `DeclaredStats`

这意味着你不需要在 `DeclareFunctions` 中显式声明已有的函数——但显式声明仍受支持且推荐用于 `Properties` 快捷方式。
