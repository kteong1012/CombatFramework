using System.Numerics;
using CombatFramework.Core;
using CombatFramework.Event;
using CombatFramework.Unit;
using ResourceSlot = CombatFramework.Stat.ResourceSystem.ResourceSlot;

namespace CombatFramework.Damage;

/// <summary>
/// 伤害管线——处理 ApplyDamage 的完整流程。
/// 四条管线：正常 / 异常 / 击破 / 治疗。
/// </summary>
public static class DamagePipeline
{
    public static IDamageFormula Formula { get; set; } = new DefaultDamageFormula();
    public static EventBus GlobalEventBus { get; set; } = new();
    public static Core.IVfxEffectService? VfxService { get; set; }

    /// <summary>正常伤害完整流程</summary>
    public static float ApplyDamage(UnitEntity victim, UnitEntity attacker, float rawDamage,
        string damageType, Core.AbilityInstance? sourceAbility = null,
        int hitEntityId = 0, Vector3 hitPoint = default)
    {
        if (victim == null || attacker == null) return 0;

        var critRate = attacker.GetStat(StatId.CritRate);
        var critDmg  = attacker.GetStat(StatId.CritDMG);
        var critMult = Formula.CalculateCriticalMultiplier(critRate, critDmg, out var isCrit);

        var resStatId = DamageTypes.GetResistanceStat(damageType);
        float resValue = 0;
        if (resStatId != null)
        {
            resValue = victim.Stats.ElementalResistance.TryGetValue(damageType, out var r)
                ? r
                : victim.GetStat(resStatId);
        }

        var reduction   = Formula.CalculateResistanceReduction(resValue);
        var finalDamage = rawDamage * (1f - reduction) * critMult;

        var eventData = new DamageEventData(victim, attacker, rawDamage, damageType)
        {
            FinalDamage = finalDamage,
            IsCritical  = isCrit,
            HitEntityId = hitEntityId,
            HitPoint    = hitPoint,
        };

        attacker.ModifierManager.DispatchEvent(ModifierHookType.OnDealDamage, eventData);
        victim.ModifierManager.DispatchEvent(ModifierHookType.OnTakeDamage, eventData);
        if (eventData.IsCancelled) return 0;

        // HpMin 夹底（薄葬式）
        var currentHp      = victim.Resources.GetCurrent(StatId.HP);
        var hpMin          = victim.ModifierManager.CollectMax(StatId.HpMin) ?? 0f;
        var maxDamage      = Math.Max(0f, currentHp - hpMin);
        var damageToConsume = Math.Min(eventData.FinalDamage, maxDamage);
        victim.Resources.TryConsume(StatId.HP, damageToConsume);
        eventData.FinalDamage = damageToConsume;

        GlobalEventBus.Publish(EventBus.Events.EntityHurt, eventData);

        // VFX — 按伤害类型播默认受击特效
        if (VfxService != null && eventData.FinalDamage > 0)
        {
            var vfxPath = DamageTypes.GetDefaultVfx(damageType);
            if (vfxPath != null) { VfxService.PlayOnUnit(vfxPath, victim, lifeTime: 1.5f); }
        }

        if (eventData.FinalDamage > 0 && victim.Resources.GetCurrent(StatId.HP) <= 0)
        {
            GlobalEventBus.Publish(EventBus.Events.EntityKilled,
                new { Victim = victim, Attacker = attacker });
        }

        return eventData.FinalDamage;
    }

    /// <summary>治疗流程</summary>
    public static float ApplyHeal(UnitEntity target, UnitEntity source, float rawHeal)
    {
        if (target == null || rawHeal <= 0) return 0;

        var healBonus = source.GetStat(StatId.HealBonus);
        var finalHeal = rawHeal * (1 + healBonus / 100f);

        var eventData = new HealEventData(target, source, rawHeal) { FinalHeal = finalHeal };
        GlobalEventBus.Publish(EventBus.Events.HealApplied, eventData);
        if (eventData.IsCancelled) return 0;

        target.Resources.Restore(StatId.HP, finalHeal);
        return finalHeal;
    }

    /// <summary>击破伤害——扣除韧性值，触发韧性变化/破韧事件。</summary>
    public static float ApplyBreakDamage(UnitEntity victim, UnitEntity attacker, float breakAmount)
    {
        if (victim == null || attacker == null || breakAmount <= 0) return 0;

        var slot = victim.Resources.Get(StatId.Toughness);
        if (slot == null) return 0;

        var oldVal = slot.Current;
        var maxVal = slot.Max;
        var consumed = victim.Resources.ConsumeSafe(StatId.Toughness, breakAmount, 0);
        var newVal = slot.Current;

        var evt = new ToughnessEventData(victim, attacker, oldVal, newVal, maxVal);
        GlobalEventBus.Publish(EventBus.Events.ToughnessChanged, evt);

        if (evt.IsBroken)
        {
            GlobalEventBus.Publish(EventBus.Events.ToughnessBreak, evt);
        }

        return consumed;
    }
}
