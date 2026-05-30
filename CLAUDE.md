# CombatFramework

独立的 .NET 战斗能力框架，逆向 Dota 2 能力系统架构（源自 Pizzalol/SpellLibrary 代码库和社区公开 API 知识）。面向 ARPG 使用，以 Lua 脚本层提供数据驱动的能力配置。

## 核心原则

本项目将 Dota 2 的 ability/modifier/event 架构移植到 C# (.NET) + MoonSharp (Lua)。目标不是 1:1 复制，而是保留原系统的可组合性和灵活性的同时，去掉 MOBA 特有元素。

## 架构分层

```
┌───────────────────────────────────────────────┐
│  第四层：Lua 脚本（数据驱动能力定义）              │
│  每个 ability 一个 .lua 文件，包含：              │
│  - 能力参数（冷却、消耗、行为）                    │
│  - 事件处理函数(OnSpellStart/OnProjectileHit)    │
│  - Modifier 定义                                 │
│  - Effects 声明式效果数据                        │
│  - Transforms 函数                              │
├───────────────────────────────────────────────┤
│  第三层：事件总线 + Modifier 系统                 │
│  ModifierManager（内部硬链轮询）                  │
│  EventBus（全局广播，供外部系统/UI 使用）          │
├───────────────────────────────────────────────┤
│  第二层：运行时实体层                             │
│  AbilityInstance | ModifierInstance | Projectile │
│  TagSystem | IVfxEffectService                  │
│  PreCastEventData | CanCast 管线                 │
├───────────────────────────────────────────────┤
│  第一层：数值引擎                                │
│  复合属性聚合 (Base + Base×% + Extra + Modifier)│
│  伤害管线（暴击→抗性→修饰器→HpMin→扣血）           │
│  资源系统（HP、能量、护盾等）                     │
│  StatDefinition / UnitStats / CompoundStat      │
└───────────────────────────────────────────────┘
```

## 语言与运行时

