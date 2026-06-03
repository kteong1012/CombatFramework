using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;
using CombatFramework.Damage;

[AbilityEventAction(typeof(HealActionData))]
public class HealAction : AbilityEventAction
{
    public new HealActionData data => (HealActionData)base.data;

    public HealAction(HealActionData data) : base(data) { }

    public override void Execute(AbilityEventContext context)
    {
        var amount = data.Heal.GetValue(context.Ability);
        foreach (var target in data.Target.Resolve(context))
        {
            BattlePipeline.ApplyHeal(target, context.Caster, amount);
        }
    }
}

public class HealActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public IAbilityValueGetter Heal;
}
