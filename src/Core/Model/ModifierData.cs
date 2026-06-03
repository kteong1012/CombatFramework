using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Modifier;
using System;
using System.Collections.Generic;

namespace CombatFramework.Core.Model;

public class ModifierData
{
    public string Name { get; set; } = string.Empty;
    public IAbilityValueGetter DurationGetter { get; set; }
    public bool IsBuff { get; set; }
    public bool IsDebuff { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPurgable { get; set; } = true;
    public ModifierStackMode StackMode { get; set; } = ModifierStackMode.None;
    public List<StatModifierEntry> Properties { get; set; } = new();
}