- **运行时**：.NET (C#)，标准类库
- **消费方**：Unity 通过 DLL 引用
- **目标框架**：net48、net471、net10.0（多目标）
- **脚本语言**：MoonSharp（纯 C# Lua 解释器，无原生依赖，内置沙箱）
- **为何不用 XLua**：XLua 与 Unity 类型系统和代码生成管线强耦合。MoonSharp 可在任意 .NET 项目直接使用

## 目标游戏类型：ARPG

**去掉的 MOBA 系统：**
- 三主属性 (STR/AGI/INT) → 替换为自定义属性系统
- 升级技能点分配
- 金钱/死亡扣钱/买活
- 小兵/防御塔/泉水
- 经验范围分摊

**保留并适配的系统：**
- Ability 实例生命周期
- Modifier 系统（增益/减益、叠加、永久、光环、标签）
- 事件系统（能力级、Modifier 级、全局）
- 投射物系统（追踪 + 线性）
- 伤害管线（类型/抗性/Modifier 钩子/HpMin 夹底）
- DeclareFunctions 轮询模式
- 特效自动播放/停止（EffectName / EffectAttachType）

## 属性系统

### 复合属性公式

三个主要战斗属性为复合属性，由多个子属性聚合计算：

```
HP   = BaseHP + (BaseHP × HPPctBonus + ExtraHP) + ModifierHP
ATK  = BaseATK  + (BaseATK  × ATKPctBonus  + ExtraATK)  + ModifierATK
DEF  = BaseDEF  + (BaseDEF  × DEFPctBonus  + ExtraDEF)  + ModifierDEF
```

- **Base**：等级基础值
- **PctBonus**：百分比加成（如 +25% 攻击力，走 `AtkMult` stat）
- **Extra**：固定加成（如装备 +50 攻击力，走 `AtkAdd` stat）
- **Modifier**：修饰器系统的原始值覆盖路径

括号部分 = 面板显示的"绿字"。

每个分量（Base / Pct / Extra）各自通过 `ModifierManager.AggregateStat(statId)` 独立聚合 modifier 贡献。直接对 "Attack" 名加 modifier 不生效——所有成长都走分量，无双路径。

### StatId 字符串常量

所有 stat 通过 `CFConstants.cs` 中的字符串常量标识：

| 常量 | 说明 |
|------|------|
| `HpBase/AtkBase/DefBase` | 复合属性基础值 |
| `HpMult/AtkMult/DefMult` | 复合属性百分比加成 |
| `HpAdd/AtkAdd/DefAdd` | 复合属性固定加成 |
| `CritRate/CritDMG` | 暴击率/暴击伤害 |
| `HealBonus/HealRecv` | 治疗加成/受疗加成 |
| `DamageInc/DamageRed` | 伤害增加/减免 |
| `DefIgnore/DefIgnoreRate` | 防御忽略（固定/百分比） |
| `WalkSpeed` | 移动速度 |
| `HpMin` | 薄葬式生命下限 |
| `AbnormalAccrueMult/BreakAccrueMult` | 异常/击破效率（占位） |

元素/抗性通过 `Dictionary<string, float>` 存储，key 为元素名（"FIRE"等）。

### 属性来源

```
角色属性 = 等级基础 + 命座 + 技能 + 武器 + 装备 + modifier 聚合
```

## 伤害系统

### 伤害流程

```
ApplyDamage(victim, attacker, rawDamage, damageType, sourceAbility?)
  │
  ├─ 1. null 检查 → return 0
  ├─ 2. 暴击判定（critRate × critDamage）
  ├─ 3. 元素抗性减免（线性，上限 90%）
  ├─ 4. 创建 DamageEventData
  ├─ 5. Modifier 回调（OnDealDamage + OnTakeDamage，可拦截）
  ├─ 6. HpMin 夹底（取 modifier 最大值，薄葬式）
  ├─ 7. 扣血（不溢出）
  ├─ 8. 全局事件 EntityHurt
  ├─ 9. VFX（按伤害类型播默认受击特效）
  └─ 10. 若 HP ≤ 0 → EntityKilled 事件
```

### 元素类型

| 元素 | 常量 | 抗性 stat |
|------|------|-----------|
| 无属性 | `NONE` | NoneRES |
| 火 | `FIRE` | FireRES |
| 水 | `WATER` | WaterRES |
| 雷 | `LIGHTNING` | LightningRES |
| 风 | `WIND` | WindRES |
| 地 | `EARTH` | EarthRES |
| 光 | `LIGHT` | LightRES |
| 暗 | `DARK` | DarkRES |

默认受击 VFX 路径：`vfx/hit_<element>`（如 `vfx/hit_fire`）。

### 治疗流程

```
ApplyHeal(target, source, rawHeal)
  → 治疗加成: final = rawHeal × (1 + HealBonus / 100)
  → 全局事件 HealApplied（可拦截）
  → 回复 HP
```

## Ability 系统

### 能力数据定义

每个 ability 一个 .lua 文件：

```lua
AbilityData = {
    id = "my_skill",
    name = "我的技能",
    cooldown = 5,
    costs = { Energy = 30 },
    parameters = { base_damage = 100 },
    projectile = { model = "xxx.vfx", speed = 1200, radius = 80, isTracking = true },
}

function OnSpellStart(caster, ability, targetPoint, targetUnit)
    -- ...
end

Modifiers = {
    my_buff = { duration = 5, isBuff = true, ... }
}

Effects = {
    main_hit = {
        picker = { type = "Area", filter = "Enemy" },
        ops = { { type = "Damage", baseStat = "Attack", coeff = 0.9 } },
    },
}
```

### Ability 实例

每个 ability 是 unit 槽位上的**独立实例**：
- `AbilityData`：Lua 模板引用（只读共享）
- `AbilityInstance`：运行时状态（冷却、等级、Owner、Data 引用）
- 事件处理函数通过 `AbilityData.EventHandlers` 字典缓存（`OnSpellStart` / `OnProjectileHit` / `OnProjectileThink` / `OnAbilityPhaseStart` / `Transforms`）

### 施法检查管线

`AbilityInstance.CanCast(out reason)` 按序检查：
1. 状态标签（stunned / dead / silenced）
2. 冷却
3. 资源消耗
4. Modifier 否决（OnAbilityPhaseStart 钩子）

`AbilityInstance.DeductCosts()` 在正式施法点调用，实际消耗资源。

## Modifier 系统

### Modifier 是什么

Modifier 是附加到 unit 上的**实例**：
1. 有**持续时间**（或永久）
2. 可**叠加**（多实例/堆叠计数/独占）
3. 通过 `DeclareFunctions` 注册**钩子**，告知引擎它修改哪些属性、监听哪些事件
4. 引擎**轮询** modifier 获取属性贡献，**调用** modifier 处理事件

### 核心隐喻

Modifier **不主动**改变数值。它们**注册兴趣**：

```csharp
// 错误：modifier 主动 HP += 50
// 正确：modifier 声明"我对 MaxHP 贡献 +50"
// 属性系统在计算时轮询所有 modifier
```

### Modifier 生命周期

```
OnCreated(kv)    → modifier 首次被施加（自动播 VFX）
OnRefresh(kv)    → 重新施加（刷新堆叠或持续时间）
OnStackCountChanged() → 堆叠数发生变化
OnIntervalThink() → 周期性回调（通过 StartIntervalThink 启动）
OnTakeDamage()   → 伤害事件钩子
OnDestroy()      → modifier 过期或被移除（自动停 VFX，同步标签）
```

### 叠加行为

| 模式 | 行为 |
|------|------|
| `NONE` | 同一 unit 上不能有多个同名实例，重复 Add 触发 OnRefresh |
| `MULTIPLE` | 允许多个独立实例共存 |
| `STACK_COUNT` | 单个实例，堆叠计数器递增 |
| `PERMANENT` | 死亡不消失，永不过期 |

### 统计值聚合

`ModifierManager.AggregateStat(statId, baseValue`) 聚合规则：
1. **PropertyHooks**（Lua 函数闭包）：等价 Add
2. **Properties**（KV 表）：等价 Add，与 PropertyHooks 互斥（同名 hook 优先）
3. **StatModifiers**（结构化条目）：支持 `Add` / `Override`
4. Override 存在时直接返回 override 值

