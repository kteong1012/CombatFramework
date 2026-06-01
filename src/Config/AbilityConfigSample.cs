using System.Collections.Generic;

using System.Collections.Generic;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.Ability.ActionExecutor;
using CombatFramework.Core.Executor.AbilityExecutor;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Modifier;

namespace CombatFramework.Config;

public static class AbilityConfigSample
{
    public static readonly ModifierData ModifierAtkBonus = new()
    {
        Name = "modifier_atk_bonus",
        IsBuff = true,
        IsDebuff = false,
        IsHidden = false,
        IsPurgable = false,
        Attribute = ModifierAttribute.None,
        DurationGetter = new ConstantValueGetter<ModifierSpec>(0f),
    };

    public static readonly AbilityData EveAttackLight01 = BuildEveAttackLight01();

    private static AbilityData BuildEveAttackLight01()
    {
        var ability = new AbilityData
        {
            Name = "eve_attack_light01",
            Element = "FIRE",
        };

        ability.dynamicValues["damage_k"] = new AbilityData.LevelValue { values = new[] { 0.1f } };
        ability.dynamicValues["damge_b"] = new AbilityData.LevelValue { values = new[] { 10f, 20f, 30f, 40f, 50f } };
        ability.dynamicValues["hp_cost"] = new AbilityData.LevelValue { values = new[] { 10f } };

        ability.costMaps["HP"] = new AbilitySpecialGetter("hp_cost");

        var kGetter = new AbilitySpecialGetter("damage_k");
        var bGetter = new AbilitySpecialGetter("damge_b");
        var damageGetter = new CustomValueGetter<AbilitySpec>(ctx =>
        {
            var atk = ctx.Unit?.GetStat("Atk") ?? 0f;
            var k = kGetter.GetValue(ctx);
            var b = bGetter.GetValue(ctx);
            return atk * k + b;
        });

        ability.eventExecutors["main_hit"] = new AbilityEventExecutor
        {
            TeamType = TeamType.Enemy,
            actionExecutors = new List<AbilityActionExecutor>
            {
                new AbilityActionExecutor_Damage(TargetType.Target, "FIRE", damageGetter),
            },
        };

        return ability;
    }
}
