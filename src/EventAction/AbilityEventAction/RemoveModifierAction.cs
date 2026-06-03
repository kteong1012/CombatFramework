using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;

[AbilityEventAction(typeof(RemoveModifierActionData))]
public class RemoveModifierAction : AbilityEventAction
{
    public new RemoveModifierActionData data => (RemoveModifierActionData)base.data;

    public RemoveModifierAction(RemoveModifierActionData data) : base(data)
    {
    }

    public override void Execute(AbilityEventContext context)
    {
        foreach (var target in data.Target.Resolve(context))
        {
            target.ModifierManager.RemoveByName(data.ModifierName);
        }
    }
}

public class RemoveModifierActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public string ModifierName;
}
