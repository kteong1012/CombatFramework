# GodotDemo — 战斗机制设计文档

> 本文档记录正式项目同款战斗机制，供 GodotDemo 原型复现参考。

---

## 1. 韧性值系统（Toughness / Break）

### 1.1 概念

怪物同时拥有**血条**和**韧性条**两个独立资源：

| 资源 | 说明 |
|------|------|
| HP（血量） | 归零即死亡 |
| Toughness（韧性值） | 归零触发破韧；不影响存活 |

### 1.2 削韧

技能在造成伤害的同时，可附带**削韧量（ToughnessReduction）**：

- 伤害 → 扣目标 HP
- 削韧 → 扣目标 Toughness

两者独立计算，互不影响。

### 1.3 破韧触发

当敌方 Toughness 被打至 **≤ 0** 时，立即触发**破韧（Break）**：

1. 造成一次**击破伤害（Break Damage）**
2. 目标进入**破韧状态（Break State）**

#### 击破伤害公式

$$
\text{BreakDamage} = \text{AttackerLevel} \times (1 + \text{BreakDmgBonus}) \times \text{ToughnessMax} \times K
$$

| 参数 | 说明 |
|------|------|
| `AttackerLevel` | 攻击方角色等级 |
| `BreakDmgBonus` | 攻击方破韧增伤属性（加法汇总） |
| `ToughnessMax` | 目标韧性上限；越高破韧反馈越大 |
| `K` | 全局平衡系数，暂定 **0.5** |

> 示例：Lv.60 × (1 + 0.3) × 200 × 0.5 = **7800** 击破伤害

### 1.4 破韧状态（Break State）

目标进入破韧状态后：

| 效果 | 说明 |
|------|------|
| **眩晕（Stunned）** | 目标无法行动，持续 **5 秒**（固定值） |
| **破韧 Flag** | 目标身上挂一个 `broken` 标记 |
| **破韧增伤** | 攻击方对 `broken` 目标的所有伤害乘以 $(1 + \text{BreakDmgBonus})$ |

### 1.5 破韧增伤公式

$$
\text{FinalDamage} = \text{OriginDamage} \times (1 + \text{BreakDmgBonus})
$$

`BreakDmgBonus` 与击破伤害公式中的参数来源相同（攻击方属性，可被命座/装备叠加）。

### 1.6 破韧状态恢复

破韧状态持续 **5 秒**后自动恢复：

- 韧性条**回满**（恢复至 `ToughnessMax`）
- 眩晕状态**解除**
- `broken` Flag **移除**

---

## 2. 命座系统（Constellation）

### 2.1 概念

每个 Hero 角色拥有 **6 个命座**，每个命座对应一个**被动 Ability 槽位**。

- 命座解锁后，对应被动自动 Equip 到专属槽位并**永久生效**（不可卸载）
- 命座 Ability 全部为被动（Passive），不可主动触发

### 2.2 槽位系统

#### 枚举定义

所有槽位通过枚举访问，废弃裸索引。底层仍用数组存储，`slots[(int)SlotType.Xxx]` 取值。

```csharp
public enum SlotType
{
    // ── 主动技能槽（显示在技能栏）──
    NormalAtk   = 0,   // 普攻
    Skill       = 1,   // 战技
    Burst       = 2,   // 终结技
    // ── 被动槽（不显示在技能栏）──
    Passive0    = 3,
    Passive1    = 4,
    // ── 命座槽（不显示在技能栏）──
    Const0      = 5,
    Const1      = 6,
    Const2      = 7,
    Const3      = 8,
    Const4      = 9,
    Const5      = 10,
}
```

#### 槽位显示属性（`Visible`）

每个槽位有一个 `Visible` 标志，**只有 `Visible = true` 的槽位才渲染到技能栏 UI**。

| 槽位类型 | Visible | 说明 |
|---------|---------|------|
| 主动技能槽 | ✅ true | 显示图标、快捷键、耗能 |
| 被动槽 | ❌ false | 不显示，无需配置图标 |
| 命座槽 | ❌ false | 不显示，无需配置图标 |

### 2.3 技能升级型命座（SkillBonus）

部分命座的被动效果为：**令所有带有特定 Flag 的技能等级 +N**。

#### 数据配置（可配置，JSON）

```json
{
  "id": "const_2",
  "events": {
    "OnEquip": [
      {
        "$type": "SkillLevelBonusAction",
        "TargetFlag": "skill_type_burst",
        "LevelBonus": 3
      }
    ]
  }
}
```

#### 汇总时机

`SkillBonus` 采用**汇总（Aggregate）模式**，不修改 BaseLevel：

```
技能等级求值时（GetEffectiveLevel）
  → 遍历 unit 所有已装备命座/被动槽
  → 收集 SkillBonus 中 TargetFlag 匹配当前技能 tags 的所有 LevelBonus
  → EffectiveLevel = BaseLevel + Σ LevelBonus
```

- 无需在装备时修改 `BaseLevel`，避免状态污染
- 命座永不卸载，汇总结果恒增不减
- 同一技能可被多个命座叠加升级

---

<!-- 后续章节在此追加 -->
