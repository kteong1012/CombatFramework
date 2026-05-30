using CombatFramework.Core;
using CombatFramework.Damage;
using CombatFramework.Unit;

namespace CombatFramework.Tests.Demo;

/// <summary>
/// 测试环境技能执行器——无物理拾取，直接对指定目标执行配置的 effects。
/// </summary>
public static class SkillSimulator
{
    public static void Cast(UnitEntity caster, AbilityData ability, UnitEntity target)
    {
        if (ability == null || caster == null || target == null) return;
        var sourceAbility = caster.AbilitySlots.Get(ability.Id);

        foreach (var effect in ability.Effects.Values)
        {
            foreach (var op in effect.Operations)
            {
                var victim = op.Target == "CASTER" ? caster : target;
                ExecuteOp(sourceAbility, caster, victim, op, ability);
            }
        }
    }

    private static void ExecuteOp(AbilityInstance? sourceAbility, UnitEntity caster, UnitEntity target, AbilityEffectOp op, AbilityData ability)
    {
        switch (op.Type)
        {
            case AbilityEffectOpType.Damage:
            {
                float totalDmg = 0;
                var hitNum = Math.Max(1, op.HitNum);

                if (op.Damage != null)
                {
                    if (op.Damage.RunFunctionInAbility != null && sourceAbility != null
                        && ability.EventHandlers.TryGetValue(op.Damage.RunFunctionInAbility, out var fn))
                    {
                        var ret = fn.Call(caster, sourceAbility, target);
                        totalDmg = (float)ret.Number;
                    }
                    else if (op.Damage.ParamRef != null && sourceAbility != null)
                    {
                        totalDmg = sourceAbility.GetParameter(op.Damage.ParamRef);
                    }
                    else if (op.Damage.Constant.HasValue)
                    {
                        totalDmg = op.Damage.Constant.Value;
                    }
                }

                var critRate = caster.GetStat(StatId.CritRate);
                var critDmgStat = caster.GetStat(StatId.CritDMG);
                var multi = MultiHitHelper.Process(totalDmg, hitNum, critRate, critDmgStat, () => CFServices.RandomProvider());

                DamagePipeline.ApplyDamage(target, caster, multi.TotalDamage, op.DamageType, sourceAbility);
                break;
            }
            case AbilityEffectOpType.Modifier:
            {
                if (string.IsNullOrEmpty(op.ModifierName)) break;
                if (ability.ModifierTemplates.TryGetValue(op.ModifierName, out var modData))
                    target.ModifierManager.Add(modData, caster, sourceAbility, op.Duration > 0 ? op.Duration : null);
                break;
            }
        }
    }
}
