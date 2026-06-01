using CombatFramework.Core.Executor.ValueGetter;

namespace CombatFramework.Core.Modifier;

public class ModifierData
{
    public string Name { get; set; } = string.Empty;
    public IValueGetter<ModifierSpec> DurationGetter { get; set; }
    public bool IsBuff { get; set; }
    public bool IsDebuff { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPurgable { get; set; } = true;
    public ModifierAttribute Attribute { get; set; } = ModifierAttribute.None;
}