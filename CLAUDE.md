# CombatFramework

独立的 .NET 战斗能力框架，面向 ARPG 使用。采用 JSON 数据驱动 + C# 可扩展 Action 类的架构，通过 `CFBridge` 抽象层与宿主引擎（Unity/Godot）解耦。

## 核心原则

本项目将 Dota 2 的 ability/modifier/event 架构思想移植到 C# (.NET)，去掉 MOBA 特有元素，保留可组合性和灵活性。能力定义使用 **JSON 配置文件**，行为通过 **C# Action 类** 组合实现，支持子类扩展。

## 架构分层

```
┌──────────────────────────────────────────────────┐
│  第三层：JSON 能力数据 + C# Action 行为层           │
│  每个 ability 一个 .json 文件，包含：               │
│  - AbilityData（参数、消耗、标签、转换）             │
│  - AbilityEvents（OnSpellStart 等 → Action 列表）  │
│  - AbilityModifiers（ModifierData 定义）           │
│  - Action 子类（Damage/Heal/Modifier/...）         │
│  - TargetSelector（单目标/范围/阵营过滤）            │
├──────────────────────────────────────────────────┤
│  第二层：运行时实例 + 抽象桥接                       │
│  AbilitySpec | ModifierSpec | ModifierManager      │
│  TagSystem | CFBridge（Vfx/Query/Formula/Element） │
│  AbilityEventAction | TargetSelector               │
├──────────────────────────────────────────────────┤
│  第一层：数值引擎                                   │
│  StatsManager：复合属性 Base×(1+Mult)+Add          │
│  BattlePipeline：伤害公式（防御/抗性/暴击/破韧）     │
│  ToughnessPipeline：韧性/破韧系统                   │
│  EventBus：全局事件广播                             │
└──────────────────────────────────────────────────┘
```

## 语言与运行时

