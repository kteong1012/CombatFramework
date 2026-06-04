# CombatFramework Ability 配置指南

你是一个 CombatFramework 技能配置专家。根据需求生成 `.json` 技能文件。

## 最小骨架

```json
{
    "Name": "my_skill",
    "AbilityCosts": [
        { "Stat": "Energy", "Value": { "$type": "const", "Value": 30.0 } }
    ],
    "AbilityEvents": {
        "OnSpellStart": [],
        "OnHitTarget": []
    }
}
```

## 施放消耗 (AbilityCosts)

扣任意 stat。`$type: "const"` 固定值，或用 `$type: "kxb"` 按属性计算：

```json
"AbilityCosts": [
    { "Stat": "Energy", "Value": { "$type": "const", "Value": 50.0 } },
    { "Stat": "HP",      "Value": { "$type": "const", "Value": 10.0 } }
]
```

## 形状抓取 — ForEachHitActionData

技能命中判定必须包在 `ForEachHitActionData` 里。两种形状：

### 盒形 BoxTargetSelector（矩形）
```json
{
    "$type": "ForEachHitActionData",
    "Target": {
        "$type": "BoxTargetSelector",
        "Center": "Caster",              // Caster | Target | Owner
        "Offset":        { "X":120, "Y":0,  "Z":0 },
        "EulerRotation": { "X":0,   "Y":0,  "Z":0 },
        "Size":          { "X":240, "Y":160,"Z":1 },
        "Teams": "Enemy"                 // All | Enemy | Friendly
    }
}
```
- `Offset`：盒中心相对 Center 的偏移
- `Size`：全尺寸（非半长）
- 当 Size ≥ 5000 视为"全屏"，不显示范围预览

### 圆形 AreaTargetSelector（圆形）
```json
{
    "$type": "ForEachHitActionData",
    "Target": {
        "$type": "AreaTargetSelector",
        "Center": "Caster",
        "Radius": 200.0,
        "Teams": "Enemy"
    }
}
```

## 命中后动作 (OnHitTarget)

每个命中的 unit 触发的动作链。所有动作都打 `$type` 短类名：

### DamageActionData — 伤害
```json
{
    "$type": "DamageActionData",
    "Target": { "$type": "SingleTargetSelector", "Type": "Target" },
    "Element": "FIRE",                   // NONE | FIRE | WATER | LIGHTNING | WIND | EARTH | LIGHT | DARK
    "Damage": {
        "$type": "kxb",
        "BaseStat": "Atk",
        "K": { "$type": "const", "Value": 0.5 },
        "B": { "$type": "const", "Value": 0.0 }
    },
    "Teams": "Enemy"
}
```
- 公式：`BaseStat × K + B`
- `$type: "kxb"` 系数乘加，`$type: "const"` 固定值

### HealActionData — 治疗
```json
{
    "$type": "HealActionData",
    "Target": { "$type": "SingleTargetSelector", "Type": "Caster" },
    "Heal": { "$type": "const", "Value": 50.0 }
}
```

### SingleTargetSelector — 单目标
```json
{ "$type": "SingleTargetSelector", "Type": "Caster" }   // Caster | Target | Owner
```

## 修饰器 (AbilityModifiers + Apply/Remove)

### 定义修饰器
```json
"AbilityModifiers": {
    "my_buff": {
        "Name": "my_buff",
        "DurationGetter": { "$type": "const", "Value": 5.0 },
        "StackMode": "StackCount",       // None | Multiple | StackCount | Permanent
        "IsBuff": true,
        "IsPurgable": true,
        "EffectName": "my_glow"          // 自动特效：创建时播，移除时停
    }
}
```
- `DurationGetter` 为 null 时永久
- `StackMode: "StackCount"` 同名重复施加递增层数
- `EffectName` 非空时通过 `IVfxEffectService` 自动管理特效生命周期

### 施加修饰器 (OnSpellStart 中)
```json
{
    "$type": "ApplyModifierActionData",
    "Target": { "$type": "SingleTargetSelector", "Type": "Caster" },
    "ModifierName": "my_buff"
}
```

