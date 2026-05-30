using CombatFramework.Core;

namespace CombatFramework.Unit;

/// <summary>
/// 技能槽位容器。管理 unit 上所有已装备的能力实例。
/// </summary>
public class UnitAbilitySlot
{
    private readonly List<AbilityInstance> _abilities = new();
    private readonly UnitEntity _owner;

    public IReadOnlyList<AbilityInstance> All => _abilities;

    public UnitAbilitySlot(UnitEntity owner) => _owner = owner;

    public void Equip(AbilityInstance ability)
    {
        if (_abilities.Any(a => a.Name == ability.Name)) return;
        _abilities.Add(ability);
    }

    public void Unequip(string abilityName)
    {
        _abilities.RemoveAll(a => a.Name == abilityName);
    }

    public AbilityInstance? Get(string idOrName) =>
        _abilities.FirstOrDefault(a => a.Name == idOrName || a.Data.Id == idOrName);

    public AbilityInstance? GetByIndex(int index) =>
        index >= 0 && index < _abilities.Count ? _abilities[index] : null;
}
