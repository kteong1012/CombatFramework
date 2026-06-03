using CombatFramework.Bridge;
using CombatFramework.Core;
using CombatFramework.Core.Modifier;
using CombatFramework.Event;
using CombatFramework.Unit;

namespace CombatFramework.Damage;

/// <summary>
/// 单次施法的额外伤害参数，所有字段均为叠加修正（默认不生效）。
/// 由技能/Modifier 在调用 ApplyDamage 时传入。
/// </summary>
public class DamageContext
{
    /// <summary>额外暴击率，叠加到攻击者属性上。</summary>
    public float ExtraCritRate;
    /// <summary>额外暴击伤害，叠加到攻击者属性上。</summary>
    public float ExtraCritDamage;
    /// <summary>额外防御无视率（百分比，0~1），叠加到攻击者属性上。</summary>
    public float ExtraDefIgnoreRate;
    /// <summary>部位吸收系数，默认 1（不吸收）。大型多部位 Boss 由技能设置。</summary>
    public float AbsorptionRate = 1f;
}

/// <summary>
/// 各乘区计算方法。可通过 BattlePipeline.Formula 替换实现。
/// </summary>
public class BattleFormula
{
    private Random _random = new Random();

    /// <summary>
    /// 防御乘区系数 A，用于 C = 攻击方等级 * A + B。
    /// 默认值参考客户端 TbGlobal DefCoefficientA。
    /// </summary>
    public float DefCoefficientA = 10f;

    /// <summary>防御乘区系数 B。</summary>
    public float DefCoefficientB = 100f;

    /// <summary>
    /// 防御乘区：C / (有效防御 + C)，结果至少为 1。
    /// C = 攻击方等级 * A + B
    /// 有效防御 = nowDef * (1 - defIgnoreRate) - defIgnore
    /// </summary>
    public float CalculateDefence(float damage, float attackerLevel,
        float nowDef, float defIgnoreRate, float defIgnore)
    {
        var c = attackerLevel * DefCoefficientA + DefCoefficientB;
        var effectiveDef = nowDef * (1f - defIgnoreRate) - defIgnore;
        effectiveDef = Math.Max(0f, effectiveDef);
        var defRate = c / (effectiveDef + c);
        return Math.Max(1f, damage * defRate);
    }

    /// <summary>
    /// 额外增减伤乘区：damage * (1 + DamageInc - DamageRed)，至少为 1。
    /// </summary>
    public float CalculateExtraAdd(float damage, float damageInc, float damageRed)
        => Math.Max(1f, damage * (1f + damageInc - damageRed));

    /// <summary>
    /// 易伤乘区：damage * (1 + WeakMult)，至少为 1。
    /// WeakMult 来自目标身上的减益 Modifier。
    /// </summary>
    public float CalculateDamageFix(float damage, float weakMult)
        => Math.Max(1f, damage * (1f + weakMult));

    /// <summary>
    /// 元素抗性乘区：damage * (1 - resistance + penetration)，至少为 1。
    /// resistance = 目标抗性 stat；penetration = 来源穿透 stat。
    /// </summary>
    public float CalculateElement(float damage, float resistance, float penetration)
        => Math.Max(1f, damage * (1f - resistance + penetration));

    /// <summary>
    /// 破韧增伤乘区：damage * (1 + BreakMultFinal)。
    /// BreakMultFinal 来自目标被破韧后挂上的 Modifier。
    /// </summary>
    public float CalculateBreak(float damage, float breakMult)
        => damage * (1f + breakMult);

    /// <summary>
    /// 部位吸收乘区：damage * absorptionRate（0 或负数视为 1）。
    /// 用于大型多部位 Boss 不同受击点。
    /// </summary>
    public float CalculateAbsorption(float damage, float absorptionRate)
        => damage * (absorptionRate > 0f ? absorptionRate : 1f);

    /// <summary>
    /// 暴击乘区：多段命中各自独立掷骰。
    /// 返回 (最终伤害, 暴击次数, 暴击倍率)。
    /// </summary>
    public (float finalDamage, int critCount, float critMult) CalculateCritical(
        float damage, int hit, float critRate, float critDmg)
    {
        var finalDamage = 0f;
        var perHit = damage / Math.Max(1, hit);
        var critCount = 0;
        for (var i = 0; i < hit; i++)
        {
            var d = perHit;
            if ((float)_random.NextDouble() * 100f < critRate)
            {
                d *= critDmg;
                critCount++;
            }
            finalDamage += d;
        }
        return (finalDamage, critCount, critDmg);
    }

