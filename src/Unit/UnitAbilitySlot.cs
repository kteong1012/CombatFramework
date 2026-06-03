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
        ability.Owner    = _owner;
        ability.SlotIndex = _abilities.Count;
        _abilities.Add(ability);
        ability.OnEquipped(_owner);
    }

    /// <summary>
    /// 用 newAbility 替换指定槽位的当前技能。
    /// 旧技能触发 OnUnequipped，新技能触发 OnEquipped。
    /// </summary>
    public bool Replace(int slotIndex, AbilitySpec newAbility)
    {
        if (slotIndex < 0 || slotIndex >= _abilities.Count) return false;
        var old = _abilities[slotIndex];
        old.OnUnequipped(_owner);
        old.Owner     = null;
        old.SlotIndex = -1;

        newAbility.Owner     = _owner;
        newAbility.SlotIndex = slotIndex;
        _abilities[slotIndex] = newAbility;
        newAbility.OnEquipped(_owner);
        return true;
    }

    public void Unequip(string abilityName)
    {
        var ability = Get(abilityName);
        if (ability != null)
        {
            ability.OnUnequipped(_owner);
            ability.Owner = null;
        }
        _abilities.RemoveAll(a => a.Name == abilityName);
    }

    public AbilitySpec? Get(string name) =>
        _abilities.FirstOrDefault(a => a.Name == name);

    public AbilitySpec? GetByIndex(int index) =>
        index >= 0 && index < _abilities.Count ? _abilities[index] : null;
}
