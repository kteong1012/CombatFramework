using CombatFramework.Core.Model;
using CombatFramework.Unit;

namespace CombatFramework.Core.Ability;

/// <summary>
/// 技能转换条件基类。子类通过 JSON <c>$type</c> 字段区分（使用短别名）。
/// </summary>
public abstract class AbilityCondition
{
    /// <summary>对当前施放上下文求值。</summary>
    /// <param name="ability">当前执行转换检查的 AbilitySpec。</param>
    /// <param name="caster">施放者。</param>
    /// <param name="target">目标，可为 null。</param>
    public abstract bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target);
}

// ── 复合条件 ──────────────────────────────────────────────────────────────────

/// <summary>所有子条件均为 true 时为 true。</summary>
[JsonAlias("And")]
public class AndCondition : AbilityCondition
{
    public List<AbilityCondition> Conds { get; set; }

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
        => Conds != null && Conds.All(c => c.Evaluate(ability, caster, target));
}

/// <summary>任一子条件为 true 时为 true。</summary>
[JsonAlias("Or")]
public class OrCondition : AbilityCondition
{
    public List<AbilityCondition> Conds { get; set; }

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
        => Conds != null && Conds.Any(c => c.Evaluate(ability, caster, target));
}

/// <summary>子条件取反。</summary>
[JsonAlias("Not")]
public class NotCondition : AbilityCondition
{
    public AbilityCondition Cond { get; set; }

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
        => Cond != null && !Cond.Evaluate(ability, caster, target);
}

// ── 内置条件 ──────────────────────────────────────────────────────────────────

/// <summary>检查指定单位是否持有某个 Modifier。</summary>
[JsonAlias("HasModifier")]
public class HasModifierCondition : AbilityCondition
{
    public string ModifierName { get; set; }

    /// <summary>"Caster"（默认）或 "Target"。</summary>
    public string On { get; set; } = "Caster";

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
    {
        var unit = On == "Target" ? target : caster;
        return unit?.ModifierManager.Has(ModifierName) ?? false;
    }
}

/// <summary>匹配模式：任一 / 全部 / 无。</summary>
public enum TagCheckOp { Any, All, None }

/// <summary>检查指定单位的标签。</summary>
[JsonAlias("CheckTag")]
public class CheckTagCondition : AbilityCondition
{
    public TagCheckOp Op { get; set; } = TagCheckOp.Any;
    public List<string> Tags { get; set; }

    /// <summary>"Caster"（默认）或 "Target"。</summary>
    public string On { get; set; } = "Caster";

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
    {
        var unit = On == "Target" ? target : caster;
        if (unit == null || Tags == null || Tags.Count == 0) return false;
        return Op switch
        {
            TagCheckOp.All  => Tags.All(t => unit.HasTag(t)),
            TagCheckOp.None => Tags.All(t => !unit.HasTag(t)),
            _               => Tags.Any(t => unit.HasTag(t)),
        };
    }
}

/// <summary>属性比较运算符。</summary>
public enum StatCheckOp { Gt, Gte, Lt, Lte, Eq, Neq }

/// <summary>检查指定单位的属性值。</summary>
[JsonAlias("CheckStat")]
public class CheckStatCondition : AbilityCondition
{
    public string StatName { get; set; }
    public StatCheckOp Op { get; set; } = StatCheckOp.Gte;
    public float Value { get; set; }

    /// <summary>"Caster"（默认）或 "Target"。</summary>
    public string On { get; set; } = "Caster";

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
    {
        var unit = On == "Target" ? target : caster;
        if (unit == null || string.IsNullOrEmpty(StatName)) return false;
        var val = unit.GetStat(StatName);
        return Op switch
        {
            StatCheckOp.Gt  => val > Value,
            StatCheckOp.Gte => val >= Value,
            StatCheckOp.Lt  => val < Value,
            StatCheckOp.Lte => val <= Value,
            StatCheckOp.Eq  => Math.Abs(val - Value) < 1e-5f,
            StatCheckOp.Neq => Math.Abs(val - Value) >= 1e-5f,
            _               => false,
        };
    }
}

/// <summary>
/// 调用 AbilitySpec 子类的无参布尔方法。
/// <para>方法签名：<c>public bool {MethodName}()</c>，定义在子类（如 <c>FireBallAbility</c>）中。</para>
/// </summary>
[JsonAlias("AbilityMethod")]
public class AbilityMethodCondition : AbilityCondition
{
    public string MethodName { get; set; }

    public override bool Evaluate(AbilitySpec ability, UnitEntity caster, UnitEntity target)
    {
        if (string.IsNullOrEmpty(MethodName) || ability == null) return false;
        var method = ability.GetType().GetMethod(
            MethodName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (method == null || method.ReturnType != typeof(bool)) return false;
        return (bool)method.Invoke(ability, null);
    }
}
