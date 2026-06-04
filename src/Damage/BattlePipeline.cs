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
/// 伤害公式接口。实现此接口并通过 Bridge.Formula 注入，框架管线只调用这两个方法。
/// </summary>
public interface IBattleFormula
{
    (float finalDamage, int critCount) Calculate(
        float rawDamage, int hit, UnitEntity attacker, UnitEntity victim, string element, DamageContext ctx);

    float CalculateAbnormalDamage(
        float rawDamage, UnitEntity attacker, UnitEntity victim, string element, DamageContext ctx);
}

/// <summary>
/// 内置默认伤害公式。可继承覆盖，也可完全替换为自己的 <see cref="IBattleFormula"/> 实现。
/// </summary>
public class DefaultBattleFormula : IBattleFormula
{
    private Random _random = new Random();

    public float DefCoefficientA = 10f;
    public float DefCoefficientB = 100f;

    /// <summary>
    /// 普通伤害完整管线，返回 (finalDamage, critCount)。
    /// </summary>
    public virtual (float finalDamage, int critCount) Calculate(
        float rawDamage, int hit, UnitEntity attacker, UnitEntity victim, string element, DamageContext ctx)
    {
        var damage = rawDamage;

        // 防御
        var c            = attacker.Level * DefCoefficientA + DefCoefficientB;
        var defIgnore    = attacker.GetStat("DefIgnoreRate") + ctx.ExtraDefIgnoreRate;
        var effectiveDef = Math.Max(0f, victim.GetStat("DefFinal") * (1f - defIgnore) - attacker.GetStat("DefIgnore"));
        damage = Math.Max(1f, damage * c / (effectiveDef + c));

        // 额外增减伤
        damage = Math.Max(1f, damage * (1f + attacker.GetStat("DamageInc") - victim.GetStat("DamageRed")));

        // 易伤
        damage = Math.Max(1f, damage * (1f + victim.GetStat("WeakMult")));

        // 元素抗性
        var penetration = attacker.GetStat(CFBridge.Bridge.ElementProvider.GetAmplifyStat(element));
        var resistance  = victim.GetStat(CFBridge.Bridge.ElementProvider.GetResistanceStat(element));
        damage = Math.Max(1f, damage * (1f - resistance + penetration));

        // 破韧增伤
        damage *= 1f + victim.GetStat("BreakMultFinal");

        // 部位吸收
        if (ctx.AbsorptionRate > 0f) damage *= ctx.AbsorptionRate;

        // 暴击
        var critRate    = attacker.GetStat("CritRate") + ctx.ExtraCritRate;
        var critDmg     = attacker.GetStat("CritDMG")  + ctx.ExtraCritDamage;
        var finalDamage = 0f;
        var perHit      = damage / Math.Max(1, hit);
        var critCount   = 0;
        for (var i = 0; i < hit; i++)
        {
            var d = perHit;
            if ((float)_random.NextDouble() * 100f < critRate) { d *= critDmg; critCount++; }
            finalDamage += d;
        }
        return (finalDamage, critCount);
    }

    /// <summary>
    /// 异常/DOT 伤害完整管线，无暴击。
    /// </summary>
    public virtual float CalculateAbnormalDamage(
        float rawDamage, UnitEntity attacker, UnitEntity victim, string element, DamageContext ctx)
    {
        var damage = rawDamage;

        // 防御
        var c            = attacker.Level * DefCoefficientA + DefCoefficientB;
        var defIgnore    = attacker.GetStat("DefIgnoreRate") + ctx.ExtraDefIgnoreRate;
        var effectiveDef = Math.Max(0f, victim.GetStat("DefFinal") * (1f - defIgnore) - attacker.GetStat("DefIgnore"));
        damage = Math.Max(1f, damage * c / (effectiveDef + c));

        // 额外增减伤
        damage = Math.Max(1f, damage * (1f + attacker.GetStat("DamageInc") - victim.GetStat("DamageRed")));

        // 易伤
        damage = Math.Max(1f, damage * (1f + victim.GetStat("WeakMult")));

        // 元素抗性
        var penetration = attacker.GetStat(CFBridge.Bridge.ElementProvider.GetAmplifyStat(element));
        var resistance  = victim.GetStat(CFBridge.Bridge.ElementProvider.GetResistanceStat(element));
        damage = Math.Max(1f, damage * (1f - resistance + penetration));

        // 破韧增伤
        damage *= 1f + victim.GetStat("BreakMultFinal");

        // 属性精通
        return damage * (1f + attacker.GetStat("AbnormalDamageMult"));
    }
}

public static class BattlePipeline
{
    /// <summary>
    /// 当前生效公式：优先使用 Bridge 注入的实例，否则回退到内置默认值。
    /// 游戏侧通过子类化 <see cref="BattleFormula"/> 并赋值给
    /// <see cref="Bridge.AbstractCombatFrameworkBridge.Formula"/> 来替换任意乘区。
    /// </summary>
    private static readonly IBattleFormula _defaultFormula = new DefaultBattleFormula();
    private static IBattleFormula Formula => CFBridge.Bridge?.Formula ?? _defaultFormula;

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

        var (finalDamage, critCount) = Formula.Calculate(rawDamage, hit, attacker, victim, element, ctx);

        var eventData = new DamageEventData(victim, attacker, rawDamage, element)
        {
            FinalDamage = finalDamage,
            IsCritical  = critCount > 0,
        };
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

        var finalDamage = Formula.CalculateAbnormalDamage(rawDamage, attacker, victim, element, ctx);

        var eventData = new DamageEventData(victim, attacker, rawDamage, element)
        {
            FinalDamage = finalDamage,
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
            victim.ModifierManager.DeactivateAll();
            EventBus.Global.Publish(EventBus.Events.EntityKilled,
                new { Victim = victim, Attacker = attacker });
        }
    }
}