`CollectMax(statId)` 用于 HpMin 等"取最强"语义，返回所有 modifier 中的最大值。

### 标签自动同步

Modifier 通过 `DeclareTags` 声明标签，`ModifierManager.Update()` 自动计算并集并同步到 `UnitEntity.TagSystem`。标签变化时触发 `OnTagChanged` 事件。

### 光环系统

特殊 modifier 通过 `AuraConfig` 配置：
```lua
my_aura = {
    Aura = {
        radius = 300,
        targetModifier = "aura_effect_buff",
    },
}
```
（光环实际扫描逻辑由宿主侧 Unity 驱动，框架提供数据模型）

## ModifierManager

每个 unit 拥有一个 `ModifierManager`：
- 持有所有活跃 modifier 实例
- 延迟添加（pendingAdd → Update flush）
- 过期自动移除 + VFX 停止
- 为属性系统提供 `AggregateStat` / `CollectMax` 接口
- 向相关 modifier 分发事件（`DispatchEvent`）
- 处理驱散（`Purge`/`RemoveBySourceTag`）

## 标签系统（TagSystem）

扁平的字符串标签系统：
- Modifier 通过 `DeclareTags` 声明，由 ModifierManager 自动同步并集
- 外部直接添加（游戏层标签如队伍、星级）
- `SyncFrom(HashSet)` net-change diff，只触发有变化的标签事件
- 内置标签常量：`stunned`、`dead`、`silenced`、`invincible`

## 状态系统（CheckState）

从所有活跃 modifier 的 `States` 字典聚合状态检查。Modifier 通过 `CheckState` 表声明：

