using CombatFramework.Core.Ability;
using CombatFramework.Core.Enums;

namespace CombatFramework.Unit;

/// <summary>
/// 技能槽位容器。内部使用固定大小数组，以 <see cref="SlotType"/> 枚举为索引。
/// <para>
/// 可见性规则：<see cref="SlotType"/> &lt;= <see cref="SlotType.Burst"/> 的槽位
/// (<c>NormalAtk / Skill / Burst</c>) 会显示在技能栏 UI；被动槽和命座槽不显示。
/// </para>
/// </summary>
public class UnitAbilitySlot
{
    private const int TotalSlots = 11; // SlotType 枚举成员数量

    private readonly AbilitySpec[] _slots = new AbilitySpec[TotalSlots];
    private readonly UnitEntity _owner;

    public UnitAbilitySlot(UnitEntity owner) => _owner = owner;

    // ─── 查询 ─────────────────────────────────────────────────────────────

    /// <summary>所有已装备的技能（跳过空槽）。</summary>
    public IEnumerable<AbilitySpec> All
    {
        get
        {
            foreach (var s in _slots)
                if (s != null) yield return s;
        }
    }

    /// <summary>仅返回 Visible 槽位（NormalAtk / Skill / Burst）中已装备的技能。</summary>
    public IEnumerable<(SlotType SlotType, AbilitySpec Ability)> VisibleSlots
    {
        get
        {
            for (int i = 0; i <= (int)SlotType.Burst; i++)
                if (_slots[i] != null)
                    yield return ((SlotType)i, _slots[i]);
        }
    }

    /// <summary>指定 SlotType 的技能是否应显示在技能栏 UI。</summary>
    public static bool IsVisible(SlotType slotType) => slotType <= SlotType.Burst;

    // ─── 装备 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 装备到指定枚举槽位（主推 API）。若槽位已有技能，先触发 OnUnequipped 再替换。
    /// </summary>
    public void Equip(SlotType slotType, AbilitySpec ability)
    {
        int idx = (int)slotType;
        var old = _slots[idx];
        if (old != null)
        {
            old.OnUnequipped(_owner);
            old.Owner     = null;
            old.SlotIndex = -1;
        }

        ability.Owner     = _owner;
        ability.SlotIndex = idx;
        _slots[idx]       = ability;
        ability.OnEquipped(_owner);
    }

    /// <summary>
    /// 向后兼容：自动分配到第一个空槽（从 NormalAtk=0 开始顺序查找）。
    /// 建议新代码改用 <see cref="Equip(SlotType, AbilitySpec)"/>。
    /// </summary>
    public void Equip(AbilitySpec ability)
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            if (_slots[i] == null)
            {
                ability.Owner     = _owner;
                ability.SlotIndex = i;
                _slots[i]         = ability;
                ability.OnEquipped(_owner);
                return;
            }
        }
        // 全满时静默忽略（实际不应发生）
    }

    // ─── 替换 ─────────────────────────────────────────────────────────────

    /// <summary>用 newAbility 替换指定槽位，触发旧技能 OnUnequipped + 新技能 OnEquipped。</summary>
    public bool Replace(int slotIndex, AbilitySpec newAbility)
    {
        if (slotIndex < 0 || slotIndex >= TotalSlots) return false;
        var old = _slots[slotIndex];
        if (old != null)
        {
            old.OnUnequipped(_owner);
            old.Owner     = null;
            old.SlotIndex = -1;
        }

        newAbility.Owner     = _owner;
        newAbility.SlotIndex = slotIndex;
        _slots[slotIndex]    = newAbility;
        newAbility.OnEquipped(_owner);
        return true;
    }

    public bool Replace(SlotType slotType, AbilitySpec newAbility)
        => Replace((int)slotType, newAbility);

    // ─── 卸除 ─────────────────────────────────────────────────────────────

    public void Unequip(string abilityName)
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            if (_slots[i]?.Name == abilityName)
            {
                _slots[i].OnUnequipped(_owner);
                _slots[i].Owner = null;
                _slots[i] = null;
                return;
            }
        }
    }

    // ─── 获取 ─────────────────────────────────────────────────────────────

    public AbilitySpec Get(SlotType slotType) => _slots[(int)slotType];

    public AbilitySpec Get(string name)
    {
        foreach (var s in _slots)
            if (s?.Name == name) return s;
        return null;
    }

    /// <summary>向后兼容的整数索引版本。</summary>
    public AbilitySpec GetByIndex(int index)
        => index >= 0 && index < TotalSlots ? _slots[index] : null;
}
