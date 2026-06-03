using CombatFramework.Core.Modifier;
using CombatFramework.Core.Model;
using CombatFramework.Unit;

namespace CombatFramework.Core.Ability;
public class AbilitySpec
{
    #region Variables
    public AbilityData data;
    public string Name => data?.Name;
    public UnitEntity Owner { get; set; }
    public int Level { get; set; } = 0;
    #endregion

    #region Public Methods
    public bool TryGetLevelValue(string name, out float value)
    {
        if (Level == 0)
        {
            value = 0;
            return false;
        }
        if (!data.AbilitySpecialFields.TryGetValue(name, out var levelValues))
        {
            value = 0;
            return false;
        }
        var index = Level - 1;
        if (index >= levelValues.Length)
        {
            value = levelValues[levelValues.Length - 1];
            return true;
        }
        else
        {
            value = levelValues[index];
            return true;
        }
    }
    #endregion
}