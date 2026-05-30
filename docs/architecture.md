# CombatFramework — 架构参考

## 起源

本项目逆向 Dota 2 能力系统架构，源自 Pizzalol/SpellLibrary 代码库（社区项目，~2014–2019）和公开的 Dota 2 Workshop Tools API 知识。原版 Dota 2 系统是一个**数据驱动的游戏玩法框架**，基于 C++ 实体 + Lua 脚本——本项目将架构移植到 C# (.NET) + MoonSharp。

## 这套架构为什么重要

Dota 2 能力系统的核心创新在于 **Modifier 钩子模式**：

Modifier 不"施加"改变，而是注册钩子（`DeclareFunctions`），引擎在计算任意游戏值时**轮询**所有活跃 modifier。这意味着：

- 新增增益类型不需要修改属性计算代码
- 同一属性上的多个增益自然合成（加法/取最大）
- 事件驱动的行为（OnTakeDamage、OnAttackLanded）与伤害/攻击管线解耦
- 光环只是自动施加其他 modifier 的特殊 modifier——无需特殊代码

## 分层详解

### 第一层：数值引擎

最底层，不感知"能力"或"modifier"概念——只有数值和公式。

**复合属性聚合：**
```
最终值 = 基础值 + (基础值 × 百分比加成 + 额外值) + 修饰器值
```
- `基础值`：角色等级/品质内在值
- `百分比加成`：百分比增益（+25% 攻击力）
- `额外值`：固定增益（装备 +50 ATK）
- `修饰器值`：modifier 系统通过聚合贡献的调整

此模式对每个复合属性重复。`CompoundStat` 是泛化的：
```csharp
class CompoundStat {
    float Base { get; set; }
    float PercentBonus { get; set; }    // 所有百分比加成的和
    float Extra { get; set; }           // 所有固定加成的和
    float Modifier { get; set; }        // 修饰器聚合值
    float GreenText => Base * PercentBonus + Extra;   // 面板绿字
    float Final => Base + GreenText + Modifier;
}
```

多个来源（装备、增益、被动）的不同属性都聚合到同一个属性上。系统不追踪每个来源——只聚合。

**StatId 常量系统：**
框架将所有内置 stat 定义为字符串常量（`src/Core/CFConstants.cs`）：
- 资源槽：`HP`、`Energy`、`Shield`、`BlackShield`
- 复合属性分量：`HpBase`/`HpMult`/`HpAdd`、`AtkBase`/`AtkMult`/`AtkAdd`、`DefBase`/`DefMult`/`DefAdd`
- Flat stats：`CritRate`、`CritDMG`、`HealBonus`、`HealRecv`、`DamageInc`、`DamageRed`、`DefIgnore`、`DefIgnoreRate`、`WalkSpeed` 等
- 特殊：`HpMin`（薄葬式生命下限，取最大值而非求和）

**TagId 常量：** `stunned`、`dead`、`silenced`、`invincible`

**伤害类型：**
框架内置七种元素作为伤害类型（火/水/雷/风/地/光/暗），预留 NONE。每种伤害类型注册时绑定对应的抗性 stat ID 和默认受击 VFX 路径。

**伤害流程：**
```
ApplyDamage(victim, attacker, rawDamage, damageType, sourceAbility?)
  → 暴击判定（CritRate / CritDMG，单次独立判定）
  → 元素抗性减免（线性，上限 90%）
  → 创建 DamageEventData（RawDamage / FinalDamage / IsCritical）
  → Modifier 后回调 OnDealDamage / OnTakeDamage（可设置 IsCancelled 拦截）
  → HpMin 夹底（薄葬式，取 modifier 中最大值）
  → 扣血（不溢出到最小血量以下）
  → 全局事件 EntityHurt
  → VFX（按伤害类型播默认受击特效）
  → 若 HP ≤ 0 → EntityKilled 事件
```

**治疗流程：**
```
ApplyHeal(target, source, rawHeal)
  → 治疗加成: final = rawHeal × (1 + HealBonus / 100)
  → 全局事件 HealApplied（可拦截）
  → 回复 HP
```

