using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Damage;
using CombatFramework.Unit;

[AbilityEventAction(typeof(DamageActionData))]
public class DamageAction : AbilityEventAction
{
    public new DamageActionData data => (DamageActionData)base.data;

    public DamageAction(DamageActionData data) : base(data)
    {
    }

    public override void Execute(AbilityEventContext context)
    {
        var damage = data.Damage.GetValue(context.Ability);
        foreach (var victim in data.Target.Resolve(context))
        {
            if (!PassesTeamFilter(context.Caster, victim, data.Teams)) continue;
            BattlePipeline.ApplyDamage(
                victim: victim,
                attacker: context.Caster,
                rawDamage: damage,
                element: data.Element
            );
        }
    }

    private static bool PassesTeamFilter(UnitEntity caster, UnitEntity victim, TeamFilter filter)
    {
        if (filter == TeamFilter.All) return true;
        bool sameTeam = caster?.Team == victim?.Team;
        if (filter.HasFlag(TeamFilter.Mate)  && sameTeam)  return true;
        if (filter.HasFlag(TeamFilter.Enemy) && !sameTeam) return true;
        return false;
    }
}

public class DamageActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public string Element;
    public IAbilityValueGetter Damage;
    /// <summary>对目标进行阵营过滤。默认 All = 无过滤。</summary>
    public TeamFilter Teams { get; set; } = TeamFilter.All;
}