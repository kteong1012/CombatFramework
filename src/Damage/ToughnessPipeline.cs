using CombatFramework.Core.Ability;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Core.Modifier;
using CombatFramework.Unit;

namespace CombatFramework.Damage;

/// <summary>
/// 韧性值 / 破韧管线。
/// <para>
/// 韧性相关 Stats（均通过 <c>StatsManager</c> 读写）：
/// <list type="bullet">
///   <item><c>ToughnessMax</c>  — 韧性上限（怪物配置）</item>
///   <item><c>Toughness</c>     — 当前韧性值（运行时，初始应由游戏层 = ToughnessMax）</item>
///   <item><c>BreakDmgBonus</c> — 攻击方破韧增伤属性</item>
///   <item><c>BreakMultFinal</c>— 破韧状态期间赋值，供 BattlePipeline 读取</item>
/// </list>
/// </para>
/// </summary>
public static class ToughnessPipeline
{
    /// <summary>全局平衡系数 K，参见设计文档 §1.3。</summary>
    public const float GlobalBreakCoeff = 0.5f;

    /// <summary>破韧状态持续时间（秒）。</summary>
    public const float BreakDuration = 5f;

    // ─── 公共 API ──────────────────────────────────────────────────────────

    /// <summary>
    /// 对目标施加削韧，韧性归零时触发破韧。
    /// HP 削减仍由调用方通过 <see cref="BattlePipeline.ApplyDamage"/> 处理，本方法只管韧性条。
    /// </summary>
    /// <param name="victim">受击方。</param>
    /// <param name="attacker">攻击方（提供 BreakDmgBonus）。</param>
    /// <param name="reduction">本次削韧量（&gt;0）。</param>
    public static void ApplyToughness(UnitEntity victim, UnitEntity attacker, float reduction)
    {
        if (victim == null || attacker == null || reduction <= 0f) return;
        if (victim.HasTag("broken")) return;   // 已处于破韧状态，不重复触发

        float current = victim.GetStat("Toughness");
        float newVal  = current - reduction;
        victim.Stats.Set("Toughness", Math.Max(0f, newVal));

        if (newVal <= 0f)
            TriggerBreak(victim, attacker);
    }

    /// <summary>
    /// 击破伤害公式：AttackerLevel × (1 + BreakDmgBonus) × ToughnessMax × K
    /// </summary>
    public static float CalculateBreakDamage(UnitEntity attacker, UnitEntity victim)
    {
        float level    = attacker.Level;
        float bonus    = attacker.GetStat("BreakDmgBonus");
        float toughMax = victim.GetStat("ToughnessMax");
        return level * (1f + bonus) * toughMax * GlobalBreakCoeff;
    }

    // ─── 私有实现 ──────────────────────────────────────────────────────────

    private static void TriggerBreak(UnitEntity victim, UnitEntity attacker)
    {
        // 1. 击破伤害（直接扣血，不走韧性）
        float breakDmg = CalculateBreakDamage(attacker, victim);
        victim.Stats.Set("HP", Math.Max(0f, victim.GetStat("HP") - breakDmg));

        // 2. 写入破韧增伤（管线读 victim.BreakMultFinal）
        victim.Stats.Set("BreakMultFinal", attacker.GetStat("BreakDmgBonus"));

        // 3. 施加破韧 Modifier（BreakModifierSpec.OnCreated 挂标签，OnDestroy 清理）
        var modData = new ModifierData
        {
            Name           = "broken",
            Class          = nameof(BreakModifierSpec),
            IsDebuff       = true,
            IsPurgable     = false,
            StackMode      = ModifierStackMode.None,
            DurationGetter = new ConstantValueGetter(BreakDuration),
        };
        victim.ModifierManager.Add(modData, attacker, null);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// BreakModifierSpec — 破韧状态运行时实例
// Class 字段填 "BreakModifierSpec"，由 TypeProvider 反射实例化。
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 破韧状态 Modifier。挂载 <c>stunned</c> + <c>broken</c> 标签，到期自动清理。
/// </summary>
public class BreakModifierSpec : ModifierSpec
{
    public BreakModifierSpec(ModifierData data, UnitEntity parent, UnitEntity caster, AbilitySpec sourceAbility)
        : base(data, parent, caster, sourceAbility) { }

    public override void OnCreated()
    {
        base.OnCreated();
        Parent.Tags.AddTag("stunned");
        Parent.Tags.AddTag("broken");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Parent.Tags.RemoveTag("stunned");
        Parent.Tags.RemoveTag("broken");
        Parent.Stats.Set("BreakMultFinal", 0f);
        // 韧性回满
        Parent.Stats.Set("Toughness", Parent.GetStat("ToughnessMax"));
    }
}

