using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;

/// <summary>
/// 将 Owner 的某个槽位替换为另一个 AbilityData。
/// <para>
/// 新的 AbilityData 从 <c>context.Ability.data.SubAbilities</c> 中按 <see cref="SubAbilityName"/> 查找，
/// 因此调用方的 ability JSON 里需要在 <c>SubAbilities</c> 字典里声明目标 ability 数据。
/// </para>
/// <para>
/// <see cref="SlotIndex"/> 为 -1 时，自动使用 <c>context.Ability.SlotIndex</c>（即触发技能所在槽）。
/// </para>
/// </summary>
[AbilityEventAction(typeof(ReplaceAbilityActionData))]
public class ReplaceAbilityAction : AbilityEventAction
{
    public new ReplaceAbilityActionData data => (ReplaceAbilityActionData)base.data;

    public ReplaceAbilityAction(ReplaceAbilityActionData data) : base(data) { }

    public override void Execute(AbilityEventContext context)
    {
        var owner = context.Ability?.Owner;
        if (owner == null) return;

        var subAbilities = context.Ability.data?.SubAbilities;
        if (subAbilities == null || !subAbilities.TryGetValue(data.SubAbilityName, out var abilityData))
            return;

        var slotIndex = data.SlotIndex >= 0 ? data.SlotIndex : context.Ability.SlotIndex;
        if (slotIndex < 0) return;

        var newSpec = AbilitySpec.Create(abilityData);
        owner.AbilitySlots.Replace(slotIndex, newSpec);
    }
}

public class ReplaceAbilityActionData : AbilityEventActionData
{
    /// <summary>目标 AbilityData 在父技能 SubAbilities 字典中的 key。</summary>
    public string SubAbilityName { get; set; }

    /// <summary>替换的槽位下标。-1 = 使用触发技能自身所在的槽位。</summary>
    public int SlotIndex { get; set; } = -1;
}
