using CombatFramework.Core.Ability;

namespace CombatFramework.Core.Executor.ValueGetter
{
    public interface IAbilityValueGetter
    {
        float GetValue(AbilitySpec context);
    }
}
