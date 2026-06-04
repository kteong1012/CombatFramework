# 事件总线参考

框架使用混合事件系统：内部 ModifierManager 回调 + 外部 EventBus 广播。

## EventBus

全局事件总线，**供外部系统**（圣遗物、套装、命座、UI）监听战斗事件。

```csharp
// 订阅
DamagePipeline.GlobalEventBus.Subscribe(
    EventBus.Events.EntityKilled,
    data => HandleKill(data)
);

// 取消订阅（需要保留 handler 引用）
void OnKill(object? data) { ... }
DamagePipeline.GlobalEventBus.Subscribe(EventBus.Events.EntityKilled, OnKill);
DamagePipeline.GlobalEventBus.Unsubscribe(EventBus.Events.EntityKilled, OnKill);

// 清空所有（场景切换时）
DamagePipeline.GlobalEventBus.Clear();
```

## 预定义事件

| 事件常量 | 触发时机 | 事件数据 |
|----------|----------|----------|
| `EntitySpawned` | 单位生成时 | 单位引用 |
| `EntityKilled` | 单位死亡时 | `{ Victim, Attacker }`（匿名对象） |
| `EntityHurt` | 单位受到伤害时 | `DamageEventData` |
| `AbilityUsed` | 施放技能时 | 能力引用 |
| `AbilityEquipped` | 装备技能时 | 能力引用 |
| `AbilityUnequipped` | 卸下技能时 | 能力引用 |
| `ConstellationUpgrade` | 命座升级时 | 命座信息 |
| `HealApplied` | 治疗生效时 | `HealEventData` |

## 事件数据对象

### DamageEventData

| 属性 | 类型 | 可写 | 说明 |
|------|------|------|------|
| `Victim` | `UnitEntity` | 只读 | 受击者 |
| `Attacker` | `UnitEntity` | 只读 | 攻击者 |
| `RawDamage` | float | 是 | 原始伤害 |
| `FinalDamage` | float | 是 | 最终伤害 |
| `DamageType` | string | 是 | 伤害类型 |
| `IsCritical` | bool | 是 | 是否暴击 |
| `IsCancelled` | bool | 是 | 设为 true 拦截伤害 |

### HealEventData

| 属性 | 类型 | 可写 | 说明 |
|------|------|------|------|
| `Target` | `UnitEntity` | 只读 | 目标 |
| `Source` | `UnitEntity` | 只读 | 来源 |
| `RawHeal` | float | 是 | 原始治疗 |
| `FinalHeal` | float | 是 | 最终治疗 |
| `IsCancelled` | bool | 是 | 设为 true 拦截治疗 |

## 内部 vs 外部事件

| | 内部 ModifierManager | EventBus |
|---|---|---|
| **路径** | `ModifierManager.DispatchEvent` → 所有声明了该钩子的 modifier | `EventBus.Publish` → 订阅者 |
| **性能** | 直接方法调用 | `DynamicInvoke`，有装箱开销 |
| **用途** | 核心战斗逻辑 | 外部系统集成（UI、圣遗物、成就） |
| **时机** | 伤害管线中段（Modifier 可修改事件数据） | 管线末端（事件已不可逆） |
| **取消语义** | Modifier 回调可设置 `IsCancelled` | 无取消（已发布不可撤回） |

## 外部系统集成示例

```csharp
// 圣遗物套装：2件套 +20% 攻击力
DamagePipeline.GlobalEventBus.Subscribe(
    EventBus.Events.AbilityEquipped,
    data => {
        if (CheckSetBonus(unit, 2))
            ApplySetBonus(unit, "atk_set_2pc");
    }
);

// 命座效果：6命击杀敌人重置技能冷却
DamagePipeline.GlobalEventBus.Subscribe(
    EventBus.Events.EntityKilled,
    data => {
        if (HasConstellation(unit, 6))
            unit.Abilities.Values.ToList()
                .ForEach(a => a.EndCooldown());
    }
);
```