    /// <summary>
    /// 属性精通乘区（异常伤害专用）：damage * (1 + AbnormalDamageMult)。
    /// </summary>
    public float CalculateAbnormal(float damage, float abnormalMult)
        => damage * (1f + abnormalMult);
}

public static class BattlePipeline
{
    public static BattleFormula Formula = new BattleFormula();

    /// <summary>
    /// 正常伤害完整管线。
    /// 顺序：防御 → 额外增减伤 → 易伤 → 元素抗性 → 破韧增伤 → 部位吸收 → 暴击（每段独立）
    /// </summary>
    /// <param name="rawDamage">
    /// 由 <see cref="IAbilityValueGetter.GetValue"/> 预先算好的基础伤害值。
    /// 已包含攻击者属性（ATK 等）× 技能系数 × 等级成长，管线只负责后续乘区，不再重算基础值。
    /// </param>
    /// <param name="hit">多段命中次数，每段暴击独立判定。</param>
    /// <param name="ctx">每次施法的额外修正，null 时使用默认值。</param>
    public static void ApplyDamage(UnitEntity victim, UnitEntity attacker, float rawDamage,
        string element, int hit = 1, DamageContext ctx = null)
    {
        if (victim == null || attacker == null) return;
        ctx ??= new DamageContext();

        var damage = rawDamage;

        // 1. 防御乘区
        var nowDef       = victim.GetStat("DefFinal");
        var defIgnoreRate = attacker.GetStat("DefIgnoreRate") + ctx.ExtraDefIgnoreRate;
        var defIgnore    = attacker.GetStat("DefIgnore");
        damage = Formula.CalculateDefence(damage, attacker.Level, nowDef, defIgnoreRate, defIgnore);

        // 2. 额外增减伤
        var damageInc = attacker.GetStat("DamageInc");
        var damageRed = victim.GetStat("DamageRed");
        damage = Formula.CalculateExtraAdd(damage, damageInc, damageRed);

        // 3. 易伤
        var weakMult = victim.GetStat("WeakMult");
        damage = Formula.CalculateDamageFix(damage, weakMult);

        // 4. 元素抗性
        var penetrationStat = CFBridge.Bridge.ElementProvider.GetAmplifyStat(element);
        var resistanceStat  = CFBridge.Bridge.ElementProvider.GetResistanceStat(element);
        var penetration = attacker.GetStat(penetrationStat);
        var resistance  = victim.GetStat(resistanceStat);
        damage = Formula.CalculateElement(damage, resistance, penetration);

        // 5. 破韧增伤
        var breakMult = victim.GetStat("BreakMultFinal");
        damage = Formula.CalculateBreak(damage, breakMult);

        // 6. 部位吸收
        damage = Formula.CalculateAbsorption(damage, ctx.AbsorptionRate);

        // 7. 暴击（多段各自掷骰）
        var critRate = attacker.GetStat("CritRate") + ctx.ExtraCritRate;
        var critDmg  = attacker.GetStat("CritDMG")  + ctx.ExtraCritDamage;
        var (finalDamage, critCount, _) = Formula.CalculateCritical(damage, hit, critRate, critDmg);

        var eventData = new DamageEventData(victim, attacker, rawDamage, element)
        {
            FinalDamage = finalDamage,
            IsCritical  = critCount > 0,
        };

        // 8. HpMin 夹底
        ApplyHpDamage(victim, attacker, eventData);
    }

