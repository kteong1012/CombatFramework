using System.Numerics;
using CombatFramework.Core.Ability;
using CombatFramework.Unit;

namespace CombatFramework.Core.Modifier;

/// <summary>
/// 每个 unit 的 Modifier 管理器。
/// 仅负责 ModifierSpec 的运行时管理。
/// </summary>
public class ModifierManager
{
    private readonly List<ModifierSpec> _modifiers = new();
    private readonly List<ModifierSpec> _pendingAdd = new();
    private readonly UnitEntity _owner;

    public IReadOnlyList<ModifierSpec> All => _modifiers;

    public ModifierManager(UnitEntity owner) => _owner = owner;

    public ModifierSpec Add(ModifierData data, UnitEntity caster,
        AbilitySpec sourceAbility, float? durationOverride = null,
        string sourceTag = null)
    {
        // 非 Multiple 模式：移出现有同名 modifier
        if (data.Attribute != ModifierAttribute.Multiple)
        {
            var existing = _modifiers.FirstOrDefault(m => m.Name == data.Name);
            if (existing != null)
            {
                if (data.Attribute == ModifierAttribute.StackCount)
                {
                    existing.IncrementStack();
                    data.OnStackCountChangedDel?.Invoke(existing);
                    return existing;
                }

                // 刷新
                if (durationOverride.HasValue)
                    existing.SetDuration(durationOverride.Value);
                else
                    existing.SetDuration(data.DurationGetter);
                data.OnRefreshDel?.Invoke(existing);
                return existing;
            }
        }

        var instance = new ModifierSpec(data, _owner, caster, sourceAbility);
        if (durationOverride.HasValue) instance.SetDuration(durationOverride.Value);
        if (sourceTag != null) instance.SourceTag = sourceTag;

        // 延迟添加，避免在迭代时修改集合
        _pendingAdd.Add(instance);

        // 调用 OnCreated
        data.OnCreatedDel?.Invoke(instance);

        return instance;
    }

    public int RemoveBySourceTag(string tag)
    {
        var removed = 0;
        removed += _pendingAdd.RemoveAll(m => m.SourceTag == tag);
        removed += _modifiers.RemoveAll(m => m.SourceTag == tag);
        return removed;
    }

    public int RemoveByName(string name)
    {
        var removed = 0;
        removed += _pendingAdd.RemoveAll(m => m.Name == name);
        removed += _modifiers.RemoveAll(m => m.Name == name);
        return removed;
    }

    public void Remove(ModifierSpec instance)
    {
        if (_pendingAdd.Remove(instance)) return;
        _modifiers.Remove(instance);
    }

    public void Clear()
    {
        _pendingAdd.Clear();
        _modifiers.Clear();
    }

    public bool Has(string modifierName)
        => _modifiers.Any(m => m.Name == modifierName) || _pendingAdd.Any(m => m.Name == modifierName);

    public ModifierSpec Find(string modifierName)
        => _modifiers.FirstOrDefault(m => m.Name == modifierName)
           ?? _pendingAdd.FirstOrDefault(m => m.Name == modifierName);

    public float CollectExtraFromModifierSpecs(string statId)
    {
        var add = 0f;
        var hasOverride = false;
        var overrideValue = 0f;

        foreach (var mod in _modifiers)
        {
            if (TryGetPropertyHookValue(mod, statId, out var hookValue))
            {
                add += hookValue;
            }
            else if (TryGetPropertyValue(mod, statId, out var propertyValue))
            {
                add += propertyValue;
            }
            else if (TryGetPropertyRefValue(mod, statId, out var refValue))
            {
                add += refValue;
            }

            foreach (var entry in mod.Data.StatModifiers)
            {
                if (entry.Stat != statId) continue;
                if (entry.Op == StatOp.Add)
                {
                    add += entry.Value;
                }
                else if (entry.Op == StatOp.Override)
                {
                    hasOverride = true;
                    overrideValue = entry.Value;
                }
            }
        }

        return hasOverride ? overrideValue : baseValue + add;

        static bool TryGetPropertyValue(ModifierSpec mod, string id, out float value)
            => mod.Data.Properties.TryGetValue(id, out value);

        static bool TryGetPropertyHookValue(ModifierSpec mod, string id, out float value)
        {
            if (mod.Data.PropertyHooks.TryGetValue(id, out var fn))
            {
                try
                {
                    value = fn.Invoke(mod);
                    return true;
                }
                catch
                {
                }
            }
            value = 0f;
            return false;
        }

        static bool TryGetPropertyRefValue(ModifierSpec mod, string id, out float value)
        {
            if (mod.SourceAbility != null && mod.Data.PropertyRefs.TryGetValue(id, out var refName))
            {
                value = mod.SourceAbility.GetParameter(refName);
                return true;
            }
            value = 0f;
            return false;
        }
    }

    public int Purge(bool purgePositive, bool purgeNegative)
    {
        return _modifiers.RemoveAll(m =>
        {
            if (purgePositive && m.Data.IsBuff) return m.TryPurge();
            if (purgeNegative && m.Data.IsDebuff) return m.TryPurge();
            return false;
        });
    }

    public void Update(float dt)
    {
        if (_pendingAdd.Count > 0)
        {
            _modifiers.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }

        _modifiers.RemoveAll(m => m.Update(dt));
    }
}
