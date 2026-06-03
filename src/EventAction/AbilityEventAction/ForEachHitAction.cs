using CombatFramework.Core;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;

/// <summary>
/// 对目标选择器返回的每个 unit 触发 ability.OnHitTarget。
/// OnHitTarget 的 context.Target 被替换为当前迭代的 unit，
/// 后续 Action（如 DamageAction）可在 OnHitTarget 事件中对单个目标生效。
/// </summary>
[AbilityEventAction(typeof(ForEachHitActionData))]
public class ForEachHitAction : AbilityEventAction
{
    public new ForEachHitActionData data => (ForEachHitActionData)base.data;

    public ForEachHitAction(ForEachHitActionData data) : base(data)
    {
    }

    public override void Execute(AbilityEventContext context)
    {
        foreach (var target in data.Target.Resolve(context))
        {
            var hitCtx = new AbilityEventContext
            {
                Ability = context.Ability,
                Caster  = context.Caster,
                Target  = target,
            };
            context.Ability.OnHitTarget(hitCtx);
        }
    }
}

public class ForEachHitActionData : AbilityEventActionData
{
    public TargetSelector Target;
}