- **运行时**：.NET (C#)，标准类库
- **消费方**：Unity / Godot 通过 DLL 引用
- **目标框架**：`net48;net8.0`（多目标）
- **序列化**：Newtonsoft.Json 13.0.4，多态 `$type` 解析
- **无脚本语言依赖**：能力定义纯 JSON，无需 Lua

## 目标游戏类型：ARPG

**去掉的 MOBA 系统：**
- 三主属性 (STR/AGI/INT) → 替换为自定义属性系统
- 升级技能点分配
- 金钱/死亡扣钱/买活
- 小兵/防御塔/泉水
- 经验范围分摊

**保留并适配的系统：**
- Ability 实例生命周期
- Modifier 系统（增益/减益、叠加、永久、标签）
- 事件系统（能力级、Modifier 级、全局）
- 伤害管线（防御/抗性/暴击/破韧/部位吸收）
- 韧性/破韧系统（削韧→破韧→击破伤害→眩晕恢复）
- 特效自动播放/停止（EffectName，通过 CFBridge.Vfx）

## 属性系统

### 复合属性公式

属性通过 `StatsManager` 管理，分量命名约定为 `{Name}_{Suffix}`：

```
X = X_Base × (1 + X_Mult) + X_Add
若存在 X_Max，则结果 = Min(结果, X_Max - X_Block)
```

- **_Base**：基础值（等级/配置赋予）
- **_Mult**：百分比加成（如 +25% = 0.25）
- **_Add**：固定加成（如装备 +50）
- **_Max**：属性上限（可选）
- **_Block**：上限扣除量（可选，用于负面状态缩减可用上限）

### 关键 Stat ID

游戏侧自由命名，无预设枚举。常用约定：

| Stat | 说明 |
|------|------|
| `Atk_Base / Atk_Mult / Atk_Add` | 攻击力三分量 |
| `DefFinal` | 最终防御值 |
| `HP` | 当前生命值 |
| `CritRate` / `CritDMG` | 暴击率(%) / 暴击伤害(倍率) |
| `DamageInc` / `DamageRed` | 伤害增加 / 减免 |
| `DefIgnore` / `DefIgnoreRate` | 防御忽略（固定/百分比 0~1） |
| `WeakMult` | 易伤倍率 |
| `BreakDmgBonus` | 破韧增伤属性 |
| `BreakMultFinal` | 破韧状态增伤（框架自动写入） |
| `Toughness` / `ToughnessMax` | 韧性值 / 韧性上限 |
| `Energy` | 能量资源（示例） |

### 属性来源

```
角色属性 = 配置初始值 + 被动技能(Modifier Properties) + modifier 聚合
```

## 伤害系统

### 伤害流程（DefaultBattleFormula）

```
ApplyDamage(victim, attacker, rawDamage, element)
  │
  ├─ 1. 防御减免：damage = raw × c / (effectiveDef + c)
  │      c = attacker.Level × 10 + 100
  │      effectiveDef = victim.DefFinal × (1 - defIgnoreRate) - defIgnore
  ├─ 2. 增减伤：× (1 + attacker.DamageInc - victim.DamageRed)
  ├─ 3. 易伤：  × (1 + victim.WeakMult)
  ├─ 4. 元素抗性：× (1 - resistance + penetration)
  │      抗性/穿透 stat 由 IElementProvider 提供映射
  ├─ 5. 破韧增伤：× (1 + victim.BreakMultFinal)
  ├─ 6. 部位吸收：× absorptionRate（默认 1.0）
  ├─ 7. 暴击判定：每 hit 独立 (critRate% 概率 × critDmg)
  ├─ 8. 扣血 + EventBus.EntityHurt
  └─ 9. HP ≤ 0 → EventBus.EntityKilled
```

### DamageContext

调用方可传入 `DamageContext` 叠加修正：
- `ExtraCritRate` / `ExtraCritDamage` — 额外暴击
- `ExtraDefIgnoreRate` — 额外破甲
- `AbsorptionRate` — 部位吸收系数

### 伤害公式可替换

通过 `CFBridge.Bridge.Formula` 注入自定义 `IBattleFormula` 实现。

## 韧性/破韧系统

怪物拥有独立韧性条（Toughness），技能可附带削韧效果。

### 流程

```
ApplyToughness(victim, attacker, reduction)
  → 扣减 Toughness
  → 若 ≤ 0 → TriggerBreak:
     1. 击破伤害 = attacker.Level × (1 + BreakDmgBonus) × ToughnessMax × 0.5
     2. 写入 victim.BreakMultFinal（后续伤害增伤）
     3. 施加 "broken" modifier（挂 stunned + broken 标签，持续 5s）
     4. 5s 后自动恢复：韧性回满，标签移除，BreakMultFinal 清零
```

### 关键参数

| 参数 | 说明 |
|------|------|
| `ToughnessMax` | 韧性上限（怪物配置） |
| `Toughness` | 当前韧性（运行时） |
| `BreakDmgBonus` | 攻击方破韧增伤 |
| `GlobalBreakCoeff` | 全局系数 K = 0.5 |
| `BreakDuration` | 破韧状态持续 5s |

## Ability 系统

### 能力数据定义（JSON）

```jsonc
{
    "Name": "my_skill",
    "Tags": ["skill_type_burst"],
    "AbilityCosts": [
        { "Stat": "Energy", "Value": { "$type": "const", "Value": 30 } }
    ],
    "AbilitySpecialFields": {
        "damage": [100, 150, 200]  // 按等级取值
    },
    "AbilityEvents": {
        "OnSpellStart": [
            {
                "$type": "DamageAction",
                "Target": { "$type": "SingleTargetSelector", "Type": "Target" },
                "Element": "FIRE",
                "Damage": { "$type": "ability_special", "Name": "damage" }
            }
        ]
    },
    "AbilityModifiers": {
        "my_buff": {
            "Name": "my_buff",
            "IsBuff": true,
            "DurationGetter": { "$type": "const", "Value": 5 },
            "Properties": [
                { "Stat": "Atk_Mult", "Op": "Add", "Value": { "$type": "const", "Value": 0.2 } }
            ]
        }
    }
}
```

### AbilitySpec 运行时实例

- `AbilityData`：JSON 反序列化的共享模板（只读）
- `AbilitySpec`：运行时状态（Level、Owner、SlotIndex）
- `TryGetLevelValue(name)`：按当前等级从 `AbilitySpecialFields` 取值
- `GetEffectiveLevel()`：BaseLevel + 所有命座/被动 SkillBonus 汇总
- `CanCast(out reason)`：逐项对比 AbilityCosts 与 Owner Stats
- `DeductCosts()`：实际扣除资源

### 事件驱动模型

`AbilitySpec` 提供虚方法，默认行为是 dispatch `AbilityData.AbilityEvents` 中配置的 Action 列表：

| 事件 | 触发时机 |
|------|----------|
| `OnSpellStart` | 施法完成瞬间 |
| `OnAbilityPhaseStart` | 开始蓄力 |
| `OnProjectileHitUnit` | 弹射物命中单位 |
| `OnProjectileFinish` | 弹射物飞行结束 |
| `OnChannelFinish` | 引导完成 |
| `OnChannelInterrupted` | 引导被打断 |
| `OnToggleOn/Off` | 开关技能 |
| `OnUpgrade` | 技能升级 |
| `OnEquipped/OnUnequipped` | 装备/卸下（被动技能入口） |
| `OnHitTarget` | ForEachHitAction 命中每个目标 |

### 技能转换（Transforms）

`AbilityData.Transforms` 支持条件技能替换：

```jsonc
{
    "Transforms": [
        {
            "To": "skill_variant",
            "Condition": { "$type": "HasModifier", "ModifierName": "mod_charged" }
        }
    ]
}
```

`TryCast` 时按顺序评估，首个满足条件的转换生效。支持复合条件：`And` / `Or` / `Not` / `HasModifier` / `CheckTag`。

子类可通过 override 虚方法实现自定义逻辑。

## Modifier 系统

### ModifierData 配置

```jsonc
{
    "Name": "my_buff",
    "Class": "MyCustomSpec",          // 可选：自定义 ModifierSpec 子类
    "IsBuff": true,                   // 增益
    "IsDebuff": false,                // 减益
    "IsHidden": false,                // 是否隐藏
    "IsPurgable": true,               // 可驱散
    "StackMode": "StackCount",        // None | Multiple | StackCount | Permanent
    "DurationGetter": { "$type": "const", "Value": 5 },
    "ThinkInterval": 1.0,             // OnIntervalThink 间隔（秒）
    "EffectName": "vfx_buff_aura",   // 自动特效
    "EffectScale": { "$type": "const", "Value": 1.5 },
    "Properties": [
        { "Stat": "Atk_Mult", "Op": "Add", "Value": { "$type": "const", "Value": 0.2 } }
    ],
    "Events": {
        "OnCreated": [ /* actions */ ],
        "OnIntervalThink": [ /* actions */ ]
    }
}
```

### ModifierSpec 运行时

- `ModifierSpec` 是附加在 unit 上的状态实例
- 通过 `ModifierData.Properties` 声明属性修改（OnCreated 时 Apply，OnDestroy 时 Remove）
- 通过 `ModifierData.Events` 声明事件响应
- `Class` 字段支持反射实例化自定义子类（如 `BreakModifierSpec`）

### 叠加模式

| 模式 | 行为 |
|------|------|
| `None` | 同名不重复，重复施加刷新持续时间 |
| `Multiple` | 允许多个独立实例共存 |
| `StackCount` | 单实例，堆叠计数递增/递减 |
| `Permanent` | 永不过期，死亡保持 |

### Modifier 事件

| 事件 | 触发时机 |
|------|----------|
| `OnCreated` | 施加时（ApplyProperties + PlayEffect） |
| `OnDestroy` | 过期/移除时（RemoveProperties + StopEffect） |
| `OnIntervalThink` | 每 ThinkInterval 秒 |
| `OnAttackStart` | 攻击动作开始 |
| `OnAttack` | 弹射物发射 |
| `OnAttackLanded` | 攻击命中 |
| `OnAttackFailed` | 攻击 miss |
| `OnAttacked` | 持有者被攻击 |
| `OnTakeDamage` | 持有者受伤害 |
| `OnDeath` | 持有者死亡 |
| `OnOrder` | 持有者收到新命令 |
| `OnUnitMoved` | 持有者移动 |

### 标签系统

- 扁平的 `HashSet<string>` 标签系统
- Modifier 通过 override `OnCreated`/`OnDestroy` 管理标签（如 `BreakModifierSpec` 挂 `stunned` + `broken`）
- `UnitEntity.HasTag(tag)` 查询
- `TagSystem` 支持 `AddTag` / `RemoveTag` / `HasTag` / `SyncFrom`

## ModifierManager

每个 unit 拥有一个 `ModifierManager`：
- `Add(data, caster, sourceAbility)` — 施加 modifier（处理叠加逻辑）
- `RemoveByName(name)` — 按名移除（StackCount 递减）
- `RemoveBySourceTag(tag)` — 按来源标签移除
- `PurgeAll()` — 死亡驱散全部
- `DeactivateAll()` / `ActivateAll()` — 死亡/复活时停用/恢复
- `Has(name)` / `Find(name)` — 查询
- `Update(dt)` — 逐帧 Tick（刷入 pending、推进时间、触发间隔）

## AbilityEventAction 系统

### 内置 Action（7 种）

所有 Action 通过 `AbilityEventActionData` 多态序列化，`$type` 使用短别名：

| Action | `$type` | 功能 |
|--------|---------|------|
| `DamageAction` | `DamageAction` | 对目标造成伤害（支持元素、阵营过滤） |
| `HealAction` | `HealAction` | 治疗目标 |
| `ApplyModifierAction` | `ApplyModifierAction` | 施加 modifier |
| `RemoveModifierAction` | `RemoveModifierAction` | 移除 modifier |
| `ModifyStatAction` | `ModifyStatAction` | 直接修改 stat 值 |
| `ReplaceAbilityAction` | `ReplaceAbilityAction` | 替换技能槽位 |
| `ForEachHitAction` | `ForEachHitAction` | 范围查询 + 逐个命中回调 |

### TargetSelector（目标选择）

| Selector | `$type` | 说明 |
|----------|---------|------|
| `SingleTargetSelector` | `SingleTargetSelector` | Owner / Caster / Target |
| `AreaTargetSelector` | `AreaTargetSelector` | 圆形范围（通过 `IUnitQueryService`） |

### ValueGetter（数值计算）

| Getter | `$type` | 说明 |
|--------|---------|------|
| `ConstantValueGetter` | `const` | 固定值 |
| `AbilitySpecialGetter` | `ability_special` | 从 AbilitySpecialFields 按等级取值 |
| `KXBValueGetter` | `kxb` | kx + b 线性公式 |

## CFBridge 抽象桥接

`CFBridge.Initialize(bridge)` 在游戏启动时注入，解耦框架与引擎：

```csharp
abstract class AbstractCombatFrameworkBridge {
    IElementProvider ElementProvider;    // 元素→抗性/穿透 stat 映射
    IMethodProvider MethodProvider;     // 外部 C# 方法暴露
    ITypeProvider TypeProvider;         // 反射类型扫描
    IBattleFormula Formula;             // 伤害公式（可替换）
    IShapeQueryService ShapeQuery;      // 盒形/圆形范围查询
    IUnitQueryService UnitQuery;        // 圆形单位查询
    IVfxEffectService Vfx;              // 特效播放
    abstract void StartAbility(...);    // 启动技能执行（Unity Timeline 等）
}
```

## 外部系统边界

战斗框架**不感知**圣遗物、套装、命座等概念。它只提供：

- `UnitEntity.HasTag` / `GetStat` / `GetAbilitySpecByName` / `TryCast` / `EquipAbility` / `UnequipAbility`
- `ModifierManager.Add` / `RemoveByName` / `RemoveBySourceTag` / `PurgeAll`
- `EventBus` 事件广播
- `CFBridge` 注入自定义服务

外部系统的集成链路：

```
外部换装备 → 套装系统判断触发装/卸技能 → EquipAbility
    → OnEquipped 事件 → ApplyModifierAction（属性加成 + 被动效果）
    → UnitEntity.GetStat 自动计算
```

## EventBus 全局事件

| 事件 | 触发时机 |
|------|----------|
| `StatChanged` | 属性变化 |
| `EntitySpawned` | 单位生成 |
| `EntityKilled` | 单位死亡 |
| `EntityHurt` | 单位受伤害 |
| `AbilityUsed` | 技能施放 |
| `AbilityEquipped / Unequipped` | 技能装备/卸下 |
| `ConstellationUpgrade` | 命座升级 |
| `HealApplied` | 治疗生效 |
| `OnDealDamage` | 造成伤害前 |
| `OnTakeDamage` | 受到伤害前 |
| `ToughnessChanged` | 韧性值变化 |
| `ToughnessBreak` | 破韧触发 |

## 运行时初始化序列

```
1. CFBridge.Initialize(yourBridge)              — 注入引擎实现
2. 加载 AbilityData JSON 文件 → AbilitySpec.Create()
3. UnitEntity 创建 → EquipAbility → ...
4. 每帧 unit.Update(dt) → ModifierManager.Update(dt)
```

## 项目结构

```
CombatFramework/
├── CLAUDE.md
├── ROADMAP.md
├── docs/
│   ├── INDEX.md / architecture.md
│   ├── guides/     (lua-ability.md — 已过时，实际为 JSON)
│   └── reference/  (damage-pipeline/event-bus/modifier-hooks/unit-tests)
├── src/
│   ├── Bridge/
│   │   ├── AbstractCombatFrameBridge.cs   — 抽象桥接基类
│   │   ├── CFBridge.cs                    — 全局 Bridge 入口
│   │   └── IElementProvider.cs
│   ├── Core/
│   │   ├── Ability/
│   │   │   ├── AbilitySpec.cs             — 运行时技能实例
│   │   │   ├── AbilityCondition.cs        — 转换条件（And/Or/Not/HasModifier/CheckTag）
│   │   │   └── AbilityEvent/              — 事件名常量 + Action 工厂 + 上下文
│   │   ├── Model/
│   │   │   ├── AbilityData.cs             — 技能模板数据
│   │   │   ├── AbilityCostData.cs         — 消耗配置
│   │   │   ├── AbilityEventActionData.cs  — Action 数据基类
│   │   │   ├── AbilityTransformData.cs    — 技能转换配置
│   │   │   ├── SkillBonusEntry.cs         — 命座等级加成
│   │   │   ├── ModifierData.cs            — Modifier 模板数据
│   │   │   ├── AbilityJsonSettings.cs     — JSON 序列化设置
│   │   │   ├── JsonAliasAttribute.cs      — $type 短别名
│   │   │   └── ValueGetterAliasBinder.cs  — ValueGetter 多态绑定
│   │   ├── Modifier/
│   │   │   ├── ModifierManager.cs
│   │   │   ├── ModifierSpec.cs
│   │   │   ├── ModifierStackMode.cs       — 叠加模式枚举
│   │   │   ├── StatModifierEntry.cs       — 属性修改条目
│   │   │   └── StatOp.cs                  — Add / Override
│   │   ├── Stat/
│   │   │   └── StatsManager.cs            — 属性容器（复合公式 + 平值）
│   │   ├── Executor/ValueGetter/
│   │   │   ├── IAbilityValueGetter.cs
│   │   │   ├── ConstantValueGetter.cs
│   │   │   ├── AbilitySpecialGetter.cs
│   │   │   └── KXBValueGetter.cs
│   │   ├── Enums/
│   │   │   ├── GlobalConstants.cs
│   │   │   ├── TargetType.cs
│   │   │   └── TeamFilter.cs
│   │   ├── TagSystem.cs
│   │   ├── TargetSelector.cs              — 单目标/范围选择器
│   │   ├── CFLog.cs
│   │   ├── CFServices.cs
│   │   └── IVfxEffectService.cs
│   ├── Damage/
│   │   ├── BattlePipeline.cs             — 伤害公式 + ApplyDamage
│   │   ├── ToughnessPipeline.cs          — 韧性/破韧管线
│   │   └── MultiHitHelper.cs
│   ├── Event/
│   │   ├── EventBus.cs
│   │   └── DamageEvent.cs
│   ├── EventAction/AbilityEventAction/
│   │   ├── DamageAction.cs
│   │   ├── HealAction.cs
│   │   ├── ApplyModifierAction.cs
│   │   ├── RemoveModifierAction.cs
│   │   ├── ModifyStatAction.cs
│   │   ├── ReplaceAbilityAction.cs
│   │   └── ForEachHitAction.cs
│   └── Unit/
│       └── UnitEntity.cs
├── tests/
│   ├── TestBridge.cs                      — 测试用 Bridge mock
│   ├── Fixtures/abilities/                — 6 个 JSON 测试能力
│   └── Integration/
│       ├── AbilityEventActionTests.cs     — Action 序列化 + 触发的测试
│       └── FullBattleFlowTests.cs         — 完整战斗流程集成测试
└── GodotDemo/                             — Godot 可运行原型
    ├── BATTLE_DESIGN.md                   — 战斗机制设计文档
    ├── Abilities/                         — JSON 能力配置
    ├── Scripts/                           — Godot C# 桥接实现
    └── Scenes/
```

## 编码约定

- **C#**：标准 .NET 命名（PascalCase 方法、camelCase 参数），文件范围命名空间
- **JSON**：`$type` 字段用于多态反序列化，通过 `JsonAliasAttribute` 使用短别名
- 能力文件中不硬编码数值——始终使用 ValueGetter（`const` / `ability_special`）
- **一个 JSON 文件 = 一个能力 + 其所有 modifier**
- Stat 用字符串 ID，游戏侧自由命名，框架不预设枚举
- Action 子类通过 `AbilityEventActionAttribute` 注册 Data 类型映射

## 当前状态

**核心引擎已完成**，JSON Action 系统可用。韧性/破韧系统已实现。Godot Demo 可运行。测试覆盖序列化往返 + 完整战斗流程。见 `ROADMAP.md` 了解完整路线图。