注意：当前实现只有一条伤害管线，不区分"正常/异常/击破"——异常/击破相关的 stat（`AbnormalAccrueMult`、`BreakAccrueMult` 等）已在 `StatId` 中定义占位，但管线未做多路路由。未来可按需在应用层扩展。

**资源系统：**
每个 unit 上的泛化键值资源容器。游戏启动时注册资源类型（HP 必备，其他可选）。操作为 Get、Set、Modify，变更时触发事件（用于 UI 更新、modifier 触发）。支持安全自伤（`ConsumeSafe` 带下限夹底）。

**属性和 stat 注册：**
```csharp
StatDefinition.Register("ATK_Base", "基础攻击力");
StatDefinition.Register("ATK", "攻击力", isCompound: true);
```
`StatDefinition.RegisterDefaults()` 注册 ATK/DEF/HP 分量、元素精通、暴击率/暴击伤害等。`DamageTypes.RegisterDefaults()` 注册七元素 + NONE。

### 第二层：运行时实体层

实体层管理 ability、modifier 和投射物的实例状态。

**AbilityInstance：**
```csharp
class AbilityInstance {
    string Name;
    int Level;
    float CooldownRemaining;
    bool IsOnCooldown;
    UnitEntity Owner;
    AbilityData Data;           // 只读模板，来自 Lua
}
```

关键设计：`AbilityData` 是**共享模板**（从 Lua 加载一次），`AbilityInstance` 是**每槽位运行时状态**（每个 unit 一个）。

`AbilityInstance` 还提供 `CanCast(out string? reason)` 方法——检查状态（眩晕/死亡/沉默）、冷却、资源，并分发 `OnAbilityPhaseStart` 事件供 modifier 否决。

**ModifierInstance：**
```csharp
class ModifierInstance {
    string Name;
    float Duration;
    float RemainingTime;
    int StackCount;
    UnitEntity Parent;
    UnitEntity Caster;
    AbilityInstance? SourceAbility;
    ModifierAttribute Attributes;   // None | Multiple | StackCount | Permanent
    string? SourceTag;              // 来源标签，用于分组清除
    int VfxHandle;                  // VFX 实例句柄
}
```

**投射物实例：**
两种变体：
- `TrackingProjectile`：追踪，跟随目标单位
- `LinearProjectile`：直线方向飞行，恒定速度，防重复命中

两者在到达目标时触发 `OnProjectileHit(target, position)`。

**TagSystem：**
扁平的字符串标签系统，支持两个来源：
1. Modifier 通过 `DeclareTags` 声明的标签，由 `ModifierManager` 自动同步并集
2. 外部直接添加（游戏层标签如队伍、星级等）

支持 `SyncFrom(HashSet)` 做 net-change diff，只触发有变化的标签事件。

**IVfxEffectService：**
VFX 桥接接口，CF 通过此接口通知 Unity 播放/停止特效：
```csharp
interface IVfxEffectService {
    int PlayAtPoint(string assetPath, Vector3 position, float? lifeTime);
    int PlayOnUnit(string assetPath, UnitEntity target, float? lifeTime);
    void Stop(int vfxId);
}
```

### 第三层：事件总线

事件总线连接各层。事件在层级间流转：

```
[游戏状态变更]
    ↓
DamagePipeline.ApplyDamage
    ↓
ModifierManager.DispatchEvent("OnTakeDamage", eventData)
    ↓
对于每个注册了 OnTakeDamage 钩子的 modifier：
    → 调用 Lua PropertyHook
    ↓
GlobalEventBus.Publish("EntityHurt", eventData)
```

全局 EventBus 只用于**外部系统**（圣遗物、套装、命座、UI）。核心战斗逻辑通过 `ModifierManager.DispatchEvent` 内部硬链分发。

### 第四层：Lua 脚本

**文件格式（每个能力一个 .lua 文件）：**

