using CombatFramework.Unit;

namespace CombatFramework.Core.Ability.AbilityEvent
{
    public class AbilityEventContext
    {
        public AbilitySpec Ability { get; set; }
        public UnitEntity Caster { get; set; }
        public UnitEntity Target { get; set; }
    }
}
