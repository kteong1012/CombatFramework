using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Modifier;
using CombatFramework.Event;
using CombatFramework.Unit;
namespace CombatFramework.Damage;

public class BattleFormula
{
    private Random _random = new Random();

    /// <summary>暴击伤害倍率计算</summary>
    public float CalculateCriticalMultiplier(float critRate, float critDmg, out bool isCrit)
    {
        var roll = (float)_random.NextDouble() * 100f;
        isCrit = roll < critRate;
        return isCrit ? 1f + critDmg / 100f : 1f;
    }
    /// <summary>抗性乘区，1 + amplify - resistance  </summary>
    public float CalculateResistanceReduction(float amplify, float resisstance)
    {
        return 1 + amplify - resisstance;
    }
}

public static class BattlePipeline
{
    public static BattleFormula Formula = new BattleFormula();
    /// <summary>正常伤害完整流程</summary>
    public static void ApplyDamage(UnitEntity victim, UnitEntity attacker, float rawDamage,
        string element)
    {
        if (victim == null || attacker == null)
        {
            return;
        }

        // todo 乘区还有需求，暂时后续会变
        var critRate = attacker.GetStat(StatId.CritRate);
        var critDmg = attacker.GetStat(StatId.CritDMG);
        var critMult = Formula.CalculateCriticalMultiplier(critRate, critDmg, out var isCrit);

        var amplifyStat = CFBridge.Bridge.ElementProvider.GetAmplifyStat(element);
        var resistanceStat = CFBridge.Bridge.ElementProvider.GetResistanceStat(element);
        var am = attacker.GetStat(amplifyStat);
        var rm = victim.GetStat(resistanceStat);

        var elementMult = Formula.CalculateResistanceReduction(am, rm);
        var finalDamage = rawDamage * elementMult * critMult;

        var eventData = new DamageEventData(victim, attacker, rawDamage, element)
        {
            FinalDamage = finalDamage,
            IsCritical = isCrit
        };


        // HpMin 夹底
        var currentHp = victim.GetStat(StatId.HP);
        var hpMin = victim.GetStat(StatId.HpMin);
        var maxDamage = Math.Max(0f, currentHp - hpMin);
        var damageToConsume = Math.Min(eventData.FinalDamage, maxDamage);
        victim.Stats.Add(StatId.HP, damageToConsume);
        eventData.ModifiedDamage = damageToConsume;

        EventBus.Global.Publish(EventBus.Events.OnDealDamage, eventData);

        if (eventData.ModifiedDamage > 0 && victim.GetStat(StatId.HP) <= 0)
        {
            EventBus.Global.Publish(EventBus.Events.EntityKilled,
                new { Victim = victim, Attacker = attacker });
        }
    }

    /// <summary>治疗流程</summary>
    public static void ApplyHeal(UnitEntity target, UnitEntity source, float rawHeal)
    {
        //if (target == null || rawHeal <= 0) return;

        //var healBonus = source.GetStat(StatId.HealBonus);
        //var finalHeal = rawHeal * (1 + healBonus / 100f);

        //var eventData = new HealEventData(target, source, rawHeal) { FinalHeal = finalHeal };
        //GlobalEventBus.Publish(EventBus.Events.HealApplied, eventData);
        //if (eventData.IsCancelled) return 0;

        //target.Resources.Restore(StatId.HP, finalHeal);
        //return finalHeal;
    }

    /// <summary>击破伤害——扣除韧性值，触发韧性变化/破韧事件。</summary>
    public static void ApplyToughnessReduce(UnitEntity victim, UnitEntity attacker, float toughnessReduce)
    {
        //if (victim == null || attacker == null || breakAmount <= 0) return 0;

        //var slot = victim.Resources.Get(StatId.Toughness);
        //if (slot == null) return 0;

        //var oldVal = slot.Current;
        //var maxVal = slot.Max;
        //var consumed = victim.Resources.ConsumeSafe(StatId.Toughness, breakAmount, 0);
        //var newVal = slot.Current;

        //var evt = new ToughnessEventData(victim, attacker, oldVal, newVal, maxVal);
        //GlobalEventBus.Publish(EventBus.Events.ToughnessChanged, evt);

        //if (evt.IsBroken)
        //{
        //    GlobalEventBus.Publish(EventBus.Events.ToughnessBreak, evt);
        //}

        //return consumed;
    }
}
