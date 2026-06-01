using CombatFramework.Core.Executor.AbilityExecutor;

namespace CombatFramework.Core.Executor.Ability.ActionExecutor
{
    public abstract class AbilityActionExecutor
    {
        public abstract void Execute(AbilityEventContext context);
    }
}
