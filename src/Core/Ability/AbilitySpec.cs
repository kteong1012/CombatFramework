using CombatFramework.Core.Executor.AbilityExecutor;
using CombatFramework.Core.Executor.ValueGetter;
using CombatFramework.Core.Modifier;
using CombatFramework.Unit;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CombatFramework.Core.Ability;

public class AbilityData
{
    public Dictionary<string, LevelValue> dynamicValues = new();


    public Dictionary<string, IValueGetter<AbilitySpec>> costMaps = new();

    public Dictionary<string, AbilityEventExecutor> eventExecutors = new();

    public string Name { get; internal set; }
    public string Element { get; internal set; }

    #region Nested Types
    public class LevelValue
    {
        public float[] values;
    }
    #endregion
}
public class AbilitySpec
{
    #region Delegates Definitions
    delegate float GetDynamicValueDelegate();
    #endregion

    #region Variables
    public AbilityData data;
    public UnitEntity Unit { get; set; }
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
        if (!data.dynamicValues.TryGetValue(name, out var levelValue))
        {
            value = 0;
            return false;
        }
        var index = Level - 1;
        // 如果index > length， 就取最后一个
        if (index >= levelValue.values.Length)
        {
            value = levelValue.values[levelValue.values.Length - 1];
            return true;
        }
        else
        {
            value = levelValue.values[index];
            return true;
        }

    }
    #endregion

    #region Private Methods
    #endregion
}