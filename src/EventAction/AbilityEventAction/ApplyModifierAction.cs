using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;

[AbilityEventAction(typeof(ApplyModifierActionData))]
public class ApplyModifierAction : AbilityEventAction
{
    public new ApplyModifierActionData data => (ApplyModifierActionData)base.data;

    public ApplyModifierAction(ApplyModifierActionData data) : base(data)
    {
    }

    public override void Execute(AbilityEventContext context)
    {
        var modifiers = context.Ability.data?.AbilityModifiers;
        if (modifiers == null || !modifiers.TryGetValue(data.ModifierName, out var modifierData))
        {
            return;
        }

        foreach (var target in data.Target.Resolve(context))
        {
            target.ModifierManager.Add(
                data: modifierData,
                caster: context.Caster,
                sourceAbility: context.Ability
            );
        }
    }
}

public class ApplyModifierActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public string ModifierName;
}