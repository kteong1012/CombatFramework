# 伤害管线参考

当前框架实现**一条**通用伤害管线和一条治疗管线。异常伤害/击破伤害的 stat 已在 `StatId` 中占位，但管线未做多路路由。

## 伤害流程

```
ApplyDamage(victim, attacker, rawDamage, damageType, sourceAbility?)
    │
    ├─ 1. null 检查 → return 0
    ├─ 2. 暴击判定（CritRate/CritDMG，每次独立判定）
    ├─ 3. 元素抗性减免（线性，上限 90%）
    │      reduction = Clamp(resValue / 100, 0, 0.9)
    │      final = rawDamage × (1 - reduction) × critMult
    ├─ 4. 创建 DamageEventData
    ├─ 5. Modifier 回调（OnDealDamage + OnTakeDamage）
    │     可设置 IsCancelled = true 拦截
    ├─ 6. HpMin 夹底（薄葬式，取 modifier 最大值）
    ├─ 7. 扣血（HP - finalDamage，不溢出到下限以下）
    ├─ 8. 全局事件 EntityHurt
    ├─ 9. VFX（按伤害类型播默认受击特效）
    └─ 10. 若 HP ≤ 0 → EntityKilled 事件
```

## 治疗流程

```
ApplyHeal(target, source, rawHeal)
    │
    ├─ 1. null / ≤ 0 检查 → return 0
    ├─ 2. 治疗加成: final = rawHeal × (1 + HealBonus / 100)
    ├─ 3. 创建 HealEventData
    ├─ 4. 全局事件 HealApplied
    └─ 5. Restore("HP", finalHeal)
```

## DamageEventData

| 属性 | 类型 | 说明 |
|------|------|------|
| `Victim` | `UnitEntity` | 受击者 |
| `Attacker` | `UnitEntity` | 攻击者 |
| `RawDamage` | float | 原始伤害（可写） |
| `FinalDamage` | float | 最终伤害（抗性/暴击后，可写） |
| `DamageType` | string | 伤害类型（"FIRE"、"WATER" 等，可写） |
| `IsCritical` | bool | 是否为暴击 |
| `IsCancelled` | bool | 设为 true 可拦截伤害 |

## HealEventData

| 属性 | 类型 | 说明 |
|------|------|------|
| `Target` | `UnitEntity` | 治疗目标 |
| `Source` | `UnitEntity` | 治疗来源 |
| `RawHeal` | float | 原始治疗量（可写） |
| `FinalHeal` | float | 最终治疗量（可写） |
| `IsCancelled` | bool | 设为 true 可拦截治疗 |

## 元素抗性

框架使用字典查找机制：

```
DamageTypes.GetResistanceStat("FIRE") → "FireRES"
抗性减免系数 = Clamp(抗性值 / 100, 0, 0.9)    // 最高 90% 减免
```

默认公式是线性减免，上限 90%。可通过 `DamagePipeline.Formula = new MyFormula()` 替换。

## 暴击公式

```
critMult = isCrit ? 1 + critDamage / 100 : 1
CritRate 决定暴击概率（0~1），CritDMG 决定暴击倍率。
```

暴击判定使用 `System.Random`（非线程安全，单线程使用），每次伤害独立判定。

## IDamageFormula 接口

```csharp
public interface IDamageFormula
{
    float CalculateResistanceReduction(float resistanceValue);
    float CalculateCriticalMultiplier(float critRate, float critDamage, out bool isCrit);
}
```

框架提供 `DefaultDamageFormula` 实现（线性抗性 + RNG 暴击）。游戏方可全局替换。

## HpMin 夹底（薄葬式）

管线在扣血前收集所有活跃 modifier 的 `HpMin` Property 值并取最大值：

```
hpMin = max(所有 modifier 的 HpMin Property)
maxDamage = max(0, currentHP - hpMin)
actualDamage = min(finalDamage, maxDamage)
```

多个来源同时挂 HpMin 时取最大值，最强守护生效。

## 常用代码模式

```csharp
// 造成伤害
DamagePipeline.ApplyDamage(victim, attacker, rawDamage, DamageTypes.Fire, sourceAbility);

// 施加治疗
DamagePipeline.ApplyHeal(target, source, rawHeal);

// 替换伤害公式
DamagePipeline.Formula = new MyFormula();

// 收集 stat 贡献（base + modifier 轮询求和）
float totalMoveSpeed = unit.GetStat("WalkSpeed");

// 订阅全局伤害事件
DamagePipeline.GlobalEventBus.Subscribe(EventBus.Events.EntityHurt, data => {
    var dmgEvent = (DamageEventData)data!;
    Console.WriteLine($"{dmgEvent.Victim.Name} 受到 {dmgEvent.FinalDamage} 点伤害");
});
```
