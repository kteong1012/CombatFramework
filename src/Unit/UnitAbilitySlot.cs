using CombatFramework.Core.Ability;

namespace CombatFramework.Unit;

/// <summary>
/// 技能槽位容器。管理 unit 上所有已装备的能力实例。
/// </summary>
public class UnitAbilitySlot
{
    private readonly List<AbilitySpec> _abilities = new();
    private readonly UnitEntity _owner;

    public IReadOnlyList<AbilitySpec> All => _abilities;

    public UnitAbilitySlot(UnitEntity owner) => _owner = owner;

    public void Equip(AbilitySpec ability)
    {
        if (_abilities.Any(a => a.Name == ability.Name)) return;
        _abilities.Add(ability);
    }

    public void Unequip(string abilityName)
    {
        _abilities.RemoveAll(a => a.Name == abilityName);
    }

    public AbilitySpec? Get(string name) =>
        _abilities.FirstOrDefault(a => a.Name == name);

    public AbilitySpec? GetByIndex(int index) =>
        index >= 0 && index < _abilities.Count ? _abilities[index] : null;
}
