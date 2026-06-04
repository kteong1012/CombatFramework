using CombatFramework.Core;
using CombatFramework.Core.Ability;
using CombatFramework.Core.Ability.AbilityEvent;
using CombatFramework.Core.Model;

/// <summary>
/// 替换 Owner 的一个已装备技能为另一个 AbilityData。
/// <para>
/// 新的 AbilityData 从 <c>context.Ability.data.SubAbilities</c> 中按 <see cref="SubAbilityName"/> 查找，
/// 因此调用方的 ability JSON 里需要在 <c>SubAbilities</c> 字典里声明目标 ability 数据。
/// </para>
/// <para>
/// <see cref="TargetAbilityName"/> 为空时，自动使用 <c>context.Ability.Name</c>（即替换触发技能自身）。
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

        var targetName = string.IsNullOrEmpty(data.TargetAbilityName)
            ? context.Ability.Name
            : data.TargetAbilityName;
        if (string.IsNullOrEmpty(targetName)) return;

        owner.UnequipAbility(targetName);
        var newSpec = AbilitySpec.Create(abilityData);
        owner.EquipAbility(newSpec);
    }
}

public class ReplaceAbilityActionData : AbilityEventActionData
{
    /// <summary>目标 AbilityData 在父技能 SubAbilities 字典中的 key。</summary>
    public string SubAbilityName { get; set; }

    /// <summary>要被替换的技能名。为空时替换触发技能自身。</summary>
    public string TargetAbilityName { get; set; }
}
