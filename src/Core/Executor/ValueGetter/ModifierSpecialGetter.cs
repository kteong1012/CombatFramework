using CombatFramework.Core.Modifier;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public class ModifierSpecialGetter : IValueGetter<ModifierSpec>
    {
        private readonly string name;
        public ModifierSpecialGetter(string name)
        {
            this.name = name;
        }
        public float GetValue(ModifierSpec context)
        {
            if (context.Ability.TryGetLevelValue(name, out var value))
            {
                return value;
            }
            else
            {
                CFLog.Warning($"ModifierSpecial {name} not found in modifier {context.data.Name}, return 0");
                return 0f;
            }
        }
    }
}
