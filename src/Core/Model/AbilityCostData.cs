using CombatFramework.Core.Executor.ValueGetter;

namespace CombatFramework.Core.Model;

public class AbilityCostData
{
    public string Stat { get; set; }
    public IAbilityValueGetter Value { get; set; }
}
