namespace CombatFramework.Core.Modifier;

public class StatModifierEntry
{
    public string Stat { get; set; } = string.Empty;
    public StatOp Op { get; set; } = StatOp.Add;
    public float Value { get; set; }
}