```lua
my_modifier = {
    CheckState = {
        stunned = true,
    },
}
```

`UnitEntity.CheckState("stunned")` 遍历所有 modifier，任一返回 true 即命中。

## 事件系统

### 双层结构

| 路径 | 机制 | 用途 |
|------|------|------|
| 内部（硬链） | `ModifierManager.DispatchEvent` | Ability → Projectile → Damage → ModifierManager（性能关键路径） |
| 外部（广播） | `EventBus.Publish` | EntityKilled、EntityHurt、AbilityUsed（供外部系统如圣遗物触发、命座检测） |

### EventBus 预定义事件

| 事件 | 触发时机 |
|------|----------|
| `EntitySpawned` | 单位生成时 |
| `EntityKilled` | 单位死亡时 |
| `EntityHurt` | 单位受到伤害时 |
| `AbilityUsed` | 施放技能时 |
| `AbilityEquipped` | 装备技能时 |
| `AbilityUnequipped` | 卸下技能时 |
| `ConstellationUpgrade` | 命座升级时 |
| `HealApplied` | 治疗生效时 |

## Ability 加载管线

```
.lua 文件 → LuaAbilityLoader.LoadFile() → AbilityData（共享模板，全局缓存）
    → 扫描 AbilityData / Modifiers / Effects 表
    → 缓存事件处理函数（OnSpellStart、OnProjectileHit 等）
    → 解析 ModifierData（生命周期闭包、PropertyHooks、StatModifiers、Aura 等）
    → 存入 Cache 字典（key = ability id）
```

一个 .lua 加载一次 = 一个模板。N 个 unit 拥有该能力 = N 个实例共享一个模板。

## 投射物系统

### 两种投射物

- **追踪弹**：锁定目标自动追踪
- **线性弹**：指定方向和距离直线飞行，防重复命中

### 两个回调

- `OnProjectileHit(target, position)` — 命中或到达终点
- `OnProjectileThink(position)` — 飞行期间每帧

### 实现方案

- 框架的 `ProjectileManager` 用 C# 管理所有投射物实例，更新位置和生命周期
- 碰撞检测抽象为接口 `ICollisionService`，由 Unity 游戏项目实现
- 框架负责命中后的过滤和回调逻辑

```csharp
interface ICollisionService {
    HitResult[] CheckHits(Vector3 position, float radius, TeamFlag team);
}
```

## Effects 声明式效果数据

Lua 中通过 `Effects = { key = { picker, ops } }` 声明。每个 effect 包含：

- **Picker**：目标选择配置（type / filter / shape）
- **Ops**：操作列表，支持类型：
  - `Damage`：伤害（公式 = baseStat × coeff + raw，支持 target=CASTER）
  - `Heal`：治疗
  - `Modifier`：施加 modifier（按名引用，支持 target=CASTER）
  - `Projectile`：发射投射物

效果执行由 Timeline clip 调度，数据读取与执行解耦，Editor 可仅凭数据预览。

## 外部系统边界

战斗框架**不感知**圣遗物、套装、命座等概念。它只提供：

- `UnitEntity.CheckState`、`HasTag`、`GetStat`、`GetResource` 等查询
- Modifier 管理接口（`Add` / `RemoveBySourceTag` / `Purge`）
- `EventBus` 事件广播
- `IVfxEffectService` 接口供 Unity 实现特效桥接

外部系统（装备/套装/命座）的集成链路：

```
外部换装备 → 套装系统判断触发装/卸技能 → 调用框架 API
    → ModifierManager.Add（属性加成 + 被动效果）
    → UnitEntity.GetStat 自动重算
```

## 资源系统（取代 Dota 2 的 Mana）

- 非硬编码，游戏启动时注册
- `unit.Resources` 带命名槽位：`Register(id, initial, max, min)` / `Get` / `Set` / `TryConsume` / `Restore`
- 安全自伤 `ConsumeSafe(amount, minLeft)` 保障不扣到下限以下
- 能力通过名称引用资源：`costs = { Energy = 30 }`
- 支持多能量条（绝区零式个人能量、星铁式战技点等）

