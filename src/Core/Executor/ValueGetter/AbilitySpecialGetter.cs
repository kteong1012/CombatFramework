using CombatFramework.Core.Ability;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public class AbilitySpecialGetter : IValueGetter<AbilitySpec>
    {
        private readonly string name;

        public AbilitySpecialGetter(string name)
        {
            this.name = name;
        }

        public float GetValue(AbilitySpec context)
        {
            if (context.TryGetLevelValue(name, out var value))
            {
                return value;
            }
            else
            {
                CFLog.Warning($"AbilitySpecial {name} not found in ability {context.data.Name}, return 0");
                return 0f;
            }
        }
    }
}