```lua
AbilityData = {
    id = "fireball",
    cooldown = 8,
}

-- 可选事件处理函数（签名匹配 LuaAbilityLoader 的缓存逻辑）：
function OnSpellStart(caster, ability, targetPoint, targetUnit)
end

-- 可选的 modifier 块：
Modifiers = {
    fireball_burn = {
        duration = 3,
        isDebuff = true,
        OnCreated = function(self) end,
        DeclareFunctions = { "DamageInc" },
        DamageInc = function(self) return -20 end,
    }
}

-- 可选的 Effects 表（声明式效果数据，由 Timeline clip 调度）：
Effects = {
    main_hit = {
        picker = { type = "Area", filter = "Enemy", shape = { type = "Box", radius = 5 } },
        ops = {
            { type = "Damage", baseStat = "Attack", coeff = 0.9, damageType = "FIRE" },
            { type = "Modifier", modifier = "fireball_burn", duration = 3 },
        },
    },
}
```

**LuaEngine 包装：**
```csharp
class LuaEngine : IDisposable {
    Script Script;  // MoonSharp Script 实例（独立沙箱）

    void DoFile(string path);
    void DoString(string code);
    Table GetGlobalTable();
    DynValue GetGlobal(string name);
    DynValue Call(string functionName, params object[] args);
    Closure? GetClosure(string name);
    void SetGlobal(string name, object value);
}
```

每个能力文件有独立的 `LuaEngine`（隔离的 `Script` 实例）以保证封装性。
注册的 Lua 全局 API：`ApplyHeal(target, source, amount)`、`PlayVfxOnUnit(path, unit, lifeTime?)`。
伤害类型常量：`DAMAGE_TYPE_NONE`、`DAMAGE_TYPE_FIRE` 等（通过 `LuaModifierBridge.RegisterGlobals` 注册）。

## 设计决策总结

| 决策 | 选择 | 原因 |
|------|------|------|
| Lua 嵌入 | MoonSharp | 纯 C#，无原生依赖，内置沙箱 |
| 实体模型 | OOP + 内部组合 | Modifier 系统天然映射到带钩子的对象；ECS 过度复杂化轮询模式 |
| 能力配置格式 | 单 .lua 文件 | 比 KV + Lua 拆分更易维护；Lua 表取代 JSON |
| 属性聚合 | 基础值 + 基础值×百分比 + 额外值 + 修饰器 | 来自策划数值文档 |
| 伤害管线 | 单管线 + 可替换公式接口 | 一套通用流程处理所有伤害；异常/击破通过 stat 参数调节，不设独立管线 |
| 资源系统 | 注册槽位，非硬编码 | 适配各游戏需求 |
| Modifier 钩子 | DeclareFunctions → 轮询 | 核心创新；保持系统可扩展性 |
| 钩子实现 | Lua 闭包，非接口 | `PropertyHooks: Dictionary<string, Closure>` 更灵活，无需预定义接口 |
| 属性修饰 | 三分量聚合 | CompoundStat 的 Base/Pct/Extra 各走独立 modifier 管道，杜绝"直接改 Attack"双路径 |
| UGC 安全 | MoonSharp 沙箱 + 每文件独立 Script 实例 | 脚本不能互相干扰或调用危险 API |
| 事件分发 | 内部硬链 + EventBus 外部广播 | 性能关键路径走直接调用；外部系统通过总线监听 |
| 投射物 | C# 管理轨迹 + 接口隔离碰撞 | 碰撞检测由 Unity 实现，过滤回调由框架处理 |
| 标签系统 | 扁平字符串 + SyncFrom diff | 非布尔/位掩码，免枚举，natural join 语义 |
| Stat 标识 | 字符串 ID（无枚举） | Lua modifier 直接使用字符串名，无需在 C# 中注册 stat 名 |
| 公式接口 | IDamageFormula | 游戏方可全局替换抗性/暴击公式，无需修改管线代码 |
| VFX 桥接 | IVfxEffectService 接口 | CF 不引用 Unity，完全通过接口契约解耦 |
