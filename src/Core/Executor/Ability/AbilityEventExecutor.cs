using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;
using CombatFramework.Core.Executor.Ability.ActionExecutor;
using CombatFramework.Damage;
using CombatFramework.Unit;

namespace CombatFramework.Core.Executor.AbilityExecutor
{
    public class AbilityEventContext
    {
        public AbilitySpec ability;
        public UnitEntity target;
        public UnitEntity caster;
        public UnitEntity owner;
    }
    public class AbilityEventExecutor
    {
        public TeamType TeamType;

        public List<AbilityActionExecutor> actionExecutors;

        public void Execute(AbilityEventContext context)
        {
            foreach (var executor in actionExecutors)
            {
                executor.Execute(context);
            }
        }
    }
}
