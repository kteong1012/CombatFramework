using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Model;

/// <summary>
/// 对目标选择器返回的每个 unit 修改指定属性值（增加/减少均可）。
/// </summary>
[AbilityEventAction(typeof(ModifyStatActionData))]
public class ModifyStatAction : AbilityEventAction
{
    public new ModifyStatActionData data => (ModifyStatActionData)base.data;

    public ModifyStatAction(ModifyStatActionData data) : base(data) { }

    public override void Execute(AbilityEventContext context)
    {
        var amount = data.Value.GetValue(context.Ability);
        foreach (var target in data.Target.Resolve(context))
        {
            target.Stats.Add(data.Stat, amount);
        }
    }
}

public class ModifyStatActionData : AbilityEventActionData
{
    public TargetSelector Target;
    public string Stat;
    public IAbilityValueGetter Value;
}
