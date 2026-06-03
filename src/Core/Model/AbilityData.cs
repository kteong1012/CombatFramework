namespace CombatFramework.Core.Model;

public partial class AbilityData
{
    #region Model
    public string Name;

    /// <summary>
    /// 可选。填写继承自 AbilitySpec 的子类短类名，
    /// AbilitySpec.Create() 会用反射实例化该子类。
    /// 不填则使用默认 AbilitySpec。
    /// </summary>
    public string Class;

    /// <summary>
    /// 技能标签列表（如 "skill_type_burst"、"skill_atk"）。
    /// 用于 SkillBonus 等级加成筛选，以及外部系统按类型分类。
    /// </summary>
    public List<string> Tags;

    /// <summary>
    /// 该技能携带的等级加成条目（命座/被动使用）。
    /// 装备后由 <see cref="CombatFramework.Core.Ability.AbilitySpec.GetEffectiveLevel"/> 对目标技能汇总。
    /// </summary>
    public List<SkillBonusEntry> SkillBonusEntries;

    public Dictionary<string, float[]> AbilitySpecialFields;

    public List<AbilityCostData> AbilityCosts;

    /// <summary>事件名 → Action 列表。Key 对应 AbilityEvents 常量。</summary>
    public Dictionary<string, List<AbilityEventActionData>> AbilityEvents;

    public Dictionary<string, ModifierData> AbilityModifiers;

    /// <summary>
    /// 子技能数据表。Key 为局部名，供 ReplaceAbilityAction 按名查找。
    /// 与 AbilityModifiers 平级，每个子技能是一份完整的 AbilityData。
    /// </summary>
    public Dictionary<string, AbilityData> SubAbilities;

    /// <summary>
    /// 技能转换列表。在 CheckCost 之前按顺序评估，首个满足条件的转换生效。
    /// 转换目标通过 <see cref="AbilityTransformData.To"/> 引用 <see cref="SubAbilities"/> 中的 key。
    /// </summary>
    public List<AbilityTransformData> Transforms;
    #endregion
}
