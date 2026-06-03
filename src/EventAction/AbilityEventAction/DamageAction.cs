using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Damage;

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
            BattlePipeline.ApplyDamage(
                victim: victim,
                attacker: context.Caster,
                rawDamage: damage,
                element: data.Element
            );
        }
    }
}


public class DamageActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public string Element;
    public IAbilityValueGetter Damage;
}