    /// <summary>
    /// 异常伤害管线（DOT 等）。
    /// 顺序：防御 → 额外增减伤 → 易伤 → 元素抗性 → 破韧增伤 → 属性精通
    /// 无暴击、无部位吸收。
    /// </summary>
    /// <param name="rawDamage">
    /// 由 <see cref="IAbilityValueGetter.GetValue"/> 预先算好的基础伤害值，含属性和等级成长。
    /// </param>
    public static void ApplyAbnormalDamage(UnitEntity victim, UnitEntity attacker, float rawDamage,
        string element, DamageContext ctx = null)
    {
        if (victim == null || attacker == null) return;
        ctx ??= new DamageContext();

        var damage = rawDamage;

        // 1. 防御
        var nowDef        = victim.GetStat("DefFinal");
        var defIgnoreRate = attacker.GetStat("DefIgnoreRate") + ctx.ExtraDefIgnoreRate;
        var defIgnore     = attacker.GetStat("DefIgnore");
        damage = Formula.CalculateDefence(damage, attacker.Level, nowDef, defIgnoreRate, defIgnore);

        // 2. 额外增减伤
        damage = Formula.CalculateExtraAdd(damage, attacker.GetStat("DamageInc"), victim.GetStat("DamageRed"));

        // 3. 易伤
        damage = Formula.CalculateDamageFix(damage, victim.GetStat("WeakMult"));

        // 4. 元素抗性
        var penetration = attacker.GetStat(CFBridge.Bridge.ElementProvider.GetAmplifyStat(element));
        var resistance  = victim.GetStat(CFBridge.Bridge.ElementProvider.GetResistanceStat(element));
        damage = Formula.CalculateElement(damage, resistance, penetration);

        // 5. 破韧增伤
        damage = Formula.CalculateBreak(damage, victim.GetStat("BreakMultFinal"));

        // 6. 属性精通（异常伤害专用）
        damage = Formula.CalculateAbnormal(damage, attacker.GetStat("AbnormalDamageMult"));

        var eventData = new DamageEventData(victim, attacker, rawDamage, element)
        {
            FinalDamage = damage,
            IsCritical  = false,
        };

        ApplyHpDamage(victim, attacker, eventData);
    }

    /// <summary>治疗流程</summary>
    public static void ApplyHeal(UnitEntity target, UnitEntity source, float rawHeal)
    {
        //if (target == null || rawHeal <= 0) return;

        //var healBonus = source.GetStat("HealBonus");
        //var finalHeal = rawHeal * (1 + healBonus / 100f);

        //var eventData = new HealEventData(target, source, rawHeal) { FinalHeal = finalHeal };
        //GlobalEventBus.Publish(EventBus.Events.HealApplied, eventData);
        //if (eventData.IsCancelled) return 0;

        //target.Resources.Restore("HP", finalHeal);
        //return finalHeal;
    }

    /// <summary>击破伤害——扣除韧性值，触发韧性变化/破韧事件。</summary>
    public static void ApplyToughnessReduce(UnitEntity victim, UnitEntity attacker, float toughnessReduce)
    {
        //if (victim == null || attacker == null || breakAmount <= 0) return 0;

        //var slot = victim.Resources.Get("Toughness");
        //if (slot == null) return 0;

        //var oldVal = slot.Current;
        //var maxVal = slot.Max;
        //var consumed = victim.Resources.ConsumeSafe("Toughness", breakAmount, 0);
        //var newVal = slot.Current;

        //var evt = new ToughnessEventData(victim, attacker, oldVal, newVal, maxVal);
        //GlobalEventBus.Publish(EventBus.Events.ToughnessChanged, evt);

        //if (evt.IsBroken)
        //{
        //    GlobalEventBus.Publish(EventBus.Events.ToughnessBreak, evt);
        //}

        //return consumed;
    }

    // ── 内部 ──────────────────────────────────────────────────────────────

    private static void ApplyHpDamage(UnitEntity victim, UnitEntity attacker, DamageEventData eventData)
    {
        var currentHp = victim.GetStat("HP");
        var hpMin     = victim.GetStat("HpMin");
        var maxDamage = Math.Max(0f, currentHp - hpMin);
        var damageToConsume = Math.Min(eventData.FinalDamage, maxDamage);
        victim.Stats.Add("HP", -damageToConsume);
        eventData.ModifiedDamage = damageToConsume;

        EventBus.Global.Publish(EventBus.Events.OnDealDamage, eventData);

        if (damageToConsume > 0 && victim.GetStat("HP") <= 0)
        {
            EventBus.Global.Publish(EventBus.Events.EntityKilled,
                new { Victim = victim, Attacker = attacker });
        }
    }
}
