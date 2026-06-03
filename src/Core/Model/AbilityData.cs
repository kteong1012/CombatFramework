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
    #endregion
}
