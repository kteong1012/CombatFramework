using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.AbilityExecutor;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Damage;
using CombatFramework.Unit;

namespace CombatFramework.Core.Executor.Ability.ActionExecutor
{
    public class AbilityActionExecutor_ApplyModifier : AbilityActionExecutor
    {
        public TargetType targetType;
        public string modifierName;
        public IValueGetter<AbilitySpec> durationGetter;
        public AbilityActionExecutor_ApplyModifier(TargetType targetType, string modifierName, IValueGetter<AbilitySpec> durationGetter)
        {
            this.targetType = targetType;
            this.modifierName = modifierName;
            this.durationGetter = durationGetter;
        }
        public override void Execute(AbilityEventContext context)
        {
            if (context == null)
            {
                CFLog.Warning("AbilityHitUnitContext is null, skipping hit execution.");
                return;
            }
            var target = (UnitEntity)null;
            if (targetType == TargetType.Target)
            {
                target = context.target;
            }
            else if (targetType == TargetType.Caster)
            {
                target = context.caster;
            }
            else if (targetType == TargetType.Owner)
            {
                target = context.owner;
            }
            if (target == null)
            {
                CFLog.Warning($"Target is null for target type {targetType}, skipping hit execution.");
                return;
            }
            var ability = context.ability;
            var caster = context.caster;
            var duration = durationGetter.GetValue(ability);
            target.ModifierManager.Add(target, modifierName, duration);
        }
    }
}