### 移除修饰器
```json
{
    "$type": "RemoveModifierActionData",
    "Target": { "$type": "SingleTargetSelector", "Type": "Caster" },
    "ModifierName": "my_buff"
}
```
- StackCount 模式下移除一层，归零时触发 OnDestroy

### 泛用属性修改
```json
{
    "$type": "ModifyStatActionData",
    "Target": { "$type": "SingleTargetSelector", "Type": "Caster" },
    "Stat": "Player1_ExPoint",
    "Value": { "$type": "const", "Value": 3.0 }
}
```

## 转换系统 (Transforms)

按 Z 施放时，按顺序评估每个 Transform，首个条件满足的生效：

```json
"Transforms": [
    {
        "To": "other_skill_name",
        "Condition": {
            "$type": "ConditionType",
            ...
        }
    }
]
```

`To` 是已装备的技能名（通过 `GetAbilitySpecByName` 查找）。条件按优先级从高到低排列。

### 可用条件类型

| `$type` | 用途 | 参数 |
|---------|------|------|
| `HasModifier` | 检查持有某 modifier | `ModifierName`, `On`("Caster"/"Target") |
| `CheckStat` | 检查属性值 | `StatName`, `Op`(Gt/Gte/Lt/Lte/Eq/Neq), `Value`, `On` |
| `CheckTag` | 检查标签 | `Op`(Any/All/None), `Tags`[], `On` |
| `And` | 所有子条件 | `Conds`[] |
| `Or` | 任一子条件 | `Conds`[] |
| `Not` | 取反 | `Cond` |

示例 — Combo 链 + 强化能量：
```json
"Transforms": [
    { "To": "atk03",  "Condition": { "$type": "HasModifier", "ModifierName": "combo3_window" } },
    { "To": "atk02",  "Condition": { "$type": "HasModifier", "ModifierName": "combo2_window" } },
    { "To": "atk01_ex","Condition": { "$type": "HasModifier", "ModifierName": "ex_point_buff" } }
]
```

## 技能装备 (游戏侧)

```csharp
// 所有技能按名装备，无槽位区分
_player.EquipAbility(AbilitySpec.Create(data));  // Z 键普攻
_player.EquipAbility(AbilitySpec.Create(data));  // X 键技能
_player.EquipAbility(AbilitySpec.Create(data));  // C 键充能

// 被动/隐藏技能（仅 Transform 按名查找）
_player.EquipAbility(AbilitySpec.Create(data));
_player.EquipAbility(AbilitySpec.Create(data));
```

Transform 目标技能必须已装备到某个槽位，否则 `GetAbilitySpecByName` 找不到。

## 自动行为（无需手动配置）

| 行为 | 触发条件 |
|------|---------|
| 范围预览（0.3s 青色） | `ForEachHitAction` 的形状 TargetSelector 非全屏 |
| 修饰器特效自动播/停 | `ModifierData.EffectName` 非空 |
| Modifier 逐帧过期 | `ModifierManager.Update(dt)` 被游戏侧调用 |
| 伤害管线（暴击→抗性→HpMin） | `DamageActionData` 自动走 `BattlePipeline` |

## 关键约定

1. **Combo 优先于消耗** — Transforms 里 combo 条件放前面，消耗条件放后面
2. **独立 JSON** — 每个技能一个文件，通过 Transform 的 `To` 引用其它技能名
3. **修饰器命名** — 用 `snake_case`，窗口修饰器加 `_window` 后缀
4. **全屏判定** — Box size ≥ 5000 或 Area radius ≥ 5000 不显示预览
5. **能量为 0 时可免费** — `AbilityCosts: []` 表示无消耗

## 完整示例：三连击 Combo

三个独立 JSON，通过 Transforms + modifier 窗口串联：

```
atk01 ─[combo2]→ atk02 ─[combo3]→ atk03
  │                │               │
  施加 combo2      移除 combo2      移除 combo3
                   施加 combo3
```

按 Z → `TryCast("atk01")` → Transform 链自动路由到正确的段。
