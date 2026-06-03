using CombatFramework.Core.Executor.ValueGetter;

namespace CombatFramework.Core.Modifier;

public class StatModifierEntry
{
    public string Stat { get; set; } = string.Empty;
    public StatOp Op { get; set; } = StatOp.Add;
    public IAbilityValueGetter Value { get; set; }
}