## VFX 桥接

框架通过 `IVfxEffectService` 接口与 Unity 解耦：

```csharp
int PlayAtPoint(string assetPath, Vector3 position, float? lifeTime);
int PlayOnUnit(string assetPath, UnitEntity target, float? lifeTime);
void Stop(int vfxId);
```

Modifier 通过 `EffectName` / `EffectAttachType` 配置自动特效：OnCreated 自动播放，OnDestroy 自动停止。伤害管线按伤害类型播默认受击特效。

## UGC 安全

- MoonSharp 沙箱（禁用 os/io/package 访问）
- 每个能力文件独立 Script 实例隔离
- 仅白名单 C# 类型通过 UserData 注册暴露给 Lua
- `LuaEngine` 用 `IDisposable` 保证资源释放

## 运行时初始化序列

框架预期使用方在游戏启动时按序执行：

```
1. StatDefinition.RegisterDefaults()          — 注册所有属性枚举和公式
2. DamageTypes.RegisterDefaults()             — 注册元素类型及抗性 stat
3. 可选：注册自定义 stat / 伤害类型 / 资源槽
4. LuaAbilityLoader.ScanDirectory(path)       — 扫描并加载所有 .lua 能力文件
5. Game starts → UnitEntity 创建 → EquipAbility → ...
```

## 项目结构

```
CombatFramework/
├── CLAUDE.md
├── docs/
│   ├── INDEX.md
│   ├── architecture.md
│   ├── guides/
│   │   ├── lua-ability.md
│   │   └── extending.md
│   └── reference/
│       ├── damage-pipeline.md
│       ├── event-bus.md
│       ├── modifier-hooks.md
│       └── unit-tests.md
├── examples/abilities/
│   ├── fireball.lua          (文档示例，Dota 2 风格)
│   └── test_frostbolt.lua    (匹配桥接签名的可测试用例)
└── src/
    ├── Core/
    │   ├── AbilityData.cs
    │   ├── AbilityEffectData.cs
    │   ├── AbilityInstance.cs
    │   ├── CFConstants.cs
    │   ├── CFLog.cs
    │   ├── IVfxEffectService.cs
    │   ├── ModifierData.cs
    │   ├── ModifierInstance.cs
    │   ├── PreCastEventData.cs
    │   └── TagSystem.cs
    ├── Modifier/
    │   └── ModifierManager.cs
    ├── Event/
    │   ├── DamageEvent.cs
    │   └── EventBus.cs
    ├── Projectile/
    │   ├── ProjectileManager.cs
    │   └── ProjectileTypes.cs
    ├── Damage/
    │   ├── DamagePipeline.cs
    │   ├── DamageTypes.cs
    │   └── IDamageFormula.cs
    ├── Stat/
    │   ├── CompoundStat.cs
    │   ├── ResourceSystem.cs
    │   ├── StatDefinition.cs
    │   └── UnitStats.cs
    ├── Lua/
    │   ├── LuaEngine.cs
    │   ├── LuaAbilityLoader.cs
    │   └── LuaModifierBridge.cs
    └── Unit/
        ├── UnitEntity.cs
        └── UnitAbilitySlot.cs
```

## 编码约定

- **C#**：标准 .NET 命名（PascalCase 方法、camelCase 参数、`_camelCase` 私有字段），文件范围命名空间
- **Lua**：snake_case 函数和变量
- Lua 中不硬编码数值值——始终从 `AbilityData.Parameters` 表或能力参数引用
- **能力文件必须自包含** — 一个 `.lua` = 一个能力 + 其所有 modifier
- **无全局 Lua 状态** — 每个能力脚本在自己的 MoonSharp Script 实例中运行
- 修改器 stat 用字符串 ID，无需修改 C# 枚举

## 当前状态

**开发中**。核心引擎已完成，Lua 能力系统可用，90+ 测试覆盖。见 `ROADMAP.md` 了解完整路线图。
