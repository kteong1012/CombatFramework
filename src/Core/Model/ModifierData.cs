using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Modifier;
using System;
using System.Collections.Generic;

namespace CombatFramework.Core.Model;

public class ModifierData
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 可选。填写继承自 ModifierSpec 的子类短类名，
    /// ModifierSpec.Create() 会用反射实例化该子类。
    /// </summary>
    public string Class { get; set; }

    public IAbilityValueGetter DurationGetter { get; set; }
    public bool IsBuff { get; set; }
    public bool IsDebuff { get; set; }
    public bool IsHidden { get; set; }
    public bool IsPurgable { get; set; } = true;
    public ModifierStackMode StackMode { get; set; } = ModifierStackMode.None;
    public List<StatModifierEntry> Properties { get; set; } = new();

    /// <summary>自动特效名。非空时 OnCreated 自动调用 Vfx.PlayOnUnit，OnDestroy 自动调用 StopOnUnit。</summary>
    public string EffectName { get; set; }

    /// <summary>OnIntervalThink 触发间隔（秒）。0 表示不启用间隔触发。</summary>
    public float ThinkInterval { get; set; }

    /// <summary>事件名 → Action 列表。Key 对应 ModifierEvents 常量。</summary>
    public Dictionary<string, List<AbilityEventActionData>> Events { get; set; }
}