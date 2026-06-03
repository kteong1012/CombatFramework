namespace CombatFramework.Core.Model;

public partial class AbilityData
{
    #region Model
    public string Name;

    public Dictionary<string, float[]> AbilitySpecialFields;

    public List<AbilityCostData> AbilityCosts;

    public List<AbilityEventActionData> AbilityEvents;

    public Dictionary<string, ModifierData> AbilityModifiers;
    #endregion
}
